/**
 * Tokens del design system (docs/specs/0002-design-system.md).
 *
 * Provisionales: el ticket #31 los moverá al `@theme` de Uniwind. Hasta entonces son la única
 * fuente de color, espaciado y tipografía de la app; ningún componente usa valores sueltos.
 */
export const colors = {
  canvas: '#111214',
  surface: '#1E2022',
  border: '#33373B',
  text: '#E2E4E9',
  textMuted: '#8A9099',
  primary: '#FFCC00',
  onPrimary: '#111214',
  error: '#FFB4AB',
} as const;

export const spacing = {
  sm: 8,
  md: 16,
  lg: 24,
} as const;

export const radius = {
  base: 4,
  md: 6,
} as const;

export const borders = {
  hairline: 1,
  active: 1.5,
} as const;

export const typography = {
  eyebrow: {
    fontSize: 11,
    letterSpacing: 0.9,
    textTransform: 'uppercase',
  },
  title: {
    fontSize: 28,
    fontWeight: '700',
    letterSpacing: -0.6,
    lineHeight: 32,
  },
  metric: {
    fontSize: 24,
    fontWeight: '700',
    letterSpacing: -0.5,
    lineHeight: 28,
  },
  body: {
    fontSize: 16,
    lineHeight: 24,
  },
  bodyBold: {
    fontSize: 16,
    fontWeight: '700',
    lineHeight: 24,
  },
  meta: {
    fontSize: 12,
    lineHeight: 16,
  },
} as const;
