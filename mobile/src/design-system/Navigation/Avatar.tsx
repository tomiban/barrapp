import { Pressable, type PressableProps } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { Icon, UserRound } from '../Icon';
import { cn } from '../utils/cn';

/**
 * Props de `Avatar`: las de un `Pressable` (sin `children`) más la etiqueta
 * accesible.
 */
export type AvatarProps = Omit<PressableProps, 'children'> & {
  /** Nombre accesible del control. Default: «Perfil». */
  accessibilityLabel?: string;
  /** Utilities del botón; se fusionan con `cn()` (gana la última). */
  className?: string;
};

/**
 * Avatar del design system (spec 0003): control cuadrado con esquinas suaves que
 * representa al atleta en la cabecera. Es presentacional —no navega—; quien lo
 * consume decide la acción (el `ProfileAvatar` de la app lo enlaza a Perfil).
 *
 * Tamaño táctil secundario (48 dp), fondo `primary` y glifo `on-primary`, sin
 * sombras. Sin foto de perfil todavía: muestra el icono de persona.
 */
export function Avatar({ accessibilityLabel = 'Perfil', className, ...rest }: AvatarProps) {
  const onPrimary = useCSSVariable('--color-on-primary');

  return (
    <Pressable
      {...rest}
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel}
      className={cn(
        'h-control-secondary w-control-secondary items-center justify-center rounded-lg bg-primary',
        className,
      )}
    >
      <Icon
        icon={UserRound}
        size={22}
        color={typeof onPrimary === 'string' ? onPrimary : undefined}
      />
    </Pressable>
  );
}
