using Barrapp.Application.Abstractions;

namespace Barrapp.Application.Features.SoloSessions;

/// <summary>
/// Marca una sesión suelta generada como registrada (el atleta la dio por hecha). El aislamiento
/// es estructural: registrar una suelta solo cambia su propio estado y no alimenta el avance de
/// etapa ni el ajuste de máximos (D8).
/// </summary>
/// <param name="Id">Identificador de la suelta en el historial.</param>
public sealed record RecordSoloSessionCommand(Guid Id) : ICommand<SessionSueltaResponse>;
