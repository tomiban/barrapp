namespace Barrapp.Application.Common;

/// <summary>
/// MVP mono-usuario y sin login: todos los datos pertenecen a este usuario fijo. Aísla en un
/// único sitio la decisión de <i>quién</i> es el atleta hoy; cuando llegue la autenticación,
/// se sustituye por la resolución del usuario real sin tocar los handlers.
/// </summary>
public static class SingleUser
{
    /// <summary>Identificador estable del único usuario del MVP.</summary>
    public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
