import { fireEvent, render, screen } from '@testing-library/react-native';

import { TabBarItem } from '../TabBar';

/**
 * `TabBarItem`: contrato de la pestaña como componente (ticket #45).
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** y el rol/estado accesibles, no el estilo
 * computado. La integración de `TabBar` con el router vive en
 * `tab-bar-router-test.tsx`.
 */
describe('TabBarItem', () => {
  it('es un tab seleccionado en primary/on-primary cuando está activo', async () => {
    await render(<TabBarItem label="Inicio" isFocused />);

    const tab = screen.getByRole('tab', { name: 'Inicio', selected: true });
    expect(tab).toBeOnTheScreen();
    expect(tab).toHaveProp('className', expect.stringContaining('bg-primary'));
    expect(screen.getByText('Inicio')).toHaveProp(
      'className',
      expect.stringContaining('text-on-primary'),
    );
  });

  it('es un tab no seleccionado en surface-muted/text-muted cuando está inactivo', async () => {
    await render(<TabBarItem label="Plan" />);

    const tab = screen.getByRole('tab', { name: 'Plan', selected: false });
    expect(tab).toBeOnTheScreen();
    expect(tab).toHaveProp('className', expect.stringContaining('bg-surface-muted'));
    expect(screen.getByText('Plan')).toHaveProp(
      'className',
      expect.stringContaining('text-text-muted'),
    );
  });

  it('reenvía onPress', async () => {
    const onPress = jest.fn();
    await render(<TabBarItem label="Inicio" onPress={onPress} />);

    fireEvent.press(screen.getByRole('tab', { name: 'Inicio' }));

    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
