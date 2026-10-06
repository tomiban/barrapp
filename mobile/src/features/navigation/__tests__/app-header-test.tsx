import { Stack } from 'expo-router';
import { Text } from 'react-native';
import { fireEvent, renderRouter, waitFor } from 'expo-router/testing-library';

import { AppHeader } from '../AppHeader';

/**
 * `AppHeader` (ticket #82): la cabecera común `BARRAS / <SECCIÓN>` y el avatar
 * que abre Perfil. Se comprueba sobre un mini-árbol de rutas real: al pulsar el
 * avatar, la ruta de Perfil aparece en pantalla, que es la costura observable.
 */
const Home = () => <AppHeader section="Inicio" />;
const Profile = () => <Text testID="profile-route">Perfil</Text>;

function RootLayout() {
  return <Stack screenOptions={{ headerShown: false }} />;
}

describe('AppHeader', () => {
  it('muestra el patrón BARRAS / <SECCIÓN> y abre Perfil desde el avatar', async () => {
    const { getByText, getByRole, getByTestId } = await renderRouter(
      { _layout: RootLayout, index: Home, profile: Profile },
      { initialUrl: '/' },
    );

    expect(getByText('BARRAS')).toBeOnTheScreen();
    expect(getByRole('header', { name: 'Inicio' })).toBeOnTheScreen();

    fireEvent.press(getByRole('button', { name: 'Perfil' }));

    await waitFor(() => expect(getByTestId('profile-route')).toBeOnTheScreen());
  });
});
