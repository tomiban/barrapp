import type { ReactNode } from 'react';
import { View, type ViewProps } from 'react-native';

import { Text } from '../Text';
import { cn } from '../utils/cn';

/**
 * Props de `Header`: las de un `View` más el título, la sección opcional y dos
 * slots.
 *
 * `leading` y `trailing` son huecos para controles (volver, avatar, acciones);
 * el `Header` no decide su contenido, sólo los coloca a los lados del título.
 * `section` añade la segunda línea del patrón `BARRAS / <SECCIÓN>` del
 * mockup: el lector de pantalla anuncia marca y sección juntas.
 */
export type HeaderProps = Omit<ViewProps, 'children'> & {
  /** Título del encabezado, en la escala `headlineSm` del design system. */
  title: string;
  /** Sección bajo el título (patrón `BARRAS / <SECCIÓN>`); opcional. */
  section?: string;
  /** Slot a la izquierda del título (p. ej. un control de volver). */
  leading?: ReactNode;
  /** Slot a la derecha del título (p. ej. una acción). */
  trailing?: ReactNode;
  /** Utilities del contenedor; se fusionan con `cn()` (gana la última). */
  className?: string;
};

/**
 * Encabezado de pantalla del design system (spec 0002).
 *
 * Fondo del contenedor (lo pone el `Screen`/padre), título `headlineSm` y
 * hairline inferior de 1 px en `border`. **Sin sombras**: la profundidad sale
 * del borde. Con `section` dibuja el patrón del mockup (`BARRAS` + sección) y
 * pone el rol de cabecera en el conjunto, para que el lector de pantalla
 * anuncie la sección completa.
 */
export function Header({ title, section, leading, trailing, className, ...rest }: HeaderProps) {
  return (
    <View
      className={cn(
        'flex-row items-center gap-sm border-b border-border px-margin py-md',
        className,
      )}
      {...rest}
    >
      {leading}
      {section === undefined ? (
        <Text
          accessibilityRole="header"
          numberOfLines={1}
          variant="headlineSm"
          className="flex-1 text-text"
        >
          {title}
        </Text>
      ) : (
        <View
          accessible
          accessibilityRole="header"
          accessibilityLabel={`${title} / ${section}`}
          className="flex-1"
        >
          <Text numberOfLines={1} variant="headlineSm" className="text-text">
            {title}
          </Text>
          <Text numberOfLines={1} variant="labelTechnical" className="text-text-muted">
            {section}
          </Text>
        </View>
      )}
      {trailing}
    </View>
  );
}
