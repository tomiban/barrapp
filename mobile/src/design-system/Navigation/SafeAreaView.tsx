import { SafeAreaView as RNSafeAreaView } from 'react-native-safe-area-context';
import { withUniwind } from 'uniwind';

/**
 * `SafeAreaView` de `react-native-safe-area-context` con soporte de `className`.
 *
 * Uniwind solo transforma `className` en los componentes de `react-native`; los
 * de terceros hay que envolverlos con `withUniwind`, o el `className` se ignora
 * (y con él `flex-1`, `bg-canvas`…) y el layout colapsa. Ver
 * https://docs.uniwind.dev/components/other-components.
 *
 * Se envuelve una sola vez a nivel de módulo para no recrear el HOC al render.
 */
export const SafeAreaView = withUniwind(RNSafeAreaView);
