import type { ReactNode } from 'react';
import { ScrollView, View, type ViewProps } from 'react-native';
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
   * Si `true`, el contenido va dentro de un `ScrollView` (pantallas más largas
   * que la ventana). Por defecto el contenido es un `View` fijo; las pantallas
   * cortas no pagan el coste del scroll.
   */
  scrollable?: boolean;
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
  scrollable = false,
  edges = DEFAULT_EDGES,
  testID,
  ...rest
}: ScreenProps) {
  const contentTestID = testID ? `${testID}-content` : undefined;

  return (
    <SafeAreaView
      edges={edges}
      className={cn('flex-1 bg-canvas', className)}
      testID={testID}
      {...rest}
    >
      {header}
      {scrollable ? (
        <ScrollView
          className="flex-1"
          contentContainerClassName={cn('px-margin py-md', contentClassName)}
          testID={contentTestID}
        >
          {children}
        </ScrollView>
      ) : (
        <View className={cn('flex-1 px-margin py-md', contentClassName)} testID={contentTestID}>
          {children}
        </View>
      )}
    </SafeAreaView>
  );
}
