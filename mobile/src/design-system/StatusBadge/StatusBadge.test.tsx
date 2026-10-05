import { render, screen } from '@testing-library/react-native';

import { StatusBadge } from './StatusBadge';

/*
 * Contrato de tokens de la spec (docs/specs/0002-design-system.md), escrito a
 * mano aquí como fuente de verdad independiente: los cuatro roles semánticos y
 * los tokens que les corresponden. No se importa el mapa del componente para no
 * validar el código contra sí mismo.
 *
 * Regla de la spec: el estado **nunca** se comunica solo por color; siempre hay
 * etiqueta textual.
 */
const ROLES = ['active', 'confirmed', 'error', 'inactive'] as const;

const DEFAULT_LABEL = {
  active: 'En curso',
  confirmed: 'Confirmado',
  error: 'Fallo',
  inactive: 'Inactivo',
} as const;

/** Relleno de la variante sólida por rol. `error` se pinta sobre su contenedor. */
const SOLID_FILL = {
  active: 'bg-primary',
  confirmed: 'bg-secondary',
  error: 'bg-error-container',
  inactive: 'bg-surface-muted',
} as const;

/** Texto legible sobre el relleno sólido de cada rol. */
const SOLID_TEXT = {
  active: 'text-on-primary',
  confirmed: 'text-on-secondary',
  error: 'text-on-error-container',
  inactive: 'text-text-muted',
} as const;

/**
 * Token de acento canónico del rol (borde y punto). Es el mapeo explícito que
 * pide el ticket: active→primary, confirmed→secondary, error→error,
 * inactive→textMuted.
 */
const ACCENT = {
  active: 'border-primary',
  confirmed: 'border-secondary',
  error: 'border-error',
  inactive: 'border-border',
} as const;

const ACCENT_DOT = {
  active: 'bg-primary',
  confirmed: 'bg-secondary',
  error: 'bg-error',
  inactive: 'bg-text-muted',
} as const;

describe('StatusBadge', () => {
  it.each(ROLES)('muestra la etiqueta por defecto en español para el rol %s', async (role) => {
    await render(<StatusBadge role={role} />);

    expect(screen.getByText(DEFAULT_LABEL[role])).toBeOnTheScreen();
  });

  it.each(ROLES)('sólido: el rol %s usa el relleno y el texto de su token', async (role) => {
    await render(<StatusBadge role={role} testID="badge" />);

    const badge = screen.getByTestId('badge');
    expect(badge).toHaveProp('className', expect.stringContaining(SOLID_FILL[role]));
    expect(badge).toHaveProp('className', expect.stringContaining(ACCENT[role]));

    const label = screen.getByText(DEFAULT_LABEL[role]);
    expect(label).toHaveProp('className', expect.stringContaining(SOLID_TEXT[role]));
  });

  it.each(ROLES)('sólido: el punto del rol %s usa su color de acento', async (role) => {
    await render(<StatusBadge role={role} testID="badge" />);

    // El punto es decorativo: se oculta a accesibilidad, así que la consulta
    // tiene que pedir explícitamente los elementos ocultos.
    expect(screen.getByTestId('badge-dot', { includeHiddenElements: true })).toHaveProp(
      'className',
      expect.stringContaining(ACCENT_DOT[role]),
    );
  });

  it.each(ROLES)(
    'contorno: el rol %s se distingue por borde y punto sobre canvas',
    async (role) => {
      await render(<StatusBadge role={role} variant="outline" testID="badge" />);

      const badge = screen.getByTestId('badge');
      expect(badge).toHaveProp('className', expect.stringContaining('bg-transparent'));
      expect(badge).toHaveProp('className', expect.stringContaining(ACCENT[role]));

      expect(screen.getByTestId('badge-dot', { includeHiddenElements: true })).toHaveProp(
        'className',
        expect.stringContaining(ACCENT_DOT[role]),
      );
    },
  );

  it('usa la etiqueta del texto técnico en mayúsculas', async () => {
    await render(<StatusBadge role="active" />);

    const label = screen.getByText(DEFAULT_LABEL.active);
    expect(label).toHaveProp('className', expect.stringContaining('text-label-technical'));
    expect(label).toHaveProp('className', expect.stringContaining('uppercase'));
  });

  it('acepta una etiqueta propia en lugar de la derivada', async () => {
    await render(<StatusBadge role="error" label="Sobrecarga" />);

    expect(screen.getByText('Sobrecarga')).toBeOnTheScreen();
    expect(screen.queryByText(DEFAULT_LABEL.error)).not.toBeOnTheScreen();
  });

  it('nunca queda sin texto: una etiqueta vacía cae al valor por defecto', async () => {
    await render(<StatusBadge role="confirmed" label="   " />);

    expect(screen.getByText(DEFAULT_LABEL.confirmed)).toBeOnTheScreen();
  });

  it('acepta `status` como alias de `role`', async () => {
    await render(<StatusBadge status="error" testID="badge" />);

    expect(screen.getByText(DEFAULT_LABEL.error)).toBeOnTheScreen();
    expect(screen.getByTestId('badge')).toHaveProp(
      'className',
      expect.stringContaining(SOLID_FILL.error),
    );
  });

  it('se anuncia como texto con la etiqueta del rol (no solo color)', async () => {
    await render(<StatusBadge role="active" />);

    const badge = screen.getByLabelText(DEFAULT_LABEL.active);
    expect(badge).toBeOnTheScreen();
    expect(badge).toHaveProp('accessibilityRole', 'text');
    // El texto también está en el árbol: la vista y el lector comparten la etiqueta.
    expect(screen.getByText(DEFAULT_LABEL.active)).toBeOnTheScreen();
  });

  it('el punto es decorativo: no aporta información por sí solo', async () => {
    await render(<StatusBadge role="inactive" showDot={false} testID="badge" />);

    expect(
      screen.queryByTestId('badge-dot', { includeHiddenElements: true }),
    ).not.toBeOnTheScreen();
    // Sin punto, el estado sigue comunicándose con texto y color.
    expect(screen.getByText(DEFAULT_LABEL.inactive)).toBeOnTheScreen();
    expect(screen.getByTestId('badge')).toHaveProp(
      'className',
      expect.stringContaining(SOLID_FILL.inactive),
    );
  });

  it('fusiona el `className` del consumidor y permite sobrescribir el rol', async () => {
    await render(<StatusBadge role="active" className="mt-lg border-secondary" testID="badge" />);

    const badge = screen.getByTestId('badge');
    expect(badge).toHaveProp('className', expect.stringContaining('mt-lg'));
    expect(badge).toHaveProp('className', expect.stringContaining('border-secondary'));
    expect(badge).not.toHaveProp('className', expect.stringContaining('border-primary'));
  });
});
