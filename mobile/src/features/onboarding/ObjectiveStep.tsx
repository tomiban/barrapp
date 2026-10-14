import { Pressable } from 'react-native';

import { EXERCISE_GROUP_LABELS } from '@/api/catalog/exercises';
import type { Skill } from '@/api/catalog/skills';
import type { TrainingWeekdayCode } from '@/api/athleteProfile';
import { TRAINING_DAYS_LIMITS, TRAINING_WEEKDAYS, WEEKDAY_LABELS } from '@/api/athleteProfile';
import { Button } from '@/design-system/Button';
import { Chip } from '@/design-system/Chip';
import { Banner, Loading } from '@/design-system/Feedback';
import { Box, Stack } from '@/design-system/layout';
import { StatusBadge } from '@/design-system/StatusBadge';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

import { StepSectionLabel } from './StepSectionLabel';

/** Props de una tarjeta de skill seleccionable. */
type SkillCardProps = {
  skill: Skill;
  selected: boolean;
  onSelect: () => void;
};

/** Tarjeta de skill del paso 2: nombre, patrón y estado `SELECCIONADO` textual. */
function SkillCard({ skill, selected, onSelect }: SkillCardProps) {
  return (
    <Pressable
      accessible
      accessibilityRole="button"
      accessibilityLabel={skill.name}
      accessibilityState={{ selected }}
      onPress={onSelect}
      className={cn(
        'gap-sm rounded-base border bg-surface p-md',
        selected ? 'border-primary' : 'border-border',
      )}
      testID={`onboarding-skill-${skill.id}`}
    >
      <Stack direction="row" gap="sm" className="items-center justify-between">
        <Text variant="headlineSm">{skill.name}</Text>
        {selected ? (
          <StatusBadge
            role="active"
            label="SELECCIONADO"
            testID={`onboarding-skill-${skill.id}-selected`}
          />
        ) : null}
      </Stack>
      <Text variant="bodySm" className="text-text-muted">
        {EXERCISE_GROUP_LABELS[skill.group]}
      </Text>
    </Pressable>
  );
}

/** Props del paso 2: catálogo de skills, skill elegido y días de entrenamiento. */
export type ObjectiveStepProps = {
  /** Skills del catálogo (`GET /catalog/skills`). */
  skills: Skill[];
  /** Estado de la lectura del catálogo. */
  loadState: 'loading' | 'ready' | 'error';
  /** Mensaje de error de la lectura, si la hubo. */
  loadError: string;
  /** Reintenta la lectura del catálogo. */
  onRetry: () => void;
  /** Skill objetivo elegido (`''` si todavía no hay). */
  skillId: string;
  /** Cambia el skill objetivo. */
  onSelectSkill: (skillId: string) => void;
  /** Días de la semana elegidos. */
  weekdays: TrainingWeekdayCode[];
  /** Alterna un día de la semana. */
  onToggleWeekday: (day: TrainingWeekdayCode, selected: boolean) => void;
  /** Error de validación del paso (skill o días), si lo hay. */
  error?: string;
};

/**
 * Paso 2 · Objetivo y frecuencia (spec 0003, ticket #90): elección del skill
 * objetivo del catálogo y de los días de la semana que entrena (3–5). El número
 * de días es cuántos se eligen, según la spec (no un segmentado 3/4/5).
 */
export function ObjectiveStep({
  skills,
  loadState,
  loadError,
  onRetry,
  skillId,
  onSelectSkill,
  weekdays,
  onToggleWeekday,
  error,
}: ObjectiveStepProps) {
  return (
    <Stack gap="lg" testID="onboarding-step-objective">
      <Text variant="bodyMd" className="text-text-muted">
        ¿Qué skill quieres conseguir en este mesociclo?
      </Text>

      {error ? (
        <Text
          variant="bodySm"
          className="text-error"
          accessibilityRole="alert"
          testID="onboarding-objective-error"
        >
          {error}
        </Text>
      ) : null}

      <Stack gap="md">
        <Stack gap="xs">
          <StepSectionLabel index={1}>SKILL OBJETIVO</StepSectionLabel>
          <Text variant="labelCode" className="text-text-muted">
            [SYS.PRIORIDAD]
          </Text>
        </Stack>

        {loadState === 'loading' ? (
          <Loading label="Leyendo los skills…" testID="onboarding-skills-loading" />
        ) : null}

        {loadState === 'error' ? (
          <Stack gap="sm">
            <Banner role="error" message={loadError} testID="onboarding-skills-error" />
            <Button variant="tertiary" onPress={onRetry} testID="onboarding-skills-retry">
              Reintentar
            </Button>
          </Stack>
        ) : null}

        {loadState === 'ready' ? (
          <Stack gap="sm">
            {skills.map((skill) => (
              <SkillCard
                key={skill.id}
                skill={skill}
                selected={skill.id === skillId}
                onSelect={() => onSelectSkill(skill.id)}
              />
            ))}
          </Stack>
        ) : null}
      </Stack>

      <Stack gap="md">
        <Stack gap="xs">
          <StepSectionLabel index={2}>DÍAS DE ENTRENAMIENTO</StepSectionLabel>
          <Text variant="labelCode" className="text-text-muted">
            [FREQ / 7D]
          </Text>
        </Stack>
        <Text variant="bodySm" className="text-text-muted">
          Elige los días de la semana que entrenas (entre {TRAINING_DAYS_LIMITS.min} y{' '}
          {TRAINING_DAYS_LIMITS.max}).
        </Text>
        <Box className="flex-row flex-wrap gap-sm">
          {TRAINING_WEEKDAYS.map((day) => (
            <Chip
              key={day}
              label={WEEKDAY_LABELS[day]}
              selected={weekdays.includes(day)}
              onChange={(selected) => onToggleWeekday(day, selected)}
              testID={`onboarding-weekday-${day}`}
            />
          ))}
        </Box>
      </Stack>
    </Stack>
  );
}
