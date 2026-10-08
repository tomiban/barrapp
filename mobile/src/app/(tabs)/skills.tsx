import { useState } from 'react';

import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Stack } from '@/design-system/layout';
import { AppHeader, Screen } from '@/design-system/Navigation';
import { ExercisesView } from '@/features/library/ExercisesView';
import { RoutinesView } from '@/features/library/RoutinesView';
import { SkillsView } from '@/features/library/SkillsView';

/** Segmentos del hub Skills, en el orden del mockup. `Escaleras` es el principal. */
type SkillsSection = 'ladders' | 'exercises' | 'routines';

const SECTION_OPTIONS: readonly SegmentedOption<SkillsSection>[] = [
  { value: 'ladders', label: 'Escaleras' },
  { value: 'exercises', label: 'Ejercicios' },
  { value: 'routines', label: 'Rutinas' },
];

/**
 * Pantalla Skills (#82): un hub con tres segmentos —**Escaleras** (el
 * principal, con las escaleras de progresión), **Ejercicios** y **Rutinas**—
 * sobre el catálogo que antes vivía en la pantalla Biblioteca.
 *
 * Las tres vistas son hermanas en `@/features/library/`; cada una lee su
 * endpoint del catálogo y sustituye a la anterior sin tocar el resto de la
 * pantalla.
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
          label="Segmento de skills"
          testID="skills-section"
        />

        {section === 'ladders' ? <SkillsView /> : null}

        {section === 'exercises' ? <ExercisesView /> : null}

        {section === 'routines' ? <RoutinesView /> : null}
      </Stack>
    </Screen>
  );
}
