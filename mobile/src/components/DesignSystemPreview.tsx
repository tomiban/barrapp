import { Text, View } from 'react-native';

/**
 * Muestra de verificación del design system (ticket #31).
 *
 * No es un componente del producto: existe para comprobar en Expo Go que
 * Uniwind compila `className` contra los tokens de `@theme`. Sin colores,
 * espaciado ni radios sueltos.
 */
export function DesignSystemPreview() {
  return (
    <View className="gap-md rounded-md bg-canvas p-md">
      <View className="gap-sm rounded-md border border-border bg-surface p-md">
        <Text className="text-text-muted">Barrapp · design system</Text>
        <Text className="text-text">Nivel 1 · celdas y paneles</Text>
      </View>

      <View className="rounded-md bg-surface-muted p-md">
        <Text className="text-text-muted">Nivel 2 · inputs y chips inactivos</Text>
      </View>

      <View className="flex-row gap-sm">
        <View className="rounded-base bg-primary px-md py-sm">
          <Text className="text-on-primary">Acción primaria</Text>
        </View>
        <View className="rounded-base bg-secondary px-md py-sm">
          <Text className="text-on-secondary">Referencia</Text>
        </View>
      </View>
    </View>
  );
}
