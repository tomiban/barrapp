import { StyleSheet, Text } from 'react-native';
import { fireEvent, renderRouter, waitFor } from 'expo-router/testing-library';

import TabsLayout, { TABS } from '@/app/(tabs)/_layout';

/**
 * `TabBar` a nivel de router (tickets #45 y #82): comprueba que las pestañas se
 * resuelven contra las rutas reales de `expo-router/ui` y que el estado activo
 * sigue a la ruta. `renderRouter` aísla un mini-árbol de rutas con el layout
 * real de `(tabs)` para no depender del root layout de fuentes de la app.
 *
 * Las rutas son dobles rotulados: al pulsar una pestaña, el contenido de la
 * ruta destino aparece en el `TabSlot`, que es la costura observable.
 */
function RouteStub({ label }: { label: string }) {
  return <Text testID={`route-${label}`}>{label}</Text>;
}

function routes() {
  return {
    _layout: TabsLayout,
    index: () => <RouteStub label="inicio" />,
    plan: () => <RouteStub label="plan" />,
    entreno: () => <RouteStub label="entreno" />,
    skills: () => <RouteStub label="skills" />,
    historial: () => <RouteStub label="historial" />,
  };
}

describe('TabBar (integración con Expo Router)', () => {
  it('define las cinco pestañas en el orden del diseño, con GO destacada', () => {
    expect(TABS.map((tab) => tab.label)).toEqual(['Inicio', 'Plan', 'GO', 'Skills', 'Historial']);
    expect(TABS.filter((tab) => tab.prominent).map((tab) => tab.label)).toEqual(['GO']);
  });

  it('muestra las pestañas y marca la ruta activa', async () => {
    const { getByRole, getByTestId } = await renderRouter(routes(), { initialUrl: '/' });

    expect(getByTestId('route-inicio')).toBeOnTheScreen();
    expect(getByRole('tab', { name: 'Inicio', selected: true })).toBeOnTheScreen();
    expect(getByRole('tab', { name: 'Plan', selected: false })).toBeOnTheScreen();
    expect(getByRole('tab', { name: 'GO', selected: false })).toBeOnTheScreen();
    expect(getByRole('tab', { name: 'Skills', selected: false })).toBeOnTheScreen();
    expect(getByRole('tab', { name: 'Historial', selected: false })).toBeOnTheScreen();
  });

  // Regresión: `TabTrigger` (expo-router/ui) inyecta `flexDirection: 'row'` +
  // `justifyContent: 'space-between'` vía Slot de Radix, y ese estilo inline
  // gana al `className` de Uniwind. Las celdas se disponían en fila: iconos
  // junto a las etiquetas y textos largos recortados en el borde derecho.
  //
  // Se assertion sobre el `style` (detalle de implementación) porque en Jest no
  // hay layout real: es la única costura observable del apilado sin abrir un
  // emulador. Si el fix cambia de estrategia, actualizar este contrato.
  it('stacks the icon above the label with a column layout', async () => {
    const { getByRole } = await renderRouter(routes(), { initialUrl: '/' });

    for (const name of ['Inicio', 'Plan', 'GO', 'Skills', 'Historial']) {
      const tab = getByRole('tab', { name });
      expect(StyleSheet.flatten(tab.props.style)).toMatchObject({
        flexDirection: 'column',
        justifyContent: 'center',
      });
    }
  });

  it('cambia la pestaña activa al pulsar otra', async () => {
    const { getByRole, getByTestId } = await renderRouter(routes(), { initialUrl: '/' });

    fireEvent.press(getByRole('tab', { name: 'Historial' }));

    await waitFor(() => {
      expect(getByTestId('route-historial')).toBeOnTheScreen();
      expect(getByRole('tab', { name: 'Historial', selected: true })).toBeOnTheScreen();
      expect(getByRole('tab', { name: 'Inicio', selected: false })).toBeOnTheScreen();
    });
  });
});
