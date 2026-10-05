import { render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { Header } from '../Header';

/**
 * `Header` del design system (ticket #45): título `headlineSm`, hairline
 * inferior y slots laterales. Se comprueba el contrato de clases y el rol
 * accesible, no el estilo computado (Jest no corre Metro).
 */
describe('Header', () => {
  it('muestra el título en headlineSm con hairline inferior', async () => {
    await render(<Header testID="header" title="Plan" />);

    const title = screen.getByRole('header', { name: 'Plan' });
    expect(title).toBeOnTheScreen();
    expect(title).toHaveProp('className', expect.stringContaining('text-headline-sm'));
    expect(screen.getByTestId('header')).toHaveProp(
      'className',
      expect.stringContaining('border-b'),
    );
    expect(screen.getByTestId('header')).toHaveProp(
      'className',
      expect.stringContaining('border-border'),
    );
  });

  it('usa el margen de página en horizontal y sin sombras', async () => {
    await render(<Header testID="header" title="Plan" />);

    expect(screen.getByTestId('header')).toHaveProp(
      'className',
      expect.stringContaining('px-margin'),
    );
    expect(screen.getByTestId('header').props.className).not.toContain('shadow');
  });

  it('coloca los slots leading y trailing alrededor del título', async () => {
    await render(
      <Header title="Plan" leading={<Text>atrás</Text>} trailing={<Text>hecho</Text>} />,
    );

    expect(screen.getByText('atrás')).toBeOnTheScreen();
    expect(screen.getByText('hecho')).toBeOnTheScreen();
  });
});
