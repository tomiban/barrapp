import { fireEvent, render, screen } from '@testing-library/react-native';

import type { SemanticRole } from '../semantic';
import { Text } from '../Text';
import { ListRow } from './ListRow';

/**
 * `ListRow` del design system (ticket #44): fila de semanas, sesiones y
 * ejercicios.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** que consume el build (como en el resto de
 * tests del DS) y el comportamiento accesible (rol, `onPress` y estado textual),
 * nunca el estilo computado.
 */

/*
 * Contrato de tokens de la spec (docs/specs/0002-design-system.md), escrito a
 * mano como fuente de verdad independiente: rol semántico → token de acento.
 * Regla de la spec: el estado **nunca** se comunica solo por color.
 */
const STATE_MARK: Record<SemanticRole, string> = {
  active: 'bg-primary',
  confirmed: 'bg-secondary',
  error: 'bg-error',
  inactive: 'bg-text-muted',
};

const DEFAULT_LABEL: Record<SemanticRole, string> = {
  active: 'En curso',
  confirmed: 'Confirmado',
  error: 'Fallo',
  inactive: 'Inactivo',
};

const STATES = Object.keys(STATE_MARK) as SemanticRole[];

describe('ListRow', () => {
  it('renderiza título y subtítulo', async () => {
    await render(<ListRow title="Semana 1" subtitle="Acumulación" testID="row" />);

    expect(screen.getByText('Semana 1')).toBeOnTheScreen();
    expect(screen.getByText('Acumulación')).toBeOnTheScreen();
  });

  it('renderiza los slots leading y trailing', async () => {
    await render(
      <ListRow
        title="Semana 1"
        leading={<Text testID="leading">1</Text>}
        trailing={<Text testID="trailing">8/12</Text>}
        testID="row"
      />,
    );

    expect(screen.getByTestId('leading')).toBeOnTheScreen();
    expect(screen.getByTestId('trailing')).toBeOnTheScreen();
  });

  it('dispara onPress al pulsar la fila', async () => {
    const onPress = jest.fn();
    await render(<ListRow title="Semana 1" onPress={onPress} testID="row" />);

    fireEvent.press(screen.getByTestId('row'));

    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('es un botón cuando hay onPress y un texto cuando no', async () => {
    await render(<ListRow title="Semana 1" onPress={jest.fn()} testID="pressable" />);
    expect(screen.getByTestId('pressable').props.accessibilityRole).toBe('button');

    await render(<ListRow title="Semana 1" testID="static" />);
    expect(screen.getByTestId('static').props.accessibilityRole).toBe('text');
  });

  it.each(STATES)('aplica el token de acento del estado %s al marcador', async (state) => {
    await render(<ListRow title="Semana 1" role={state} testID="row" />);

    expect(
      screen.getByTestId('row-state', { includeHiddenElements: true }).props.className,
    ).toContain(STATE_MARK[state]);
  });

  it.each(STATES)('anuncia el estado %s textualmente, nunca solo por color', async (state) => {
    await render(<ListRow title="Semana 1" role={state} testID="row" />);

    // Etiqueta visible: el estado no depende del color.
    expect(screen.getByText(DEFAULT_LABEL[state])).toBeOnTheScreen();
    // Y también se anuncia al lector de pantalla.
    expect(screen.getByTestId('row').props.accessibilityLabel).toContain(DEFAULT_LABEL[state]);
  });

  it('acepta una etiqueta de estado propia', async () => {
    await render(<ListRow title="Semana 1" role="error" stateLabel="Sobrecarga" testID="row" />);

    expect(screen.getByText('Sobrecarga')).toBeOnTheScreen();
    expect(screen.queryByText(DEFAULT_LABEL.error)).not.toBeOnTheScreen();
    expect(screen.getByTestId('row').props.accessibilityLabel).toContain('Sobrecarga');
  });

  it('ignora una etiqueta de estado en blanco y usa la del rol', async () => {
    await render(<ListRow title="Semana 1" role="active" stateLabel="   " testID="row" />);

    expect(screen.getByText(DEFAULT_LABEL.active)).toBeOnTheScreen();
  });

  it('respeta el tamaño táctil secundario de la spec (48 dp)', async () => {
    await render(<ListRow title="Semana 1" testID="row" />);

    expect(screen.getByTestId('row').props.className).toContain('min-h-control-secondary');
  });

  it('no muestra marcador ni etiqueta de estado si no hay state', async () => {
    await render(<ListRow title="Semana 1" testID="row" />);

    expect(
      screen.queryByTestId('row-state', { includeHiddenElements: true }),
    ).not.toBeOnTheScreen();
    expect(screen.getByTestId('row').props.accessibilityLabel).toBeUndefined();
  });

  it('dibuja el separador hairline por defecto', async () => {
    await render(<ListRow title="Semana 1" testID="row" />);

    const className = screen.getByTestId('row').props.className as string;
    expect(className).toContain('border-b');
    expect(className).toContain('border-border');
  });

  it('omite el separador en la última fila', async () => {
    await render(<ListRow title="Semana 1" last testID="row" />);

    expect(screen.getByTestId('row').props.className).not.toContain('border-b');
  });

  it('no usa sombras: la profundidad sale de bordes y capas tonales', async () => {
    await render(<ListRow title="Semana 1" role="active" testID="row" />);

    expect(screen.getByTestId('row').props.className).not.toContain('shadow');
    expect(
      screen.getByTestId('row-state', { includeHiddenElements: true }).props.className,
    ).not.toContain('shadow');
  });

  it('fusiona el className del consumidor', async () => {
    await render(<ListRow title="Semana 1" className="mt-lg bg-surface" testID="row" />);

    expect(screen.getByTestId('row').props.className).toContain('mt-lg');
    expect(screen.getByTestId('row').props.className).toContain('bg-surface');
  });
});
