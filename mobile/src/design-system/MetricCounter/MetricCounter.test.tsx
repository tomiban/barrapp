import { render, screen } from '@testing-library/react-native';

import { MetricCounter } from './MetricCounter';

/*
 * Contrato de tokens de la spec (docs/specs/0002-design-system.md), escrito a
 * mano aquí como fuente de verdad independiente: los roles semánticos que puede
 * tomar el readout y los tokens que les corresponden. No se importa el mapa del
 * componente para no validar el código contra sí mismo.
 *
 * `neutral` es el rol por defecto: el dato principal se pinta con `text` (no es
 * un estado, así que no lleva acento).
 */
const TONE_CLASS = {
  neutral: 'text-text',
  active: 'text-primary',
  confirmed: 'text-secondary',
  error: 'text-error',
  inactive: 'text-text-muted',
} as const;

/**
 * MetricCounter (ticket #40): celda modular con micro-label, índice, readout
 * monolítico y unidad.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build) y el
 * comportamiento accesible, no el estilo computado. Mismo criterio que
 * StatusBadge (#43) y BiomechanicalCard (#39).
 */
describe('MetricCounter', () => {
  it('renderiza micro-label, readout, unidad e índice', async () => {
    await render(
      <MetricCounter label="Peso" value={100} unit="KG" index="SEC.01" testID="metric" />,
    );

    expect(screen.getByText('Peso')).toBeOnTheScreen();
    expect(screen.getByText('100')).toBeOnTheScreen();
    expect(screen.getByText('KG')).toBeOnTheScreen();
    expect(screen.getByText('[SEC.01]')).toBeOnTheScreen();
  });

  it('usa la escala tipográfica del DS: micro-label técnica y readout monolítico', async () => {
    await render(<MetricCounter label="Peso" value={100} unit="KG" testID="metric" />);

    expect(screen.getByText('Peso')).toHaveProp(
      'className',
      expect.stringContaining('text-label-technical'),
    );
    expect(screen.getByTestId('metric-value')).toHaveProp(
      'className',
      expect.stringContaining('text-headline-metric'),
    );
    expect(screen.getByTestId('metric-unit')).toHaveProp(
      'className',
      expect.stringContaining('text-label-technical'),
    );
  });

  it('coloca el índice arriba a la derecha en mono', async () => {
    await render(<MetricCounter label="Peso" value={100} index="SEC.01" testID="metric" />);

    const header = screen.getByTestId('metric-header');
    expect(header).toHaveProp('className', expect.stringContaining('flex-row'));
    expect(header).toHaveProp('className', expect.stringContaining('justify-between'));

    expect(screen.getByTestId('metric-index')).toHaveProp(
      'className',
      expect.stringContaining('text-label-code'),
    );
  });

  it('alinea la unidad con el readout por línea base', async () => {
    await render(<MetricCounter label="Peso" value={100} unit="KG" testID="metric" />);

    expect(screen.getByTestId('metric-readout')).toHaveProp(
      'className',
      expect.stringContaining('items-baseline'),
    );
  });

  it.each(Object.entries(TONE_CLASS))(
    'el rol semántico %s pinta el readout con su token',
    async (tone, expectedClass) => {
      await render(
        <MetricCounter
          label="Carga"
          value={80}
          unit="%"
          tone={tone as keyof typeof TONE_CLASS}
          testID="metric"
        />,
      );

      expect(screen.getByTestId('metric-value')).toHaveProp(
        'className',
        expect.stringContaining(expectedClass),
      );
    },
  );

  it('sin rol usa el dato principal neutro (`text`)', async () => {
    await render(<MetricCounter label="Peso" value={100} testID="metric" />);

    expect(screen.getByTestId('metric-value')).toHaveProp(
      'className',
      expect.stringContaining(TONE_CLASS.neutral),
    );
  });

  it('combina label, value y unit en una única etiqueta accesible', async () => {
    await render(
      <MetricCounter label="Peso" value={100} unit="KG" index="SEC.01" testID="metric" />,
    );

    const node = screen.getByLabelText('Peso 100 KG');
    expect(node).toHaveProp('accessibilityRole', 'text');
  });

  it('no deja espacios sobrantes cuando falta la unidad', async () => {
    await render(<MetricCounter label="RPE" value={8} testID="metric" />);

    expect(screen.getByLabelText('RPE 8')).toBeOnTheScreen();
  });

  it('formatea un índice numérico a dos dígitos, con prefijo opcional', async () => {
    const { rerender } = await render(
      <MetricCounter label="Serie" value={1} index={2} testID="metric" />,
    );
    expect(screen.getByText('[02]')).toBeOnTheScreen();

    await rerender(
      <MetricCounter label="Serie" value={1} index={2} indexPrefix="SEC" testID="metric" />,
    );
    expect(screen.getByText('[SEC.02]')).toBeOnTheScreen();
  });

  it('respeta un índice que ya viene entre corchetes', async () => {
    await render(<MetricCounter label="Bloque" value={3} index="[BLQ.01]" testID="metric" />);

    expect(screen.getByText('[BLQ.01]')).toBeOnTheScreen();
    expect(screen.queryByText('[[BLQ.01]]')).toBeNull();
  });

  it('omite el índice cuando no se provee', async () => {
    await render(<MetricCounter label="Peso" value={100} testID="metric" />);

    expect(screen.queryByTestId('metric-index')).toBeNull();
  });

  it('es una celda de nivel 1 con hairline y radio, sin sombras', async () => {
    await render(<MetricCounter label="Peso" value={100} testID="metric" />);

    const className = screen.getByTestId('metric').props.className;
    expect(className).toContain('bg-surface');
    expect(className).toContain('border');
    expect(className).toContain('border-border');
    expect(className).toContain('rounded-base');
    expect(className).not.toContain('shadow');
  });

  it('fusiona el `className` del consumidor y permite sobrescribir el borde', async () => {
    await render(
      <MetricCounter label="Peso" value={100} className="mt-lg border-secondary" testID="metric" />,
    );

    const className = screen.getByTestId('metric').props.className;
    expect(className).toContain('mt-lg');
    expect(className).toContain('border-secondary');
    expect(className).not.toContain('border-border');
  });
});
