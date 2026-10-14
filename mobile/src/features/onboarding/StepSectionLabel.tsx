import { Text } from '@/design-system/Text';

/** Rótulo de sección numerado del mockup: `0n // <texto>`. */
export function StepSectionLabel({ index, children }: { index: number; children: string }) {
  return (
    <Text variant="labelTechnical" className="text-primary">
      {`0${index} // ${children}`}
    </Text>
  );
}
