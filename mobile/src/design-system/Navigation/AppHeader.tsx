import { router } from 'expo-router';
import { Pressable, type PressableProps } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { ArrowLeft, Icon, User } from '../Icon';
import { Header, type HeaderProps } from './Header';

/**
 * Props de `AppHeader`: las del `Header` (sin el título, que fija la marca)
 * más la sección y los controles propios de la app.
 */
export type AppHeaderProps = Omit<HeaderProps, 'title' | 'section'> & {
  /** Sección visible bajo la marca, p. ej. `Entreno`. */
  section: string;
  /** Muestra el control de volver a la izquierda (pantallas apiladas, p. ej. Perfil). */
  showBack?: boolean;
  /** Muestra el avatar que abre Perfil. Por defecto, `true`. */
  showAvatar?: boolean;
  /** Sustituye la navegación por defecto del avatar (`/profile`). */
  onAvatarPress?: () => void;
};

/** Botón de volver del patrón de cabecera: `ArrowLeft` con etiqueta accesible. */
function BackButton() {
  return (
    <Pressable
      onPress={() => router.back()}
      accessibilityRole="button"
      accessibilityLabel="Volver"
      className="items-center justify-center p-xs"
    >
      <Icon icon={ArrowLeft} size={24} />
    </Pressable>
  );
}

/** Botón del avatar (glifo `User` sobre baldosa `primary`) que abre Perfil. */
function AvatarButton({ onPress }: Pick<PressableProps, 'onPress'>) {
  const onPrimary = useCSSVariable('--color-on-primary');

  return (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel="Perfil"
      className="items-center justify-center rounded-base bg-primary p-sm"
    >
      <Icon icon={User} size={20} color={typeof onPrimary === 'string' ? onPrimary : undefined} />
    </Pressable>
  );
}

/**
 * Cabecera de la app (spec 0003, ticket #82): el patrón del mockup
 * `BARRAS / <SECCIÓN>` con el avatar de **Perfil** a la derecha. Perfil dejó
 * de ser pestaña: desde aquí se abre como pantalla apilada (deja el `showBack`
 * para volver).
 *
 * ```tsx
 * <AppHeader section="Entreno" />
 * ```
 */
export function AppHeader({
  section,
  showBack = false,
  showAvatar = true,
  onAvatarPress,
  leading,
  trailing,
  ...rest
}: AppHeaderProps) {
  return (
    <Header
      title="BARRAS"
      section={section}
      leading={showBack ? <BackButton /> : leading}
      trailing={
        <>
          {trailing}
          {showAvatar ? (
            <AvatarButton onPress={onAvatarPress ?? (() => router.push('/profile'))} />
          ) : null}
        </>
      }
      {...rest}
    />
  );
}
