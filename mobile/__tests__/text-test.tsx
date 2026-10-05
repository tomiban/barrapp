import { render, screen } from '@testing-library/react-native';

import { Text, type TextVariant } from '../src/design-system/Text';

/**
 * `Text` del design system (ticket #33).
 *
 * En Jest no corre Metro, así que Uniwind **no** resuelve `className` a estilo:
 * se comprueba el **contrato de clases** (lo que consume el build) y el estilo
 * inline de los números tabulares, que sí llega intacto al árbol de React.
 */
describe('Text', () => {
  it('renderiza el contenido con la variante por defecto (bodyMd)', async () => {
    await render(<Text testID="text">hola</Text>);

    expect(screen.getByText('hola')).toBeOnTheScreen();
    expect(screen.getByTestId('text')).toHaveProp(
      'className',
      expect.stringContaining('font-body'),
    );
    expect(screen.getByTestId('text')).toHaveProp(
      'className',
      expect.stringContaining('text-body-md'),
    );
  });

  const cases: {
    variant: TextVariant;
    font: string;
    size: string;
    extra?: string;
  }[] = [
    { variant: 'displayHero', font: 'font-display', size: 'text-display-hero' },
    { variant: 'headlineMetric', font: 'font-mono-bold', size: 'text-headline-metric' },
    { variant: 'headlineLg', font: 'font-display', size: 'text-headline-lg' },
    { variant: 'headlineMd', font: 'font-display-semibold', size: 'text-headline-md' },
    { variant: 'headlineSm', font: 'font-display-semibold', size: 'text-headline-sm' },
    { variant: 'bodyLg', font: 'font-body', size: 'text-body-lg' },
    { variant: 'bodyMd', font: 'font-body', size: 'text-body-md' },
    { variant: 'bodySm', font: 'font-body', size: 'text-body-sm' },
    {
      variant: 'labelTechnical',
      font: 'font-mono-semibold',
      size: 'text-label-technical',
      extra: 'uppercase',
    },
    { variant: 'labelCode', font: 'font-mono-medium', size: 'text-label-code' },
  ];

  it.each(cases)('mapea $variant a $font + $size', async ({ variant, font, size, extra }) => {
    await render(
      <Text testID="text" variant={variant}>
        contenido
      </Text>,
    );

    const element = screen.getByTestId('text');
    expect(screen.getByText('contenido')).toBeOnTheScreen();
    expect(element).toHaveProp('className', expect.stringContaining(font));
    expect(element).toHaveProp('className', expect.stringContaining(size));
    if (extra) {
      expect(element).toHaveProp('className', expect.stringContaining(extra));
    }
  });

  it('usa números tabulares en las métricas para que no haya jitter', async () => {
    await render(
      <Text testID="metric" variant="headlineMetric">
        60
      </Text>,
    );

    expect(screen.getByTestId('metric')).toHaveStyle({ fontVariant: ['tabular-nums'] });
  });

  it('no fuerza números tabulares en las variantes de cuerpo', async () => {
    await render(
      <Text testID="body" variant="bodyMd">
        texto
      </Text>,
    );

    expect(screen.getByTestId('body')).not.toHaveStyle({ fontVariant: ['tabular-nums'] });
  });

  it('combina el className del consumidor con el de la variante', async () => {
    await render(
      <Text testID="text" variant="bodyMd" className="mt-md opacity-70">
        texto
      </Text>,
    );

    const element = screen.getByTestId('text');
    expect(element).toHaveProp('className', expect.stringContaining('font-body'));
    expect(element).toHaveProp('className', expect.stringContaining('text-body-md'));
    expect(element).toHaveProp('className', expect.stringContaining('mt-md'));
    expect(element).toHaveProp('className', expect.stringContaining('opacity-70'));
  });

  it('reenvía las props de React Native Text', async () => {
    const onPress = jest.fn();
    await render(
      <Text testID="text" numberOfLines={1} onPress={onPress}>
        texto
      </Text>,
    );

    expect(screen.getByTestId('text')).toHaveProp('numberOfLines', 1);
  });
});
