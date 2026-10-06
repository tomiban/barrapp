import { render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { SafeAreaView } from '../SafeAreaView';

/**
 * Regresión (#80): el `SafeAreaView` de `react-native-safe-area-context` ignora
 * `className` (Uniwind solo lo transforma en componentes de `react-native`), así
 * que `flex-1`/`bg-canvas` se perdían y el contenido colapsaba a 0 de alto.
 *
 * El wrapper de este módulo resuelve `className` a un `style`; este test fija ese
 * contrato: sin `withUniwind` no habría `style` y el layout volvería a romperse.
 */
describe('SafeAreaView', () => {
  it('resolves className into a style prop', async () => {
    await render(
      <SafeAreaView testID="safe-area" className="flex-1 bg-canvas">
        <Text>contenido</Text>
      </SafeAreaView>,
    );

    expect(screen.getByText('contenido')).toBeOnTheScreen();
    expect(screen.getByTestId('safe-area').props.style).toBeDefined();
  });
});
