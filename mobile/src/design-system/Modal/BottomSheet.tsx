import type { ReactNode } from 'react';
import { Modal, Pressable, View } from 'react-native';

import { Icon, X } from '@/design-system/Icon';
import { Text } from '@/design-system/Text';
import { cn } from '@/design-system/utils/cn';

/**
 * Props públicas de `BottomSheet`.
 *
 * Mantiene una API mínima y estable (`visible`/`onClose`/`title`/`children`)
 * para que una futura implementación con gesto de arrastre pueda sustituir a
 * este modal sin tocar a los consumidores.
 */
export type BottomSheetProps = {
  /** Controla la visibilidad. Con `false`, React Native no monta el modal. */
  visible: boolean;
  /** Se invoca al cerrar: backdrop, afordancia de cierre o «atrás» de Android. */
  onClose: () => void;
  /** Título opcional, renderizado como encabezado accesible. */
  title?: string;
  /** Contenido del panel. */
  children?: ReactNode;
  /** Etiqueta accesible de la afordancia de cierre. Default: `Cerrar`. */
  closeLabel?: string;
  /** Clases de la superficie; se fusionan con `cn()` para permitir sobrescritura. */
  className?: string;
  /** `testID` de la superficie. Deriva `<testID>-backdrop`, `-surface` y `-close`. */
  testID?: string;
};

/**
 * Modal de nivel 3 del design system (spec 0002, ticket #46).
 *
 * Usa el `Modal` nativo de React Native (cero dependencias nuevas, seguro en
 * Expo Go) con `transparent` + `animationType="fade"`, sin gestos ni blur:
 * - **Superficie**: `surface` con **marco de 2 px en `text`** (nivel 3).
 * - **Scrim**: capa explícita `bg-scrim` (negro 80 %).
 * - **Sin sombras**: la profundidad sale del marco y del scrim.
 *
 * El scrim se dibuja como capa propia y no con `backdropColorClassName`: en
 * React Native, `backdropColor` se **ignora** cuando `transparent` es `true`
 * (lo comprueba el propio render del `Modal`), así que la capa explícita es la
 * que garantiza el velo.
 *
 * El `onRequestClose` cubre el botón «atrás» de Android y la afordancia de
 * cierre y el backdrop quedan etiquetados para lectores de pantalla.
 */
export function BottomSheet({
  visible,
  onClose,
  title,
  children,
  closeLabel = 'Cerrar',
  className,
  testID,
}: BottomSheetProps) {
  return (
    <Modal
      animationType="fade"
      onRequestClose={onClose}
      testID={testID}
      transparent
      visible={visible}
    >
      <View className="flex-1 justify-end">
        <Pressable
          accessible={false}
          className="absolute inset-0 bg-scrim"
          onPress={onClose}
          testID={testID ? `${testID}-backdrop` : undefined}
        />

        <View
          accessibilityViewIsModal
          className={cn('border-2 border-text bg-surface p-lg', className)}
          testID={testID ? `${testID}-surface` : undefined}
        >
          <View className="flex-row items-start justify-between gap-md">
            {title ? (
              <Text accessibilityRole="header" className="flex-1 text-text" variant="headlineSm">
                {title}
              </Text>
            ) : (
              <View className="flex-1" />
            )}

            <Pressable
              accessibilityLabel={closeLabel}
              accessibilityRole="button"
              className="p-xs"
              hitSlop={12}
              onPress={onClose}
              testID={testID ? `${testID}-close` : undefined}
            >
              <Icon icon={X} size={20} />
            </Pressable>
          </View>

          {children ? <View className="mt-md">{children}</View> : null}
        </View>
      </View>
    </Modal>
  );
}
