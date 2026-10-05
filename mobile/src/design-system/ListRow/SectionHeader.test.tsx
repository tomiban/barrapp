import { render, screen } from '@testing-library/react-native';

import { Text } from '../Text';
import { SectionHeader } from './SectionHeader';

/**
 * `SectionHeader` del design system (ticket #44): encabezado técnico y
 * reutilizable para listas de semanas, sesiones y ejercicios.
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** que consume el build (como en el resto de
 * tests del DS) y el comportamiento accesible (rol de encabezado), no el estilo
 * computado.
 */
describe('SectionHeader', () => {
  it('renderiza la etiqueta como encabezado accesible', async () => {
    await render(<SectionHeader label="Semanas" testID="header" />);

    expect(screen.getByText('Semanas')).toBeOnTheScreen();
    expect(screen.getByRole('header', { name: 'Semanas' })).toBeOnTheScreen();
  });

  it('usa la escala técnica para la etiqueta', async () => {
    await render(<SectionHeader label="Semanas" />);

    expect(screen.getByText('Semanas')).toHaveProp(
      'className',
      expect.stringContaining('text-label-technical'),
    );
  });

  it('muestra el contador cuando se provee', async () => {
    await render(<SectionHeader label="Semanas" count={4} testID="header" />);

    expect(screen.getByTestId('header-count')).toBeOnTheScreen();
    expect(screen.getByText('4')).toBeOnTheScreen();
  });

  it('no muestra contador si se omite', async () => {
    await render(<SectionHeader label="Semanas" testID="header" />);

    expect(screen.queryByTestId('header-count')).not.toBeOnTheScreen();
  });

  it('acepta el contador como texto', async () => {
    await render(<SectionHeader label="Ejercicios" count="12" testID="header" />);

    expect(screen.getByTestId('header-count')).toHaveTextContent('12');
  });

  it('muestra el contador 0 (no lo confunde con ausencia)', async () => {
    await render(<SectionHeader label="Semanas" count={0} testID="header" />);

    expect(screen.getByTestId('header-count')).toBeOnTheScreen();
    expect(screen.getByText('0')).toBeOnTheScreen();
  });

  it('renderiza el slot trailing', async () => {
    await render(
      <SectionHeader
        label="Semanas"
        trailing={<Text testID="trailing">ver todo</Text>}
        testID="header"
      />,
    );

    expect(screen.getByTestId('trailing')).toBeOnTheScreen();
  });

  it('subraya con el hairline del DS', async () => {
    await render(<SectionHeader label="Semanas" testID="header" />);

    const className = screen.getByTestId('header').props.className as string;
    expect(className).toContain('border-b');
    expect(className).toContain('border-border');
    expect(className).not.toContain('shadow');
  });

  it('fusiona el className del consumidor', async () => {
    await render(<SectionHeader label="Semanas" className="mt-lg" testID="header" />);

    expect(screen.getByTestId('header').props.className).toContain('mt-lg');
    expect(screen.getByTestId('header').props.className).toContain('border-b');
  });
});
