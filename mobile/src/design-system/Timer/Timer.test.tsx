import { act, fireEvent, render, screen } from '@testing-library/react-native';

import { Countdown, Timer } from './Timer';

/**
 * Timer / Countdown (ticket #41): cuenta atrás en segundos para holds y TUT.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build) y el
 * comportamiento accesible, no el estilo computado. Mismo criterio que
 * MetricCounter (#40) y ProgressIndicator (#42).
 */
describe('Timer', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.clearAllTimers();
    jest.useRealTimers();
  });

  it('reutiliza MetricCounter para el readout con números monoespaciados tabulares', async () => {
    await render(<Timer durationSeconds={30} testID="timer" />);

    const readout = screen.getByTestId('timer-metric-value');
    expect(readout).toHaveProp('className', expect.stringContaining('text-headline-metric'));
    // Los dígitos tabulares los aplica `Text` como estilo inline (Uniwind no
    // tiene utility de `fontVariant`): el número no baila durante la cuenta.
    expect(readout).toHaveStyle({ fontVariant: ['tabular-nums'] });
  });

  it('pinta el countdown con el rol activo (`primary`)', async () => {
    await render(<Timer durationSeconds={30} testID="timer" />);

    expect(screen.getByTestId('timer-metric-value')).toHaveProp(
      'className',
      expect.stringContaining('text-primary'),
    );
  });

  it('descuenta un segundo por tick mientras corre', async () => {
    await render(<Timer durationSeconds={5} running testID="timer" />);
    expect(screen.getByText('5')).toBeOnTheScreen();

    await act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText('4')).toBeOnTheScreen();

    await act(() => {
      jest.advanceTimersByTime(2000);
    });
    expect(screen.getByText('2')).toBeOnTheScreen();
  });

  it('dispara `onComplete` exactamente una vez al llegar a 0', async () => {
    const onComplete = jest.fn();
    await render(<Timer durationSeconds={3} running onComplete={onComplete} testID="timer" />);

    await act(() => {
      jest.advanceTimersByTime(3000);
    });

    expect(screen.getByText('0')).toBeOnTheScreen();
    expect(onComplete).toHaveBeenCalledTimes(1);

    // El intervalo ya se detuvo: avanzar más no vuelve a completar.
    await act(() => {
      jest.advanceTimersByTime(3000);
    });
    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('mantiene el tiempo restante al pausar (`running=false`) y reanuda', async () => {
    const { rerender } = await render(<Timer durationSeconds={5} running={false} testID="timer" />);

    await act(() => {
      jest.advanceTimersByTime(3000);
    });
    expect(screen.getByText('5')).toBeOnTheScreen();

    await rerender(<Timer durationSeconds={5} running testID="timer" />);
    await act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText('4')).toBeOnTheScreen();

    await rerender(<Timer durationSeconds={5} running={false} testID="timer" />);
    await act(() => {
      jest.advanceTimersByTime(3000);
    });
    // Pausa: conserva el restante, no reinicia a la duración.
    expect(screen.getByText('4')).toBeOnTheScreen();
  });

  it('se reinicia al cambiar `durationSeconds`', async () => {
    const { rerender } = await render(<Timer durationSeconds={5} running testID="timer" />);

    await act(() => {
      jest.advanceTimersByTime(2000);
    });
    expect(screen.getByText('3')).toBeOnTheScreen();

    await rerender(<Timer durationSeconds={10} running testID="timer" />);
    expect(screen.getByText('10')).toBeOnTheScreen();
  });

  it('permite repetir la cuenta al arrancar de nuevo tras completarse', async () => {
    const onComplete = jest.fn();
    const { rerender } = await render(
      <Timer durationSeconds={3} running onComplete={onComplete} testID="timer" />,
    );

    await act(() => {
      jest.advanceTimersByTime(3000);
    });
    expect(screen.getByText('0')).toBeOnTheScreen();

    await rerender(
      <Timer durationSeconds={3} running={false} onComplete={onComplete} testID="timer" />,
    );
    await rerender(<Timer durationSeconds={3} running onComplete={onComplete} testID="timer" />);

    expect(screen.getByText('3')).toBeOnTheScreen();

    await act(() => {
      jest.advanceTimersByTime(3000);
    });
    expect(onComplete).toHaveBeenCalledTimes(2);
  });

  it('no dispara `onComplete` si la duración es 0', async () => {
    const onComplete = jest.fn();
    await render(<Timer durationSeconds={0} running onComplete={onComplete} testID="timer" />);

    await act(() => {
      jest.advanceTimersByTime(3000);
    });

    expect(screen.getByText('0')).toBeOnTheScreen();
    expect(onComplete).not.toHaveBeenCalled();
  });

  it('limpia el intervalo al desmontar (sin timers colgados)', async () => {
    const intervalSpy = jest.spyOn(global, 'setInterval');
    const clearSpy = jest.spyOn(global, 'clearInterval');

    const { unmount } = await render(<Timer durationSeconds={30} running testID="timer" />);

    await act(() => {
      jest.advanceTimersByTime(1000);
    });

    await unmount();

    // El id del intervalo del componente sale de `clearInterval` al desmontar.
    const intervalIds = intervalSpy.mock.results.map((result) => result.value);
    expect(intervalIds.length).toBeGreaterThan(0);
    expect(clearSpy.mock.calls.some(([id]) => intervalIds.includes(id))).toBe(true);

    intervalSpy.mockRestore();
    clearSpy.mockRestore();
  });

  it('renderiza un control pulsable cuando se provee `onPress`', async () => {
    const onPress = jest.fn();
    await render(<Timer durationSeconds={5} onPress={onPress} testID="timer" />);

    const control = screen.getByRole('button');
    expect(control).toHaveProp('accessibilityLabel', expect.stringContaining('Tiempo'));

    await fireEvent.press(control);
    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('usa la etiqueta por defecto y acepta una propia', async () => {
    const { rerender } = await render(<Timer durationSeconds={5} testID="timer" />);
    expect(screen.getByText('Tiempo')).toBeOnTheScreen();

    await rerender(<Timer durationSeconds={5} label="Sostén" testID="timer" />);
    expect(screen.getByText('Sostén')).toBeOnTheScreen();
  });

  it('expone `Countdown` como alias del mismo componente', async () => {
    await render(<Countdown durationSeconds={4} running testID="timer" />);

    expect(screen.getByText('4')).toBeOnTheScreen();
    expect(Countdown).toBe(Timer);
  });
});
