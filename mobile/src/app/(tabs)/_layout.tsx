import { TabBar, type TabDefinition } from '@/design-system/Navigation';

/**
 * Pestañas de la app (#82): `index` (Inicio) apunta a `/`, `plan` a `/plan`,
 * `entreno` a `/entreno`, `skills` a `/skills` y `historial` a `/historial`,
 * en el orden del mockup. `Entreno` es la pestaña central/destacada.
 *
 * **Perfil ya no es pestaña**: se abre desde el avatar de la cabecera
 * (`AppHeader`) como pantalla apilada en `/profile`. Los nombres son únicos y
 * las rutas existen en este mismo grupo `(tabs)`, que es lo que exige
 * `TabTrigger` dentro de un `TabList`. El showcase del design system es una
 * ruta de desarrollo en `/showcase`, fuera del grupo.
 */
const TABS: readonly TabDefinition[] = [
  { name: 'index', href: '/', label: 'Inicio', icon: 'house' },
  { name: 'plan', href: '/plan', label: 'Plan', icon: 'calendar' },
  { name: 'entreno', href: '/entreno', label: 'Entreno', icon: 'zap', featured: true },
  { name: 'skills', href: '/skills', label: 'Skills', icon: 'layers' },
  { name: 'historial', href: '/historial', label: 'Historial', icon: 'scrollText' },
];

/**
 * Layout del grupo `(tabs)`: delega en el navegador custom del design system.
 */
export default function TabsLayout() {
  return <TabBar tabs={TABS} />;
}
