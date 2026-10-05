import { useCallback, useEffect, useState } from 'react';

import { EXERCISE_GROUP_LABELS, fetchExerciseCatalog } from '@/api/catalog/exercises';
import {
  fetchSkillCatalog,
  type PatternRoutine,
  type Skill,
  type SkillStage,
  type StageCriterion,
} from '@/api/catalog/skills';
import { Button } from '@/design-system/Button';
import { Banner, EmptyState, Loading } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { Text } from '@/design-system/Text';
import { buildExerciseNameIndex } from '@/features/library/exerciseNames';
import { formatRoutineItem } from '@/features/library/routineItemText';

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; skills: Skill[]; exerciseNames: Map<string, string> }
  | { status: 'error'; message: string };

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido';
}

/** Texto del criterio de una etapa, p. ej. `3 × 20 s` o `3 × 5 reps`. */
function formatCriterion(criterion: StageCriterion): string {
  return criterion.metric === 'seconds'
    ? `${criterion.sets} × ${criterion.target} s`
    : `${criterion.sets} × ${criterion.target} reps`;
}

/** Bloques de una rutina de patrón: modelos con sus filas. */
function PatternRoutineBlock({
  skillId,
  routine,
  nameOf,
}: {
  skillId: string;
  routine: PatternRoutine;
  nameOf: (exerciseId: string) => string;
}) {
  return (
    <Stack gap="xs" testID={`library-skill-${skillId}-routine-${routine.id}`}>
      <Text variant="labelTechnical" className="text-text-muted">
        {routine.name}
      </Text>
      {routine.items.map((item, index) => (
        <ListRow
          key={`${routine.id}-${item.exerciseId}-${index}`}
          title={nameOf(item.exerciseId)}
          subtitle={formatRoutineItem(item)}
          last={index === routine.items.length - 1}
        />
      ))}
    </Stack>
  );
}

/**
 * Vista «Skills» de la Biblioteca: lee `GET /catalog/skills` y pinta la escalera de cada skill
 * (movimiento y criterio por etapa) y sus rutinas de patrón (ejercicios, series, reps/segundos y
 * descanso). Solo lectura.
 *
 * El endpoint de skills referencia los ejercicios por id; para mostrar el nombre en español se
 * combinan el catálogo de ejercicios (acondicionamiento) con los nombres de etapa del propio
 * skill (movimientos de la escalera). Es hermana de `ExercisesView` (#7) y `RoutinesView` (#67).
 *
 * Deja la estructura por etapa para que #9 resalte la etapa actual sin reorganizarla.
 */
export function SkillsView() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });

  const load = useCallback((signal?: AbortSignal) => {
    Promise.all([fetchSkillCatalog(signal), fetchExerciseCatalog(signal)])
      .then(([skills, catalog]) =>
        setState({
          status: 'ready',
          skills,
          exerciseNames: buildExerciseNameIndex(catalog, skills),
        }),
      )
      .catch((error: unknown) => {
        if (error instanceof Error && error.name === 'AbortError') {
          return;
        }
        setState({ status: 'error', message: messageOf(error) });
      });
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    load(controller.signal);
    return () => controller.abort();
  }, [load]);

  const retry = useCallback(() => {
    setState({ status: 'loading' });
    load();
  }, [load]);

  if (state.status === 'loading') {
    return <Loading label="Leyendo las escaleras de skills…" testID="library-skills-loading" />;
  }

  if (state.status === 'error') {
    return (
      <Stack gap="sm">
        <Banner role="error" message={state.message} testID="library-skills-error" />
        <Button onPress={retry} testID="library-skills-retry">
          Reintentar
        </Button>
      </Stack>
    );
  }

  if (state.skills.length === 0) {
    return (
      <EmptyState
        title="Sin skills"
        description="El catálogo todavía no tiene skills."
        testID="library-skills-empty"
      />
    );
  }

  return (
    <Stack gap="xl">
      {state.skills.map((skill) => {
        const nameOf = (exerciseId: string) => state.exerciseNames.get(exerciseId) ?? exerciseId;

        return (
          <Stack key={skill.id} gap="md" testID={`library-skill-${skill.id}`}>
            <SectionHeader label={skill.name} testID={`library-skill-${skill.id}-header`} />
            <Text variant="bodySm" className="text-text-muted">
              {`${EXERCISE_GROUP_LABELS[skill.group]}${skill.lever ? ' · Apalancado' : ''}`}
            </Text>

            <Stack gap="sm">
              <SectionHeader
                label="Escalera"
                count={skill.stages.length}
                testID={`library-skill-${skill.id}-ladder`}
              />
              {skill.stages.map((stage: SkillStage, index: number) => (
                <ListRow
                  key={stage.order}
                  title={stage.name}
                  subtitle={`Etapa ${stage.order} · ${formatCriterion(stage.criterion)}`}
                  last={index === skill.stages.length - 1}
                  testID={`library-skill-${skill.id}-stage-${stage.order}`}
                />
              ))}
            </Stack>

            <Stack gap="sm">
              <SectionHeader
                label="Rutinas de patrón"
                count={skill.patternRoutines.length}
                testID={`library-skill-${skill.id}-routines`}
              />
              {skill.patternRoutines.map((routine) => (
                <PatternRoutineBlock
                  key={routine.id}
                  skillId={skill.id}
                  routine={routine}
                  nameOf={nameOf}
                />
              ))}
            </Stack>
          </Stack>
        );
      })}
    </Stack>
  );
}
