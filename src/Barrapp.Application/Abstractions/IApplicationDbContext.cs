using Barrapp.Domain.Athlete;
using Barrapp.Domain.Objectives;
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
}
