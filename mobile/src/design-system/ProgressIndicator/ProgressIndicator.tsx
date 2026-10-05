import { View } from 'react-native';
import Svg, { Circle } from 'react-native-svg';
import { useCSSVariable } from 'uniwind';

import { ROLE_BG_CLASS, ROLE_COLOR_VARIABLE, type SemanticRole } from '@/design-system/semantic';
import { Text } from '../Text';
import { cn } from '../utils/cn';

/** Forma del indicador: barra (sets, sesiones) o anillo (progreso del mesociclo). */
export type ProgressVariant = 'bar' | 'ring';

/** Props públicas de `ProgressIndicator`. */
export type ProgressIndicatorProps = {
  /** Progreso actual dentro de `[0, max]`. Se recorta defensivamente. */
  value: number;
  /** Total de la escala. Default 1, de modo que `value` es una fracción `0..1`. */
  max?: number;
  /** Rol semántico de estado. Default `active` (`primary`). */
  role?: SemanticRole;
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

/**
 * Grosor del anillo en dp cuando el token `--spacing-sm` no se puede resolver
 * (p. ej. en Jest, sin Metro). En runtime el grosor se lee del token, que es la
 * misma altura que la barra (`h-sm`); este valor sólo es la red de seguridad.
 */
const RING_STROKE_FALLBACK = 8;

/** Lee `--spacing-sm` (dp) para que el anillo comparta el grosor de la barra. */
function useRingStrokeWidth(): number {
  const token = useCSSVariable('--spacing-sm');

  if (typeof token === 'number' && Number.isFinite(token)) {
    return token;
  }
  if (typeof token === 'string') {
    const parsed = Number.parseFloat(token);
    if (Number.isFinite(parsed)) {
      return parsed;
    }
  }

  return RING_STROKE_FALLBACK;
}

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
  role: SemanticRole;
  size: number;
  accessibilityValue: { min: number; max: number; now: number };
  accessibilityLabel?: string;
  testID?: string;
};

/** Anillo de progreso con `react-native-svg`; color resuelto desde tokens. */
function Ring({ ratio, role, size, accessibilityValue, accessibilityLabel, testID }: RingProps) {
  const strokeWidth = useRingStrokeWidth();
  const radius = Math.max((size - strokeWidth) / 2, 0);
  const circumference = 2 * Math.PI * radius;
  const dashOffset = circumference * (1 - ratio);
  const colors = useCSSVariable([ROLE_COLOR_VARIABLE[role], '--color-surface-muted']);
  const roleColor = typeof colors[0] === 'string' ? colors[0] : undefined;
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
          stroke={roleColor}
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
 * comunica con uno de los cuatro roles semánticos de `semantic.ts` y **etiqueta
 * textual** cuando se provee; nunca solo por color.
 */
export function ProgressIndicator({
  value,
  max = 1,
  role = 'active',
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
          role={role}
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
            className={cn('h-full rounded-sm', ROLE_BG_CLASS[role])}
            style={{ width: `${ratio * 100}%` }}
          />
        </View>
      )}
    </View>
  );
}
