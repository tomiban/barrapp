using System.Net;
using System.Net.Http.Json;
using Barrapp.Application.Features.SkillProgress;
using Microsoft.AspNetCore.Mvc;

namespace Barrapp.Api.FunctionalTests;

/// <summary>
/// El avance de etapa del skill (spec 0001, US-19; ticket #23) se evalúa con un <c>POST</c> sobre
/// los registros de sesión: se sube de etapa al cumplir el criterio de la etapa actual en dos
/// sesiones consecutivas y no se avanza sin el criterio, ni más allá de la última etapa.
/// </summary>
public sealed class SkillStageAdvanceEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_advance_promotes_the_stage_after_two_consecutive_sessions_meeting_the_criterion()
    {
        using var client = factory.CreateClient();

        // Etapa 1 del pino: mantener 30 s × 3 series en el ejercicio de la etapa.
        await client.RegisterSkill("handstand-wall-support", sessionDay: 1, 30, 30, 30);
        await client.RegisterSkill("handstand-wall-support", sessionDay: 2, 30, 30, 30);

        using var response = await client.PostAsync("/catalog/progress/handstand/advance", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SkillStageAdvanceResponse>();
        Assert.NotNull(result);
        Assert.Equal("handstand", result!.SkillId);
        Assert.True(result.Advanced);
        Assert.Equal(2, result.StageOrder);

        // El avance queda persistido en la progresión del atleta.
        var progress = await client.GetFromJsonAsync<List<SkillProgressResponse>>("/catalog/progress");
        Assert.NotNull(progress);
        Assert.Equal(2, progress!.Single(entry => entry.SkillId == "handstand").StageOrder);
    }
}

/// <summary>
/// Clase aparte con su propia base: sin el criterio cumplido en las dos sesiones más recientes no
/// se avanza.
/// </summary>
public sealed class SkillStageAdvanceNotMetEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_advance_keeps_the_stage_when_the_last_session_falls_short()
    {
        using var client = factory.CreateClient();

        // Etapa 1 de la planche: 20 s × 3; la segunda sesión se queda a 15 s en la tercera serie.
        await client.RegisterSkill("planche-lean", sessionDay: 1, 20, 20, 20);
        await client.RegisterSkill("planche-lean", sessionDay: 2, 20, 20, 15);

        using var response = await client.PostAsync("/catalog/progress/planche/advance", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SkillStageAdvanceResponse>();
        Assert.NotNull(result);
        Assert.Equal("planche", result!.SkillId);
        Assert.False(result.Advanced);
        Assert.Equal(1, result.StageOrder);
    }
}

/// <summary>
/// Clase aparte con su propia base: en la última etapa de la escalera no se avanza, aunque el
/// criterio se cumpla en dos sesiones consecutivas.
/// </summary>
public sealed class SkillStageAdvanceLastStageEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_advance_keeps_the_last_stage_when_the_criterion_is_met()
    {
        using var client = factory.CreateClient();

        // Quinta (y última) etapa del pistol squat: 3 reps × 3 en pistol-paused.
        using var putProgress = await client.PutAsJsonAsync(
            "/catalog/progress/pistol-squat",
            new { stageOrder = 5 });
        Assert.Equal(HttpStatusCode.OK, putProgress.StatusCode);

        await client.RegisterSkill("pistol-paused", sessionDay: 1, 3, 3, 3);
        await client.RegisterSkill("pistol-paused", sessionDay: 2, 3, 3, 3);

        using var response = await client.PostAsync("/catalog/progress/pistol-squat/advance", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SkillStageAdvanceResponse>();
        Assert.NotNull(result);
        Assert.Equal("pistol-squat", result!.SkillId);
        Assert.False(result.Advanced);
        Assert.Equal(5, result.StageOrder);
    }
}

/// <summary>
/// Clase aparte con su propia base: un skill desconocido no admite avance, y no hace falta ni un
/// perfil ni un objetivo guardados para evaluar el avance.
/// </summary>
public sealed class SkillStageAdvanceUnknownSkillEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_advance_with_an_unknown_skill_returns_400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/catalog/progress/ghost/advance", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("El skill indicado no existe en el catálogo.", problem!.Detail);
    }
}

/// <summary>
/// Clase aparte con su propia base: el avance solo cuenta los registros del mesociclo en curso
/// (FIX-3). Tras cerrar el mesociclo, sus sesiones —que cumplirían el criterio— no disparan el
/// avance; solo lo hacen las sesiones registradas después del cierre.
/// </summary>
public sealed class SkillStageAdvanceClosedMesocycleEndpointTests(BarrappApiFactory factory)
    : IClassFixture<BarrappApiFactory>
{
    [Fact]
    public async Task Post_advance_ignores_the_sessions_of_a_closed_mesocycle()
    {
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync("/profile", PlanTestData.Profile(3));
        await client.PutAsJsonAsync("/profile/objective", new { skillId = "planche" });

        // Materializa y cierra un mesociclo para fijar la frontera temporal: las sesiones del
        // mesociclo anterior quedan antes del cierre.
        await client.PostAsync("/plan", content: null);
        await client.RegisterSkill("handstand-wall-support", sessionDay: 1, 30, 30, 30);
        await client.RegisterSkill("handstand-wall-support", sessionDay: 2, 30, 30, 30);
        using var close = await client.PostAsync("/plan/close", content: null);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);

        // Las dos sesiones del mesociclo cerrado cumplirían el criterio, pero ya no cuentan.
        using var advance = await client.PostAsync("/catalog/progress/handstand/advance", null);
        Assert.Equal(HttpStatusCode.OK, advance.StatusCode);
        var kept = await advance.Content.ReadFromJsonAsync<SkillStageAdvanceResponse>();
        Assert.NotNull(kept);
        Assert.False(kept!.Advanced);
        Assert.Equal(1, kept.StageOrder);

        // Dos sesiones nuevas (posteriores al cierre) sí hacen avanzar.
        await client.RegisterSkill("handstand-wall-support", sessionDay: 3, 30, 30, 30);
        await client.RegisterSkill("handstand-wall-support", sessionDay: 4, 30, 30, 30);

        using var secondAdvance = await client.PostAsync("/catalog/progress/handstand/advance", null);
        var advanced = await secondAdvance.Content.ReadFromJsonAsync<SkillStageAdvanceResponse>();
        Assert.NotNull(advanced);
        Assert.True(advanced!.Advanced);
        Assert.Equal(2, advanced.StageOrder);
    }
}

/// <summary>
/// Ayudante: registra la sesión del ejercicio de la etapa, serie a serie, con los valores dados.
/// </summary>
internal static class SkillStageAdvanceTestData
{
    public static async Task RegisterSkill(
        this HttpClient client,
        string exerciseId,
        int sessionDay,
        params int[] values)
    {
        var response = await client.PostAsJsonAsync(
            "/session-logs",
            new
            {
                exerciseId,
                mesocycleId = (Guid?)null,
                sessionDay,
                sets = values
                    .Select((value, index) => new { setNumber = index + 1, value })
                    .ToList(),
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
