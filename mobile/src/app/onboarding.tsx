import { OnboardingScreen } from '@/features/onboarding';

/**
 * Ruta `/onboarding` (spec 0003, ticket #90): primer arranque. El layout raíz la
 * protege con `Stack.Protected` y sólo la deja accesible mientras el arranque
 * decide que el atleta todavía no tiene perfil u objetivo.
 */
export default function OnboardingRoute() {
  return <OnboardingScreen />;
}
