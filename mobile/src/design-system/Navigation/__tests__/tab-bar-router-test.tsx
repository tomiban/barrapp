import { fireEvent, renderRouter, screen, waitFor } from 'expo-router/testing-library';

import { TabBar, type TabDefinition } from '../TabBar';

/**
 * `TabBar` a nivel de router (ticket #45): comprueba que las pestañas se
 * resuelven contra las rutas reales de `expo-router/ui` y que el estado activo
 * sigue a la ruta. `renderRouter` aísla un mini-árbol de rutas con nuestro
 * layout para no depender del root layout de fuentes de la app.
 *
 * El shell de #82 usa el mismo patrón que la app: iconos en todas las
 * pestañas y la central (`Entreno`) destacada. Como `renderRouter` conserva el
 * estado del router entre tests, el test que **pulsa** va el último.
 */
const TABS: readonly TabDefinition[] = [
  { name: 'index', href: '/', label: 'Inicio', icon: 'house' },
  { name: 'entreno', href: '/entreno', label: 'Entreno', icon: 'zap', featured: true },
  { name: 'plan', href: '/plan', label: 'Plan', icon: 'calendar' },
];

function TabsLayout() {
  return <TabBar tabs={TABS} />;
}

const Home = () => null;
const Train = () => null;
const Plan = () => null;

describe('TabBar (integración con Expo Router)', () => {
  it('muestra las pestañas, sus iconos y la baldosa de la destacada', async () => {
    await renderRouter(
      { _layout: TabsLayout, index: Home, entreno: Train, plan: Plan },
      { initialUrl: '/' },
    );

    expect(screen.getByRole('tab', { name: 'Inicio', selected: true })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Entreno', selected: false })).toBeOnTheScreen();
    expect(screen.getByRole('tab', { name: 'Plan', selected: false })).toBeOnTheScreen();
    const icons = screen.getAllByTestId('tab-item-icon');
    expect(icons).toHaveLength(TABS.length);
    // La destacada lleva su baldosa `primary` aunque no esté activa.
    expect(icons[1]).toHaveProp('className', expect.stringContaining('bg-primary'));
    expect(icons[0].props.className).not.toContain('bg-primary');
  });

  it('cambia la pestaña activa al pulsar la otra', async () => {
    await renderRouter(
      { _layout: TabsLayout, index: Home, entreno: Train, plan: Plan },
      { initialUrl: '/' },
    );

    fireEvent.press(screen.getByRole('tab', { name: 'Plan' }));

    await waitFor(() => {
      expect(screen.getByRole('tab', { name: 'Plan', selected: true })).toBeOnTheScreen();
      expect(screen.getByRole('tab', { name: 'Inicio', selected: false })).toBeOnTheScreen();
    });
  });
});
