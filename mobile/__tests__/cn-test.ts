import { cn } from '../src/design-system/utils/cn';

describe('cn', () => {
  it('une las clases verdaderas y descarta las falsas', () => {
    expect(cn('text-text', false && 'bg-primary', undefined, 'p-md')).toBe('text-text p-md');
  });

  it('resuelve conflictos con tailwind-merge: gana la última clase', () => {
    expect(cn('bg-surface', 'bg-primary')).toBe('bg-primary');
  });

  it('resuelve conflictos dentro de la escala de espaciado del DS', () => {
    expect(cn('gap-md', 'gap-lg')).toBe('gap-lg');
    expect(cn('p-md', 'px-lg')).toBe('p-md px-lg');
  });

  it('clasifica border-active como ancho, no como color', () => {
    expect(cn('border-active', 'border-primary')).toBe('border-active border-primary');
    expect(cn('border-active', 'border')).toBe('border');
  });

  it('trata la escala tipográfica como tamaño, no como color', () => {
    // `text-body-md` (tamaño) y `text-primary` (color) son grupos distintos:
    // deben sobrevivir ambos.
    expect(cn('text-body-md', 'text-primary')).toBe('text-body-md text-primary');
    expect(cn('text-primary', 'text-error')).toBe('text-error');
    expect(cn('text-label-technical', 'text-text-muted')).toBe(
      'text-label-technical text-text-muted',
    );
  });
});
