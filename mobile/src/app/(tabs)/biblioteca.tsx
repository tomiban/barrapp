import { useState } from 'react';

import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Stack } from '@/design-system/layout';
import { Header, Screen } from '@/design-system/Navigation';
import { Text } from '@/design-system/Text';
import { ExercisesView } from '@/features/library/ExercisesView';
import { RoutinesView } from '@/features/library/RoutinesView';
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
 * `Ejercicios` (#7), `Skills` (#8) y `Rutinas` (#67) son vistas hermanas en
 * `@/features/library/`; cada una lee su endpoint del catálogo y sustituye el
 * `EmptyState` original sin tocar el resto de la pantalla.
 */
export default function LibraryScreen() {
  const [section, setSection] = useState<LibrarySection>('exercises');

  return (
    <Screen testID="library-screen" header={<Header title="Biblioteca" />} scrollable>
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

        {section === 'routines' ? <RoutinesView /> : null}
      </Stack>
    </Screen>
  );
}
