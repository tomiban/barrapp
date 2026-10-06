import { useState } from 'react';
import { TextInput, View, type TextInputProps } from 'react-native';

import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

/**
 * Props de `TextField`: las de `TextInput` (`placeholder`, `editable`,
 * `keyboardType`, `secureTextEntry`, `className`…) más el contrato del design
 * system: `label` estática, `value`/`onChangeText` controlados y `error`.
 *
 * `className` se aplica al `TextInput` (hereda las props), no al contenedor.
 */
export type TextFieldProps = TextInputProps & {
  /** Etiqueta estática sobre el campo; se anuncia como `accessibilityLabel`. */
  label: string;
  /** Valor controlado del campo. */
  value: string;
  /** Notifica cada cambio del texto. */
  onChangeText: (text: string) => void;
  /**
   * Mensaje de error. Si viene, el campo pasa a estado `error` (borde
   * `border-error`) y el mensaje se muestra como texto anunciable.
   */
  error?: string;
};

/**
 * Clases comunes del valor: fondo `canvas` (nivel 0), radio técnico, padding
 * de la escala y tipografía monoespaciada del spec. El borde va aparte porque
 * cambia por estado y su ancho no es fusionable (`border` 1 px vs
 * `border-active` 1.5 px).
 */
const BASE_INPUT_CLASSES =
  'rounded-sm bg-canvas px-md py-md font-mono-medium text-label-code text-text';

/**
 * Campo de texto del design system (spec 0002, ticket #36).
 *
 * Fondo `canvas` y borde hairline en reposo; en foco el borde pasa a amarillo
 * `primary` de 1.5 px (`border-active`); si hay `error`, el borde es
 * `border-error` y el mensaje se muestra como texto. Sin sombras: la
 * profundidad es solo borde + capa tonal.
 *
 * Accesibilidad: `accessibilityLabel` es la label, y el error se comunica como
 * texto con `accessibilityRole="alert"` + `accessibilityLiveRegion` (el estado
 * nunca depende solo del color).
 */
export function TextField({
  label,
  value,
  onChangeText,
  error,
  onFocus,
  onBlur,
  className,
  testID,
  ...rest
}: TextFieldProps) {
  const [focused, setFocused] = useState(false);
  const invalid = Boolean(error);
  const showActiveBorder = focused && !invalid;

  // El color del borde pasa por `cn()` para que el `className` del consumidor
  // pueda sobrescribirlo. El ancho va aparte porque `tailwind-merge` no conoce
  // la utility custom `border-active` y la clasifica como color de borde: al
  // fusionarla con `border-primary` la descartaría.
  const colorClass = invalid ? 'border-error' : focused ? 'border-primary' : 'border-border';
  const widthClass = showActiveBorder ? 'border-active' : 'border';

  return (
    <View className="gap-xs">
      <Text variant="labelTechnical" className="text-text-muted">
        {label}
      </Text>
      <TextInput
        {...rest}
        testID={testID}
        value={value}
        onChangeText={onChangeText}
        className={`${cn(BASE_INPUT_CLASSES, colorClass, className)} ${widthClass}`}
        placeholderTextColorClassName="accent-text-muted"
        accessibilityLabel={label}
        onFocus={(event) => {
          setFocused(true);
          onFocus?.(event);
        }}
        onBlur={(event) => {
          setFocused(false);
          onBlur?.(event);
        }}
      />
      {error ? (
        <Text
          variant="bodySm"
          className="text-error"
          accessibilityRole="alert"
          accessibilityLiveRegion="polite"
        >
          {error}
        </Text>
      ) : null}
    </View>
  );
}
