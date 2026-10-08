import { baseIcons } from '@/design-system/Icon';
import { TabBar, type TabDefinition } from '@/design-system/Navigation';

/**
 * Pestañas de la app (spec 0003, ticket #82): Inicio · Plan · GO · Skills ·
 * Historial, con la ruta `entreno` como pestaña central destacada. Por ahora la
 * central se rotula `GO` (etapa temporal; el diseño la llama `Entreno`). Perfil
 * ya no es pestaña: se abre desde el avatar de la cabecera (`ProfileAvatar`).
 *
 * `index` es Inicio (ruta `/`) y los demás nombres coinciden con sus archivos
 * en este grupo `(tabs)`, que es lo que exige `TabTrigger` dentro de un
 * `TabList`. El showcase del design system vive fuera del grupo, en `/showcase`.
 */
export const TABS: readonly TabDefinition[] = [
  { name: 'index', href: '/', label: 'Inicio', icon: baseIcons.layoutGrid },
  { name: 'plan', href: '/plan', label: 'Plan', icon: baseIcons.calendarRange },
  {
    name: 'entreno',
    href: '/entreno',
    label: 'GO',
    icon: baseIcons.zap,
    prominent: true,
  },
  { name: 'skills', href: '/skills', label: 'Skills', icon: baseIcons.trendingUp },
  { name: 'historial', href: '/historial', label: 'Historial', icon: baseIcons.history },
];

/**
 * Layout del grupo `(tabs)`: delega en el navegador custom del design system.
 */
export default function TabsLayout() {
  return <TabBar tabs={TABS} />;
}
