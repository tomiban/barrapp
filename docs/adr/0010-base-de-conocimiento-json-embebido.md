# Base de conocimiento como JSON embebido en un catálogo en memoria

La base de conocimiento curada (catálogo de ejercicios, escaleras de skill y programas generales) se guarda como **JSON versionado embebido** (`knowledge/exercises.json`, `knowledge/skills.json`, `knowledge/routines.json`) que un cargador lee al arrancar, **valida con fail-fast** y expone como un **catálogo en memoria**. La arquitectura ya pedía "datos, no código" —retocar una escalera no debe recompilar el dominio— y meter el catálogo en tablas EF obligaría a una **migración por cada retoque** de contenido, justo lo contrario. El catálogo es de solo lectura y cabe en memoria; lo único mutable (la progresión del atleta) sí va a SQLite. El dominio define los **tipos puros**; Application declara el **puerto de lectura** (`IKnowledgeBase`); Persistence implementa el cargador. Consecuencia: no hay claves foráneas de base de datos que protejan el catálogo; la integridad la garantiza la **validación al arrancar**.

## Considered Options

- **Tablas EF sembradas con `HasData`**: integridad referencial por el esquema y consultas SQL, pero cada retoque del catálogo genera una migración y mezcla contenido con esquema. Descartado.
- **JSON como fuente, sembrado a tablas al arrancar**: mantiene el JSON editable y da FK en runtime, pero duplica la fuente de verdad y complica el arranque. Descartado para el MVP.

## Consequences

- El contenido vive en `knowledge/` y se embebe como recurso; `IKnowledgeBase` lo sirve en memoria.
- Un JSON inválido **impide arrancar** (fail-fast), con mensaje claro.
- `AthleteSkillProgress` (etapa actual) y `Objective` (skill objetivo) siguen en SQLite.
