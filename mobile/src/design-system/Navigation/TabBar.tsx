import type { Href } from 'expo-router';
import { TabList, TabSlot, Tabs, TabTrigger } from 'expo-router/ui';
import { Pressable, type PressableProps } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Text } from '../Text';
import { cn } from '../utils/cn';

/**
 * Definición de una pestaña: nombre lógico, ruta de Expo Router y etiqueta.
 */
export type TabDefinition = {
  /** Nombre único de la pestaña (clave del trigger). */
  name: string;
  /** Ruta a la que navega (p. ej. `/` o `/plan`). */
  href: Href;
  /** Etiqueta visible; el estado nunca se comunica sólo por color. */
  label: string;
};

/**
 * Props de `TabBarItem`: las de un `Pressable` (sin `children`) más la etiqueta.
 *
 * `isFocused` lo inyecta `TabTrigger asChild`; en uso directo es opcional.
 */
export type TabBarItemProps = Omit<PressableProps, 'children'> & {
  /** Texto de la pestaña. */
  label: string;
  /** Estado activo. Se refleja en `accessibilityState.selected` y en el color. */
  isFocused?: boolean;
  /** Utilities del botón; se fusionan con `cn()` (gana la última). */
  className?: string;
};

/**
 * Pestaña individual del design system: botón con `accessibilityRole="tab"` y
 * estado `selected` explícito. Activa en `primary`/`on-primary`, inactiva en
 * `surface-muted`/`text-muted` (roles semánticos de la spec 0002).
 */
export function TabBarItem({ label, isFocused = false, className, ...rest }: TabBarItemProps) {
  return (
    <Pressable
      {...rest}
      accessibilityRole="tab"
      accessibilityState={{ selected: isFocused }}
      className={cn(
        'flex-1 items-center justify-center py-md',
        isFocused ? 'bg-primary' : 'bg-surface-muted',
        className,
      )}
    >
      <Text
        variant="labelTechnical"
        className={cn('flex-1 text-center', isFocused ? 'text-on-primary' : 'text-text-muted')}
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
 * Barra de pestañas inferior del design system (spec 0002, ticket #45).
 *
 * Es el navegador custom completo sobre `expo-router/ui`: `Tabs` provee el
 * contexto, `TabSlot` renderiza la ruta activa y `TabList`/`TabTrigger` la
 * franja de pestañas. La franja es una capa `surface` con hairline superior y
 * respeta el inset inferior con `SafeAreaView`, **sin sombras**.
 *
 * ```tsx
 * <TabBar
 *   tabs={[
 *     { name: 'index', href: '/', label: 'Inicio' },
 *     { name: 'plan', href: '/plan', label: 'Plan' },
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
              <TabBarItem label={tab.label} />
            </TabTrigger>
          ))}
        </SafeAreaView>
      </TabList>
    </Tabs>
  );
}
