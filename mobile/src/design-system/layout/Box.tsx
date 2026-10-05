import { View, type ViewProps } from 'react-native';

import { cn } from '../utils/cn';

/**
 * Props de `Box`: las de un `View` de React Native, con `className` del
 * design system (Uniwind) y un `className` de consumidor que se fusiona con
 * `cn()`.
 */
export type BoxProps = ViewProps & {
  /** Utilities de Uniwind. Se fusionan resolviendo conflictos (gana la última). */
  className?: string;
};

/**
 * Primitiva base de layout: un `View` que sólo añade la fusión de `className`
 * con `cn()`. El resto de primitivas (`Stack`, `Spacer`, `Grid`, `GridItem`)
 * se construyen sobre ella.
 *
 * No fija color, espaciado ni tamaño: cada consumidor pasa sus utilities desde
 * los tokens del design system. Sirve como punto único donde se resuelven
 * conflictos de clases (Uniwind no deduplica `className`).
 */
export function Box({ className, ...rest }: BoxProps) {
  return <View className={cn(className)} {...rest} />;
}
