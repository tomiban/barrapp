# Ajuste por palanca en los skills de tensión

Los skills de tensión (planche, front lever) exigen un torque de hombro que escala con **masa × brazo de palanca** (τ = m·g·d), así que la altura y las proporciones encarecen el movimiento más allá de lo que captura el máximo de reps. En cambio, los ejercicios no apalancados (empuje/tirón/pierna) tienen una carga que es un porcentaje de la masa corporal e independiente de la altura. Evidencia en `docs/research/antropometria-palanca-skills.md`. La historia 1 pedía que el plan use "mis datos corporales reales", pero el perfil solo guardaba peso y altura y ninguna regla los consumía. Decidimos que el motor **sí** los use, pero solo donde la física lo justifica.

## Decision

- **Fuerza general** (empuje/tirón/pierna): se programa por el **máximo**; altura y peso **no** ajustan cargas/series/RIR.
- **Skills apalancados** (planche, front lever; flag `lever` en `skills.json`): el motor clasifica la **palanca** del atleta —un proxy determinista de peso × altura × factor de proporción (envergadura/entrepierna)— en **tres cubos** (favorable, neutra, desfavorable). Efecto: **±1 serie** en el bloque de skill y una **nota de ritmo esperado**. El **criterio de etapa no cambia** (sigue siendo dato de la escalera).
- `AthleteProfile` gana **envergadura** y **entrepierna** (cuatro medidas físicas en total).
- **Sin categorías demográficas** (sexo/raza): solo medidas físicas. Los umbrales exactos de los cubos se calibran en el ticket del motor.

## Considered Options

- **Sin ajuste (one-size-fits-all)**: más simple, pero ignora una variable que la biomecánica (Wang & Shan 2023) y el coaching (Antranik, Steven Low) señalan como crítica para planche/front lever.
- **Fórmula continua de torque**: sobreajusta una precisión que no existe — el campo está casi sin estudiar y el valor que circula ("palanca efectiva ≈ 22,5 % de la altura") proviene de un blog de practicante, no de una fuente revisada por pares.
- **Cubos con proxy determinista (elegido)**: captura la dirección del efecto con honestidad sobre la incertidumbre; es determinista y testeable.

## Consequences

- Migración de `AthleteProfile` (+2 campos) y dos pasos más de onboarding.
- `skills.json` con flag `lever` en planche y front lever.
- El motor incorpora la regla de cubos (determinista, cubierta por tests de comportamiento).
- Riesgo aceptado: los umbrales son heurísticos y se recalibrarán con datos reales post-MVP.
