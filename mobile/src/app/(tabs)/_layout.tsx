import { TabBar, type TabDefinition } from '@/design-system/Navigation';

/**
 * Pestañas de la app: `index` (Entrenar) apunta a `/`, `plan` a `/plan` y
 * `profile` (Perfil) a `/profile`. La pestaña Biblioteca la añadirá #7.
 *
 * Los nombres son únicos y las rutas existen en este mismo grupo `(tabs)`, que
 * es lo que exige `TabTrigger` dentro de un `TabList`. El showcase del design
 * system (antigua pestaña «Catálogo») ya no se lista aquí: es una ruta de
 * desarrollo en `/showcase`, fuera del grupo.
 */
const TABS: readonly TabDefinition[] = [
  { name: 'index', href: '/', label: 'Entrenar' },
  { name: 'plan', href: '/plan', label: 'Plan' },
  { name: 'profile', href: '/profile', label: 'Perfil' },
];

/**
 * Layout del grupo `(tabs)`: delega en el navegador custom del design system.
 */
export default function TabsLayout() {
  return <TabBar tabs={TABS} />;
}
