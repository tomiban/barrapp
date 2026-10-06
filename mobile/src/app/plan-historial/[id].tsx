import { router, useLocalSearchParams } from 'expo-router';
import { Pressable } from 'react-native';

import { ArrowLeft, Icon } from '@/design-system/Icon';
import { Header, Screen } from '@/design-system/Navigation';
import { MesocycleDetail } from '@/features/history/MesocycleDetail';

/**
 * Detalle de un mesociclo del historial (#27): abre el mesociclo pasado por su identificador y
 * pinta su plan tal y como se guardó, con la misma vista que el plan actual. El id llega como
 * parámetro de ruta desde la sección de historial del tab Plan.
 */
export default function MesocycleHistoryDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();

  const goBack = () => {
    if (router.canGoBack()) {
      router.back();
    } else {
      router.replace('/plan');
    }
  };

  return (
    <Screen
      testID="history-detail-screen"
      header={
        <Header
          title="Mesociclo pasado"
          leading={
            <Pressable
              onPress={goBack}
              accessibilityRole="button"
              accessibilityLabel="Volver al historial"
              testID="history-detail-back"
            >
              <Icon icon={ArrowLeft} size={24} />
            </Pressable>
          }
        />
      }
    >
      <MesocycleDetail mesocycleId={id} />
    </Screen>
  );
}
