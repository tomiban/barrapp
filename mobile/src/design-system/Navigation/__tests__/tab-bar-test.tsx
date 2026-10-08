import { fireEvent, render, screen } from '@testing-library/react-native';

import { baseIcons } from '@/design-system/Icon';

import { TabBarItem } from '../TabBar';

/**
 * `TabBarItem`: contrato de la pestaña como componente (ticket #45, rediseño #82).
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** y el rol/estado accesibles, no el estilo
 * computado. La integración de `TabBar` con el router vive en
 * `tab-bar-router-test.tsx`.
 */
describe('TabBarItem', () => {
  it('marca la pestaña activa en primary, sin fondo relleno', async () => {
    await render(<TabBarItem label="Inicio" icon={baseIcons.play} isFocused />);

    const tab = screen.getByRole('tab', { name: 'Inicio', selected: true });
    expect(tab).toBeOnTheScreen();
    expect(screen.getByText('Inicio')).toHaveProp(
      'className',
      expect.stringContaining('text-primary'),
    );
    expect(tab.props.className).not.toContain('bg-primary');
  });

  it('deja la pestaña inactiva en text-muted', async () => {
    await render(<TabBarItem label="Plan" icon={baseIcons.calendar} />);

    const tab = screen.getByRole('tab', { name: 'Plan', selected: false });
    expect(tab).toBeOnTheScreen();
    expect(screen.getByText('Plan')).toHaveProp(
      'className',
      expect.stringContaining('text-text-muted'),
    );
  });

  it('destaca la pestaña central como celda rellena en primary/on-primary', async () => {
    await render(<TabBarItem label="Entreno" icon={baseIcons.dumbbell} prominent />);

    const tab = screen.getByRole('tab', { name: 'Entreno', selected: false });
    expect(tab).toBeOnTheScreen();
    expect(tab).toHaveProp('className', expect.stringContaining('bg-primary'));
    expect(tab).toHaveProp('className', expect.stringContaining('rounded-lg'));
    expect(screen.getByText('Entreno')).toHaveProp(
      'className',
      expect.stringContaining('text-on-primary'),
    );
  });

  it('reenvía onPress', async () => {
    const onPress = jest.fn();
    await render(<TabBarItem label="Inicio" icon={baseIcons.play} onPress={onPress} />);

    fireEvent.press(screen.getByRole('tab', { name: 'Inicio' }));

    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
