import { TabBar, type TabDefinition } from '@/design-system/Navigation';

/**
 * Pestañas de la app. `index` (Inicio) apunta a `/` y `plan` a `/plan`.
 *
 * Los nombres son únicos y las rutas existen en este mismo grupo `(tabs)`, que
 * es lo que exige `TabTrigger` dentro de un `TabList`.
 */
const TABS: readonly TabDefinition[] = [
  { name: 'index', href: '/', label: 'Inicio' },
  { name: 'plan', href: '/plan', label: 'Plan' },
];

/**
 * Layout del grupo `(tabs)`: delega en el navegador custom del design system.
 */
export default function TabsLayout() {
  return <TabBar tabs={TABS} />;
}
