# Design system

Lenguaje visual de Barrapp (spec `docs/specs/0002-design-system.md`): tokens en
`@theme` (Uniwind), tipografía y componentes reutilizables.

## Tokens

La **fuente única de verdad** es `mobile/global.css`, en un único bloque
`@theme` plano (theme-independent) → la app es **dark-only** por construcción.
Ningún componente escribe colores, espaciado, radios ni tipografía a mano; todo
sale de un utility.

| Categoría      | Variables                                                                                                                                                                                                                                                                                             | Utilities                                                                   |
| -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| Color          | `--color-canvas`, `--color-surface`, `--color-surface-muted`, `--color-border`, `--color-text`, `--color-text-muted`, `--color-primary`, `--color-on-primary`, `--color-secondary`, `--color-on-secondary`, `--color-error`, `--color-error-container`, `--color-on-error-container`, `--color-scrim` | `bg-canvas`, `text-text-muted`, `border-border`, `text-on-primary`…         |
| Espaciado (dp) | `--spacing-xs/sm/md/lg/xl`, `--spacing-gutter`, `--spacing-margin`                                                                                                                                                                                                                                    | `p-md`, `gap-sm`, `px-lg`, `mt-md`…                                         |
| Tamaños (dp)   | `--spacing-control-primary` (56), `--spacing-control-secondary` (48), `--spacing-checkbox` (20), `--spacing-radio-dot` (10)                                                                                                                                                                           | `h-control-primary`, `min-h-control-secondary`, `h-checkbox`, `w-radio-dot` |
| Radios (dp)    | `--radius-sm/base/md/lg/xl`                                                                                                                                                                                                                                                                           | `rounded-sm`, `rounded-base`, `rounded-md`…                                 |
| Bordes         | `border` (hairline 1px), `@utility border-active` (1.5px), `border-2` (marco de overlay)                                                                                                                                                                                                              | color con `border-<color>`                                                  |
| Familias       | `--font-display`, `--font-display-semibold`, `--font-body`, `--font-mono-medium`, `--font-mono-semibold`, `--font-mono-bold`                                                                                                                                                                          | `font-display`, `font-mono-bold`…                                           |
| Tipografía     | `--text-display-hero`, `--text-headline-metric/lg/md/sm`, `--text-body-lg/md/sm`, `--text-label-technical`, `--text-label-code` (+ `--line-height` / `--letter-spacing` / `--font-weight`)                                                                                                            | `text-headline-md`, `text-label-technical`…                                 |
| Motion         | `--transition-duration-fast/base`, `--ease-standard`                                                                                                                                                                                                                                                  | `duration-fast`, `duration-base`, `ease-standard`                           |

