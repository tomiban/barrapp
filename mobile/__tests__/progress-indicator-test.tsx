import { render, screen } from '@testing-library/react-native';

import { ProgressIndicator } from '../src/design-system/ProgressIndicator';

/**
 * ProgressIndicator: barra/anillo sobre los tokens del design system.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build) más el
 * comportamiento accesible (rol, valor y etiqueta), no el estilo computado.
 */
const getProgress = () => screen.getByRole('progressbar');

describe('ProgressIndicator', () => {
  it('expone el rol progressbar y el valor accesible por defecto (0..1)', async () => {
    await render(<ProgressIndicator value={0.5} testID="progress" />);

    expect(getProgress()).toBeOnTheScreen();
    expect(getProgress().props.accessibilityValue).toEqual({ min: 0, max: 1, now: 0.5 });
  });

  it('fija el ancho de la barra a la proporción indicada', async () => {
    await render(<ProgressIndicator value={0.25} testID="progress" />);

    expect(screen.getByTestId('progress-fill')).toHaveStyle({ width: '25%' });
  });

  it('normaliza value/max cuando se provee max', async () => {
    await render(<ProgressIndicator value={5} max={10} testID="progress" />);

    expect(getProgress().props.accessibilityValue).toEqual({ min: 0, max: 10, now: 5 });
    expect(screen.getByTestId('progress-fill')).toHaveStyle({ width: '50%' });
  });

  it('recorta defensivamente un value por encima de 1', async () => {
    await render(<ProgressIndicator value={2} testID="progress" />);

    expect(getProgress().props.accessibilityValue).toEqual({ min: 0, max: 1, now: 1 });
    expect(screen.getByTestId('progress-fill')).toHaveStyle({ width: '100%' });
  });

  it('recorta defensivamente un value por debajo de 0', async () => {
    await render(<ProgressIndicator value={-1} testID="progress" />);

    expect(getProgress().props.accessibilityValue).toEqual({ min: 0, max: 1, now: 0 });
    expect(screen.getByTestId('progress-fill')).toHaveStyle({ width: '0%' });
  });

  it('trata un value no numérico como 0', async () => {
    await render(<ProgressIndicator value={Number.NaN} testID="progress" />);

    expect(getProgress().props.accessibilityValue).toEqual({ min: 0, max: 1, now: 0 });
  });

  it.each([
    ['active', 'bg-primary'],
    ['confirmed', 'bg-secondary'],
    ['error', 'bg-error'],
    ['inactive', 'bg-text-muted'],
  ] as const)('aplica el color del rol %s', async (role, expectedClass) => {
    await render(<ProgressIndicator value={0.5} role={role} testID="progress" />);

    expect(screen.getByTestId('progress-fill')).toHaveProp(
      'className',
      expect.stringContaining(expectedClass),
    );
  });

  it('usa primary como rol por defecto (estado activo/en curso)', async () => {
    await render(<ProgressIndicator value={0.5} testID="progress" />);

    expect(screen.getByTestId('progress-fill')).toHaveProp(
      'className',
      expect.stringContaining('bg-primary'),
    );
  });

  it('muestra la etiqueta textual y la asocia al rol cuando se provee', async () => {
    await render(<ProgressIndicator value={0.5} label="SERIE 2/4" testID="progress" />);

    expect(screen.getByText('SERIE 2/4')).toBeOnTheScreen();
    expect(getProgress().props.accessibilityLabel).toBe('SERIE 2/4');
  });

  it('no añade texto cuando no hay label (evita ruido)', async () => {
    await render(<ProgressIndicator value={0.5} testID="progress" />);

    expect(screen.queryByText('SERIE 2/4')).toBeNull();
  });

  it('no usa sombras: la profundidad sale de bordes y capas tonales', async () => {
    await render(<ProgressIndicator value={0.5} testID="progress" />);

    expect(screen.getByTestId('progress-fill').props.className).not.toContain('shadow');
    expect(getProgress().props.className).not.toContain('shadow');
  });

  it('renderiza la variante anillo con el mismo rol y valor accesibles', async () => {
    await render(<ProgressIndicator value={0.75} variant="ring" size={64} testID="progress" />);

    expect(getProgress()).toBeOnTheScreen();
    expect(getProgress().props.accessibilityValue).toEqual({ min: 0, max: 1, now: 0.75 });
    expect(screen.getByTestId('progress-ring')).toBeOnTheScreen();
  });
});
