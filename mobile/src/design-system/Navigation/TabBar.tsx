import type { Href } from 'expo-router';
import { TabList, TabSlot, Tabs, TabTrigger } from 'expo-router/ui';
import { Pressable, View, type PressableProps } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { baseIcons, Icon, type IconName } from '../Icon';
import { Text } from '../Text';
import { cn } from '../utils/cn';
import { SafeAreaView } from './SafeAreaView';

/**
 * Definición de una pestaña: nombre lógico, ruta de Expo Router, etiqueta e
 * icono; `featured` destaca la pestaña central (la `Entreno` del mockup).
 */
export type TabDefinition = {
  /** Nombre único de la pestaña (clave del trigger). */
  name: string;
  /** Ruta a la que navega (p. ej. `/` o `/plan`). */
  href: Href;
  /** Etiqueta visible; el estado nunca se comunica sólo por color. */
  label: string;
  /** Icono del set base dibujado sobre la etiqueta. */
  icon: IconName;
  /** Si es `true`, el icono se pinta sobre una baldosa `primary` (pestaña destacada). */
  featured?: boolean;
};

/**
 * Props de `TabBarItem`: las de un `Pressable` (sin `children`) más la
 * etiqueta, el icono y el destaque.
 *
 * `isFocused` lo inyecta `TabTrigger asChild`; en uso directo es opcional.
 */
export type TabBarItemProps = Omit<PressableProps, 'children'> & {
  /** Texto de la pestaña. */
  label: string;
  /** Icono del set base (`baseIcons`) dibujado sobre la etiqueta. */
  icon: IconName;
  /** Estado activo. Se refleja en `accessibilityState.selected` y en el color. */
  isFocused?: boolean;
  /** Pestaña destacada del diseño: baldosa `primary` alrededor del icono. */
  featured?: boolean;
  /** Utilities del botón; se fusionan con `cn()` (gana la última). */
  className?: string;
};

/**
 * Pestaña individual del design system: botón con `accessibilityRole="tab"`,
 * `accessibilityLabel` explícito y estado `selected`. Activa, su icono y su
 * etiqueta van en `primary`; inactiva, en `surface-muted`/`text-muted` (roles
 * semánticos de la spec 0002). La pestaña `featured` mantiene su baldosa
 * amarilla también inactiva, igual que en el mockup.
 *
 * El icono es decorativo (lo anuncia la etiqueta): se oculta del lector de
 * pantalla con `accessibilityElementsHidden`.
 */
export function TabBarItem({
  label,
  icon,
  isFocused = false,
  featured = false,
  className,
  ...rest
}: TabBarItemProps) {
  const iconColorVariable = featured
    ? '--color-on-primary'
    : isFocused
      ? '--color-primary'
      : '--color-text-muted';
  const iconColor = useCSSVariable(iconColorVariable);

  return (
    <Pressable
      {...rest}
      accessibilityRole="tab"
      accessibilityLabel={label}
      accessibilityState={{ selected: isFocused }}
      className={cn('flex-1 items-center justify-center gap-xs py-sm', className)}
    >
      <View
        testID="tab-item-icon"
        className={cn('items-center justify-center rounded-base p-xs', featured && 'bg-primary')}
      >
        <Icon
          icon={baseIcons[icon]}
          size={20}
          color={typeof iconColor === 'string' ? iconColor : undefined}
        />
      </View>
      <Text
        variant="labelTechnical"
        className={cn('text-center', isFocused ? 'text-primary' : 'text-text-muted')}
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
 * Barra de pestañas inferior del design system (spec 0002, ticket #45,
 * rediseño de 5 pestañas en #82).
 *
 * Es el navegador custom completo sobre `expo-router/ui`: `Tabs` provee el
 * contexto, `TabSlot` renderiza la ruta activa y `TabList`/`TabTrigger` la
 * franja de pestañas. La franja es una capa `surface` con hairline superior y
 * respeta el inset inferior con `SafeAreaView`, **sin sombras**. Cada pestaña
 * pinta su icono sobre la etiqueta; la activa va en `primary` y la
 * `featured` (la central del mockup) sobre su baldosa amarilla.
 *
 * ```tsx
 * <TabBar
 *   tabs={[
 *     { name: 'index', href: '/', label: 'Inicio', icon: 'house' },
 *     { name: 'entreno', href: '/entreno', label: 'Entreno', icon: 'zap', featured: true },
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
              <TabBarItem label={tab.label} icon={tab.icon} featured={tab.featured} />
            </TabTrigger>
          ))}
        </SafeAreaView>
      </TabList>
    </Tabs>
  );
}
