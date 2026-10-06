import { SoloSessionView } from '@/features/suelta/SoloSessionView';
import { Header, Screen } from '@/design-system/Navigation';

/**
 * Pestaña «Entrenar»: el generador de sesión suelta. El atleta elige tiempo, energía y foco y el
 * motor del servidor compone la sesión del día (requiere conexión; ver ADR-0003). La comprobación
 * de conexión del esqueleto caminante queda implícita: el propio generador falla con un aviso si el
 * API no responde.
 */
export default function HomeScreen() {
  return (
    <Screen header={<Header title="Sesión suelta" />} testID="entrenar-screen">
      <SoloSessionView />
    </Screen>
  );
}
