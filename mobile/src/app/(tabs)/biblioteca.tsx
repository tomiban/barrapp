import { useState } from 'react';

import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { EmptyState } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { Header, Screen } from '@/design-system/Navigation';
import { Text } from '@/design-system/Text';
import { ExercisesView } from '@/features/library/ExercisesView';
import { SkillsView } from '@/features/library/SkillsView';

/** Secciones de la Biblioteca. */
type LibrarySection = 'exercises' | 'skills' | 'routines';

const SECTION_OPTIONS: readonly SegmentedOption<LibrarySection>[] = [
  { value: 'exercises', label: 'Ejercicios' },
  { value: 'skills', label: 'Skills' },
  { value: 'routines', label: 'Rutinas' },
];

/**
 * Pantalla de la Biblioteca: un `SegmentedControl` con tres secciones.
 *
 * `Ejercicios` es esta entrega (#7). `Skills` y `Rutinas` son puntos de extensión:
 * los implementan #8 y #67 como vistas hermanas en `@/features/library/`
 * (`SkillsView.tsx` / `RoutinesView.tsx`), sustituyendo su `EmptyState` sin tocar
 * el resto de la pantalla.
 */
export default function LibraryScreen() {
  const [section, setSection] = useState<LibrarySection>('exercises');

  return (
    <Screen testID="library-screen" header={<Header title="Biblioteca" />}>
      <Stack gap="md">
        <Text variant="labelTechnical" className="text-text-muted">
          BARRAPP · BIBLIOTECA
        </Text>

        <SegmentedControl
          options={SECTION_OPTIONS}
          value={section}
          onChange={setSection}
          label="Sección de la biblioteca"
          testID="library-section"
        />

        {section === 'exercises' ? <ExercisesView /> : null}

        {section === 'skills' ? <SkillsView /> : null}

        {section === 'routines' ? (
          <EmptyState
            title="Próximamente"
            description="Las rutinas de patrón llegarán en el próximo módulo."
            testID="library-routines-empty"
          />
        ) : null}
      </Stack>
    </Screen>
  );
}
