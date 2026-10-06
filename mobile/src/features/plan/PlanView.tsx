import { useState } from 'react';

import { EXERCISE_GROUP_LABELS } from '@/api/catalog/exercises';
import type { Plan, PlanItemRole, PlanSessionItem } from '@/api/plan';
import { EmptyState } from '@/design-system/Feedback';
import { Stack } from '@/design-system/layout';
import { ListRow, SectionHeader } from '@/design-system/ListRow';
import { SegmentedControl, type SegmentedOption } from '@/design-system/Chip';
import { Text } from '@/design-system/Text';

/** Nombre para la UI de cada papel de la sesión. */
const ROLE_LABELS: Record<PlanItemRole, string> = {
  skill: 'Skill',
  strength: 'Fuerza',
  core: 'Core',
};

/** Texto del rango de una fila, p. ej. `3 × 20–30 s` o `3 × 8–12 reps`. */
function formatRange(item: PlanSessionItem): string {
  if (item.holdSecondsMin !== null && item.holdSecondsMax !== null) {
    return `${item.sets} × ${item.holdSecondsMin}–${item.holdSecondsMax} s`;
  }

  if (item.repsMin !== null && item.repsMax !== null) {
    return `${item.sets} × ${item.repsMin}–${item.repsMax} reps`;
  }

  return `${item.sets} series`;
}

/** Subtítulo de una fila: papel, patrón (en fuerza) y prescripción. */
function describeItem(item: PlanSessionItem): string {
  const parts = [ROLE_LABELS[item.role]];

  if (item.pattern) {
    parts.push(EXERCISE_GROUP_LABELS[item.pattern]);
  }

  parts.push(formatRange(item));

  return parts.join(' · ');
}

/**
 * Vista del plan: selector de semana (1–4) y, debajo, las sesiones de la semana elegida con sus
 * filas. Es presentacional: recibe el plan ya cargado y solo lo pinta con el design system.
 */
export function PlanView({ plan }: { plan: Plan }) {
  const [selectedWeek, setSelectedWeek] = useState(() => String(plan.microcycles[0]?.number ?? 1));

  if (plan.microcycles.length === 0) {
    return (
      <EmptyState
        title="Sin plan"
        description="El mesociclo todavía no tiene semanas."
        testID="plan-empty"
      />
    );
  }

  const weekOptions: readonly SegmentedOption[] = plan.microcycles.map((microcycle) => ({
    value: String(microcycle.number),
    label: `Semana ${microcycle.number}`,
  }));

  const microcycle =
    plan.microcycles.find((candidate) => String(candidate.number) === selectedWeek) ??
    plan.microcycles[0];

  return (
    <Stack gap="lg">
      <Stack gap="sm">
        <Text variant="labelTechnical" className="text-text-muted">
          BARRAPP · PLAN
        </Text>
        <Text variant="bodySm" className="text-text-muted">
          {`${plan.trainingDays} días/semana · ${plan.microcycles.length} semanas`}
        </Text>

        <SegmentedControl
          options={weekOptions}
          value={selectedWeek}
          onChange={setSelectedWeek}
          label="Semana del mesociclo"
          testID="plan-week"
        />
      </Stack>

      <Stack gap="lg" testID={`plan-microcycle-${microcycle.number}`}>
        {microcycle.sessions.map((session) => (
          <Stack key={session.day} gap="sm" testID={`plan-session-${session.day}`}>
            <SectionHeader
              label={`Día ${session.day}`}
              count={session.items.length}
              testID={`plan-session-${session.day}-header`}
            />

            {session.items.map((item, index) => (
              <ListRow
                key={`${session.day}-${item.exerciseId}`}
                title={item.exerciseName}
                subtitle={describeItem(item)}
                last={index === session.items.length - 1}
                testID={`plan-item-${session.day}-${item.exerciseId}`}
              />
            ))}
          </Stack>
        ))}
      </Stack>
    </Stack>
  );
}
