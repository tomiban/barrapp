import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { BottomSheet } from './BottomSheet';

/**
 * `BottomSheet` (spec 0002, ticket #46): modal de nivel 3 — superficie
 * `surface`, marco de 2 px en `text` y scrim negro 80 % sin blur.
 *
 * Jest no corre Metro/Uniwind, así que se comprueba el comportamiento, la
 * accesibilidad y el contrato de clases; nunca el estilo computado.
 */
describe('BottomSheet', () => {
  it('no muestra nada cuando visible es false', async () => {
    await render(
      <BottomSheet visible={false} onClose={jest.fn()} title="Ajustes">
        <Text>contenido</Text>
      </BottomSheet>,
    );

    expect(screen.queryByText('Ajustes')).toBeNull();
    expect(screen.queryByText('contenido')).toBeNull();
  });

  it('muestra el título como encabezado y el contenido cuando visible es true', async () => {
    await render(
      <BottomSheet visible onClose={jest.fn()} title="Ajustes">
        <Text>contenido</Text>
      </BottomSheet>,
    );

    expect(screen.getByRole('header', { name: 'Ajustes' })).toBeOnTheScreen();
    expect(screen.getByText('contenido')).toBeOnTheScreen();
  });

  it('cierra al pulsar el scrim (backdrop)', async () => {
    const onClose = jest.fn();
    await render(<BottomSheet visible onClose={onClose} title="Ajustes" testID="sheet" />);

    // El scrim es decorativo: `accessibilityViewIsModal` deja fuera del árbol
    // accesible a sus hermanos, por eso se consulta incluyendo ocultos.
    await fireEvent.press(screen.getByTestId('sheet-backdrop', { includeHiddenElements: true }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('cierra con el botón «atrás» de Android (onRequestClose)', async () => {
    const onClose = jest.fn();
    await render(<BottomSheet visible onClose={onClose} title="Ajustes" testID="sheet" />);

    // RN `Modal` reenvía `testID` y `onRequestClose` al host nativo.
    fireEvent(screen.getByTestId('sheet'), 'requestClose');

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('cierra al pulsar la afordancia de cierre, con etiqueta accesible', async () => {
    const onClose = jest.fn();
    await render(<BottomSheet visible onClose={onClose} title="Ajustes" testID="sheet" />);

    expect(screen.getByLabelText('Cerrar')).toBeOnTheScreen();
    await fireEvent.press(screen.getByTestId('sheet-close'));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('aplica el marco de 2 px en text sobre surface, sin sombras', async () => {
    await render(<BottomSheet visible onClose={jest.fn()} testID="sheet" />);

    const surface = screen.getByTestId('sheet-surface');
    expect(surface).toHaveProp('className', expect.stringContaining('border-2'));
    expect(surface).toHaveProp('className', expect.stringContaining('border-text'));
    expect(surface).toHaveProp('className', expect.stringContaining('bg-surface'));
    expect(surface.props.className).not.toContain('shadow');
  });

  it('pinta el scrim negro al 80 % sin blur', async () => {
    await render(<BottomSheet visible onClose={jest.fn()} testID="sheet" />);

    const backdrop = screen.getByTestId('sheet-backdrop', { includeHiddenElements: true });
    expect(backdrop).toHaveProp('className', expect.stringContaining('bg-scrim'));
    expect(backdrop.props.className).not.toContain('blur');
  });

  it('declara la superficie como modal para accesibilidad', async () => {
    await render(<BottomSheet visible onClose={jest.fn()} title="Ajustes" testID="sheet" />);

    expect(screen.getByTestId('sheet-surface')).toHaveProp('accessibilityViewIsModal', true);
  });

  it('deja que el className del consumidor sobrescriba la superficie', async () => {
    await render(
      <BottomSheet visible onClose={jest.fn()} className="bg-surface-muted" testID="sheet" />,
    );

    const surface = screen.getByTestId('sheet-surface');
    expect(surface.props.className).toContain('bg-surface-muted');
    expect(surface.props.className).not.toContain('bg-surface ');
  });
});
