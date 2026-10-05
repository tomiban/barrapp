import { fireEvent, render, screen } from '@testing-library/react-native';

import { Radio } from './Radio';

/**
 * `Radio` (spec 0002, ticket #38): **cuadrado**, no círculo. Misma caja de
 * 20×20 dp con radios 0 y borde activo (1.5 px); al activarse muestra un bloque
 * interior sólido en `primary`.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build) y el
 * comportamiento accesible.
 */
const getRadio = () => screen.getByRole('radio');
const getControl = () => screen.getByTestId('choice-control');
const queryInner = () => screen.queryByTestId('choice-inner');

describe('Radio', () => {
  it('arranca inactivo y con el estado accesible por defecto', async () => {
    await render(<Radio checked={false} testID="choice" />);

    expect(getRadio()).toBeOnTheScreen();
    expect(getRadio()).not.toBeChecked();
    expect(getRadio().props.accessibilityState).toEqual({ checked: false, disabled: false });
  });

  it('refleja el estado activo y muestra el bloque interior (controlado)', async () => {
    await render(<Radio checked testID="choice" />);

    expect(getRadio()).toBeChecked();
    expect(queryInner()).toBeOnTheScreen();
  });

  it('no renderiza el bloque interior cuando está inactivo', async () => {
    await render(<Radio checked={false} testID="choice" />);

    expect(queryInner()).toBeNull();
  });

  it('es cuadrado, no un círculo: 20×20 dp, radios 0 y sin pills', async () => {
    await render(<Radio checked={false} testID="choice" />);

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

  it('usa un bloque interior cuadrado (radios 0) y primary', async () => {
    await render(<Radio checked testID="choice" />);

    const innerClassName = queryInner()?.props.className as string;
    expect(innerClassName).toContain('h-[10px]');
    expect(innerClassName).toContain('w-[10px]');
    expect(innerClassName).toContain('rounded-none');
    expect(innerClassName).toContain('bg-primary');
    expect(innerClassName).not.toContain('rounded-full');
  });

  it('al pulsar un radio inactivo pide activarlo', async () => {
    const onChange = jest.fn();
    await render(<Radio checked={false} onChange={onChange} testID="choice" />);

    await fireEvent.press(getRadio());

    expect(onChange).toHaveBeenCalledTimes(1);
    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('además de onChange, invoca onPress en una pulsación válida', async () => {
    const onPress = jest.fn();
    await render(<Radio checked={false} onPress={onPress} testID="choice" />);

    await fireEvent.press(getRadio());

    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('no dispara onChange cuando está deshabilitado', async () => {
    const onChange = jest.fn();
    await render(<Radio checked={false} disabled onChange={onChange} testID="choice" />);

    await fireEvent.press(getRadio());

    expect(onChange).not.toHaveBeenCalled();
    expect(getRadio()).toBeDisabled();
    expect(getRadio().props.accessibilityState).toEqual({ checked: false, disabled: true });
  });

  it('renderiza la etiqueta textual y permite pulsar desde ella', async () => {
    const onChange = jest.fn();
    await render(<Radio checked={false} onChange={onChange} label="Opción A" testID="choice" />);

    expect(screen.getByText('Opción A')).toBeOnTheScreen();
    await fireEvent.press(screen.getByText('Opción A'));

    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('combina el className del consumidor con el del componente', async () => {
    await render(<Radio checked={false} className="mt-md" testID="choice" />);

    const className = getRadio().props.className as string;
    expect(className).toContain('flex-row');
    expect(className).toContain('mt-md');
  });
});
