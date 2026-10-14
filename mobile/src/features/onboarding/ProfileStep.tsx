import {
  BASIC_EXERCISES,
  BODY_MEASUREMENT_LIMITS,
  EXERCISE_PATTERN_LABELS,
  EXERCISE_PATTERNS,
} from '@/api/athleteProfile';
import { Stack } from '@/design-system/layout';
import { Stepper } from '@/design-system/Stepper';
import { Text } from '@/design-system/Text';

import type { MeasurementField, OnboardingDraft } from './draft';
import { StepSectionLabel } from './StepSectionLabel';

/** Una medida de antropometría con su etiqueta y unidad. */
type MeasurementDefinition = {
  field: MeasurementField;
  label: string;
  unit: string;
};

/** Las cuatro medidas del paso 1, en el orden del mockup. */
const MEASUREMENTS: readonly MeasurementDefinition[] = [
  { field: 'weightKilograms', label: 'Peso', unit: 'KG' },
  { field: 'heightCentimeters', label: 'Altura', unit: 'CM' },
  { field: 'armSpanCentimeters', label: 'Envergadura', unit: 'CM' },
  { field: 'inseamCentimeters', label: 'Piernas', unit: 'CM' },
];

/** Props del paso 1: el borrador y los cambios de medidas y máximos. */
export type ProfileStepProps = {
  /** Borrador con medidas y máximos. */
  draft: OnboardingDraft;
  /** Cambio de una medida de antropometría. */
  onChangeMeasurement: (field: MeasurementField, value: number) => void;
  /** Cambio del máximo de un ejercicio básico. */
  onChangeMaximum: (exerciseCode: string, value: number) => void;
};

/**
 * Paso 1 · Perfil (spec 0003, ticket #90): antropometría y test de fuerza
 * básica con steppers. Reutiliza los rangos de validación de 0001 y los tres
 * ejercicios básicos del catálogo (los que el API acepta como máximos).
 */
export function ProfileStep({ draft, onChangeMeasurement, onChangeMaximum }: ProfileStepProps) {
  return (
    <Stack gap="lg" testID="onboarding-step-profile">
      <Stack gap="xs">
        <Text variant="bodyMd" className="text-text-muted">
          Ingresa tus datos corporales y máximos actuales para calibrar tu plan de entrenamiento.
        </Text>
      </Stack>

      <Stack gap="md">
        <Stack gap="xs">
          <StepSectionLabel index={1}>ANTROPOMETRÍA</StepSectionLabel>
          <Text variant="labelCode" className="text-text-muted">
            PALANCAS [UNIDADES: SI]
          </Text>
        </Stack>
        <Stack gap="md">
          {MEASUREMENTS.map(({ field, label, unit }) => (
            <Stepper
              key={field}
              label={label}
              value={draft[field]}
              min={BODY_MEASUREMENT_LIMITS[field].min}
              max={BODY_MEASUREMENT_LIMITS[field].max}
              unit={unit}
              onChange={(value) => onChangeMeasurement(field, value)}
              testID={`onboarding-${field}`}
            />
          ))}
        </Stack>
      </Stack>

      <Stack gap="md">
        <Stack gap="xs">
          <StepSectionLabel index={2}>TEST DE FUERZA BÁSICA</StepSectionLabel>
          <Text variant="labelCode" className="text-text-muted">
            [REPETICIONES ESTRICTAS]
          </Text>
        </Stack>
        <Stack gap="md">
          {EXERCISE_PATTERNS.map((pattern) => {
            const exercises = BASIC_EXERCISES.filter((exercise) => exercise.pattern === pattern);
            return (
              <Stack key={pattern} gap="sm">
                <Text variant="bodySm" className="text-text-muted">
                  {EXERCISE_PATTERN_LABELS[pattern]}
                </Text>
                {exercises.map((exercise) => (
                  <Stepper
                    key={exercise.code}
                    label={exercise.name}
                    value={draft.maximums[exercise.code] ?? 0}
                    min={0}
                    unit="REPS"
                    onChange={(value) => onChangeMaximum(exercise.code, value)}
                    testID={`onboarding-maximum-${exercise.code}`}
                  />
                ))}
              </Stack>
            );
          })}
        </Stack>
      </Stack>
    </Stack>
  );
}
