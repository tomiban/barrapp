import { fireEvent, render, screen } from '@testing-library/react-native';

import { StageAdvanceAction, type StageAdvanceState } from './StageAdvanceAction';

/**
 * `StageAdvanceAction` (ticket #23): botón de avance de etapa con su feedback. Como en el resto de
 * tests del DS, se comprueba el comportamiento accesible (rol, etiqueta, `onPress` y banners), no
 * el estilo computado.
 */

const idle: StageAdvanceState = { status: 'idle' };

describe('StageAdvanceAction', () => {
  it('muestra el botón de avance y dispara onAdvance al pulsarlo', async () => {
    const onAdvance = jest.fn();
    await render(<StageAdvanceAction state={idle} onAdvance={onAdvance} testID="advance" />);

    expect(screen.getByRole('button')).toHaveTextContent('Avanzar etapa');

    fireEvent.press(screen.getByRole('button'));

    expect(onAdvance).toHaveBeenCalledTimes(1);
  });

  it('no muestra feedback mientras está idle', async () => {
    await render(<StageAdvanceAction state={idle} onAdvance={jest.fn()} testID="advance" />);

    expect(screen.queryByTestId('advance-success')).toBeNull();
    expect(screen.queryByTestId('advance-kept')).toBeNull();
    expect(screen.queryByTestId('advance-error')).toBeNull();
  });

  it('marca Evaluando… y deshabilita el botón mientras evalúa', async () => {
    const onAdvance = jest.fn();
    await render(
      <StageAdvanceAction state={{ status: 'running' }} onAdvance={onAdvance} testID="advance" />,
    );

    expect(screen.getByRole('button')).toHaveTextContent('Evaluando…');
    expect(screen.getByRole('button').props.accessibilityState).toEqual({ disabled: true });

    fireEvent.press(screen.getByRole('button'));

    expect(onAdvance).not.toHaveBeenCalled();
  });

  it('confirma la etapa nueva al avanzar', async () => {
    await render(
      <StageAdvanceAction
        state={{ status: 'success', stageOrder: 2 }}
        onAdvance={jest.fn()}
        testID="advance"
      />,
    );

    expect(screen.getByTestId('advance-success')).toBeOnTheScreen();
    expect(screen.getByText('¡Subiste a la etapa 2!')).toBeOnTheScreen();
  });

  it('explica que el criterio aún no se cumple cuando no avanza', async () => {
    await render(
      <StageAdvanceAction state={{ status: 'kept' }} onAdvance={jest.fn()} testID="advance" />,
    );

    expect(screen.getByTestId('advance-kept')).toBeOnTheScreen();
    expect(
      screen.getByText('El criterio aún no se cumple en dos sesiones consecutivas.'),
    ).toBeOnTheScreen();
  });

  it('muestra el error del API cuando la evaluación falla', async () => {
    await render(
      <StageAdvanceAction
        state={{ status: 'error', message: 'Sin conexión con el API.' }}
        onAdvance={jest.fn()}
        testID="advance"
      />,
    );

    expect(screen.getByTestId('advance-error')).toBeOnTheScreen();
    expect(screen.getByText('Sin conexión con el API.')).toBeOnTheScreen();
  });
});
