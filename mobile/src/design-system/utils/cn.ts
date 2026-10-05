import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Combina clases de Tailwind resolviendo conflictos: gana la última.
 *
 * Uniwind no deduplica `className`, así que este helper es la forma de dejar
 * que un consumidor sobrescriba los estilos de un componente del design system.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
