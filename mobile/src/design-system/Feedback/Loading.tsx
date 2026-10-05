import { View } from 'react-native';
import ActivityIndicator from 'uniwind/components/ActivityIndicator';

import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

/** Props públicas de `Loading`. */
export type LoadingProps = {
  /** Etiqueta visible y accesible opcional; sin ella se anuncia `Cargando`. */
  label?: string;
  /** Tamaño del indicador. Default `small`. */
  size?: 'small' | 'large';
  /** Clases del contenedor; se combinan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` del contenedor; el indicador deriva `<testID>-indicator`. */
  testID?: string;
};

/**
 * Estado de carga (spec 0002, ticket #47).
 *
 * `ActivityIndicator` coloreado con el token `primary` vía `colorClassName`
 * (`accent-primary` de Uniwind), sobre las capas del design system. El estado
 * se comunica con una etiqueta textual (visible si se provee, siempre
 * accesible): nunca solo por color ni solo por un indicador animado.
 */
export function Loading({ label, size = 'small', className, testID }: LoadingProps) {
  return (
    <View
      accessible
      accessibilityLabel={label ?? 'Cargando'}
      accessibilityRole="progressbar"
      className={cn('items-center justify-center gap-sm p-md', className)}
      testID={testID}
    >
      <ActivityIndicator
        colorClassName="accent-primary"
        size={size}
        testID={testID ? `${testID}-indicator` : undefined}
      />
      {label ? (
        <Text variant="bodySm" className="text-text-muted">
          {label}
        </Text>
      ) : null}
    </View>
  );
}
