import { useCallback, useEffect, useState } from 'react';

import {
  EXERCISE_GROUP_LABELS,
  fetchExerciseCatalog,
  type ExerciseCatalog,
} from '@/api/catalog/exercises';
import { Button } from '@/design-system/Button';
import { Banner, EmptyState, Loading } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';

type LoadState =
  | { status: 'loading' }
  | { status: 'ready'; catalog: ExerciseCatalog }
  | { status: 'error'; message: string };

function messageOf(error: unknown): string {
  return error instanceof Error ? error.message : 'Error desconocido';
}

/**
 * Vista «Ejercicios» de la Biblioteca: lee `GET /catalog/exercises` y pinta la lista
 * agrupada por patrón (Empuje, Tirón, Pierna, Core, Cardio). Solo lectura.
 *
 * Es una vista hermana de `SkillsView` (#8) y `RoutinesView` (#67): comparten el
 * `SegmentedControl` de `biblioteca.tsx` pero viven en su propio módulo para no pisarse.
 */
export function ExercisesView() {
  const [state, setState] = useState<LoadState>({ status: 'loading' });

  const load = useCallback((signal?: AbortSignal) => {
    fetchExerciseCatalog(signal)
      .then((catalog) => setState({ status: 'ready', catalog }))
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
    return (
      <Loading label="Leyendo el catálogo de ejercicios…" testID="library-exercises-loading" />
    );
  }

  if (state.status === 'error') {
    return (
      <Stack gap="sm">
        <Banner role="error" message={state.message} testID="library-exercises-error" />
        <Button onPress={retry} testID="library-exercises-retry">
          Reintentar
        </Button>
      </Stack>
    );
  }

  if (state.catalog.groups.length === 0) {
    return (
      <EmptyState
        title="Sin ejercicios"
        description="El catálogo todavía no tiene ejercicios."
        testID="library-exercises-empty"
      />
    );
  }

  return (
    <Stack gap="lg">
      {state.catalog.groups.map((group) => (
        <Stack key={group.group} gap="sm">
          <SectionHeader
            label={EXERCISE_GROUP_LABELS[group.group]}
            count={group.exercises.length}
            testID={`library-group-${group.group}`}
          />
          {group.exercises.map((exercise, index) => (
            <ListRow
              key={exercise.id}
              title={exercise.name}
              last={index === group.exercises.length - 1}
              testID={`library-exercise-${exercise.id}`}
            />
          ))}
        </Stack>
      ))}
    </Stack>
  );
}
