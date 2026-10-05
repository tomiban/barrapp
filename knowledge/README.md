# Base de conocimiento

Contenido curado de la app, en **JSON versionado y embebido** (ADR-0009). Un cargador lo lee al arrancar, lo valida con **fail-fast** y lo expone como catálogo en memoria. Retocar una escalera o añadir un ejercicio **no** recompila el dominio ni genera una migración.

## Ficheros

| Fichero | Contenido |
| --- | --- |
| `exercises.json` | Catálogo de ejercicios (acondicionamiento + movimientos de skill). |
| `skills.json` | Los skills con su escalera de progresión y sus rutinas de patrón. |
| `routines.json` | Programas generales de acondicionamiento (circuitos por tiempo o por repeticiones). |

Los tres llevan `"schemaVersion": 1`.

## `exercises.json`

```jsonc
{
  "schemaVersion": 1,
  "exercises": [
    {
      "id": "push-up",            // slug estable, único
      "name": "Flexiones",        // español
      "kind": "conditioning",     // "conditioning" | "skill"
      "group": "push",            // "push" | "pull" | "leg" | "core" | "cardio"; null si kind=skill
      "metric": "reps",           // "reps" | "seconds"
      "tracksMaximum": true,      // true solo en básicos (máximo)
      "regressionId": "incline-push-up", // regresión cuando el máximo es 0 (básicos)
      "skillId": null             // opcional: acondicionamiento que apoya un skill
    }
  ]
}
```

Reglas:

- `kind: "conditioning"` → `group` obligatorio; `skillId` opcional (si viene, resuelve).
- `kind: "skill"` → `skillId` obligatorio, `group` `null`.
- `metric`: `seconds` en holds/movimientos por tiempo; `reps` el resto.
- `group: "cardio"` agrupa movimientos metabólicos (no es un *patrón*; los patrones siguen siendo empuje/tirón/pierna).

## `skills.json`

```jsonc
{
  "schemaVersion": 1,
  "skills": [
    {
      "id": "planche",
      "name": "Planche",
      "group": "push",            // patrón que entrena
      "stages": [
        {
          "order": 1,             // 4–6, consecutivo desde 1
          "name": "Planche inclinada",
          "exerciseId": "planche-lean",
          "criterion": { "metric": "seconds", "target": 20, "sets": 3 },
          "notes": "..."
        }
      ],
      "patternRoutines": [        // varios modelos (por intensidad/material)
        {
          "id": "r1",
          "name": "Modelo R1",
          "intensity": 2,          // 2 | 3
          "equipment": "Sin equipamiento",
          "items": [ /* RoutineItem... */ ]
        }
      ]
    }
  ]
}
```

## `routines.json`

```jsonc
{
  "schemaVersion": 1,
  "programs": [
    {
      "id": "ponte-en-forma",
      "name": "Ponte en forma",
      "type": "circuit",           // "circuit" (por tiempo) | "strength" (por reps)
      "description": "...",
      "routines": [
        {
          "id": "ponte-en-forma-r1",
          "name": "Rutina 1",
          "intensity": 2,
          "durationMinutes": 14,
          "blocks": [
            {
              "name": "SET 1",
              "rounds": 3,          // vueltas del circuito
              "restSeconds": 0,     // descanso entre vueltas
              "notes": "sin descanso",
              "items": [ /* RoutineItem... */ ]
            }
          ]
        }
      ]
    }
  ]
}
```

## `RoutineItem` (compartido por skills y routines)

```jsonc
{
  "exerciseId": "planche-tuck",
  "sets": 4,
  "repsMin": null,          // rango de repeticiones…
  "repsMax": null,
  "holdSecondsMin": 5,       // …o de segundos mantenidos
  "holdSecondsMax": 15,
  "restSeconds": 180,        // descanso tras la serie (o tras el grupo)
  "tempo": null,             // cadencia opcional, p. ej. "2:1:3"
  "supersetGroup": null,     // entero: items consecutivos con el mismo valor = biserie/superserie
  "notes": null
}
```

Reglas de `RoutineItem`:

- `sets > 0`, `restSeconds >= 0`.
- Al menos un rango (`repsMin/Max` o `holdSecondsMin/Max`) y `min <= max`.
- `supersetGroup`: agrupa items **consecutivos** con el mismo entero; se ejecutan sin descanso entre sí y el `restSeconds` aplica al final del grupo.
- `tempo`: cadencia (positiva : isométrica : excéntrica) cuando el ejercicio la especifica; si no, `null`.
