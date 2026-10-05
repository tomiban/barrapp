import { clsx, type ClassValue } from 'clsx';
import { extendTailwindMerge } from 'tailwind-merge';

/*
 * `tailwind-merge` no conoce los nombres de la escala de espaciado del design
 * system (`xs`…`margin`), así que sin registrarlos no resolvería conflictos
 * como `gap-md` vs `gap-lg` y el `className` de un consumidor no podría
 * sobrescribir el espaciado de un componente. Los radios y el color sí los
 * conoce por defecto.
 */
const SPACING_SCALE = ['xs', 'sm', 'md', 'lg', 'xl', 'gutter', 'margin'];

const twMerge = extendTailwindMerge({
  extend: {
    theme: {
      spacing: SPACING_SCALE,
    },
  },
});

/**
 * Combina clases de Tailwind resolviendo conflictos: gana la última.
 *
 * Uniwind no deduplica `className`, así que este helper es la forma de dejar
 * que un consumidor sobrescriba los estilos de un componente del design system.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
