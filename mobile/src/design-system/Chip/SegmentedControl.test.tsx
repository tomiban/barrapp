import { useState } from 'react';

import { render, screen, userEvent } from '@testing-library/react-native';

import { SegmentedControl, type SegmentedOption } from './SegmentedControl';

/**
 * SegmentedControl: control segmentado tipo rocker switch (ticket #37).
 *
 * Comprueba el contrato de clases (Uniwind no resuelve en Jest) y, sobre todo,
 * la semántica accesible de grupo de radios y la **selección única**.
 */
const options: SegmentedOption[] = [
  { value: 'fuerza', label: 'FUERZA' },
  { value: 'resistencia', label: 'RESISTENCIA' },
];

function ControlledSegmented({ initial = 'fuerza' }: { initial?: string }) {
  const [value, setValue] = useState(initial);

  return <SegmentedControl options={options} value={value} onChange={setValue} />;
}

describe('SegmentedControl', () => {
  it('es un radiogroup con una radio por opción', async () => {
    await render(
      <SegmentedControl options={options} value="fuerza" onChange={() => {}} testID="segment" />,
    );

    // El contenedor no usa `accessible` para no agrupar y ocultar las radios:
    // se comprueba su rol por contrato de props.
    expect(screen.getByTestId('segment')).toHaveProp('accessibilityRole', 'radiogroup');
    expect(screen.getAllByRole('radio')).toHaveLength(2);
  });

  it('marca solo la opción elegida', async () => {
    await render(<SegmentedControl options={options} value="fuerza" onChange={() => {}} />);

    expect(screen.getByRole('radio', { name: 'FUERZA' })).toBeSelected();
    expect(screen.getByRole('radio', { name: 'RESISTENCIA' })).not.toBeSelected();
  });

  it('mantiene selección única: al elegir otra se deselecciona la anterior', async () => {
    await render(<ControlledSegmented />);
    const fuerza = screen.getByRole('radio', { name: 'FUERZA' });
    const resistencia = screen.getByRole('radio', { name: 'RESISTENCIA' });
    expect(fuerza).toBeSelected();

    await userEvent.setup().press(resistencia);

    expect(resistencia).toBeSelected();
    expect(fuerza).not.toBeSelected();
  });

  it('emite onChange con el value de la opción pulsada', async () => {
    const onChange = jest.fn();
    await render(<SegmentedControl options={options} value="fuerza" onChange={onChange} />);

    await userEvent.setup().press(screen.getByRole('radio', { name: 'RESISTENCIA' }));

    expect(onChange).toHaveBeenCalledWith('resistencia');
  });

  it('distingue activo (primary/on-primary) de inactivo (surface-muted/text-muted)', async () => {
    await render(
      <SegmentedControl options={options} value="fuerza" onChange={() => {}} testID="segment" />,
    );

    expect(screen.getByTestId('segment-fuerza')).toHaveProp(
      'className',
      expect.stringContaining('bg-primary'),
    );
    expect(screen.getByTestId('segment-resistencia')).toHaveProp(
      'className',
      expect.stringContaining('bg-surface-muted'),
    );
    expect(screen.getByText('FUERZA')).toHaveProp(
      'className',
      expect.stringContaining('text-on-primary'),
    );
    expect(screen.getByText('RESISTENCIA')).toHaveProp(
      'className',
      expect.stringContaining('text-text-muted'),
    );
  });

  it('no usa sombras ni pills', async () => {
    await render(
      <SegmentedControl options={options} value="fuerza" onChange={() => {}} testID="segment" />,
    );

    const className = screen.getByTestId('segment').props.className;
    expect(className).not.toContain('shadow');
    expect(className).not.toContain('rounded-full');
  });

  it('fusiona el className del consumidor en el contenedor', async () => {
    await render(
      <SegmentedControl
        options={options}
        value="fuerza"
        onChange={() => {}}
        className="mt-lg"
        testID="segment"
      />,
    );

    expect(screen.getByTestId('segment')).toHaveProp('className', expect.stringContaining('mt-lg'));
  });
});
