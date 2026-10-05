import { fireEvent, render, screen } from '@testing-library/react-native';

import { Checkbox } from './Checkbox';

/**
 * `Checkbox` (spec 0002, ticket #38): caja cuadrada de 20×20 dp, radios 0 y
 * borde activo (1.5 px); al marcar se rellena en `primary` y muestra una marca.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build) y el
 * comportamiento accesible. Lucide marca sus SVG como decorativos, así que la
 * marca se busca con `includeHiddenElements`.
 */
const getCheckbox = () => screen.getByRole('checkbox');
const getControl = () => screen.getByTestId('check-control');
const queryMark = () => screen.queryByTestId('check-mark', { includeHiddenElements: true });

describe('Checkbox', () => {
  it('arranca desmarcado y con el estado accesible por defecto', async () => {
    await render(<Checkbox checked={false} testID="check" />);

    expect(getCheckbox()).toBeOnTheScreen();
    expect(getCheckbox()).not.toBeChecked();
    expect(getCheckbox().props.accessibilityState).toEqual({ checked: false, disabled: false });
  });

  it('refleja el estado marcado (componente controlado)', async () => {
    await render(<Checkbox checked testID="check" />);

    expect(getCheckbox()).toBeChecked();
    expect(queryMark()).toBeOnTheScreen();
  });

  it('no renderiza la marca cuando está desmarcado', async () => {
    await render(<Checkbox checked={false} testID="check" />);

    expect(queryMark()).toBeNull();
  });

  it('al pulsar un checkbox desmarcado pide marcarlo', async () => {
    const onChange = jest.fn();
    await render(<Checkbox checked={false} onChange={onChange} testID="check" />);

    await fireEvent.press(getCheckbox());

    expect(onChange).toHaveBeenCalledTimes(1);
    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('al pulsar un checkbox marcado pide desmarcarlo', async () => {
    const onChange = jest.fn();
    await render(<Checkbox checked onChange={onChange} testID="check" />);

    await fireEvent.press(getCheckbox());

    expect(onChange).toHaveBeenCalledTimes(1);
    expect(onChange).toHaveBeenCalledWith(false);
  });

  it('además de onChange, invoca onPress en una pulsación válida', async () => {
    const onPress = jest.fn();
    await render(<Checkbox checked={false} onPress={onPress} testID="check" />);

    await fireEvent.press(getCheckbox());

    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('no dispara onChange cuando está deshabilitado', async () => {
    const onChange = jest.fn();
    await render(<Checkbox checked={false} disabled onChange={onChange} testID="check" />);

    await fireEvent.press(getCheckbox());

    expect(onChange).not.toHaveBeenCalled();
    expect(getCheckbox()).toBeDisabled();
    expect(getCheckbox().props.accessibilityState).toEqual({ checked: false, disabled: true });
  });

  it('es cuadrado: 20×20 dp, borde activo y radios 0 (sin pills)', async () => {
    await render(<Checkbox checked={false} testID="check" />);

    const className = getControl().props.className as string;
    expect(className).toContain('h-[20px]');
    expect(className).toContain('w-[20px]');
    expect(className).toContain('border-active');
    expect(className).toContain('rounded-none');
    for (const radius of [
      'rounded-sm',
      'rounded-base',
      'rounded-md',
      'rounded-lg',
      'rounded-xl',
      'rounded-full',
    ]) {
      expect(className).not.toContain(radius);
    }
  });

  it('se rellena en primary cuando está marcado', async () => {
    await render(<Checkbox checked testID="check" />);

    const className = getControl().props.className as string;
    expect(className).toContain('bg-primary');
    expect(className).toContain('border-primary');
  });

  it('renderiza la etiqueta textual y permite pulsar desde ella', async () => {
    const onChange = jest.fn();
    await render(<Checkbox checked={false} onChange={onChange} label="Acepto" testID="check" />);

    expect(screen.getByText('Acepto')).toBeOnTheScreen();
    await fireEvent.press(screen.getByText('Acepto'));

    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('combina el className del consumidor con el del componente', async () => {
    await render(<Checkbox checked={false} className="mt-md opacity-70" testID="check" />);

    const className = getCheckbox().props.className as string;
    expect(className).toContain('flex-row');
    expect(className).toContain('mt-md');
    expect(className).toContain('opacity-70');
  });
});
