import { Box, type BoxProps } from '@/design-system/layout';
import { ROLE_TEXT_CLASS, type SemanticRole } from '@/design-system/semantic';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

/**
 * Rol semántico del readout. Sin rol, el dato principal se pinta neutro
 * (`text`): no es un estado y por eso no lleva acento. Con un rol canónico de
 * `semantic.ts` (`active` · `confirmed` · `error` · `inactive`) el color sale
 * del token del rol.
 */
const NEUTRAL_TEXT_CLASS = 'text-text';

/**
 * Formatea el índice de la celda como `[SEC.01]`.
 *
 * - Número → dos dígitos con cero a la izquierda, con `indexPrefix` opcional.
 * - Texto → se usa tal cual; si ya viene entre corchetes se respeta.
 * - Vacío / no finito → sin índice.
 */
function formatIndex(
  index: string | number | undefined,
  prefix: string | undefined,
): string | undefined {
  if (index === undefined) {
    return undefined;
  }

  if (typeof index === 'number') {
    if (!Number.isFinite(index)) {
      return undefined;
    }
    const digits = String(Math.trunc(index)).padStart(2, '0');
    return prefix ? `[${prefix}.${digits}]` : `[${digits}]`;
  }

  const text = index.trim();
  if (text.length === 0) {
    return undefined;
  }
  return /^\[.*\]$/.test(text) ? text : `[${text}]`;
}

/** Etiqueta accesible: label + value + unit, sin separadores sobrantes. */
function accessibilityText(label: string, value: string, unit: string | undefined): string {
  return [label, value, unit]
    .filter((part): part is string => typeof part === 'string')
    .map((part) => part.trim())
    .filter((part) => part.length > 0)
    .join(' ');
}

/** Props públicas de `MetricCounter`. */
export type MetricCounterProps = Omit<BoxProps, 'children' | 'role'> & {
  /** Micro-label de la celda (se renderiza en `labelTechnical`). */
  label: string;
  /** Readout monolítico; admite número o texto ya formateado. */
  value: string | number;
  /** Unidad legible alineada con el readout (KG/DEG/SEC/RPE…). */
  unit?: string;
  /** Índice de la celda: texto (`SEC.01`) o número formateado a dos dígitos. */
  index?: string | number;
  /** Prefijo del índice numérico (`SEC` → `[SEC.01]`). Ignorado si `index` es texto. */
  indexPrefix?: string;
  /** Rol semántico de estado que colorea el readout. Sin rol, neutro (`text`). */
  role?: SemanticRole;
  /** `className` del contenedor; se combina con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor. Deriva `-header`, `-index`, `-readout`, `-value` y `-unit`. */
  testID?: string;
};

/**
 * Celda modular de métrica del design system (spec 0002, ticket #40).
 *
 * Micro-label arriba a la izquierda, índice arriba a la derecha y un readout
 * monolítico de números monoespaciados tabulares (`headlineMetric`), legible a
 * distancia, con su unidad alineada por línea base. Superficie de nivel 1 con
 * hairline y radio `base`, **sin sombras**: la profundidad sale de bordes y
 * capas tonales.
 *
 * ```tsx
 * <MetricCounter label="Peso" value={100} unit="KG" index="SEC.01" />
 * <MetricCounter label="Carga" value={80} unit="%" role="active" index={2} indexPrefix="SER" />
 * ```
 */
export function MetricCounter({
  label,
  value,
  unit,
  index,
  indexPrefix,
  role,
  className,
  testID,
  ...rest
}: MetricCounterProps) {
  const indexText = formatIndex(index, indexPrefix);
  const valueText = String(value);

  return (
    <Box
      accessible
      accessibilityRole="text"
      accessibilityLabel={accessibilityText(label, valueText, unit)}
      className={cn('gap-xs rounded-base border border-border bg-surface p-md', className)}
      testID={testID}
      {...rest}
    >
      <Box
        className="flex-row items-start justify-between gap-sm"
        testID={testID ? `${testID}-header` : undefined}
      >
        <Text variant="labelTechnical" className="flex-1 text-text-muted">
          {label}
        </Text>
        {indexText ? (
          <Text
            variant="labelCode"
            className="text-text-muted"
            testID={testID ? `${testID}-index` : undefined}
          >
            {indexText}
          </Text>
        ) : null}
      </Box>
      <Box
        className="flex-row items-baseline gap-xs"
        testID={testID ? `${testID}-readout` : undefined}
      >
        <Text
          variant="headlineMetric"
          className={role ? ROLE_TEXT_CLASS[role] : NEUTRAL_TEXT_CLASS}
          testID={testID ? `${testID}-value` : undefined}
        >
          {valueText}
        </Text>
        {unit ? (
          <Text
            variant="labelTechnical"
            className="text-text-muted"
            testID={testID ? `${testID}-unit` : undefined}
          >
            {unit}
          </Text>
        ) : null}
      </Box>
    </Box>
  );
}
