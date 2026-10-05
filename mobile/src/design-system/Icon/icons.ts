import {
  Activity,
  ArrowLeft,
  Calendar,
  Check,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  CircleCheck,
  CloudOff,
  Dumbbell,
  Info,
  Minus,
  Pause,
  Play,
  Plus,
  RotateCcw,
  Settings,
  Timer,
  TrendingUp,
  X,
} from 'lucide-react-native';
import type { LucideIcon } from 'lucide-react-native';

export {
  Activity,
  ArrowLeft,
  Calendar,
  Check,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  CircleCheck,
  CloudOff,
  Dumbbell,
  Info,
  Minus,
  Pause,
  Play,
  Plus,
  RotateCcw,
  Settings,
  Timer,
  TrendingUp,
  X,
};

/**
 * Set base de iconos del design system, indexado por nombre semántico del
 * dominio de entrenamiento. Todas las entradas salen del paquete instalado
 * `lucide-react-native`; el wrapper `Icon` les aplica el trazo técnico del DS.
 */
export const baseIcons = {
  activity: Activity,
  arrowLeft: ArrowLeft,
  calendar: Calendar,
  check: Check,
  chevronLeft: ChevronLeft,
  chevronRight: ChevronRight,
  circleAlert: CircleAlert,
  circleCheck: CircleCheck,
  cloudOff: CloudOff,
  dumbbell: Dumbbell,
  info: Info,
  minus: Minus,
  pause: Pause,
  play: Play,
  plus: Plus,
  rotateCcw: RotateCcw,
  settings: Settings,
  timer: Timer,
  trendingUp: TrendingUp,
  x: X,
} satisfies Record<string, LucideIcon>;
