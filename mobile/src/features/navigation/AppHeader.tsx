import type { ReactNode } from 'react';

import { Header } from '@/design-system/Navigation';

import { ProfileAvatar } from './ProfileAvatar';

/**
 * Props de `AppHeader`: la sección que se anuncia y un slot `leading` opcional
 * (p. ej. el control de volver).
 */
export type AppHeaderProps = {
  /** Nombre de la sección; compone el patrón `BARRAS / <SECCIÓN>`. */
  section: string;
  /** Slot a la izquierda del título (p. ej. un control de volver). */
  leading?: ReactNode;
};

/**
 * Cabecera común de la app (spec 0003, ticket #82): el patrón
 * `BARRAS / <SECCIÓN>` del diseño más el avatar que abre Perfil en el slot
 * `trailing`. Las pantallas la usan en vez de montar el `Header` a mano para
 * que la cabecera sea consistente en toda la app.
 */
export function AppHeader({ section, leading }: AppHeaderProps) {
  return <Header kicker="BARRAS" title={section} leading={leading} trailing={<ProfileAvatar />} />;
}
