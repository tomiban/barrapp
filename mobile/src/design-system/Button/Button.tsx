import type { ReactNode } from 'react';
import { Pressable, View, type PressableProps } from 'react-native';

import { Text } from '../Text';
import { cn } from '../utils/cn';

/**
 * Variante visual del botón (spec 0002). Fija color, altura y borde:
 * - `primary`   → acción primaria (`primary` / `on-primary`), 56 dp.
 * - `secondary` → referencia o confirmación (`secondary` / `on-secondary`), 48 dp.
 * - `tertiary`  → acción de menor jerarquía sobre `surface` (`text` / `border`), 48 dp.
 */
export type ButtonVariant = 'primary' | 'secondary' | 'tertiary';

/**
 * Props de `Button`: las de `Pressable` de React Native (incluido `onPress`,
 * `disabled`, `testID`…) más la variante y un `className` que se fusiona con
 * `cn()` para que el consumidor pueda sobrescribir.
 */
export type ButtonProps = PressableProps & {
  /** Variante visual. Default: `primary`. */
  variant?: ButtonVariant;
  /** Utilities de Uniwind; se fusionan resolviendo conflictos (gana la última). */
  className?: string;
  /** Etiqueta del botón. */
  children?: ReactNode;
};

/*
 * Clases completas y estáticas: Uniwind/Tailwind escanea el código en build
 * time, así que nunca se interpolan nombres de clase.
 */
const BASE = 'flex-row items-center justify-center gap-sm rounded-sm px-lg';

/** Tamaños táctiles de la spec (dp): primary 56, secondary/tertiary 48. */
const HEIGHT: Record<ButtonVariant, string> = {
  primary: 'h-control-primary',
  secondary: 'h-control-secondary',
  tertiary: 'h-control-secondary',
};

/** Relleno en reposo. */
const SURFACE: Record<ButtonVariant, string> = {
  primary: 'bg-primary',
  secondary: 'bg-secondary',
  tertiary: 'bg-surface',
};

/** Relleno pulsado: inversión del fondo. */
const PRESSED_SURFACE: Record<ButtonVariant, string> = {
  primary: 'bg-on-primary',
  secondary: 'bg-on-secondary',
  tertiary: 'bg-text',
};

/** Color de la etiqueta en reposo. */
const LABEL: Record<ButtonVariant, string> = {
  primary: 'text-on-primary',
  secondary: 'text-on-secondary',
  tertiary: 'text-text',
};

/** Color de la etiqueta pulsada: inversión del texto. */
const PRESSED_LABEL: Record<ButtonVariant, string> = {
  primary: 'text-primary',
  secondary: 'text-secondary',
  tertiary: 'text-surface',
};

/**
 * Borde de 1.5 dp (`border-active`) en las tres variantes (spec #35), con el
 * color de cada una: `primary` en primario, `secondary` en secundario y
 * `border` en terciario.
 *
 * Se concatena **fuera** de `cn()`: `tailwind-merge` clasifica el utility propio
 * `border-active` como color de borde y lo descartaría al fusionarlo con un
 * `border-<color>`. Fuera de `cn()` sobreviven ancho y color.
 */
const BORDER: Record<ButtonVariant, string> = {
  primary: 'border-active border-primary',
  secondary: 'border-active border-secondary',
  tertiary: 'border-active border-border',
};

/** Estado inactivo (spec: `textMuted`); sin inversión al pulsar. */
const DISABLED_SURFACE = 'bg-surface border-active border-border';
const DISABLED_LABEL = 'text-text-muted';

/**
 * Botón del design system (spec 0002, ticket #35).
 *
 * Profundidad por capas tonales y bordes, **sin sombras**, radios de 2 dp y
 * alturas de 56/48 dp. Al pulsar invierte fondo y texto (estado `pressed` de
 * `Pressable`), salvo cuando está `disabled`.
 */
export function Button({
  variant = 'primary',
  disabled = false,
  onPress,
  children,
  className,
  testID,
  ...rest
}: ButtonProps) {
  return (
    <Pressable
      {...rest}
      accessibilityRole="button"
      accessibilityState={{ disabled: Boolean(disabled) }}
      disabled={disabled}
      onPress={onPress}
      testID={testID}
    >
      {({ pressed }) => {
        const isPressed = pressed && !disabled;
        const surface = disabled
          ? DISABLED_SURFACE
          : isPressed
            ? PRESSED_SURFACE[variant]
            : SURFACE[variant];
        const label = disabled
          ? DISABLED_LABEL
          : isPressed
            ? PRESSED_LABEL[variant]
            : LABEL[variant];
        const border = disabled ? '' : BORDER[variant];

        return (
          <View
            testID={testID ? `${testID}-surface` : undefined}
            className={`${cn(BASE, HEIGHT[variant], surface, className)} ${border}`.trim()}
          >
            <Text variant="labelTechnical" className={label}>
              {children}
            </Text>
          </View>
        );
      }}
    </Pressable>
  );
}
