import type { ExerciseCatalog } from '@/api/catalog/exercises';
import type { Skill } from '@/api/catalog/skills';

/**
 * Índice `exerciseId → nombre` con el que las vistas resuelven el nombre en español de las filas
 * de una rutina.
 *
 * Combina el catálogo de ejercicios (acondicionamiento) con los nombres de etapa de los skills:
 * los movimientos de skill no declaran grupo, así que no viajan en `/catalog/exercises` y su
 * nombre solo vive en la escalera del skill. El nombre del catálogo tiene prioridad; si no hay
 * coincidencia, la etapa lo rellena.
 */
export function buildExerciseNameIndex(
  catalog: ExerciseCatalog,
  skills: Skill[],
): Map<string, string> {
  const names = new Map<string, string>();

  for (const group of catalog.groups) {
    for (const exercise of group.exercises) {
      names.set(exercise.id, exercise.name);
    }
  }

  for (const skill of skills) {
    for (const stage of skill.stages) {
      if (!names.has(stage.exerciseId)) {
        names.set(stage.exerciseId, stage.name);
      }
    }
  }

  return names;
}
