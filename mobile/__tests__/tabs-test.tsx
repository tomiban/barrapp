import { renderRouter, screen } from 'expo-router/testing-library';

import TabsLayout from '../src/app/(tabs)/_layout';
import ShowcaseRoute from '../src/app/showcase';

/**
 * Pestañas de la app: el TabBar queda en Entrenar · Plan · Biblioteca · Perfil.
 *
 * `Showcase` deja de ser pestaña pero se conserva como ruta de desarrollo en
 * `/showcase`, fuera del grupo `(tabs)`. `renderRouter` aísla un mini-árbol con
 * los módulos reales de navegación para no depender del root layout de la app
 * (que carga fuentes).
 */
const Home = () => null;
const Plan = () => null;
const Library = () => null;
const Profile = () => null;

function renderApp(initialUrl: string) {
  return renderRouter(
    {
      '(tabs)/_layout': TabsLayout,
      '(tabs)/index': Home,
      '(tabs)/plan': Plan,
      '(tabs)/biblioteca': Library,
      '(tabs)/profile': Profile,
      showcase: ShowcaseRoute,
    },
    { initialUrl },
  );
}

describe('app tabs', () => {
  it('shows Entrenar, Plan, Biblioteca and Perfil with no Showcase tab', async () => {
    await renderApp('/');

    expect(screen.getByRole('tab', { name: 'Entrenar', selected: true })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Plan', selected: false })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Biblioteca', selected: false })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Perfil', selected: false })).toBeOnTheScreen();
    expect(screen.queryByRole('tab', { name: 'Catálogo' })).toBeNull();
    expect(screen.queryByRole('tab', { name: 'Showcase' })).toBeNull();
  });

  it('keeps Showcase reachable as a development route', async () => {
    await renderApp('/showcase');

    expect(screen.getByTestId('showcase-screen')).toBeOnTheScreen();
    // Fuera del grupo de pestañas no hay TabBar.
    expect(screen.queryByRole('tab')).toBeNull();
  });
});
