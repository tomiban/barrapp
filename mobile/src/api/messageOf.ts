/** Mensaje legible de un error desconocido, para mostrarlo en la UI. */
export function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido';
}
