using Barrapp.Application.Abstractions;
using MediatR;

namespace Barrapp.Application.Features.SessionLogs;

/// <summary>
/// Elimina un registro de sesión ya guardado (spec 0001, US-36; decisión D5): borrar elimina el
/// registro y sus series. El comando no devuelve valor; el handler responde <see cref="Unit"/>.
/// </summary>
/// <param name="Id">Identificador del registro a eliminar.</param>
public sealed record DeleteSessionLogCommand(Guid Id) : ICommand<Unit>;
