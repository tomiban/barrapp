using Barrapp.Domain.SkillProgress;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Puerto de persistencia de la etapa actual del atleta por skill. En el MVP mono-usuario hay como
/// mucho una progresión por skill y atleta.
/// </summary>
public interface IAthleteSkillProgressRepository
{
    /// <summary>Devuelve la progresión del atleta en el skill o <c>null</c> si no existe.</summary>
    Task<AthleteSkillProgress?> GetAsync(Guid userId, string skillId, CancellationToken cancellationToken);

    /// <summary>Marca una progresión nueva para que se inserte al guardar la unidad de trabajo.</summary>
    void Add(AthleteSkillProgress progress);
}
