import { useState } from 'react';

import { render, screen, userEvent } from '@testing-library/react-native';

import { Chip } from './Chip';

/**
 * Chip: toggle del design system (ticket #37).
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build), el rol y
 * estado accesible, y el comportamiento (`onChange`), no el estilo computado.
 */
function ControlledChip({ initial = false }: { initial?: boolean }) {
  const [selected, setSelected] = useState(initial);

  return <Chip label="FUERZA" selected={selected} onChange={setSelected} />;
}

describe('Chip', () => {
  it('expone el rol button con nombre accesible', async () => {
    await render(<Chip label="FUERZA" />);

    expect(screen.getByRole('button', { name: 'FUERZA' })).toBeOnTheScreen();
  });

  it('comunica el estado seleccionado (no solo por color)', async () => {
    await render(<Chip label="FUERZA" selected />);

    expect(screen.getByRole('button', { name: 'FUERZA' })).toBeSelected();
  });

  it('no marca seleccionado un chip inactivo', async () => {
    await render(<Chip label="FUERZA" />);

    expect(screen.getByRole('button', { name: 'FUERZA' })).not.toBeSelected();
  });

  it('alterna el estado al pulsar (controlado)', async () => {
    await render(<ControlledChip />);
    const chip = screen.getByRole('button', { name: 'FUERZA' });
    expect(chip).not.toBeSelected();

    await userEvent.setup().press(chip);

    expect(chip).toBeSelected();
  });

  it('emite onChange con el nuevo selected', async () => {
    const onChange = jest.fn();
    await render(<Chip label="FUERZA" selected={false} onChange={onChange} />);

    await userEvent.setup().press(screen.getByRole('button', { name: 'FUERZA' }));

    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('usa surface-muted/text-muted en inactivo', async () => {
    await render(<Chip label="FUERZA" testID="chip" />);

    expect(screen.getByTestId('chip')).toHaveProp(
      'className',
      expect.stringContaining('bg-surface-muted'),
    );
    expect(screen.getByText('FUERZA')).toHaveProp(
      'className',
      expect.stringContaining('text-text-muted'),
    );
  });

  it('invierte a primary/on-primary en activo', async () => {
    await render(<Chip label="FUERZA" selected testID="chip" />);

    expect(screen.getByTestId('chip')).toHaveProp(
      'className',
      expect.stringContaining('bg-primary'),
    );
    expect(screen.getByText('FUERZA')).toHaveProp(
      'className',
      expect.stringContaining('text-on-primary'),
    );
  });

  it('no usa sombras ni pills', async () => {
    await render(<Chip label="FUERZA" testID="chip" />);

    const className = screen.getByTestId('chip').props.className;
    expect(className).not.toContain('shadow');
    expect(className).not.toContain('rounded-full');
  });

  it('fusiona el className del consumidor', async () => {
    await render(<Chip label="FUERZA" testID="chip" className="self-start" />);

    expect(screen.getByTestId('chip')).toHaveProp(
      'className',
      expect.stringContaining('self-start'),
    );
  });
});
