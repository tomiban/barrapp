using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SkillProgress;

/// <summary>
/// Evalúa el avance de etapa de un skill con los registros de sesión del atleta (spec 0001, US-19;
/// ticket #23): carga la progresión y los registros, delega en el motor de dominio
/// (<see cref="Barrapp.Domain.SkillProgress.SkillStageAdvancer"/>) y persiste la etapa nueva solo
/// si el motor decide avanzar. El skill va en la ruta.
/// </summary>
/// <param name="SkillId">Slug del skill a evaluar, presente en el catálogo.</param>
public sealed record AdvanceSkillStageCommand(string SkillId) : ICommand<SkillStageAdvanceResponse>;
