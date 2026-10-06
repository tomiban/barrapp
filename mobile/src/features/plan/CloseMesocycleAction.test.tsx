import { fireEvent, render, screen } from '@testing-library/react-native';

import { CloseMesocycleAction, type CloseMesocycleState } from './CloseMesocycleAction';

/**
 * `CloseMesocycleAction` (ticket #24): «Cerrar mesociclo» pide confirmación (cerrar es
 * irreversible: pasa el mesociclo al historial y ajusta los máximos) y solo entonces dispara
 * `onClose`. Como en el resto de tests de la app, se comprueba el comportamiento accesible (rol,
 * etiqueta, `onPress` y banners), no el estilo computado.
 */

const idle: CloseMesocycleState = { status: 'idle' };

describe('CloseMesocycleAction', () => {
  it('muestra el botón de cierre y pide confirmación antes de disparar onClose', async () => {
    const onClose = jest.fn();
    await render(<CloseMesocycleAction state={idle} onClose={onClose} testID="close" />);

    expect(screen.getByRole('button')).toHaveTextContent('Cerrar mesociclo');
    expect(screen.queryByTestId('close-confirm-surface')).toBeNull();

    await fireEvent.press(screen.getByRole('button'));

    // La confirmación aparece y confirmar dispara el cierre una sola vez.
    expect(screen.getByTestId('close-confirm-surface')).toBeOnTheScreen();
    await fireEvent.press(screen.getByTestId('close-confirm-accept'));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('cancela sin disparar onClose', async () => {
    const onClose = jest.fn();
    await render(<CloseMesocycleAction state={idle} onClose={onClose} testID="close" />);

    await fireEvent.press(screen.getByRole('button'));
    await fireEvent.press(screen.getByTestId('close-confirm-cancel'));

    expect(onClose).not.toHaveBeenCalled();
    expect(screen.queryByTestId('close-confirm-surface')).toBeNull();
  });

  it('marca Cerrando… y deshabilita el botón mientras cierra', async () => {
    const onClose = jest.fn();
    await render(
      <CloseMesocycleAction state={{ status: 'running' }} onClose={onClose} testID="close" />,
    );

    expect(screen.getByRole('button')).toHaveTextContent('Cerrando…');
    expect(screen.getByRole('button').props.accessibilityState).toEqual({ disabled: true });

    fireEvent.press(screen.getByRole('button'));

    expect(screen.queryByTestId('close-confirm-surface')).toBeNull();
    expect(onClose).not.toHaveBeenCalled();
  });

  it('confirma que el mesociclo se cerró cuando termina bien', async () => {
    await render(
      <CloseMesocycleAction state={{ status: 'success' }} onClose={jest.fn()} testID="close" />,
    );

    expect(screen.getByTestId('close-success')).toBeOnTheScreen();
    expect(
      screen.getByText('Mesociclo cerrado: los máximos quedan ajustados para el próximo mes.'),
    ).toBeOnTheScreen();
  });

  it('muestra el error del API cuando el cierre falla', async () => {
    await render(
      <CloseMesocycleAction
        state={{ status: 'error', message: 'No hay ningún mesociclo en curso para cerrar.' }}
        onClose={jest.fn()}
        testID="close"
      />,
    );

    expect(screen.getByTestId('close-error')).toBeOnTheScreen();
    expect(screen.getByText('No hay ningún mesociclo en curso para cerrar.')).toBeOnTheScreen();
  });
});
