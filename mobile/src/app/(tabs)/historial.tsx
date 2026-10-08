import { MesocycleHistory } from '@/features/history/MesocycleHistory';
import { SoloSessionHistory } from '@/features/suelta/SoloSessionHistory';
import { SectionHeader } from '@/design-system/ListRow';
import { Stack } from '@/design-system/layout';
import { AppHeader, Screen } from '@/design-system/Navigation';

/**
 * Pantalla Historial (#82): cabecera `BARRAS / HISTORIAL` y las listas que ya
 * existen —mesociclos pasados y sesiones sueltas— como base del rediseño
 * completo (filtros por fecha y por tipo, edición y borrado de registros) del
 * ticket #87.
 */
export default function HistoryScreen() {
  return (
    <Screen testID="history-screen" header={<AppHeader section="Historial" />} scrollable>
      <Stack gap="sm">
        <SectionHeader label="Mesociclos" testID="history-mesocycles-header" />
        <MesocycleHistory />

        <SectionHeader label="Sesiones sueltas" testID="history-solo-header" />
        <SoloSessionHistory />
      </Stack>
    </Screen>
  );
}
