import { router } from 'expo-router';

import { ChevronRight, Icon } from '@/design-system/Icon';
import { ListRow } from '@/design-system/ListRow';
import { Stack } from '@/design-system/layout';
import { AppHeader, Screen } from '@/design-system/Navigation';
import { Text } from '@/design-system/Text';

/**
 * Pantalla Inicio (#82): cabecera `BARRAS / INICIO` y accesos rápidos al
 * resto de pestañas. El dashboard del mesociclo (semana, adherencia, objetivo
 * y sesión de hoy) llega en #83; aquí sólo se cubre el chasis de navegación.
 */
export default function HomeScreen() {
  return (
    <Screen testID="home-screen" header={<AppHeader section="Inicio" />} scrollable>
      <Stack gap="sm">
        <Text variant="labelTechnical" className="text-text-muted">
          Accesos rápidos
        </Text>

        <ListRow
          title="Plan del mesociclo"
          subtitle="Semanas, volumen y sesiones"
          onPress={() => router.push('/plan')}
          trailing={<Icon icon={ChevronRight} size={20} />}
          testID="home-shortcut-plan"
        />
        <ListRow
          title="Empezar entreno"
          subtitle="Sesión de hoy y registro"
          onPress={() => router.push('/entreno')}
          trailing={<Icon icon={ChevronRight} size={20} />}
          testID="home-shortcut-entreno"
        />
        <ListRow
          title="Historial"
          subtitle="Mesociclos y sesiones sueltas"
          onPress={() => router.push('/historial')}
          trailing={<Icon icon={ChevronRight} size={20} />}
          last
          testID="home-shortcut-historial"
        />
      </Stack>
    </Screen>
  );
}
