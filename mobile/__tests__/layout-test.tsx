import { render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { Box, Grid, GridItem, Spacer, Stack } from '../src/design-system/layout';

/**
 * Primitivas de layout del design system (ticket #34).
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo:
 * se comprueba el **contrato de clases** (lo que consume el build) y que los
 * hijos se renderizan. Es el mismo criterio que los tests de tokens (#32).
 */
describe('Box', () => {
  it('renderiza sus hijos', async () => {
    await render(
      <Box testID="box">
        <Text>contenido</Text>
      </Box>,
    );

    expect(screen.getByText('contenido')).toBeOnTheScreen();
  });

  it('pasa el className al View subyacente', async () => {
    await render(<Box testID="box" className="bg-surface p-md" />);

    expect(screen.getByTestId('box')).toHaveProp(
      'className',
      expect.stringContaining('bg-surface'),
    );
  });
});

describe('Stack', () => {
  it('apila en columna con gap `md` por defecto', async () => {
    await render(
      <Stack testID="stack">
        <Text>uno</Text>
      </Stack>,
    );

    const className = screen.getByTestId('stack').props.className;
    expect(className).toContain('flex-col');
    expect(className).toContain('gap-md');
  });

  it('dispone en fila y acepta un gap de la escala', async () => {
    await render(<Stack testID="stack" direction="row" gap="lg" />);

    const className = screen.getByTestId('stack').props.className;
    expect(className).toContain('flex-row');
    expect(className).toContain('gap-lg');
  });

  it('permite al consumidor sobrescribir el gap con su className', async () => {
    await render(<Stack testID="stack" gap="sm" className="gap-xl" />);

    const className = screen.getByTestId('stack').props.className;
    expect(className).toContain('gap-xl');
    expect(className).not.toContain('gap-sm');
  });
});

describe('Spacer', () => {
  it('ocupa el espacio libre por defecto', async () => {
    await render(<Spacer testID="spacer" />);

    expect(screen.getByTestId('spacer').props.className).toContain('flex-1');
  });

  it('con `size` reserva un hueco fijo de la escala', async () => {
    await render(<Spacer testID="spacer" size="lg" />);

    const className = screen.getByTestId('spacer').props.className;
    expect(className).toContain('basis-lg');
    expect(className).not.toContain('flex-1');
  });
});

describe('Grid', () => {
  it('usa la retícula de 4 columnas con gutter y margen de los tokens', async () => {
    await render(<Grid testID="grid" />);

    const className = screen.getByTestId('grid').props.className;
    expect(className).toContain('flex-row');
    expect(className).toContain('flex-wrap');
    expect(className).toContain('-mx-sm');
    expect(className).toContain('px-margin');
  });

  it('puede omitir el margen de página', async () => {
    await render(<Grid testID="grid" margin={false} />);

    expect(screen.getByTestId('grid').props.className).not.toContain('px-margin');
  });

  it('renderiza sus celdas', async () => {
    await render(
      <Grid>
        <GridItem testID="cell">
          <Text>dato</Text>
        </GridItem>
      </Grid>,
    );

    expect(screen.getByText('dato')).toBeOnTheScreen();
  });
});

describe('GridItem', () => {
  it('ocupa una columna de las cuatro por defecto', async () => {
    await render(<GridItem testID="cell" />);

    const className = screen.getByTestId('cell').props.className;
    expect(className).toContain('basis-1/4');
    expect(className).toContain('px-sm');
  });

  it('puede ocupar varias columnas con `span`', async () => {
    await render(<GridItem testID="cell" span={2} />);

    const className = screen.getByTestId('cell').props.className;
    expect(className).toContain('basis-1/2');
    expect(className).not.toContain('basis-1/4');
  });
});
