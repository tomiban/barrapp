# Motor determinista primero; LLM como capa validada

El plan y las sesiones se generan con un **motor determinista de reglas** sobre la base de conocimiento curada, con o sin IA. Descartamos el LLM como generador principal en la v1 porque no elimina la necesidad de las reglas —las degrada de generador a validador— y arriesga la "planificación bien estructurada" que justifica el proyecto: mismo input daría planes distintos, sería más costoso y no determinista de testear. Un LLM se añadirá después como **capa**: traduce lenguaje natural a parámetros y aporta variedad, siempre dentro de invariantes que el motor valida y corrige.

## Considered Options

- **LLM de punta a punta** (descartado para la v1): máxima flexibilidad, pero adiós al determinismo y coste/API ya en la primera versión.
- **Híbrido con el LLM generando dentro de límites** (futuro): el LLM propone, el motor valida y corrige. Se pospone hasta que el motor determinista exista, porque el motor es prerrequisito del validador.
