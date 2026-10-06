using Barrapp.Application.Abstractions;
using Barrapp.Domain.Athlete;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.Sessions;
using Barrapp.Domain.SkillProgress;
using Barrapp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Persistence;

/// <summary>
/// Contexto de EF Core sobre SQLite. Implementa los puertos de Application:
/// <see cref="IApplicationDbContext"/> para las lecturas (proyección directa a DTO) y
/// <see cref="IUnitOfWork"/> para confirmar las escrituras.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext, IUnitOfWork
{
    /// <summary>Usuarios propietarios de los datos; en el MVP, uno solo.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Perfiles de atleta.</summary>
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();

    /// <summary>Objetivos del mesociclo.</summary>
    public DbSet<Objective> Objectives => Set<Objective>();

    /// <summary>Etapa actual del atleta por skill.</summary>
    public DbSet<AthleteSkillProgress> AthleteSkillProgresses => Set<AthleteSkillProgress>();

    /// <summary>Registros de sesión (serie a serie).</summary>
    public DbSet<SessionLog> SessionLogs => Set<SessionLog>();

    /// <summary>Historial de sesiones sueltas, aislado de los registros de sesión (D8).</summary>
    public DbSet<SessionSuelta> SessionSuelta => Set<SessionSuelta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
