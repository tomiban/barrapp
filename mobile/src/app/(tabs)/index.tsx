import { EmptyState } from '@/design-system/Feedback';
import { Screen } from '@/design-system/Navigation';
import { AppHeader } from '@/features/navigation';

/**
 * Pantalla **Inicio** (spec 0003, ticket #82): placeholder del shell.
 *
 * El dashboard del mesociclo (semana activa, criterio de etapa, sesión de hoy)
 * se construye en su ticket (#83); aquí solo se reserva la ruta para que la
 * pestaña navegue.
 */
export default function HomeScreen() {
  return (
    <Screen testID="home-screen" header={<AppHeader section="Inicio" />}>
      <EmptyState
        title="Inicio"
        description="Aquí vivirá el resumen del mesociclo y la sesión de hoy."
        testID="home-placeholder"
      />
    </Screen>
  );
}
