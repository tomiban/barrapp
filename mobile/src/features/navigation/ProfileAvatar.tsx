import { router } from 'expo-router';

import { Avatar } from '@/design-system/Navigation';

/**
 * Avatar de la cabecera que abre Perfil (spec 0003, ticket #82). Perfil dejó de
 * ser pestaña: este es su punto de entrada desde el slot `trailing` del
 * `Header`.
 */
export function ProfileAvatar() {
  return (
    <Avatar
      accessibilityLabel="Perfil"
      testID="header-avatar"
      onPress={() => router.push('/profile')}
    />
  );
}
