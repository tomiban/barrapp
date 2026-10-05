import { clsx, type ClassValue } from 'clsx';
import { extendTailwindMerge } from 'tailwind-merge';

/*
 * `tailwind-merge` no conoce las escalas del design system, así que sin
 * registrarlas no resolvería los conflictos entre utilities del mismo grupo y
 * el `className` de un consumidor no podría sobrescribir el estilo de un
 * componente:
 *
 * - Espaciado (`xs`…`margin`): `gap-md` vs `gap-lg`, `p-md` vs `px-lg`…
 * - Tipografía (`text-*`): sin registrar la escala, `tailwind-merge` clasifica
 *   `text-body-md` como color y lo descarta al fusionarlo con `text-primary`.
 *
 * Los radios, colores y bordes sí los conoce por defecto.
 */
const SPACING_SCALE = [
  'xs',
  'sm',
  'md',
  'lg',
  'xl',
  'gutter',
  'margin',
  'control-primary',
  'control-secondary',
  'checkbox',
  'radio-dot',
];

const TYPE_SCALE = [
  'display-hero',
  'headline-metric',
  'headline-lg',
  'headline-md',
  'headline-sm',
  'body-lg',
  'body-md',
  'body-sm',
  'label-technical',
  'label-code',
];

const twMerge = extendTailwindMerge({
  extend: {
    theme: {
      spacing: SPACING_SCALE,
    },
    classGroups: {
      'font-size': [{ text: TYPE_SCALE }],
      // `border-active` (1.5 px) es un utility propio; sin registrarlo,
      // tailwind-merge lo clasifica como color de borde y lo descarta al
      // fusionarlo con `border-<color>`.
      'border-w': [{ border: ['active'] }],
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
