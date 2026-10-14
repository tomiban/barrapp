import { fireEvent, render, screen } from '@testing-library/react-native';

import { Stepper } from './Stepper';

/**
 * Stepper (spec 0002/0003, ticket #90): control de valor numérico con botones
 * `−`/`+` para los formularios de onboarding y perfil.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el comportamiento accesible (etiquetas, valor y cotas) y el
 * contrato de clases, no el estilo computado. Mismo criterio que el resto del
 * design system.
 */
describe('Stepper', () => {
  it('shows the label, the value and the unit', async () => {
    await render(<Stepper label="Peso" value={74} unit="KG" onChange={() => {}} testID="weight" />);

    expect(screen.getByText('Peso')).toBeOnTheScreen();
    expect(screen.getByTestId('weight-value')).toHaveTextContent('74');
    expect(screen.getByText('KG')).toBeOnTheScreen();
  });

  it('adds one step on `+`', async () => {
    const onChange = jest.fn();
    await render(
      <Stepper label="Peso" value={74} min={30} max={200} onChange={onChange} testID="weight" />,
    );

    await fireEvent.press(screen.getByTestId('weight-increase'));

    expect(onChange).toHaveBeenCalledWith(75);
  });

  it('subtracts one step on `−`', async () => {
    const onChange = jest.fn();
    await render(
      <Stepper label="Peso" value={74} min={30} max={200} onChange={onChange} testID="weight" />,
    );

    await fireEvent.press(screen.getByTestId('weight-decrease'));

    expect(onChange).toHaveBeenCalledWith(73);
  });

  it('honours a step other than one', async () => {
    const onChange = jest.fn();
    await render(
      <Stepper label="Peso" value={74} step={0.5} onChange={onChange} testID="weight" />,
    );

    await fireEvent.press(screen.getByTestId('weight-increase'));

    expect(onChange).toHaveBeenCalledWith(74.5);
  });

  it('does not go below the minimum: disables `−` and does not notify', async () => {
    const onChange = jest.fn();
    await render(
      <Stepper label="Peso" value={30} min={30} max={200} onChange={onChange} testID="weight" />,
    );

    const decrease = screen.getByTestId('weight-decrease');
    expect(decrease).toBeDisabled();

    await fireEvent.press(decrease);
    expect(onChange).not.toHaveBeenCalled();
  });

  it('does not go above the maximum: disables `+` and does not notify', async () => {
    const onChange = jest.fn();
    await render(
      <Stepper label="Peso" value={200} min={30} max={200} onChange={onChange} testID="weight" />,
    );

    const increase = screen.getByTestId('weight-increase');
    expect(increase).toBeDisabled();

    await fireEvent.press(increase);
    expect(onChange).not.toHaveBeenCalled();
  });

  it('can be disabled entirely', async () => {
    await render(<Stepper label="Peso" value={74} onChange={() => {}} disabled testID="weight" />);

    expect(screen.getByTestId('weight-decrease')).toBeDisabled();
    expect(screen.getByTestId('weight-increase')).toBeDisabled();
  });

  it('announces each button with its action and the label', async () => {
    await render(<Stepper label="Peso" value={74} onChange={() => {}} testID="weight" />);

    expect(screen.getByLabelText('Disminuir Peso')).toBeOnTheScreen();
    expect(screen.getByLabelText('Aumentar Peso')).toBeOnTheScreen();
  });

  it('shows the error as an announceable alert', async () => {
    await render(
      <Stepper
        label="Peso"
        value={10}
        min={30}
        max={200}
        onChange={() => {}}
        error="El peso debe estar entre 30 y 200 kg."
        testID="weight"
      />,
    );

    const error = screen.getByText('El peso debe estar entre 30 y 200 kg.');
    expect(error).toBeOnTheScreen();
    expect(error).toHaveProp('accessibilityRole', 'alert');
  });

  it('uses no shadows: depth comes from borders and tonal layers', async () => {
    await render(<Stepper label="Peso" value={74} onChange={() => {}} testID="weight" />);

    expect(screen.getByTestId('weight').props.className).not.toContain('shadow');
    expect(screen.getByTestId('weight-increase').props.className).not.toContain('shadow');
  });
});
