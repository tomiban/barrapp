import { ShowcaseScreen } from '@/design-system/Showcase';

/**
 * Ruta de desarrollo del showcase del design system (ticket #49). Vive fuera
 * del grupo `(tabs)` a propósito: ya no es una pestaña y solo se abre por URL
 * directa (`/showcase`) para consultar los componentes.
 */
export default function ShowcaseRoute() {
  return <ShowcaseScreen />;
}
