/// <reference types="node" />
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

/**
 * Contrato de los tokens del design system (ticket #32): `global.css` es la
 * fuente única de verdad y ningún componente debe volver al módulo provisional
 * `src/theme/tokens.ts`.
 */

const ROOT = join(__dirname, '..');

const REQUIRED_TOKENS = [
  // Color
  '--color-canvas',
  '--color-surface',
  '--color-surface-muted',
  '--color-border',
  '--color-text',
  '--color-text-muted',
  '--color-primary',
  '--color-on-primary',
  '--color-secondary',
  '--color-on-secondary',
  '--color-error',
  '--color-error-container',
  '--color-on-error-container',
  '--color-scrim',
  // Espaciado
  '--spacing-xs',
  '--spacing-sm',
  '--spacing-md',
  '--spacing-lg',
  '--spacing-xl',
  '--spacing-gutter',
  '--spacing-margin',
  // Tamaños físicos (alturas táctiles y lados de controles)
  '--spacing-control-primary',
  '--spacing-control-secondary',
  '--spacing-checkbox',
  '--spacing-radio-dot',
  // Radios
  '--radius-sm',
  '--radius-base',
  '--radius-md',
  '--radius-lg',
  '--radius-xl',
  // Familias tipográficas
  '--font-display',
  '--font-display-semibold',
  '--font-body',
  '--font-mono-medium',
  '--font-mono-semibold',
  '--font-mono-bold',
  // Escala tipográfica (tamaño, línea y letterSpacing)
  '--text-display-hero',
  '--text-display-hero--line-height',
  '--text-display-hero--letter-spacing',
  '--text-headline-metric',
  '--text-headline-metric--line-height',
  '--text-headline-metric--letter-spacing',
  '--text-headline-lg',
  '--text-headline-lg--line-height',
  '--text-headline-lg--letter-spacing',
  '--text-headline-md',
  '--text-headline-md--line-height',
  '--text-headline-md--letter-spacing',
  '--text-headline-sm',
  '--text-headline-sm--line-height',
  '--text-headline-sm--letter-spacing',
  '--text-body-lg',
  '--text-body-lg--line-height',
  '--text-body-md',
  '--text-body-md--line-height',
  '--text-body-sm',
  '--text-body-sm--line-height',
  '--text-label-technical',
  '--text-label-technical--line-height',
  '--text-label-technical--letter-spacing',
  '--text-label-technical--font-weight',
  '--text-label-code',
  '--text-label-code--line-height',
  '--text-label-code--letter-spacing',
  // Motion
  '--transition-duration-fast',
  '--transition-duration-base',
  '--ease-standard',
] as const;

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function walk(dir: string): string[] {
  return readdirSync(dir).flatMap((entry) => {
    const full = join(dir, entry);
    return statSync(full).isDirectory() ? walk(full) : [full];
  });
}

describe('tokens en global.css', () => {
  const css = readFileSync(join(ROOT, 'global.css'), 'utf8');

  it.each(REQUIRED_TOKENS)('declara %s', (token) => {
    expect(css).toMatch(new RegExp(`${escapeRegExp(token)}\\s*:`));
  });

  it('define el utility border-active a 1.5px', () => {
    expect(css).toMatch(/@utility\s+border-active\s*\{[^}]*border-width:\s*1\.5px/);
  });

  it('es dark-only: usa @theme plano, sin variantes de tema', () => {
    expect(css).not.toMatch(/@variant\s+(dark|light)/);
  });
});

describe('fuente única de tokens', () => {
  const src = join(ROOT, 'src');

  it('ningún archivo de src/ importa el módulo provisional @/theme/tokens', () => {
    const offenders = walk(src)
      .filter((file) => /\.(ts|tsx)$/.test(file))
      .filter((file) => /@\/theme\/tokens|theme\/tokens/.test(readFileSync(file, 'utf8')));

    expect(offenders).toEqual([]);
  });

  it('el módulo provisional src/theme/tokens.ts fue eliminado', () => {
    expect(() => statSync(join(src, 'theme', 'tokens.ts'))).toThrow();
  });
});
