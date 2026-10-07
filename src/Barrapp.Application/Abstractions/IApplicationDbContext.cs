using Barrapp.Domain.Athlete;
using Barrapp.Domain.Objectives;
using Barrapp.Domain.Planning;
using Barrapp.Domain.Sessions;
using Barrapp.Domain.SkillProgress;
using Microsoft.EntityFrameworkCore;

namespace Barrapp.Application.Abstractions;

/// <summary>
/// Acceso de lectura a la base de datos para las <i>queries</i>, que proyectan directamente a DTO.
/// Expone <see cref="DbSet{TEntity}"/>, así que Application referencia EF Core: es el compromiso
/// pragmático recomendado para el lado de lectura (ver <c>docs/architecture.md</c>). Las
/// escrituras siguen pasando por repositorios y <see cref="IUnitOfWork"/>.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>Perfiles de atleta.</summary>
    DbSet<AthleteProfile> AthleteProfiles { get; }

    /// <summary>Objetivos del mesociclo.</summary>
    DbSet<Objective> Objectives { get; }

    /// <summary>Etapa actual del atleta por skill.</summary>
    DbSet<AthleteSkillProgress> AthleteSkillProgresses { get; }

    /// <summary>Registros de sesión (cabecera con su clave de sesión y su marca de completada).</summary>
    DbSet<SessionLog> SessionLogs { get; }

    /// <summary>Historial de sesiones sueltas (aislado de los registros de sesión).</summary>
    DbSet<SessionSuelta> SessionSuelta { get; }

    /// <summary>Mesociclos persistidos: el plan guardado al generarse (D7).</summary>
    DbSet<Mesocycle> Mesocycles { get; }
}
