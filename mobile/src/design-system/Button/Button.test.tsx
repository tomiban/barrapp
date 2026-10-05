import { fireEvent, render, screen } from '@testing-library/react-native';

import { Button, type ButtonVariant } from './Button';

/**
 * `Button` del design system (ticket #35).
 *
 * En Jest no corre Metro, así que Uniwind no resuelve `className` a estilo: se
 * comprueba el **contrato de clases** que consume el build (como en el resto de
 * tests del DS) y el comportamiento accesible (rol, estado y `onPress`), nunca
 * el estilo computado.
 */

const surface = (testID = 'button') => screen.getByTestId(`${testID}-surface`);

describe('Button', () => {
  it('renderiza la etiqueta con rol de botón', async () => {
    await render(<Button testID="button">Empezar</Button>);

    expect(screen.getByRole('button')).toBeOnTheScreen();
    expect(screen.getByText('Empezar')).toBeOnTheScreen();
  });

  it('usa la variante primary por defecto', async () => {
    await render(<Button testID="button">Empezar</Button>);

    expect(surface().props.className).toContain('bg-primary');
    expect(surface().props.className).toContain('h-14');
  });

  const contract: { variant: ButtonVariant; present: string[]; absent: string[] }[] = [
    {
      variant: 'primary',
      present: ['bg-primary', 'h-14', 'rounded-sm', 'border-active', 'border-primary'],
      absent: ['h-12', 'shadow'],
    },
    {
      variant: 'secondary',
      present: ['bg-secondary', 'h-12', 'rounded-sm'],
      absent: ['h-14', 'border-active', 'shadow'],
    },
    {
      variant: 'tertiary',
      present: ['bg-surface', 'h-12', 'rounded-sm', 'border-active', 'border-border'],
      absent: ['h-14', 'shadow'],
    },
  ];

  it.each(contract)(
    'aplica el contrato de clases de $variant',
    async ({ variant, present, absent }) => {
      await render(
        <Button testID="button" variant={variant}>
          Etiqueta
        </Button>,
      );

      const className = surface().props.className as string;
      for (const cls of present) {
        expect(className).toContain(cls);
      }
      for (const cls of absent) {
        expect(className).not.toContain(cls);
      }
    },
  );

  it('usa la escala técnica para la etiqueta', async () => {
    await render(<Button testID="button">Etiqueta</Button>);

    expect(screen.getByText('Etiqueta')).toHaveProp(
      'className',
      expect.stringContaining('text-label-technical'),
    );
  });

  it('llama a onPress al pulsar', async () => {
    const onPress = jest.fn();
    await render(
      <Button testID="button" onPress={onPress}>
        Empezar
      </Button>,
    );

    fireEvent.press(screen.getByRole('button'));

    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('marca el estado disabled y no dispara onPress', async () => {
    const onPress = jest.fn();
    await render(
      <Button testID="button" disabled onPress={onPress}>
        Empezar
      </Button>,
    );

    expect(screen.getByRole('button').props.accessibilityState).toEqual({ disabled: true });
    fireEvent.press(screen.getByRole('button'));
    expect(onPress).not.toHaveBeenCalled();
  });

  it('expone el estado habilitado cuando no está disabled', async () => {
    await render(<Button testID="button">Empezar</Button>);

    expect(screen.getByRole('button').props.accessibilityState).toEqual({ disabled: false });
  });

  const inversion: {
    variant: ButtonVariant;
    surface: string;
    label: string;
    restSurface: string;
  }[] = [
    {
      variant: 'primary',
      surface: 'bg-on-primary',
      label: 'text-primary',
      restSurface: 'bg-primary',
    },
    {
      variant: 'secondary',
      surface: 'bg-on-secondary',
      label: 'text-secondary',
      restSurface: 'bg-secondary',
    },
    { variant: 'tertiary', surface: 'bg-text', label: 'text-surface', restSurface: 'bg-surface' },
  ];

  it.each(inversion)(
    'invierte fondo y texto en $variant al pulsar',
    async ({ variant, surface: pressedSurface, label, restSurface }) => {
      await render(
        <Button testID="button" variant={variant} testOnly_pressed>
          Etiqueta
        </Button>,
      );

      expect(surface().props.className).toContain(pressedSurface);
      expect(surface().props.className).not.toContain(restSurface);
      expect(screen.getByText('Etiqueta').props.className).toContain(label);
    },
  );

  it('usa text-muted y no invierte cuando está disabled', async () => {
    await render(
      <Button testID="button" variant="primary" disabled testOnly_pressed>
        Etiqueta
      </Button>,
    );

    expect(surface().props.className).toContain('bg-surface');
    expect(surface().props.className).not.toContain('bg-on-primary');
    expect(screen.getByText('Etiqueta').props.className).toContain('text-text-muted');
  });

  it('combina el className del consumidor', async () => {
    await render(
      <Button testID="button" className="mt-md">
        Empezar
      </Button>,
    );

    expect(surface().props.className).toContain('mt-md');
    expect(surface().props.className).toContain('bg-primary');
  });
});
