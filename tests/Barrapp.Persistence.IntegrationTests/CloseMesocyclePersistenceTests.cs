using Barrapp.Application;
using Barrapp.Application.Abstractions;
using Barrapp.Application.Common;
using Barrapp.Application.Features.Plans;
using Barrapp.Domain.Athlete;
using Barrapp.Domain.Common;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;
using Barrapp.Persistence.Knowledge;
using Barrapp.Persistence.Repositories;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;



namespace Barrapp.Persistence.IntegrationTests;

/// <summary>
/// Round-trip real del cierre del mesociclo (ticket #24, D7) contra SQLite: el esquema se crea
/// aplicando las migraciones y el cierre recorre el pipeline completo de Application
/// (<see cref="CloseMesocycleCommand"/> vía MediatR). Comprueba que el mesociclo queda cerrado y
/// que los máximos ajustados sobreviven a la base (contexto nuevo de lectura), y que solo los
/// registros de la ventana del mesociclo alimentan el ajuste.
/// </summary>
public sealed class CloseMesocyclePersistenceTests : IDisposable
{
    /// <summary>Lunes de referencia para el calendario del mesociclo (#94).</summary>
    private static readonly DateOnly StartDate = new(2026, 3, 2);

    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;

    public CloseMesocyclePersistenceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbContext = CreateContext();
        _dbContext.Database.Migrate();
    }

    [Fact]
    public async Task Closing_persists_the_closed_mesocycle_and_the_adjusted_maximums()
    {
        SeedProfile(pushUp: 10);
        var startedAt = DateTimeOffset.Parse("2026-09-01T08:00:00+00:00");
        var mesocycle = SeedActiveMesocycle(startedAt);

        // Los registros del mesociclo: uno etiquetado con su id (siempre cuenta) y uno sin
        // etiqueta dentro de la ventana (cuenta por fecha), que es el que sube el máximo.
        _dbContext.SessionLogs.Add(Log(
            "push_up",
            mesocycleId: mesocycle.Id,
            new DateTimeOffset(2026, 9, 28, 18, 0, 0, TimeSpan.Zero),
            (1, 8)));
        _dbContext.SessionLogs.Add(Log(
            "push_up",
            mesocycleId: null,
            new DateTimeOffset(2026, 9, 15, 18, 0, 0, TimeSpan.Zero),
            (1, 14), (2, 14)));
        await _dbContext.SaveChangesAsync();

        Guid closedId;
        using (var services = CreateServices(_dbContext))
        {
            var closing = await services.GetRequiredService<ISender>()
                .Send(new CloseMesocycleCommand());

            Assert.True(closing.IsSuccess);
            closedId = closing.Value.MesocycleId;
            Assert.Equal("planche", closing.Value.SkillId);
            Assert.Equal(
                14,
                closing.Value.Maximums.Single(maximum => maximum.ExerciseCode == "push_up").Repetitions);
        }

        // Contexto nuevo: lo leído viene de la base, no de la memoria del contexto de escritura.
        await using var readContext = CreateContext();
        var profile = await readContext.AthleteProfiles
            .Include(candidate => candidate.Maximums)
            .SingleAsync(candidate => candidate.UserId == SingleUser.Id);
        Assert.Equal(14, profile.MaximumFor("push_up"));
        Assert.Equal(5, profile.MaximumFor("pull_up"));
        Assert.Equal(20, profile.MaximumFor("squat"));

        var saved = await readContext.Mesocycles.SingleAsync(mesocycle => mesocycle.Id == closedId);
        Assert.Equal(MesocycleStatus.Closed, saved.Status);
        Assert.NotNull(saved.ClosedAtUtc);
    }

    [Fact]
    public async Task Only_logs_within_the_mesocycle_window_raise_maximums()
    {
        SeedProfile(pushUp: 10);
        var startedAt = DateTimeOffset.Parse("2026-09-01T08:00:00+00:00");
        var mesocycle = SeedActiveMesocycle(startedAt);

        // Registro anterior al inicio del mesociclo: no pertenece a su ventana.
        _dbContext.SessionLogs.Add(Log(
            "push_up",
            mesocycleId: null,
            new DateTimeOffset(2026, 8, 15, 18, 0, 0, TimeSpan.Zero),
            (1, 20), (2, 20)));
        // Registro dentro de la ventana: sí pertenece.
        _dbContext.SessionLogs.Add(Log(
            "push_up",
            mesocycleId: null,
            new DateTimeOffset(2026, 9, 20, 18, 0, 0, TimeSpan.Zero),
            (1, 14), (2, 14)));
        await _dbContext.SaveChangesAsync();

        Guid closedId;
        using (var services = CreateServices(_dbContext))
        {
            var closing = await services.GetRequiredService<ISender>()
                .Send(new CloseMesocycleCommand());

            Assert.True(closing.IsSuccess);
            closedId = closing.Value.MesocycleId;
        }

        var readProfile = await CreateContext().AthleteProfiles
            .Include(candidate => candidate.Maximums)
            .SingleAsync(candidate => candidate.UserId == SingleUser.Id);
        Assert.Equal(14, readProfile.MaximumFor("push_up"));

        var saved = await CreateContext().Mesocycles.SingleAsync(mesocycle => mesocycle.Id == closedId);
        Assert.Equal(MesocycleStatus.Closed, saved.Status);
        Assert.True(saved.ClosedAtUtc!.Value >= startedAt);
    }

    [Fact]
    public async Task Closing_without_an_active_mesocycle_synthesizes_a_mesocycle_and_closes_it()
    {
        SeedProfile(pushUp: 10);
        SeedObjective("planche");

        // Registro de hace dos semanas: cae dentro de la ventana del mesociclo sintetizado
        // (empieza un mes atrás, FIX-1) y sube el máximo.
        _dbContext.SessionLogs.Add(Log(
            "push_up",
            mesocycleId: null,
            DateTimeOffset.UtcNow.AddDays(-14),
            (1, 14), (2, 14)));
        await _dbContext.SaveChangesAsync();

        Guid closedId;
        using (var services = CreateServices(_dbContext))
        {
            var closing = await services.GetRequiredService<ISender>()
                .Send(new CloseMesocycleCommand());

            Assert.True(closing.IsSuccess);
            closedId = closing.Value.MesocycleId;
            Assert.Equal("planche", closing.Value.SkillId);
            Assert.Equal(
                14,
                closing.Value.Maximums.Single(maximum => maximum.ExerciseCode == "push_up").Repetitions);
        }

        // Contexto nuevo: el mesociclo sintetizado quedó cerrado y los máximos ajustados.
        await using var readContext = CreateContext();
        var saved = await readContext.Mesocycles.SingleAsync(mesocycle => mesocycle.Id == closedId);
        Assert.Equal(MesocycleStatus.Closed, saved.Status);
        Assert.NotNull(saved.ClosedAtUtc);
        Assert.True(saved.StartedAtUtc <= DateTimeOffset.UtcNow.AddDays(-27));

        var profile = await readContext.AthleteProfiles
            .Include(candidate => candidate.Maximums)
            .SingleAsync(candidate => candidate.UserId == SingleUser.Id);
        Assert.Equal(14, profile.MaximumFor("push_up"));
    }

    [Fact]
    public async Task Closing_without_an_active_mesocycle_nor_objective_fails_with_not_found()
    {
        _dbContext.AthleteProfiles.Add(AthleteProfile.Create(
            SingleUser.Id,
            78,
            180,
            180,
            85,
            3,
            [
                new MaximumInput("push_up", 10),
                new MaximumInput("pull_up", 5),
                new MaximumInput("squat", 20),
            ]).Value);
        await _dbContext.SaveChangesAsync();

        using var services = CreateServices(_dbContext);
        var closing = await services.GetRequiredService<ISender>()
            .Send(new CloseMesocycleCommand());

        Assert.True(closing.IsFailure);
        Assert.Equal(DomainErrors.Objective.NotFound, closing.Error);

        // Nada se persistió: no hay mesociclos en la base.
        var saved = await CreateContext().Mesocycles.ToListAsync();
        Assert.Empty(saved);
    }

    private void SeedProfile(int pushUp) =>
        _dbContext.AthleteProfiles.Add(AthleteProfile.Create(
            SingleUser.Id,
            weightKilograms: 78,
            heightCentimeters: 180,
            armSpanCentimeters: 180,
            inseamCentimeters: 85,
            trainingDays: 3,
            [
                new MaximumInput("push_up", pushUp),
                new MaximumInput("pull_up", 5),
                new MaximumInput("squat", 20),
            ]).Value);

    private void SeedObjective(string skillId)
    {
        var catalog = new KnowledgeBaseCatalog(
            new KnowledgeBaseLoader(NullLogger<KnowledgeBaseLoader>.Instance).LoadEmbeddedResources());
        _dbContext.Objectives.Add(Objective.Create(SingleUser.Id, skillId, catalog).Value);
    }

    private Mesocycle SeedActiveMesocycle(DateTimeOffset startedAtUtc)
    {
        var mesocycle = Mesocycle.Create(SingleUser.Id, BuildPlan(), startedAtUtc).Value;
        _dbContext.Mesocycles.Add(mesocycle);
        return mesocycle;
    }

    private static SessionLog Log(
        string exerciseId,
        Guid? mesocycleId,
        DateTimeOffset recordedAtUtc,
        params (int Set, int Value)[] sets) =>
        SessionLog.Create(
            SingleUser.Id,
            exerciseId,
            mesocycleId,
            sessionDay: 1,
            recordedAtUtc,
            sets.Select(set => new SessionLogSetInput(set.Set, set.Value)).ToList()).Value;

    private static Plan BuildPlan()
    {
        var catalog = new KnowledgeBaseCatalog(
            new KnowledgeBaseLoader(NullLogger<KnowledgeBaseLoader>.Instance).LoadEmbeddedResources());

        var profile = AthleteProfile.Create(
            SingleUser.Id,
            78,
            180,
            180,
            85,
            3,
            [
                new MaximumInput("push_up", 10),
                new MaximumInput("pull_up", 5),
                new MaximumInput("squat", 20),
            ]).Value;

        var objective = Objective.Create(SingleUser.Id, "planche", catalog).Value;

        return PlanGenerator.Generate(profile, objective, stageOrder: 1, StartDate, catalog).Value;
    }

    private static ServiceProvider CreateServices(ApplicationDbContext dbContext)
    {
        var knowledgeBase = new KnowledgeBaseCatalog(
            new KnowledgeBaseLoader(NullLogger<KnowledgeBaseLoader>.Instance).LoadEmbeddedResources());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(dbContext);
        services.AddSingleton<IApplicationDbContext>(dbContext);
        services.AddSingleton<IUnitOfWork>(dbContext);
        services.AddSingleton<IMesocycleRepository, MesocycleRepository>();
        services.AddSingleton<IKnowledgeBase>(knowledgeBase);

        return services.BuildServiceProvider();
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
