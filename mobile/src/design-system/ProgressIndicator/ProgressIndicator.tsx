import { Text, View } from 'react-native';
import Svg, { Circle } from 'react-native-svg';
import { useCSSVariable } from 'uniwind';

import { cn } from '../utils/cn';

/**
 * Rol semántico de estado (spec 0002): define el color, nunca al revés.
 *
 * `active` → `primary` (activo/en curso) · `confirmed` → `secondary`
 * (calibrado/confirmado) · `error` → `error` (sobrecarga/fallo) ·
 * `inactive` → `textMuted` (inactivo).
 */
export type ProgressTone = 'active' | 'confirmed' | 'error' | 'inactive';

/** Forma del indicador: barra (sets, sesiones) o anillo (progreso del mesociclo). */
export type ProgressVariant = 'bar' | 'ring';

/** Props públicas de `ProgressIndicator`. */
export type ProgressIndicatorProps = {
  /** Progreso actual dentro de `[0, max]`. Se recorta defensivamente. */
  value: number;
  /** Total de la escala. Default 1, de modo que `value` es una fracción `0..1`. */
  max?: number;
  /** Rol semántico de estado. Default `active` (`primary`). */
  tone?: ProgressTone;
  /** Etiqueta textual; obligatoria para no comunicar el estado solo por color. */
  label?: string;
  /** Forma del indicador. Default `bar`. */
  variant?: ProgressVariant;
  /** Lado del anillo en dp (solo `variant="ring"`). Default 64. */
  size?: number;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor. La barra/anillo derivan `<testID>-fill`/`-ring`. */
  testID?: string;
};

/** Clases del relleno de la barra por rol semántico (contrato que consume el build). */
const toneFillClass: Record<ProgressTone, string> = {
  active: 'bg-primary',
  confirmed: 'bg-secondary',
  error: 'bg-error',
  inactive: 'bg-text-muted',
};

/**
 * Variables CSS por rol para la variante anillo. `react-native-svg` colorea por
 * la prop `stroke`, no por `className`, así que el token se resuelve en JS.
 * Cada variable aparece además en `toneFillClass`, de modo que el build la
 * incluye y `useCSSVariable` puede resolverla.
 */
const toneVariable: Record<ProgressTone, string> = {
  active: '--color-primary',
  confirmed: '--color-secondary',
  error: '--color-error',
  inactive: '--color-text-muted',
};

/** Grosor del anillo en dp; espeja la altura de la barra (`h-sm`, 8 dp). */
const RING_STROKE_WIDTH = 8;

type NormalizedProgress = {
  ratio: number;
  now: number;
  max: number;
};

/** Recorta `value`/`max` a un rango válido y devuelve la fracción `0..1`. */
function normalize(value: number, max: number): NormalizedProgress {
  const safeMax = Number.isFinite(max) && max > 0 ? max : 1;
  const safeValue = Number.isFinite(value) ? value : 0;
  const now = Math.min(Math.max(safeValue, 0), safeMax);

  return { ratio: Math.min(Math.max(now / safeMax, 0), 1), now, max: safeMax };
}

type RingProps = {
  ratio: number;
  tone: ProgressTone;
  size: number;
  accessibilityValue: { min: number; max: number; now: number };
  accessibilityLabel?: string;
  testID?: string;
};

/** Anillo de progreso con `react-native-svg`; color resuelto desde tokens. */
function Ring({ ratio, tone, size, accessibilityValue, accessibilityLabel, testID }: RingProps) {
  const strokeWidth = RING_STROKE_WIDTH;
  const radius = Math.max((size - strokeWidth) / 2, 0);
  const circumference = 2 * Math.PI * radius;
  const dashOffset = circumference * (1 - ratio);
  const colors = useCSSVariable([toneVariable[tone], '--color-surface-muted']);
  const toneColor = typeof colors[0] === 'string' ? colors[0] : undefined;
  const trackColor = typeof colors[1] === 'string' ? colors[1] : undefined;

  return (
    <View
      accessible
      accessibilityRole="progressbar"
      accessibilityValue={accessibilityValue}
      accessibilityLabel={accessibilityLabel}
      testID={testID}
    >
      <Svg width={size} height={size}>
        <Circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke={trackColor}
          strokeWidth={strokeWidth}
        />
        <Circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          fill="none"
          stroke={toneColor}
          strokeWidth={strokeWidth}
          strokeDasharray={`${circumference} ${circumference}`}
          strokeDashoffset={dashOffset}
          transform={`rotate(-90 ${size / 2} ${size / 2})`}
        />
      </Svg>
    </View>
  );
}

/**
 * Indicador de progreso del design system (spec 0002, ticket #42).
 *
 * Barra o anillo que muestra `value`/`max` con los tokens de color del DS.
 * Profundidad por capas tonales y bordes, **sin sombras**. El estado se
 * comunica con color **y** etiqueta textual cuando se provee.
 */
export function ProgressIndicator({
  value,
  max = 1,
  tone = 'active',
  label,
  variant = 'bar',
  size = 64,
  className,
  testID,
}: ProgressIndicatorProps) {
  const { ratio, now, max: safeMax } = normalize(value, max);
  const accessibilityValue = { min: 0, max: safeMax, now };

  return (
    <View className={cn('gap-xs', className)} testID={testID}>
      {label ? (
        <Text className="font-mono-medium text-label-code text-text-muted">{label}</Text>
      ) : null}
      {variant === 'ring' ? (
        <Ring
          ratio={ratio}
          tone={tone}
          size={size}
          accessibilityValue={accessibilityValue}
          accessibilityLabel={label}
          testID={testID ? `${testID}-ring` : undefined}
        />
      ) : (
        <View
          accessible
          accessibilityRole="progressbar"
          accessibilityValue={accessibilityValue}
          accessibilityLabel={label}
          className="h-sm w-full overflow-hidden rounded-sm bg-surface-muted"
        >
          <View
            testID={testID ? `${testID}-fill` : undefined}
            className={cn('h-full rounded-sm', toneFillClass[tone])}
            style={{ width: `${ratio * 100}%` }}
          />
        </View>
      )}
    </View>
  );
}