Convención: el nombre del token en kebab-case es el sufijo del utility
(`--color-surface-muted` → `bg-surface-muted`). Las familias `--font-*` deben
coincidir **exactamente** con las claves de `useFonts()` (ticket #33).

Motion: la spec lo dejó «a definir con la implementación». El set actual es
mínimo y no decorativo (duraciones + easing estándar, sin rebotes ni springs).

La paleta por defecto de Tailwind sigue disponible; el design system no la usa.
Para prohibirla, añadir `--color-*: initial;` al inicio del `@theme`.

## Layout

- Móvil: retícula de **4 columnas**, `gutter 16` (`gap-gutter`), `margin 20`
  (`p-margin` / `px-margin`).
- Split-screen de ejecución: top **60 %** telemetría, bottom **40 %** controles.
- Profundidad por bordes y capas tonales (`canvas` → `surface` →
  `surface-muted`), **sin sombras**.

### Primitivas

`src/design-system/layout/` expone primitivas finas sobre `View`; el espaciado
sale siempre de los tokens. Todas aceptan `className` y lo fusionan con `cn()`
resolviendo conflictos (gana la clase del consumidor).

| Primitiva  | API                                                                        | Clases que emite                                                              |
| ---------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| `Box`      | `ViewProps` + `className`                                                  | sólo fusiona `className`                                                      |
| `Stack`    | `direction?: 'row' \| 'column'` (def. `column`), `gap?: xs…xl` (def. `md`) | `flex-row`/`flex-col` + `gap-<token>`                                         |
| `Spacer`   | `size?: xs…xl` (si se omite, ocupa el espacio libre)                       | `flex-1` o `basis-<token> grow-0 shrink-0`                                    |
| `Grid`     | `margin?: boolean` (def. `true`)                                           | `flex-row flex-wrap -mx-sm` + `px-margin`                                     |
| `GridItem` | `span?: 1 \| 2 \| 3 \| 4` (def. `1`)                                       | `basis-1/4 \| basis-1/2 \| basis-3/4 \| basis-full` + `px-sm grow-0 shrink-0` |

```tsx
<Grid>
  <GridItem span={2}>
    <MetricCounter />
  </GridItem>
  <GridItem>
    <StatusBadge />
  </GridItem>
  <GridItem>
    <StatusBadge />
  </GridItem>
</Grid>
```

`Grid` reparte el `gutter` (16 dp) como padding de media separación (`sm`) en
cada celda y se saca 8 dp por lado con `-mx-sm`, de modo que el `px-margin`
deja el contenido a 20 dp del borde sin medir píxeles a mano. La caja de la
retícula se ensancha 8 dp por lado: **no le pongas fondo**; el fondo va en las
celdas (`GridItem`).

`cn()` registra las escalas del design system (espaciado y tipografía) en
`tailwind-merge`, así que el `className` de un consumidor puede sobrescribir
`gap-*`, `p-*`, `text-*`… de una primitiva sin perder el token del componente.

## Componentes

Cada componente vive en su propio directorio `src/design-system/<Nombre>/` con
un `index.ts` que expone su API pública, sus props y sus tipos. El catálogo
`Showcase` (pantalla «Catálogo», ruta `/showcase`) los muestra todos con sus
variantes y estados; este README es su índice escrito.

### Text

Escala tipográfica de la spec. El consumidor elige la variante; nunca fuente ni
tamaño sueltos. Reenvía todas las props de RN `Text`.

- `variant`: `displayHero` · `headlineMetric` · `headlineLg` · `headlineMd` ·
  `headlineSm` · `bodyLg` · `bodyMd` (default) · `bodySm` · `labelTechnical` ·
  `labelCode`.
- `headlineMetric` fija `tabular-nums` para que los contadores no bailen.

### Layout · `Box`, `Stack`, `Spacer`, `Grid`, `GridItem`

Primitivas finas sobre `View`; el espaciado sale de los tokens. Todas aceptan
`className` fusionado con `cn()` (ver la tabla de _Primitivas_).

### Icon

Envuelve un icono de Lucide con los defaults del DS.

- `icon`: componente de `lucide-react-native` (o `baseIcons.<nombre>`).
- `size` (24), `strokeWidth` (2), `color` (por defecto token `text`).
- `baseIcons` reúne el set base con nombres semánticos (`timer`, `dumbbell`,
  `play`, `cloudOff`…).

### Button

- `variant`: `primary` (56 dp, `primary`/`on-primary`) · `secondary` (48 dp,
  `secondary`/`on-secondary`) · `tertiary` (48 dp, `surface`/`text`).
- Las tres variantes llevan borde activo de 1.5 px (`border-active`) con su
  color: `primary`, `secondary` y `border`.
- `disabled`: fondo `surface`, borde y texto `text-muted`, sin inversión.
- Estado pulsado: inversión de fondo y texto.

### TextField

- `label`, `value`, `onChangeText`, `error?` + props de `TextInput`.
- Estados: reposo (borde hairline) · foco (borde `primary` de 1.5 px) · error
  (borde `error` + mensaje `alert`).

### Chip

Toggle controlado de etiqueta visible. `selected` (default `false`) +
`onChange(selected)`; `disabled`. Inactivo `surface-muted`/`text-muted`, activo
`primary`/`on-primary`.

### SegmentedControl

Selección única controlada. `options: { value, label }[]`, `value`, `onChange`,
`disabled`, `label` accesible, `error?`. Segmento activo invertido a `primary`.
Si llega `error`, se muestra bajo el control como texto `error` anunciable
(`alert`), igual que `TextField`.

### Checkbox / Radio

Controles controlados (`checked`, `onChange`, `label?`, `disabled`). Caja de
20×20 dp, radios 0; el `Checkbox` marca con un check y el `Radio` pinta un bloque
interior. El estado se anuncia por accesibilidad, no solo por color.

### BiomechanicalCard

Superficie nivel 1 con notch de estado y compartimentos separados por hairline.

- `title`, `header?` (encabezado a medida), `role?` (`active` · `confirmed` ·
  `error` · `inactive`), `statusLabel?` (por defecto, la del rol).
- `CardHeader` / `CardSection` para composición modular.

### MetricCounter

Readout monolítico tabular con micro-label, índice y unidad.

- `label`, `value`, `unit?`, `index?`/`indexPrefix?`.
- `role?`: `active` · `confirmed` · `error` · `inactive`; sin rol, el dato
  principal se pinta neutro (`text`).

### Timer / Countdown

Cuenta atrás reutilizando `MetricCounter` (rol `active`).

- `durationSeconds`, `running?`, `onComplete?`, `label?`, `onPress?`.
- Se reinicia al cambiar la duración y dispara `onComplete` una sola vez.

### ProgressIndicator

Barra o anillo de progreso sobre los tokens, para series y para el avance del
mesociclo. Sin sombras: la pista es `surface-muted`.

| Prop        | Tipo              | Default    | Uso                                                           |
| ----------- | ----------------- | ---------- | ------------------------------------------------------------- |
| `value`     | `number`          | —          | Progreso actual; se recorta a `[0, max]`.                     |
| `max`       | `number`          | `1`        | Total de la escala; el ratio es `value / max`.                |
| `role`      | `SemanticRole`    | `'active'` | Rol semántico de estado.                                      |
| `label`     | `string`          | —          | Etiqueta textual; el estado nunca se comunica solo por color. |
| `variant`   | `'bar' \| 'ring'` | `'bar'`    | Forma del indicador.                                          |
| `size`      | `number`          | `64`       | Lado del anillo en dp (solo `ring`).                          |
| `className` | `string`          | —          | Clases del contenedor (se combinan con `cn()`).               |
| `testID`    | `string`          | —          | Identificador; barra/anillo derivan `<testID>-fill`/`-ring`.  |

`SemanticRole` (definido en `semantic.ts`) mapea los roles: `active`→`primary`,
`confirmed`→`secondary`, `error`→`error`, `inactive`→`textMuted`. El grosor del
anillo sale del token `--spacing-sm`. Accesibilidad:
`accessibilityRole="progressbar"` con `accessibilityValue={{ min: 0, max, now }}`.

### StatusBadge

Comunica estado con rol semántico explícito **y** etiqueta textual.

- `role`: `active` · `confirmed` · `error` · `inactive`.
- `label?` (por defecto la del rol), `variant`: `solid` (default) · `outline`,
  `showDot?`.
- Etiquetas por defecto: En curso · Confirmado · Fallo · Inactivo.

### ListRow

Fila de semanas, sesiones y ejercicios, con separador hairline.

- `title`, `subtitle?`, `leading?`, `trailing?`, `onPress?`, `last?`.
- `role?`: `active` · `confirmed` · `error` · `inactive`; `stateLabel?`.
- Tamaño táctil secundario (48 dp).

### SectionHeader

Encabezado de sección con hairline y contador/slot opcionales. `label`, `count?`,
`trailing?`.

### Screen / Header

Chasis de pantalla: `Screen` envuelve `SafeAreaView` (`canvas`, márgenes de
página) y un `header` opcional; `Header` es título `headlineSm` + slot `kicker`
opcional (`labelTechnical`, para el patrón `BARRAS / <SECCIÓN>`) + slots
`leading`/`trailing` y hairline inferior.

### Avatar

`Avatar` es el control del atleta en la cabecera: celda cuadrada `primary` con
glifo `on-primary`, tamaño táctil secundario y sin sombras. Es presentacional y
acepta `accessibilityLabel` (default «Perfil»); la app lo enlaza a Perfil con
`ProfileAvatar` (`src/features/navigation/`).

### TabBar

Navegador inferior custom sobre `expo-router/ui`. `tabs: { name, href, label,
icon, prominent? }[]`; `TabBarItem` es el botón (icono sobre etiqueta). La
pestaña activa se pinta en `primary` sin relleno; la `prominent` (la central del
diseño, Entreno) es una celda rellena en `primary`/`on-primary`.

### BottomSheet

Modal nivel 3 sobre el `Modal` nativo: `visible`, `onClose`, `title?`,
`children?`, `closeLabel?`. Superficie `surface` con marco de 2 px en `text` y
scrim negro 80 %, sin blur.

### Banner / Toast

Aviso inline persistente (`Banner`) y transitorio (`Toast`).

- `message`, `role`: `active` · `confirmed` · `error` · `inactive`; `icon?`.
- `Toast` añade `visible?` y `onDismiss?`.
- El icono por defecto de cada rol vive en `Feedback/roleIcon.ts`.

### Loading / Skeleton

- `Loading`: `label?`, `size?` (`small`/`large`); anuncia «Cargando» por defecto.
- `Skeleton`: `animated?` (default estático), `className?`; decorativo (oculto a
  lectores de pantalla).

### EmptyState

`title`, `description?`, `icon?`, `action?`. Bloque centrado; comunica con texto,
nunca solo con el icono.

### Showcase

`ShowcaseScreen` (directorio `Showcase/`, ruta `/showcase`) es el catálogo: monta
todas las variantes y estados agrupados en Base, Entrenamiento, Navegación y
feedback e Iconos, cada demo rotulada. Es la referencia viva y el test de humo de
la librería.

## Estructura

- `semantic.ts` — roles semánticos compartidos: tipo `SemanticRole`, etiquetas
  por defecto y mapas rol → token (texto, fondo, borde, punto, variable CSS).
- `utils/` — infraestructura compartida (`cn()`).
- `layout/` — primitivas de layout (`Box`, `Stack`, `Spacer`, `Grid`,
  `GridItem`) con su `index.ts` local.
- `<Nombre>/` — cada componente con su `index.ts` de API pública. Un barrel por
  directorio; **sin barrel de `design-system/`** para no colisionar entre
  tickets.
- `Showcase/` — catálogo del design system (no es un componente de producto).

Reglas: nada de colores, espaciado ni tipografía hardcodeados; todo sale de los
tokens de `mobile/global.css`.
