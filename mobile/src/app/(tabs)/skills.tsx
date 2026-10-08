import { useState } from 'react';

import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Stack } from '@/design-system/layout';
import { Screen } from '@/design-system/Navigation';
import { ExercisesView } from '@/features/library/ExercisesView';
import { RoutinesView } from '@/features/library/RoutinesView';
import { SkillsView } from '@/features/library/SkillsView';
import { AppHeader } from '@/features/navigation';

/** Secciones del hub Skills. */
type SkillsSection = 'ladders' | 'exercises' | 'routines';

const SECTION_OPTIONS: readonly SegmentedOption<SkillsSection>[] = [
  { value: 'ladders', label: 'Escaleras' },
  { value: 'exercises', label: 'Ejercicios' },
  { value: 'routines', label: 'Rutinas' },
];

/**
 * Pantalla **Skills** (spec 0003, ticket #82): hub con tres segmentos.
 *
 * *Escaleras* es el principal (la escalera de progresión de cada skill); los
 * segmentos *Ejercicios* y *Rutinas* son el catálogo que antes vivía en la
 * Biblioteca. La antigua pestaña «Biblioteca» se renombra a «Skills».
 */
export default function SkillsScreen() {
  const [section, setSection] = useState<SkillsSection>('ladders');

  return (
    <Screen testID="skills-screen" header={<AppHeader section="Skills" />} scrollable>
      <Stack gap="md">
        <SegmentedControl
          options={SECTION_OPTIONS}
          value={section}
          onChange={setSection}
          label="Sección de Skills"
          testID="skills-section"
        />

        {section === 'ladders' ? <SkillsView /> : null}

        {section === 'exercises' ? <ExercisesView /> : null}

        {section === 'routines' ? <RoutinesView /> : null}
      </Stack>
    </Screen>
  );
}
