import { render, screen } from '@testing-library/react-native';

import { DesignSystemPreview } from '@/components/DesignSystemPreview';

/**
 * Contrato de tokens del preview (ticket #32).
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build), no el estilo
 * computado. Ver `docs` de testing del milestone.
 */
describe('DesignSystemPreview', () => {
  it('usa tokens de color, espaciado, radio y borde en el contenedor', async () => {
    await render(<DesignSystemPreview />);

    const preview = screen.getByTestId('design-system-preview');
    expect(preview).toHaveProp('className', expect.stringContaining('bg-canvas'));
    expect(preview).toHaveProp('className', expect.stringContaining('p-margin'));
    expect(preview).toHaveProp('className', expect.stringContaining('rounded-lg'));
    expect(preview).toHaveProp('className', expect.stringContaining('border-border'));
    expect(preview).toHaveProp('className', expect.stringContaining('gap-md'));
  });

  it('usa la escala tipográfica y las familias display/mono', async () => {
    await render(<DesignSystemPreview />);

    expect(screen.getByText('Nivel 1 · celdas y paneles')).toHaveProp(
      'className',
      expect.stringContaining('font-display'),
    );
    expect(screen.getByText('BARRAPP · DESIGN SYSTEM')).toHaveProp(
      'className',
      expect.stringContaining('text-label-technical'),
    );

    const metric = screen.getByText('60');
    expect(metric).toHaveProp('className', expect.stringContaining('font-mono-bold'));
    expect(metric).toHaveProp('className', expect.stringContaining('text-headline-metric'));
  });

  it('reserva primary y el borde activo para la acción primaria', async () => {
    await render(<DesignSystemPreview />);

    const action = screen.getByTestId('primary-action');
    expect(action).toHaveProp('className', expect.stringContaining('bg-primary'));
    expect(action).toHaveProp('className', expect.stringContaining('border-active'));
    expect(action).toHaveProp('className', expect.stringContaining('border-primary'));
  });
});
