import { BASIC_EXERCISES, WEEKDAY_LABELS, type TrainingWeekdayCode } from '@/api/athleteProfile';
import { BiomechanicalCard, CardSection } from '@/design-system/BiomechanicalCard';
import { Stack } from '@/design-system/layout';
import { Text } from '@/design-system/Text';

/** Una fila etiqueta/valor del registro de parámetros. */
function ParamRow({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" gap="sm" className="items-baseline justify-between">
      <Text variant="labelTechnical" className="text-text-muted">
        {label}
      </Text>
      <Text variant="bodyMd" className="text-right" testID={`onboarding-param-${label}`}>
        {value}
      </Text>
    </Stack>
  );
}

/** Props del paso 3: el resumen del borrador que el atleta confirma. */
export type ConfirmStepProps = {
  /** Nombre del skill objetivo elegido. */
  skillName: string;
  /** Días de la semana elegidos. */
  weekdays: TrainingWeekdayCode[];
  /** Peso en kg. */
  weightKilograms: number;
  /** Altura en cm. */
  heightCentimeters: number;
  /** Máximo en repeticiones por código de ejercicio básico. */
  maximums: Record<string, number>;
};

/**
 * Paso 3 · Confirmación (spec 0003, ticket #90): resumen del borrador antes de
 * generar el mesociclo. El NIVEL del mockup queda fuera del MVP; la etapa del
 * skill y el foco de cada sesión los asigna el motor al generar el plan, así
 * que la «semana tipo» lista los días elegidos, no el foco prescrito.
 */
export function ConfirmStep({
  skillName,
  weekdays,
  weightKilograms,
  heightCentimeters,
  maximums,
}: ConfirmStepProps) {
  return (
    <Stack gap="lg" testID="onboarding-step-confirm">
      <Text variant="bodyMd" className="text-text-muted">
        Revisa y confirma tu mesociclo de 4 semanas.
      </Text>

      <BiomechanicalCard
        title="REGISTRO DE PARÁMETROS"
        role="active"
        statusLabel="ACTIVO"
        testID="onboarding-summary"
      >
        <CardSection>
          <ParamRow label="SKILL OBJETIVO" value={skillName} />
        </CardSection>
        <CardSection>
          <ParamRow label="FRECUENCIA" value={`${weekdays.length} DÍAS / SEMANA`} />
        </CardSection>
        <CardSection>
          <ParamRow
            label="PESO / ALTURA"
            value={`${weightKilograms} KG · ${heightCentimeters} CM`}
          />
        </CardSection>
        <CardSection>
          <Text variant="labelTechnical" className="text-text-muted">
            MÁXIMOS
          </Text>
          <Text variant="bodyMd" testID="onboarding-summary-maximums">
            {BASIC_EXERCISES.map(
              (exercise) => `${exercise.name} ${maximums[exercise.code] ?? 0}`,
            ).join(' · ')}
          </Text>
        </CardSection>
      </BiomechanicalCard>

      <Stack gap="sm" testID="onboarding-typical-week">
        <Text variant="labelTechnical" className="text-primary">
          SEMANA TIPO
        </Text>
        {weekdays.map((day, index) => (
          <Stack
            key={day}
            direction="row"
            gap="sm"
            className="items-baseline"
            testID={`onboarding-typical-week-${day}`}
          >
            <Text variant="labelCode" className="text-text-muted">
              {`D0${index + 1}`}
            </Text>
            <Text variant="bodyMd">{WEEKDAY_LABELS[day].toUpperCase()}</Text>
          </Stack>
        ))}
      </Stack>
    </Stack>
  );
}
