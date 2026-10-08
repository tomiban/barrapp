import type { Href } from 'expo-router';
import { TabList, TabSlot, Tabs, TabTrigger } from 'expo-router/ui';
import type { LucideIcon } from 'lucide-react-native';
import { Pressable, type PressableProps, type ViewStyle } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { Icon } from '../Icon';
import { Text } from '../Text';
import { cn } from '../utils/cn';
import { SafeAreaView } from './SafeAreaView';

/**
 * Definición de una pestaña: nombre lógico, ruta de Expo Router, etiqueta e
 * icono. `prominent` marca la pestaña central destacada del diseño (Entreno).
 */
export type TabDefinition = {
  /** Nombre único de la pestaña (clave del trigger). */
  name: string;
  /** Ruta a la que navega (p. ej. `/` o `/plan`). */
  href: Href;
  /** Etiqueta visible; el estado nunca se comunica sólo por color. */
  label: string;
  /** Icono del set de Lucide que acompaña a la etiqueta. */
  icon: LucideIcon;
  /**
   * Pestaña central destacada: celda rellena en `primary`/`on-primary`, siempre
   * visible como acción principal (la `Entreno` del diseño).
   */
  prominent?: boolean;
};

/**
 * Apilado de la celda de pestaña: icono sobre la etiqueta, centrado (mockup de
 * Stitch: `flex flex-col items-center justify-center`).
 *
 * **Por qué va como estilo inline** (excepción al `className` de Uniwind):
 * `TabTrigger asChild` (expo-router/ui) pasa por el Slot de Radix un `style`
 * con `flexDirection: 'row'` y `justifyContent: 'space-between'` que, al ser
 * inline, gana sobre el `className`. Sin este apilado forzado al final del
 * array, las celdas se disponían en fila: iconos junto a las etiquetas y
 * textos largos recortados en el borde (test de regresión en
 * `tab-bar-router-test.tsx`).
 */
const TAB_CELL_STACK: ViewStyle = {
  flexDirection: 'column',
  justifyContent: 'center',
};

/**
 * Props de `TabBarItem`: las de un `Pressable` (sin `children`) más la etiqueta,
 * el icono y el estado.
 *
 * `isFocused` lo inyecta `TabTrigger asChild`; en uso directo es opcional.
 */
export type TabBarItemProps = Omit<PressableProps, 'children'> & {
  /** Texto de la pestaña. */
  label: string;
  /** Icono de Lucide, encima de la etiqueta. */
  icon: LucideIcon;
  /** Estado activo. Se refleja en `accessibilityState.selected` y en el color. */
  isFocused?: boolean;
  /** Pestaña central destacada: celda rellena en `primary`/`on-primary`. */
  prominent?: boolean;
  /** Utilities del botón; se fusionan con `cn()` (gana la última). */
  className?: string;
};

/**
 * Pestaña individual del design system: botón con `accessibilityRole="tab"` y
 * estado `selected` explícito. Icono sobre etiqueta; la activa se pinta en
 * `primary` y la inactiva en `text-muted` (roles semánticos de la spec 0002,
 * estado nunca solo por color). La pestaña `prominent` es una celda rellena en
 * `primary` con contenido `on-primary`, la acción central del diseño.
 *
 * El apilado vertical es **contrato del componente** (ver `TAB_CELL_STACK`): un
 * `style` del consumidor se fusiona, pero `flexDirection`/`justifyContent`
 * siempre los gana la celda.
 */
export function TabBarItem({
  label,
  icon,
  isFocused = false,
  prominent = false,
  className,
  style,
  ...rest
}: TabBarItemProps) {
  const [primary, muted, onPrimary] = useCSSVariable([
    '--color-primary',
    '--color-text-muted',
    '--color-on-primary',
  ]);

  const iconColor = prominent
    ? typeof onPrimary === 'string'
      ? onPrimary
      : undefined
    : isFocused
      ? typeof primary === 'string'
        ? primary
        : undefined
      : typeof muted === 'string'
        ? muted
        : undefined;

  return (
    <Pressable
      {...rest}
      accessibilityRole="tab"
      accessibilityState={{ selected: isFocused }}
      // `TAB_CELL_STACK` va el último del array: es lo que neutraliza el
      // `flexDirection: 'row'` que `TabTrigger` inyecta vía Slot de Radix
      // (ver JSDoc de la constante).
      style={
        typeof style === 'function'
          ? (state) => [style(state), TAB_CELL_STACK]
          : [style, TAB_CELL_STACK]
      }
      className={cn(
        // Sin `justify-center`: lo centraliza `TAB_CELL_STACK` (el `className`
        // se sobreescribe con el estilo inline).
        'flex-1 items-center gap-xs py-sm',
        prominent ? 'mx-xs rounded-lg bg-primary' : undefined,
        className,
      )}
    >
      <Icon icon={icon} size={22} color={iconColor} />
      <Text
        variant="labelTechnical"
        className={cn(
          'text-center',
          prominent ? 'text-on-primary' : isFocused ? 'text-primary' : 'text-text-muted',
        )}
      >
        {label}
      </Text>
    </Pressable>
  );
}

/**
 * Props de `TabBar`.
 */
export type TabBarProps = {
  /** Pestañas del navegador. Al menos una debe apuntar a la ruta inicial. */
  tabs: readonly TabDefinition[];
};

/**
 * Barra de pestañas inferior del design system (spec 0002, tickets #45 y #82).
 *
 * Es el navegador custom completo sobre `expo-router/ui`: `Tabs` provee el
 * contexto, `TabSlot` renderiza la ruta activa y `TabList`/`TabTrigger` la
 * franja de pestañas. La franja es una capa `surface` con hairline superior y
 * respeta el inset inferior con `SafeAreaView`, **sin sombras**.
 *
 * ```tsx
 * <TabBar
 *   tabs={[
 *     { name: 'index', href: '/', label: 'Inicio', icon: LayoutGrid },
 *     { name: 'entreno', href: '/entreno', label: 'Entreno', icon: Zap, prominent: true },
 *   ]}
 * />
 * ```
 */
export function TabBar({ tabs }: TabBarProps) {
  return (
    <Tabs>
      <TabSlot />
      <TabList asChild>
        <SafeAreaView edges={['bottom']} className="flex-row border-t border-border bg-surface">
          {tabs.map((tab) => (
            <TabTrigger key={tab.name} name={tab.name} href={tab.href} asChild>
              <TabBarItem label={tab.label} icon={tab.icon} prominent={tab.prominent} />
            </TabTrigger>
          ))}
        </SafeAreaView>
      </TabList>
    </Tabs>
  );
}
