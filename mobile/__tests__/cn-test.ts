import { cn } from '../src/design-system/utils/cn';

describe('cn', () => {
  it('une las clases verdaderas y descarta las falsas', () => {
    expect(cn('text-text', false && 'bg-primary', undefined, 'p-md')).toBe('text-text p-md');
  });

  it('resuelve conflictos con tailwind-merge: gana la última clase', () => {
    expect(cn('bg-surface', 'bg-primary')).toBe('bg-primary');
  });
});
