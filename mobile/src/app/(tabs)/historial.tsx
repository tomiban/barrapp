import { EmptyState } from '@/design-system/Feedback';
import { Screen } from '@/design-system/Navigation';
import { AppHeader } from '@/features/navigation';

/**
 * Pantalla **Historial** (spec 0003, ticket #82): placeholder del shell.
 *
 * La lista de sesiones por fecha con filtros (Todas · Mesociclo · Suelta) se
 * construye en su ticket (#87); aquí solo se reserva la ruta.
 */
export default function HistoryScreen() {
  return (
    <Screen testID="history-screen" header={<AppHeader section="Historial" />}>
      <EmptyState
        title="Historial"
        description="Aquí vivirá el registro de sesiones, agrupado por semana."
        testID="history-placeholder"
      />
    </Screen>
  );
}
