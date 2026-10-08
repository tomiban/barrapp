import { fireEvent, render, screen } from '@testing-library/react-native';
import { router } from 'expo-router';
import { Text } from 'react-native';

import { AppHeader } from '../AppHeader';

jest.mock('expo-router', () => ({ router: { push: jest.fn(), back: jest.fn() } }));

/**
 * `AppHeader` (ticket #82): cabecera de la app con el patrón `BARRAS /
 * <SECCIÓN>` del mockup. El avatar de la derecha abre **Perfil** (que dejó de
 * ser pestaña) y `showBack` añade el control de volver de las pantallas
 * apiladas (p. ej. Perfil).
 *
 * El router se mockea para no montar navegación: sólo interesa el contrato de
 * la cabecera, no el destino real (cubierto en `__tests__/tabs-test.tsx`).
 */
describe('AppHeader', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('announces the BARRAS / SECTION pattern as the screen header', async () => {
    await render(<AppHeader section="Inicio" />);

    const header = screen.getByRole('header', { name: 'BARRAS / Inicio' });
    expect(header).toBeOnTheScreen();
    expect(screen.getByText('BARRAS')).toBeOnTheScreen();
    expect(screen.getByText('Inicio')).toBeOnTheScreen();
  });

  it('opens Perfil from the avatar button', async () => {
    await render(<AppHeader section="Inicio" />);

    fireEvent.press(screen.getByRole('button', { name: 'Perfil' }));

    expect(router.push).toHaveBeenCalledWith('/profile');
  });

  it('navigates the avatar to a custom destination when onAvatarPress is given', async () => {
    const onAvatarPress = jest.fn();
    await render(<AppHeader section="Plan" onAvatarPress={onAvatarPress} />);

    fireEvent.press(screen.getByRole('button', { name: 'Perfil' }));

    expect(onAvatarPress).toHaveBeenCalledTimes(1);
    expect(router.push).not.toHaveBeenCalled();
  });

  it('shows the back control when showBack is set', async () => {
    await render(<AppHeader section="Perfil" showBack />);

    fireEvent.press(screen.getByRole('button', { name: 'Volver' }));

    expect(router.back).toHaveBeenCalledTimes(1);
  });

  it('hides the back control by default', async () => {
    await render(<AppHeader section="Perfil" />);

    expect(screen.queryByRole('button', { name: 'Volver' })).toBeNull();
  });

  it('keeps extra trailing content next to the avatar', async () => {
    await render(<AppHeader section="Entreno" trailing={<Text>sync</Text>} />);

    expect(screen.getByText('sync')).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Perfil' })).toBeOnTheScreen();
  });
});
