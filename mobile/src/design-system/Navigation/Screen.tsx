import type { ReactNode } from 'react';
import { View, type ViewProps } from 'react-native';
import type { Edge } from 'react-native-safe-area-context';

import { cn } from '../utils/cn';
import { SafeAreaView } from './SafeAreaView';

/**
 * Props de `Screen`: las de un `View` más el encabezado y el área de contenido.
 */
export type ScreenProps = ViewProps & {
  /** Encabezado opcional; normalmente un `<Header />` de este mismo directorio. */
  header?: ReactNode;
  /**
   * Clases del área de contenido. Se fusionan tras `flex-1 px-margin py-md`,
   * así que permiten quitar el margen (p. ej. `px-0`) para contenido a sangre.
   */
  contentClassName?: string;
  /**
   * Bordes seguros a respetar. Por defecto `top`/`left`/`right`: el borde
   * inferior lo gestiona la `TabBar` (que envuelve su franja en
   * `SafeAreaView edges={['bottom']}`), de modo que el contenido no deja un
   * hueco extra sobre la barra.
   */
  edges?: readonly Edge[];
};

/** Bordes por defecto: el inferior pertenece a la barra de pestañas. */
const DEFAULT_EDGES: readonly Edge[] = ['top', 'left', 'right'];

/**
 * Pantalla base del design system (spec 0002): `SafeAreaView` con fondo
 * `canvas`, un `Header` opcional y un área de contenido con el margen de página
 * (`margin`, 20 dp) del spec.
 *
 * El contenido no lleva fondo propio: la profundidad sale de bordes y capas
 * tonales (`canvas` → `surface` → `surface-muted`), nunca de sombras.
 */
export function Screen({
  header,
  children,
  className,
  contentClassName,
  edges = DEFAULT_EDGES,
  testID,
  ...rest
}: ScreenProps) {
  return (
    <SafeAreaView
      edges={edges}
      className={cn('flex-1 bg-canvas', className)}
      testID={testID}
      {...rest}
    >
      {header}
      <View
        className={cn('flex-1 px-margin py-md', contentClassName)}
        testID={testID ? `${testID}-content` : undefined}
      >
        {children}
      </View>
    </SafeAreaView>
  );
}
