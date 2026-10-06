import { renderRouter, screen } from 'expo-router/testing-library';

import TabsLayout from '../src/app/(tabs)/_layout';
import ShowcaseRoute from '../src/app/showcase';

/**
 * Pestañas de la app (spec 0003, ticket #82): Inicio · Plan · Entreno · Skills ·
 * Historial, con Perfil fuera de la barra (se abre desde el avatar).
 *
 * `Showcase` deja de ser pestaña pero se conserva como ruta de desarrollo en
 * `/showcase`, fuera del grupo `(tabs)`. `renderRouter` aísla un mini-árbol con
 * los módulos reales de navegación para no depender del root layout de la app
 * (que carga fuentes).
 */
const Home = () => null;
const Plan = () => null;
const Entreno = () => null;
const Skills = () => null;
const Historial = () => null;
const Profile = () => null;

function renderApp(initialUrl: string) {
  return renderRouter(
    {
      '(tabs)/_layout': TabsLayout,
      '(tabs)/index': Home,
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
  it('shows the five tabs and keeps Perfil out of the bar', async () => {
    await renderApp('/');

    expect(screen.getByRole('tab', { name: 'Inicio', selected: true })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Plan', selected: false })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Entreno', selected: false })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Skills', selected: false })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Historial', selected: false })).toBeOnTheScreen();
    expect(screen.queryByRole('tab', { name: 'Perfil' })).toBeNull();
    expect(screen.queryByRole('tab', { name: 'Catálogo' })).toBeNull();
  });

  it('keeps Showcase reachable as a development route', async () => {
    await renderApp('/showcase');

    expect(screen.getByTestId('showcase-screen')).toBeOnTheScreen();
    // Fuera del grupo de pestañas no hay TabBar.
    expect(screen.queryByRole('tab')).toBeNull();
  });
});
