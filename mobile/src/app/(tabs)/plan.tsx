import { Header, Screen } from '@/design-system/Navigation';
import { Text } from '@/design-system/Text';

/**
 * Pantalla de planificación. De momento es un marcador: aquí vivirá el
 * mesociclo del atleta. Sirve para comprobar la integración de `Screen`/
 * `Header` con el router y la `TabBar`.
 */
export default function PlanScreen() {
  return (
    <Screen testID="plan-screen" header={<Header title="Plan" />}>
      <Text variant="bodyMd" className="text-text-muted">
        Aquí vivirá la planificación mensual del atleta.
      </Text>
    </Screen>
  );
}
