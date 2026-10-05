import { Text, View } from 'react-native';

/**
 * Muestra de verificación del design system (tickets #31 y #32).
 *
 * No es un componente del producto: existe para comprobar en Expo Go que
 * Uniwind compila `className` contra los tokens de `@theme`. Consume color,
 * espaciado, radios, bordes y escala tipográfica; ningún valor suelto.
 */
export function DesignSystemPreview() {
  return (
    <View
      className="gap-md rounded-lg border border-border bg-canvas p-margin"
      testID="design-system-preview"
    >
      <View className="gap-sm rounded-md border border-border bg-surface p-md">
        <Text className="font-mono-semibold text-label-technical text-text-muted">
          BARRAPP · DESIGN SYSTEM
        </Text>
        <Text className="font-display text-headline-md text-text">Nivel 1 · celdas y paneles</Text>
      </View>

      <View className="rounded-sm bg-surface-muted p-md">
        <Text className="font-body text-body-md text-text-muted">
          Nivel 2 · inputs y chips inactivos
        </Text>
      </View>

      <View className="flex-row gap-sm">
        <View
          className="rounded-base border-active border-primary bg-primary px-md py-sm"
          testID="primary-action"
        >
          <Text className="font-display-semibold text-body-md text-on-primary">
            Acción primaria
          </Text>
        </View>
        <View className="rounded-base bg-secondary px-md py-sm" testID="secondary-action">
          <Text className="font-body text-body-md text-on-secondary">Referencia</Text>
        </View>
      </View>

      <View className="gap-xs">
        <Text className="font-mono-bold text-headline-metric text-primary">60</Text>
        <Text className="font-mono-medium text-label-code text-text-muted">label code · 01:30</Text>
      </View>
    </View>
  );
}
