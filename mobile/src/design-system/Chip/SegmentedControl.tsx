import { Pressable, View, type ViewProps } from 'react-native';

import { ROLE_TEXT_CLASS } from '../semantic';
import { Text } from '../Text';
import { cn } from '../utils/cn';

/** Opción del segmentado: `value` es la clave estable y `label` el texto visible. */
export type SegmentedOption<T extends string = string> = {
  value: T;
  label: string;
};

/**
 * Props de `SegmentedControl`: opciones tipadas, valor controlado y callback.
 * `className` se fusiona con `cn()` en el contenedor.
 */
export type SegmentedControlProps<T extends string = string> = ViewProps & {
  /** Opciones visibles, en orden. */
  options: readonly SegmentedOption<T>[];
  /** Valor seleccionado (controlado). */
  value: T;
  /** Cambio de selección; recibe el `value` de la opción pulsada. */
  onChange: (value: T) => void;
  /** Deshabilita todo el control. */
  disabled?: boolean;
  /** Etiqueta accesible del grupo (opcional pero recomendada). */
  label?: string;
  /**
   * Mensaje de error. Si viene, se muestra bajo el control como texto
   * anunciable, con el mismo token (`text-error`) y vocabulario que `TextField`.
   */
  error?: string;
  /** Utilities extra del contenedor; se fusionan con `cn()`. */
  className?: string;
};

/**
 * Clases del segmento por estado. El carril es `surface-muted`; el segmento
 * activo se invierte a `primary`/`on-primary`, el inactivo se queda en la capa
 * tonal con texto `text-muted`. Sin sombras.
 */
const segmentStateClass = {
  inactive: 'bg-surface-muted',
  active: 'bg-primary',
} as const;

const segmentLabelClass = {
  inactive: 'text-text-muted',
  active: 'text-on-primary',
} as const;

/**
 * Control segmentado tipo rocker switch (spec 0002, ticket #37).
 *
 * Selección **única** sobre `options` con `value`/`onChange` controlados: el
 * segmento activo se invierte a `primary`/`on-primary` y los demás quedan en
 * `surface-muted`/`text-muted`. Semántica accesible de grupo de radios
 * (`radiogroup` + `radio` con `accessibilityState.selected`), de modo que el
 * estado nunca se comunica solo por color.
 *
 * Si llega `error`, se muestra bajo el control con el token `error` del
 * design system, como texto anunciable (`accessibilityRole="alert"`), igual
 * que `TextField`.
 */
export function SegmentedControl<T extends string = string>({
  options,
  value,
  onChange,
  disabled,
  label,
  error,
  className,
  testID,
  ...rest
}: SegmentedControlProps<T>) {
  return (
    <View className="gap-xs">
      <View
        accessibilityRole="radiogroup"
        accessibilityLabel={label}
        className={cn(
          'flex-row items-stretch rounded-base border border-border bg-surface-muted p-xs',
          className,
        )}
        testID={testID}
        {...rest}
      >
        {options.map((option) => {
          const selected = option.value === value;

          return (
            <Pressable
              key={option.value}
              accessible
              accessibilityRole="radio"
              accessibilityLabel={option.label}
              accessibilityState={{ selected, disabled: Boolean(disabled) }}
              disabled={disabled}
              onPress={() => onChange(option.value)}
              testID={testID ? `${testID}-${option.value}` : undefined}
              className={cn(
                'flex-1 flex-row items-center justify-center rounded-sm px-md py-sm',
                selected ? segmentStateClass.active : segmentStateClass.inactive,
              )}
            >
              <Text
                variant="labelCode"
                className={selected ? segmentLabelClass.active : segmentLabelClass.inactive}
              >
                {option.label}
              </Text>
            </Pressable>
          );
        })}
      </View>
      {error ? (
        <Text
          variant="bodySm"
          className={ROLE_TEXT_CLASS.error}
          accessibilityRole="alert"
          accessibilityLiveRegion="polite"
        >
          {error}
        </Text>
      ) : null}
    </View>
  );
}
