import { Pressable, View, type ViewProps } from 'react-native';

import { baseIcons, Icon } from '@/design-system/Icon';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

/**
 * Props del `Stepper`: un valor numérico controlado con sus cotas y el paso.
 *
 * El consumidor fija `value`/`onChange` (como en el resto del DS); el control
 * sólo calcula el siguiente valor y lo acota a `[min, max]`. `unit` es la
 * unidad visible (p. ej. `KG`, `CM`, `REPS`) y `formatValue` permite fijar los
 * decimales sin cambiar el número que se notifica.
 */
export type StepperProps = Omit<ViewProps, 'children'> & {
  /** Etiqueta visible y nombre accesible del control. */
  label: string;
  /** Valor actual del control (controlado). */
  value: number;
  /** Notifica el siguiente valor al pulsar `−`/`+`. Nunca sale de `[min, max]`. */
  onChange: (value: number) => void;
  /** Cota inferior. Si se omite, no hay mínimo. */
  min?: number;
  /** Cota superior. Si se omite, no hay máximo. */
  max?: number;
  /** Incremento por pulsación. Default `1`. */
  step?: number;
  /** Unidad visible junto al valor (p. ej. `KG`). */
  unit?: string;
  /** Formatea el número para mostrarlo; por defecto, su representación decimal. */
  formatValue?: (value: number) => string;
  /** Mensaje de error; si viene, el control pasa a estado `error`. */
  error?: string;
  /** Deshabilita los dos botones. */
  disabled?: boolean;
  /** Utilities del contenedor; se fusionan con `cn()`. */
  className?: string;
  /** `testID` del contenedor; los botones derivan `-decrease`/`-increase` y el valor `-value`. */
  testID?: string;
};

/** Clases del botón en reposo, pulsado y deshabilitado (sin sombras). */
const BUTTON_CLASS =
  'h-control-secondary w-control-secondary items-center justify-center rounded-base border';

/**
 * `Stepper` del design system (spec 0002/0003, ticket #90).
 *
 * Control numérico de `−`/`+` para los formularios de medidas y máximos, donde
 * teclear en el gimnasio es incómodo. Los botones son cuadrados, con borde
 * activo y sin sombras; se deshabilitan en las cotas y el valor se anuncia con
 * la etiqueta, no sólo con el número. El estado no se comunica sólo por color:
 * cada botón lleva su nombre accesible (`Disminuir <etiqueta>` /
 * `Aumentar <etiqueta>`) y el error va como `alert`.
 */
export function Stepper({
  label,
  value,
  onChange,
  min,
  max,
  step = 1,
  unit,
  formatValue = (current) => String(current),
  error,
  disabled = false,
  className,
  testID,
  ...rest
}: StepperProps) {
  const atMin = min !== undefined && value <= min;
  const atMax = max !== undefined && value >= max;
  const decreaseDisabled = disabled || atMin;
  const increaseDisabled = disabled || atMax;

  return (
    <View className={cn('gap-xs', className)} testID={testID} {...rest}>
      <Text variant="labelTechnical" className="text-text-muted">
        {label}
      </Text>
      <View className="flex-row items-center gap-sm">
        <Pressable
          accessible
          accessibilityRole="button"
          accessibilityLabel={`Disminuir ${label}`}
          accessibilityState={{ disabled: decreaseDisabled }}
          disabled={decreaseDisabled}
          onPress={() => onChange(value - step)}
          className={cn(
            BUTTON_CLASS,
            decreaseDisabled
              ? 'border-border bg-surface text-text-muted'
              : 'border-primary bg-surface text-primary',
          )}
          testID={testID ? `${testID}-decrease` : undefined}
        >
          <Icon icon={baseIcons.minus} size={20} />
        </Pressable>

        <View className="min-w-control-secondary flex-1 flex-row items-baseline justify-center gap-xs">
          <Text
            variant="headlineMd"
            className={error ? 'text-error' : 'text-text'}
            accessibilityLabel={`${label}: ${formatValue(value)}${unit ? ` ${unit}` : ''}`}
            testID={testID ? `${testID}-value` : undefined}
          >
            {formatValue(value)}
          </Text>
          {unit ? (
            <Text variant="labelTechnical" className="text-text-muted">
              {unit}
            </Text>
          ) : null}
        </View>

        <Pressable
          accessible
          accessibilityRole="button"
          accessibilityLabel={`Aumentar ${label}`}
          accessibilityState={{ disabled: increaseDisabled }}
          disabled={increaseDisabled}
          onPress={() => onChange(value + step)}
          className={cn(
            BUTTON_CLASS,
            increaseDisabled
              ? 'border-border bg-surface text-text-muted'
              : 'border-primary bg-surface text-primary',
          )}
          testID={testID ? `${testID}-increase` : undefined}
        >
          <Icon icon={baseIcons.plus} size={20} />
        </Pressable>
      </View>
      {error ? (
        <Text
          variant="bodySm"
          className="text-error"
          accessibilityRole="alert"
          testID={testID ? `${testID}-error` : undefined}
        >
          {error}
        </Text>
      ) : null}
    </View>
  );
}
