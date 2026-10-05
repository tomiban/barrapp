/**
 * Roles semánticos de estado del design system (spec 0002).
 *
 * El rol define el color, nunca al revés:
 *
 * - `active` (activo / en curso) → `primary`
 * - `confirmed` (calibrado / confirmado) → `secondary`
 * - `error` (sobrecarga / fallo) → `error`
 * - `inactive` (inactivo) → `textMuted`
 *
 * Este módulo es la **fuente única** de los mapas rol → token. Los componentes
 * consumen estos helpers en vez de redeclarar el mismo mapa cada uno; así los
 * roles y sus colores no se pueden desincronizar entre sí.
 */
export type SemanticRole = 'active' | 'confirmed' | 'error' | 'inactive';

/** Etiqueta por defecto de cada rol, en español. El estado nunca es solo color. */
export const DEFAULT_ROLE_LABEL: Record<SemanticRole, string> = {
  active: 'En curso',
  confirmed: 'Confirmado',
  error: 'Fallo',
  inactive: 'Inactivo',
};

/** Token de texto/acento del rol (readouts, etiquetas de estado). */
export const ROLE_TEXT_CLASS: Record<SemanticRole, string> = {
  active: 'text-primary',
  confirmed: 'text-secondary',
  error: 'text-error',
  inactive: 'text-text-muted',
};

/** Token de relleno del rol (notch, marcador, barra y punto). */
export const ROLE_BG_CLASS: Record<SemanticRole, string> = {
  active: 'bg-primary',
  confirmed: 'bg-secondary',
  error: 'bg-error',
  inactive: 'bg-text-muted',
};

/** Token de borde del rol. En `inactive` es la hairline neutra. */
export const ROLE_BORDER_CLASS: Record<SemanticRole, string> = {
  active: 'border-primary',
  confirmed: 'border-secondary',
  error: 'border-error',
  inactive: 'border-border',
};

/** Punto de estado del rol: comparte el acento del relleno. */
export const ROLE_DOT_CLASS: Record<SemanticRole, string> = ROLE_BG_CLASS;

/**
 * Variable CSS del color del rol. `react-native-svg` y Lucide colorean por
 * prop, no por `className`, así que el token se resuelve en JS con
 * `useCSSVariable`. Las clases de arriba ya incluyen estos tokens para que el
 * build los emita.
 */
export const ROLE_COLOR_VARIABLE: Record<SemanticRole, string> = {
  active: '--color-primary',
  confirmed: '--color-secondary',
  error: '--color-error',
  inactive: '--color-text-muted',
};
