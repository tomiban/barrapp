import { fireEvent, render, screen } from '@testing-library/react-native';

import { ShowcaseScreen } from './ShowcaseScreen';

/**
 * `ShowcaseScreen` (spec 0002, ticket #49): catálogo vivo del design system.
 *
 * Es un test de humo/integración: monta la pantalla completa (todos los
 * componentes a la vez) y comprueba que las cuatro secciones y una muestra
 * representativa de variantes/estados se ven sin romper. En Jest no corre
 * Metro, así que Uniwind no resuelve `className` a estilo: se verifica lo que
 * se ve y el comportamiento, nunca el estilo computado.
 */
describe('ShowcaseScreen', () => {
  it('monta el catálogo con su encabezado y las cuatro secciones', async () => {
    await render(<ShowcaseScreen />);

    expect(screen.getByRole('header', { name: 'Catálogo' })).toBeOnTheScreen();
    expect(screen.getByRole('header', { name: 'Base' })).toBeOnTheScreen();
    expect(screen.getByRole('header', { name: 'Entrenamiento' })).toBeOnTheScreen();
    expect(screen.getByRole('header', { name: 'Navegación y feedback' })).toBeOnTheScreen();
    expect(screen.getByRole('header', { name: 'Iconos' })).toBeOnTheScreen();
  });

  it('muestra las variantes de Button y su estado deshabilitado', async () => {
    await render(<ShowcaseScreen />);

    expect(screen.getByRole('button', { name: 'Primario' })).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Secundario' })).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Terciario' })).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Deshabilitado' })).toBeDisabled();
  });

  it('muestra los campos normal y error', async () => {
    await render(<ShowcaseScreen />);

    expect(screen.getByTestId('showcase-field-normal')).toBeOnTheScreen();
    expect(screen.getByText('Fuera de rango (0–100)')).toBeOnTheScreen();
  });

  it('muestra los cuatro roles de StatusBadge', async () => {
    await render(<ShowcaseScreen />);

    for (const label of ['En curso', 'Confirmado', 'Fallo', 'Inactivo']) {
      expect(screen.getAllByText(label).length).toBeGreaterThan(0);
    }
  });

  it('muestra el readout de MetricCounter', async () => {
    await render(<ShowcaseScreen />);

    expect(screen.getByText('100')).toBeOnTheScreen();
    expect(screen.getByText('Peso')).toBeOnTheScreen();
  });

  it('muestra una selección curada de iconos', async () => {
    await render(<ShowcaseScreen />);

    expect(screen.getByText('timer')).toBeOnTheScreen();
    expect(screen.getByText('dumbbell')).toBeOnTheScreen();
  });

  it('abre el BottomSheet desde su disparador', async () => {
    await render(<ShowcaseScreen />);

    expect(screen.queryByTestId('showcase-sheet-surface')).toBeNull();

    await fireEvent.press(screen.getByRole('button', { name: 'Abrir BottomSheet' }));

    expect(screen.getByTestId('showcase-sheet-surface')).toBeOnTheScreen();
  });
});
