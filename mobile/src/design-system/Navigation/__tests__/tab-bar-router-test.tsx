import { fireEvent, renderRouter, screen, waitFor } from 'expo-router/testing-library';

import { TabBar, type TabDefinition } from '../TabBar';

/**
 * `TabBar` a nivel de router (ticket #45): comprueba que las pestañas se
 * resuelven contra las rutas reales de `expo-router/ui` y que el estado activo
 * sigue a la ruta. `renderRouter` aísla un mini-árbol de rutas con nuestro
 * layout para no depender del root layout de fuentes de la app.
 */
const TABS: readonly TabDefinition[] = [
  { name: 'index', href: '/', label: 'Inicio' },
  { name: 'plan', href: '/plan', label: 'Plan' },
];

function TabsLayout() {
  return <TabBar tabs={TABS} />;
}

const Home = () => null;
const Plan = () => null;

describe('TabBar (integración con Expo Router)', () => {
  it('muestra las pestañas y marca la ruta activa', async () => {
    await renderRouter({ _layout: TabsLayout, index: Home, plan: Plan }, { initialUrl: '/' });

    expect(screen.getByRole('tab', { name: 'Inicio', selected: true })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Plan', selected: false })).toBeOnTheScreen();
  });

  it('cambia la pestaña activa al pulsar la otra', async () => {
    await renderRouter({ _layout: TabsLayout, index: Home, plan: Plan }, { initialUrl: '/' });

    fireEvent.press(screen.getByRole('tab', { name: 'Plan' }));

    await waitFor(() => {
      expect(screen.getByRole('tab', { name: 'Plan', selected: true })).toBeOnTheScreen();
      expect(screen.getByRole('tab', { name: 'Inicio', selected: false })).toBeOnTheScreen();
    });
  });
});
