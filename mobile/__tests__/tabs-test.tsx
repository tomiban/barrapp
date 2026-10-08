import { fireEvent, renderRouter, waitFor, type RenderResult } from 'expo-router/testing-library';
import { View } from 'react-native';

import TabsLayout from '../src/app/(tabs)/_layout';
import InicioRoute from '../src/app/(tabs)/index';
import ShowcaseRoute from '../src/app/showcase';

/**
 * Shell de navegación (#82): el `TabBar` queda en Inicio · Plan · Entreno ·
 * Skills · Historial y **Perfil deja de ser pestaña** (se abre desde el avatar
 * de la cabecera). `Inicio` usa su módulo real para comprobar esa costura; el
 * resto de pantallas se sustituye por stubs para no depender de sus llamadas
 * al API.
 *
 * `Showcase` se conserva como ruta de desarrollo en `/showcase`, fuera del
 * grupo `(tabs)`. `renderRouter` aísla un mini-árbol con los módulos reales de
 * navegación para no depender del root layout de la app (que carga fuentes).
 *
 * Nota: `renderRouter` mantiene el estado del router a nivel de módulo, así
 * que las pruebas que **navegan** (pulsan) van en un solo test al final del
 * archivo; los renders anteriores no pulsan nada.
 */
const Plan = () => null;
const Entreno = () => null;
const Skills = () => null;
const Historial = () => null;
const Profile = () => <View testID="profile-stub" />;

function renderApp(initialUrl: string): Promise<RenderResult> {
  return renderRouter(
    {
      '(tabs)/_layout': TabsLayout,
      '(tabs)/index': InicioRoute,
      '(tabs)/plan': Plan,
      '(tabs)/entreno': Entreno,
      '(tabs)/skills': Skills,
      '(tabs)/historial': Historial,
      profile: Profile,
      showcase: ShowcaseRoute,
    },
    { initialUrl },
  );
}

describe('app tabs', () => {
  it('shows Inicio, Plan, Entreno, Skills and Historial in order, with no Perfil tab', async () => {
    const view = await renderApp('/');

    const labels = view.getAllByRole('tab').map((tab) => tab.props.accessibilityLabel);
    expect(labels).toEqual(['Inicio', 'Plan', 'Entreno', 'Skills', 'Historial']);
    expect(view.getByRole('tab', { name: 'Inicio', selected: true })).toBeOnTheScreen();
    expect(view.getByRole('tab', { name: 'Entreno', selected: false })).toBeOnTheScreen();
    // Perfil y la antigua Biblioteca/Catálogo ya no son pestañas.
    expect(view.queryByRole('tab', { name: 'Perfil' })).toBeNull();
    expect(view.queryByRole('tab', { name: 'Biblioteca' })).toBeNull();
    expect(view.queryByRole('tab', { name: 'Catálogo' })).toBeNull();
    expect(view.queryByRole('tab', { name: 'Showcase' })).toBeNull();
  });

  it('keeps Showcase reachable as a development route', async () => {
    const view = await renderApp('/showcase');

    expect(view.getByTestId('showcase-screen')).toBeOnTheScreen();
    // Fuera del grupo de pestañas no hay TabBar.
    expect(view.queryByRole('tab')).toBeNull();
  });

  it('navigates from the Inicio shortcuts and opens Perfil from the avatar', async () => {
    const view = await renderApp('/');

    // Atajo de Inicio: salta a la pestaña Plan.
    fireEvent.press(view.getByRole('button', { name: /Plan del mesociclo/ }));
    await waitFor(() =>
      expect(view.getByRole('tab', { name: 'Plan', selected: true })).toBeOnTheScreen(),
    );

    // Se vuelve a Inicio desde la propia barra de pestañas.
    fireEvent.press(view.getByRole('tab', { name: 'Inicio' }));
    await waitFor(() =>
      expect(view.getByRole('tab', { name: 'Inicio', selected: true })).toBeOnTheScreen(),
    );

    // Perfil no es pestaña: se abre desde el avatar de la cabecera.
    fireEvent.press(view.getByRole('button', { name: 'Perfil' }));
    await waitFor(() => expect(view.getByTestId('profile-stub')).toBeOnTheScreen());
    expect(view.queryByRole('tab')).toBeNull();
  });
});
