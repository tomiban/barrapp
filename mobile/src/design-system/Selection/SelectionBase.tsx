import type { ReactNode } from 'react';
import { Pressable, View } from 'react-native';

import { Text } from '../Text';
import { cn } from '../utils/cn';

/**
 * Lado del control de selección (spec 0002): 20×20 dp. Va como clase arbitraria
 * porque la escala de espaciado del design system no tiene un paso de 20 dp.
 */
export const SELECTION_SIZE_CLASS = 'h-[20px] w-[20px]';

/**
 * Props compartidas por `Checkbox` y `Radio` (ticket #38). Ambos son controles
 * **controlados**: el estado vive en el consumidor y se comunica por `onChange`.
 */
export type SelectionBaseProps = {
  /** Estado marcado/activo actual. Componente controlado. */
  checked: boolean;
  /** Se llama al pulsar con el estado contrario; no se llama si está deshabilitado. */
  onChange?: (checked: boolean) => void;
  /** Se llama en cada pulsación válida, además de `onChange`. */
  onPress?: () => void;
  /** Etiqueta textual opcional; el estado nunca se comunica solo por color. */
  label?: string;
  /** Deshabilita la interacción y lo anuncia a los lectores de pantalla. */
  disabled?: boolean;
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor; el control deriva en `<testID>-control`. */
  testID?: string;
};

type SelectionBaseInternalProps = SelectionBaseProps & {
  /** Rol semántico de accesibilidad del control. */
  role: 'checkbox' | 'radio';
  /** Clases de estado del control (borde/relleno según `checked`). */
  controlClassName: string;
  /** Contenido interno del control: la marca o el bloque del radio. */
  children?: ReactNode;
};

/**
 * Chasis compartido de `Checkbox` y `Radio` (ticket #38): caja cuadrada de
 * 20×20 dp con borde activo (1.5 px) y radios 0, más el `Pressable` accesible y
 * la etiqueta opcional. Los componentes concretos sólo aportan el contenido del
 * control y su rol.
 *
 * Sin pills (radios 0) y sin sombras: la profundidad sale de borde y relleno.
 */
export function SelectionBase({
  checked,
  onChange,
  onPress,
  label,
  disabled = false,
  className,
  testID,
  role,
  controlClassName,
  children,
}: SelectionBaseInternalProps) {
  const handlePress = () => {
    if (disabled) {
      return;
    }
    onChange?.(!checked);
    onPress?.();
  };

  return (
    <Pressable
      testID={testID}
      onPress={handlePress}
      disabled={disabled}
      accessibilityRole={role}
      accessibilityState={{ checked, disabled }}
      accessibilityLabel={label}
      className={cn('flex-row items-center gap-sm', disabled && 'opacity-40', className)}
    >
      <View
        testID={testID ? `${testID}-control` : undefined}
        // `border-active` (utility propio de 1.5 px) se concatena fuera de `cn()`:
        // tailwind-merge no lo conoce y lo descartaría al fusionarlo con el color
        // de borde (`border-primary`/`border-border`), que sí es del mismo grupo.
        className={`${cn(
          SELECTION_SIZE_CLASS,
          'items-center justify-center rounded-none',
          controlClassName,
        )} border-active`}
      >
        {children}
      </View>
      {label ? (
        <Text variant="bodyMd" className={cn(disabled ? 'text-text-muted' : 'text-text')}>
          {label}
        </Text>
      ) : null}
    </Pressable>
  );
}
