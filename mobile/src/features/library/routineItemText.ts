import type { RoutineItem } from '@/api/catalog/skills';

/** Texto de series y rango de una fila, p. ej. `4 × 5–15 s` o `3 × 6–8 reps`. */
export function formatRoutineItemRange(item: RoutineItem): string {
  if (typeof item.holdSecondsMin === 'number' && typeof item.holdSecondsMax === 'number') {
    return `${item.sets} × ${item.holdSecondsMin}–${item.holdSecondsMax} s`;
  }

  if (typeof item.repsMin === 'number' && typeof item.repsMax === 'number') {
    return `${item.sets} × ${item.repsMin}–${item.repsMax} reps`;
  }

  return `${item.sets} series`;
}

/** Texto completo de una fila: series, rango y descanso. */
export function formatRoutineItem(item: RoutineItem): string {
  return `${formatRoutineItemRange(item)} · descanso ${item.restSeconds} s`;
}
