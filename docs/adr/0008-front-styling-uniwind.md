# Front styling: Uniwind (Tailwind CSS v4)

El front estiliza con **Uniwind** (MIT, versión free), que trae **Tailwind CSS v4** (`className`, `@theme`, variables CSS, media queries) a React Native sobre un motor tipo Unistyles, del mismo equipo. Se elige sobre **Unistyles** porque Uniwind **free corre en Expo Go**, evitando el *development build* en el día a día, y porque alinea con el ecosistema que ya conoce el autor (**HeroUI Native**, React Native Reusables). Los tokens del design system (`docs/specs/0002-design-system.md`) se definen en `@theme`/variables CSS. Se descarta por ahora la versión **Pro** (de pago: motor C++, cero re-renders).

## Considered Options

- **Unistyles**: tokens y variantes tipados en TS; descartado por requerir módulo nativo (development build).
- **NativeWind**: Tailwind para RN; descartado por rendimiento y por no tener Tailwind v4 estable.
- **Uniwind Pro**: motor C++ y actualizaciones sin re-render; se aplaza (de pago).
