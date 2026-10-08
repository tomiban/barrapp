import { fireEvent, render, screen } from '@testing-library/react-native';

import { TabBarItem } from '../TabBar';

/**
 * `TabBarItem`: contrato de la pestaña como componente (ticket #45) sobre el
 * rediseño de cinco pestañas (ticket #82): icono sobre la etiqueta, activa en
 * `primary` y la pestaña destacada (`Entreno`) con su baldosa amarilla.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** y el rol/estado accesibles, no el estilo
 * computado. La integración de `TabBar` con el router vive en
 * `tab-bar-router-test.tsx`.
 */
describe('TabBarItem', () => {
  it('is a selected tab in primary with its icon when focused', async () => {
    await render(<TabBarItem label="Entreno" icon="zap" isFocused />);

    const tab = screen.getByRole('tab', { name: 'Entreno', selected: true });
    expect(tab).toBeOnTheScreen();
    expect(screen.getByTestId('tab-item-icon')).toBeOnTheScreen();
    expect(screen.getByText('Entreno')).toHaveProp(
      'className',
      expect.stringContaining('text-primary'),
    );
  });

  it('is an unselected tab in text-muted when inactive', async () => {
    await render(<TabBarItem label="Plan" icon="calendar" />);

    const tab = screen.getByRole('tab', { name: 'Plan', selected: false });
    expect(tab).toBeOnTheScreen();
    expect(tab.props.accessibilityLabel).toBe('Plan');
    expect(screen.getByText('Plan')).toHaveProp(
      'className',
      expect.stringContaining('text-text-muted'),
    );
  });

  it('draws the featured tab with a primary tile around the icon', async () => {
    await render(<TabBarItem label="Entreno" icon="zap" featured />);

    expect(screen.getByTestId('tab-item-icon')).toHaveProp(
      'className',
      expect.stringContaining('bg-primary'),
    );
  });

  it('does not paint a tile on a regular tab', async () => {
    await render(<TabBarItem label="Skills" icon="layers" />);

    expect(screen.getByTestId('tab-item-icon').props.className).not.toContain('bg-primary');
  });

  it('forwards onPress', async () => {
    const onPress = jest.fn();
    await render(<TabBarItem label="Entreno" icon="zap" onPress={onPress} />);

    fireEvent.press(screen.getByRole('tab', { name: 'Entreno' }));

    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
