import { render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { Header } from '../Header';
import { Screen } from '../Screen';

/**
 * `Screen` y `Header` del design system (ticket #45).
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build) y lo que se ve
 * en pantalla, no el estilo computado.
 */
describe('Screen', () => {
  it('renderiza el contenido sobre canvas con el margen de página', async () => {
    await render(
      <Screen testID="screen">
        <Text>contenido</Text>
      </Screen>,
    );

    expect(screen.getByText('contenido')).toBeOnTheScreen();
    expect(screen.getByTestId('screen')).toHaveProp(
      'className',
      expect.stringContaining('bg-canvas'),
    );
    expect(screen.getByTestId('screen-content')).toHaveProp(
      'className',
      expect.stringContaining('px-margin'),
    );
  });

  it('coloca el header opcional por encima del contenido', async () => {
    await render(
      <Screen testID="screen" header={<Header title="Plan" />}>
        <Text>contenido</Text>
      </Screen>,
    );

    expect(screen.getByRole('header', { name: 'Plan' })).toBeOnTheScreen();
  });

  it('no añade header cuando no se provee', async () => {
    await render(
      <Screen testID="screen">
        <Text>contenido</Text>
      </Screen>,
    );

    expect(screen.queryByRole('header')).toBeNull();
  });

  it('respeta top/left/right por defecto (el inferior es de la TabBar)', async () => {
    await render(<Screen testID="screen" />);

    const { edges } = screen.getByTestId('screen').props;
    expect(edges.top).not.toBe('off');
    expect(edges.left).not.toBe('off');
    expect(edges.right).not.toBe('off');
    expect(edges.bottom).toBe('off');
  });

  it('permite al consumidor cambiar los bordes seguros', async () => {
    await render(<Screen testID="screen" edges={['top', 'bottom']} />);

    const { edges } = screen.getByTestId('screen').props;
    expect(edges.top).not.toBe('off');
    expect(edges.bottom).not.toBe('off');
  });

  it('envuelve el contenido en un ScrollView cuando `scrollable`', async () => {
    await render(
      <Screen testID="screen" scrollable>
        <Text>contenido</Text>
      </Screen>,
    );

    expect(screen.getByText('contenido')).toBeOnTheScreen();
    // `contentContainerClassName` es propio de `ScrollView`: su presencia prueba
    // que el contenido scrollea y conserva el margen de página.
    expect(screen.getByTestId('screen-content')).toHaveProp(
      'contentContainerClassName',
      expect.stringContaining('px-margin'),
    );
  });
});
