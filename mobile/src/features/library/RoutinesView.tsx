import { useCallback, useEffect, useState } from 'react';

import { fetchExerciseCatalog } from '@/api/catalog/exercises';
import {
  fetchRoutineCatalog,
  ROUTINE_PROGRAM_TYPE_LABELS,
  type ProgramRoutine,
  type RoutineBlock,
  type RoutineProgram,
} from '@/api/catalog/routines';
import { fetchSkillCatalog } from '@/api/catalog/skills';
import { messageOf } from '@/api/messageOf';
import { Button } from '@/design-system/Button';
import { Banner, EmptyState, Loading } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { Text } from '@/design-system/Text';
import { buildExerciseNameIndex } from '@/features/library/exerciseNames';
import { formatRoutineItem } from '@/features/library/routineItemText';

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; programs: RoutineProgram[]; exerciseNames: Map<string, string> }
  | { status: 'error'; message: string };

/** Texto de una rutina: intensidad y duración, p. ej. `Intensidad 2 · 14 min`. */
function formatRoutineMeta(routine: ProgramRoutine): string {
  const parts = [`Intensidad ${routine.intensity}`];

  if (routine.durationMinutes !== null) {
    parts.push(`${routine.durationMinutes} min`);
  }

  return parts.join(' · ');
}

/** Cabecera de un bloque: nombre, vueltas y descanso, p. ej. `SET 1 · 3 vueltas`. */
function formatBlockMeta(block: RoutineBlock): string {
  const parts = [block.name];

  if (block.rounds > 1) {
    parts.push(`${block.rounds} vueltas`);
  }

  if (block.restSeconds > 0) {
    parts.push(`descanso ${block.restSeconds} s`);
  }

  return parts.join(' · ');
}

/** Bloque de una rutina: su cabecera y las filas de ejercicios. */
function RoutineBlockView({
  testID,
  block,
  nameOf,
}: {
  testID: string;
  block: RoutineBlock;
  nameOf: (exerciseId: string) => string;
}) {
  return (
    <Stack gap="xs" testID={testID}>
      <Text variant="labelTechnical" className="text-text-muted">
        {formatBlockMeta(block)}
      </Text>
      {block.items.map((item, index) => (
        <ListRow
          key={`${item.exerciseId}-${index}`}
          title={nameOf(item.exerciseId)}
          subtitle={formatRoutineItem(item)}
          last={index === block.items.length - 1}
        />
      ))}
    </Stack>
  );
}

/**
 * Vista «Rutinas» de la Biblioteca: lee `GET /catalog/routines` y pinta los programas generales
 * con sus rutinas (nombre, intensidad y duración) y sus bloques (nombre, vueltas y descanso, con
 * los ejercicios de cada fila). Solo lectura.
 *
 * Es hermana de `ExercisesView` (#7) y `SkillsView` (#8): comparten el `SegmentedControl` de
 * `biblioteca.tsx` pero viven en su propio módulo. El endpoint referencia los ejercicios por id;
 * para mostrar el nombre se combinan el catálogo de ejercicios con los nombres de etapa de los
 * skills (los movimientos de skill no viajan en `/catalog/exercises`).
 */
export function RoutinesView() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });

  const load = useCallback((signal?: AbortSignal) => {
    Promise.all([
      fetchRoutineCatalog(signal),
      fetchExerciseCatalog(signal),
      fetchSkillCatalog(signal),
    ])
      .then(([programs, catalog, skills]) =>
        setState({
          status: 'ready',
          programs,
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
    return <Loading label="Leyendo los programas generales…" testID="library-routines-loading" />;
  }

  if (state.status === 'error') {
    return (
      <Stack gap="sm">
        <Banner role="error" message={state.message} testID="library-routines-error" />
        <Button onPress={retry} testID="library-routines-retry">
          Reintentar
        </Button>
      </Stack>
    );
  }

  if (state.programs.length === 0) {
    return (
      <EmptyState
        title="Sin rutinas"
        description="El catálogo todavía no tiene programas."
        testID="library-routines-empty"
      />
    );
  }

  return (
    <Stack gap="xl">
      {state.programs.map((program) => {
        const nameOf = (exerciseId: string) => state.exerciseNames.get(exerciseId) ?? exerciseId;

        return (
          <Stack key={program.id} gap="md" testID={`library-program-${program.id}`}>
            <SectionHeader
              label={program.name}
              count={program.routines.length}
              testID={`library-program-${program.id}-header`}
            />
            <Text variant="bodySm" className="text-text-muted">
              {ROUTINE_PROGRAM_TYPE_LABELS[program.type]}
            </Text>
            {program.description ? (
              <Text variant="bodySm" className="text-text-muted">
                {program.description}
              </Text>
            ) : null}

            {program.routines.map((routine) => (
              <Stack
                key={routine.id}
                gap="sm"
                testID={`library-program-${program.id}-routine-${routine.id}`}
              >
                <SectionHeader
                  label={routine.name}
                  testID={`library-program-${program.id}-routine-${routine.id}-header`}
                />
                <Text variant="bodySm" className="text-text-muted">
                  {formatRoutineMeta(routine)}
                </Text>
                {routine.blocks.map((block, index) => (
                  <RoutineBlockView
                    key={`${block.name}-${index}`}
                    testID={`library-program-${program.id}-routine-${routine.id}-block-${index}`}
                    block={block}
                    nameOf={nameOf}
                  />
                ))}
              </Stack>
            ))}
          </Stack>
        );
      })}
    </Stack>
  );
}
