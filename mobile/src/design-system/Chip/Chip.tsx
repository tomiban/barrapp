import { Pressable, type PressableProps } from 'react-native';

import { Text } from '../Text';
import { cn } from '../utils/cn';

/**
 * Props del `Chip`: las de `Pressable` (sin `children`, el label es explícito)
 * más el estado controlado del toggle y su callback.
 */
export type ChipProps = Omit<PressableProps, 'children'> & {
  /** Etiqueta visible y nombre accesible del chip. */
  label: string;
  /** Estado controlado: `true` = seleccionado. Default `false`. */
  selected?: boolean;
  /** Cambio de estado; recibe el nuevo `selected` para que el consumidor lo fije. */
  onChange?: (selected: boolean) => void;
  /** Utilities extra del contenedor; se fusionan con `cn()`. */
  className?: string;
};

/**
 * Clases del contenedor por estado. Inactivo apoya en la capa tonal
 * `surface-muted` (nivel 2); activo usa la inversión `primary`/`on-primary`
 * reservada al estado activo (spec 0002). Sin sombras.
 */
const containerStateClass = {
  inactive: 'bg-surface-muted border-border',
  active: 'bg-primary border-primary',
} as const;

/** Color del texto por estado: la etiqueta es visible, el estado no va solo por color. */
const labelStateClass = {
  inactive: 'text-text-muted',
  active: 'text-on-primary',
} as const;

/**
 * Chip del design system (spec 0002, ticket #37).
 *
 * Toggle de etiqueta visible y estado controlado (`selected`/`onChange`).
 * Inactivo `surface-muted`/`text-muted`, activo `primary`/`on-primary`; radios
 * de 2–4 dp (sin pills) y profundidad por borde, nunca por sombra. El estado se
 * anuncia con `accessibilityState.selected`, además del color y la etiqueta.
 */
export function Chip({
  label,
  selected = false,
  onChange,
  className,
  disabled,
  ...rest
}: ChipProps) {
  return (
    <Pressable
      accessible
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityState={{ selected, disabled: Boolean(disabled) }}
      disabled={disabled}
      onPress={() => onChange?.(!selected)}
      className={cn(
        'flex-row items-center justify-center rounded-base border px-md py-sm',
        selected ? containerStateClass.active : containerStateClass.inactive,
        className,
      )}
      {...rest}
    >
      <Text
        variant="labelCode"
        className={selected ? labelStateClass.active : labelStateClass.inactive}
      >
        {label}
      </Text>
    </Pressable>
  );
}
