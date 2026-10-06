import { Text as RNText, type TextProps as RNTextProps, type TextStyle } from 'react-native';

import { cn } from '@/design-system/utils/cn';

/**
 * Escala tipográfica de la spec (ticket #33). Cada variante nombra un estilo de
 * `docs/specs/0002-design-system.md`; el consumidor elige la variante, nunca la
 * fuente ni el tamaño sueltos.
 */
export type TextVariant =
  | 'displayHero'
  | 'headlineMetric'
  | 'headlineLg'
  | 'headlineMd'
  | 'headlineSm'
  | 'bodyLg'
  | 'bodyMd'
  | 'bodySm'
  | 'labelTechnical'
  | 'labelCode';

/**
 * Color base de toda variante: `text` sobre las superficies oscuras del tema
 * dark-only. Sin él, RN aplicaría su negro por defecto y el texto quedaría casi
 * invisible sobre `canvas`/`surface`. Un `className` del consumidor
 * (`text-text-muted`, `text-primary`…) lo sobrescribe vía `cn()`.
 */
const BASE_CLASS = 'text-text';

/**
 * Mapa variante → utilities de `@theme`. Siempre familia + tamaño: en SDK 57
 * cada peso es una familia propia (ver `02-fonts-expo-font.md`), así que el
 * peso lo fija el `font-*`, nunca un `fontWeight`.
 */
const variantClasses: Record<TextVariant, string> = {
  displayHero: 'font-display text-display-hero',
  headlineMetric: 'font-mono-bold text-headline-metric',
  headlineLg: 'font-display text-headline-lg',
  headlineMd: 'font-display-semibold text-headline-md',
  headlineSm: 'font-display-semibold text-headline-sm',
  bodyLg: 'font-body text-body-lg',
  bodyMd: 'font-body text-body-md',
  bodySm: 'font-body text-body-sm',
  labelTechnical: 'font-mono-semibold text-label-technical uppercase',
  labelCode: 'font-mono-medium text-label-code',
};

/**
 * Métricas monolíticas: los dígitos deben ocupar el mismo ancho para que los
 * countdowns no bailen. Uniwind no tiene utilidad para `fontVariant`, así que
 * va como estilo inline (que además gana sobre `className`).
 */
const metricStyle: TextStyle = { fontVariant: ['tabular-nums'] };

export type TextProps = RNTextProps & {
  /** Estilo de la escala tipográfica. Default: `bodyMd`. */
  variant?: TextVariant;
};

/**
 * Texto del design system: envuelve `Text` de React Native y aplica la escala
 * tipográfica de la spec. Acepta y reenvía todas las props de RN `Text`, y su
 * `className` se combina (con resolución de conflictos) con el de la variante.
 */
export function Text({ variant = 'bodyMd', className, style, ...rest }: TextProps) {
  const isMetric = variant === 'headlineMetric';

  return (
    <RNText
      className={cn(BASE_CLASS, variantClasses[variant], className)}
      style={isMetric ? [metricStyle, style] : style}
      {...rest}
    />
  );
}
