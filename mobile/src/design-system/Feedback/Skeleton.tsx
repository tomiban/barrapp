import { useEffect, useState } from 'react';
import { Animated, View, type StyleProp, type ViewStyle } from 'react-native';

import { cn } from '@/design-system/utils/cn';

/** Props públicas de `Skeleton`. */
export type SkeletonProps = {
  /**
   * Pulso suave de opacidad mientras carga. Default `false`: el bloque estático
   * es suficiente y el movimiento no es decorativo.
   */
  animated?: boolean;
  /** Clases del bloque; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** Estilo extra del bloque (tiene prioridad sobre `className`). */
  style?: StyleProp<ViewStyle>;
  /** `testID` del bloque. */
  testID?: string;
};

/** Duración de cada mitad del pulso, en ms. */
const PULSE_DURATION = 600;

/**
 * Placeholder de carga (spec 0002, ticket #47).
 *
 * Bloque tonal (`surface-muted`) con radio de la escala. Es puramente
 * decorativo: queda oculto para los lectores de pantalla (el estado de carga se
 * comunica con `Loading` o con el contenido que reemplaza). La animación es
 * opcional, corta y sin rebotes; por defecto el bloque es estático.
 */
export function Skeleton({ animated = false, className, style, testID }: SkeletonProps) {
  const [opacity] = useState(() => new Animated.Value(1));

  useEffect(() => {
    if (!animated) {
      return;
    }

    const animation = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, {
          toValue: 0.4,
          duration: PULSE_DURATION,
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 1,
          duration: PULSE_DURATION,
          useNativeDriver: true,
        }),
      ]),
    );

    animation.start();

    return () => animation.stop();
  }, [animated, opacity]);

  const block = (
    <View
      accessibilityElementsHidden
      accessible={false}
      importantForAccessibility="no-hide-descendants"
      className={cn('h-md w-full rounded-base bg-surface-muted', className)}
      style={style}
      testID={testID}
    />
  );

  if (!animated) {
    return block;
  }

  return <Animated.View style={{ opacity }}>{block}</Animated.View>;
}
