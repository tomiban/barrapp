# El registro se ancla al plan en lectura por clave de sesión determinista

El plan se genera **en el lado de lectura** y no se persiste (`docs/adr/0012-generacion-de-plan-en-lado-lectura.md`), pero el **Registro** (la anotación set a set de lo ejecutado) necesita referenciar *qué sesión* y *qué serie* hizo el atleta. En vez de dar una clave foránea a un plan persistido, cada registro guarda una **clave de sesión determinista** —mesociclo, microciclo y día— más una **foto por ítem** (ejercicio, papel y objetivo de series/reps/segundos), tomada en el momento de registrar. Así se mantiene la generación en lectura sin persistir nada por plan, el motor sigue siendo la única fuente de la prescripción, y el **Historial** sobrevive a cambios de la base de conocimiento.

## Considered Options

- **Persistir el plan** al generarlo y colgar de él los registros: lo congela y obliga a reescribirlo ante cada cambio de perfil u objetivo, además de revertir ADR-0012. Descartado.
- **Materializar solo la sesión** al empezarla y colgar de ella los registros: crea una entidad «sesión» a medias, solapada con el plan en lectura, y una migración por cada cambio de anatomía de sesión. Descartado.
- **Guardar solo la clave, sin foto**: deja el historial ilegible si una etapa, un ejercicio o una rutina cambian en la base de conocimiento; el historial es un archivo y debe ser estable. Descartado.

## Consequences

- El Historial lee su propia foto y **no** necesita regenerar el plan para mostrarse.
- *Adherencia* y *Volumen semanal ejecutado* se derivan de los registros, no del plan.
- Cambiar la base de conocimiento **no** reescribe el pasado.
- La sesión suelta se guarda con la misma forma (clave de sesión con tipo *suelta*), sin mesociclo asociado.
