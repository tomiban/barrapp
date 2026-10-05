import { Activity, CircleAlert, CircleCheck, Minus, type LucideIcon } from '@/design-system/Icon';
import type { SemanticRole } from '@/design-system/semantic';

/**
 * Icono por defecto de cada rol semántico, compartido por `Banner` y `Toast`
 * para no duplicar la misma tabla en los dos avisos.
 *
 * `inactive` usa `Minus` (ausencia de actividad), en línea con su token
 * `textMuted`; el estado siempre va acompañado de mensaje textual.
 */
export const ROLE_ICON: Record<SemanticRole, LucideIcon> = {
  active: Activity,
  confirmed: CircleCheck,
  error: CircleAlert,
  inactive: Minus,
};
