import { render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { BiomechanicalCard, CardSection } from './BiomechanicalCard';

/**
 * BiomechanicalCard (ticket #39): superficie, compartimentos y status notch.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** (lo que consume el build) y el
 * comportamiento accesible (encabezado y etiqueta textual de estado), no el
 * estilo computado. Mismo criterio que ProgressIndicator (#42) y layout (#34).
 */
describe('BiomechanicalCard', () => {
  it('renderiza el título como encabezado y los hijos', async () => {
    await render(
      <BiomechanicalCard title="Sentadilla búlgara" testID="card">
        <CardSection>
          <Text>4 x 8 por pierna</Text>
        </CardSection>
      </BiomechanicalCard>,
    );

    expect(screen.getByRole('header', { name: 'Sentadilla búlgara' })).toBeOnTheScreen();
    expect(screen.getByText('4 x 8 por pierna')).toBeOnTheScreen();
  });

  it('acepta un header a medida en lugar del título por defecto', async () => {
    await render(
      <BiomechanicalCard
        title="ignorado"
        header={
          <Text testID="custom-header" accessibilityRole="header">
            Semana 2
          </Text>
        }
        testID="card"
      >
        <Text>bloque</Text>
      </BiomechanicalCard>,
    );

    expect(screen.getByTestId('custom-header')).toBeOnTheScreen();
    expect(screen.queryByText('ignorado')).toBeNull();
  });

  it('separa cada compartimento con un hairline de 1 px', async () => {
    await render(
      <BiomechanicalCard title="Bloque" testID="card">
        <CardSection>
          <Text>uno</Text>
        </CardSection>
        <CardSection>
          <Text>dos</Text>
        </CardSection>
      </BiomechanicalCard>,
    );

    for (const compartment of [0, 1]) {
      const className = screen.getByTestId(`card-compartment-${compartment}`).props.className;
      expect(className).toContain('border-t');
      expect(className).toContain('border-border');
    }
  });

  it('usa la superficie, el borde hairline y el radio del DS', async () => {
    await render(
      <BiomechanicalCard title="Bloque" testID="card">
        <Text>contenido</Text>
      </BiomechanicalCard>,
    );

    const className = screen.getByTestId('card').props.className;
    expect(className).toContain('bg-surface');
    expect(className).toContain('border');
    expect(className).toContain('border-border');
    expect(className).toContain('rounded-base');
  });

  it.each([
    ['active', 'bg-primary'],
    ['confirmed', 'bg-secondary'],
    ['error', 'bg-error'],
    ['inactive', 'bg-text-muted'],
  ] as const)('colorea el status notch con el rol %s', async (status, expectedClass) => {
    await render(
      <BiomechanicalCard title="Bloque" status={status} testID="card">
        <Text>contenido</Text>
      </BiomechanicalCard>,
    );

    expect(screen.getByTestId('card-status', { includeHiddenElements: true })).toHaveProp(
      'className',
      expect.stringContaining(expectedClass),
    );
  });

  it('no añade el notch cuando no hay status', async () => {
    await render(
      <BiomechanicalCard title="Bloque" testID="card">
        <Text>contenido</Text>
      </BiomechanicalCard>,
    );

    expect(screen.queryByTestId('card-status')).toBeNull();
  });

  it('muestra la etiqueta textual del estado, no solo el color', async () => {
    await render(
      <BiomechanicalCard title="Bloque" status="error" statusLabel="FALLO" testID="card">
        <Text>contenido</Text>
      </BiomechanicalCard>,
    );

    expect(screen.getByText('FALLO')).toBeOnTheScreen();
  });

  it('no usa sombras: la profundidad sale de bordes y capas tonales', async () => {
    await render(
      <BiomechanicalCard title="Bloque" status="active" testID="card">
        <Text>contenido</Text>
      </BiomechanicalCard>,
    );

    expect(screen.getByTestId('card').props.className).not.toContain('shadow');
    expect(
      screen.getByTestId('card-status', { includeHiddenElements: true }).props.className,
    ).not.toContain('shadow');
    expect(screen.getByTestId('card-compartment-0').props.className).not.toContain('shadow');
  });
});
