namespace Barrapp.Domain.Users;

/// <summary>
/// Atleta propietario de sus datos (perfil, plan, registros). En el MVP sin login hay una única
/// fila, sembrada por la migración; el esquema ya está preparado para multi-usuario.
/// </summary>
public sealed class User
{
    private User(Guid id) => Id = id;

    // Requerido por EF Core para materializar la entidad.
    private User()
    {
    }

    /// <summary>Identificador del usuario.</summary>
    public Guid Id { get; private set; }

    /// <summary>Crea un usuario con el identificador indicado.</summary>
    public static User Create(Guid id) => new(id);
}
