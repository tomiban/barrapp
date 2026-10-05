# Reglas de trabajo

## Flujo de ticket

- El trabajo vive en GitHub Issues. La spec es el issue **#1**; los tickets son **#2–#30**, agrupados en milestones (M1–M7).
- Trabaja la **frontera**: solo tickets sin bloqueadores abiertos (`blocked_by` en GitHub).
- Un ticket = **una rama corta + un PR**. Arranca con `/implement <n>`.
- El ticket termina cuando sus **criterios de aceptación** se cumplen y los tests pasan.

## Ramas, commits y PRs

- Rama por ticket: `feat/<n>-slug` (o `fix/`, `chore/`, `docs/`).
- **Conventional Commits**: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`.
- PR hacia `main` con **squash merge**; el PR cierra el issue (`Closes #<n>`).
- Revisa con `/code-review` antes de mergear (Standards + Spec).

## Idiomas

- Código e identificadores en **inglés**.
- Documentación, comentarios y UI en **español**.
- Usa el vocabulario de `GLOSSARY.md` y evita sus términos `_Avoid_`.

## Testing

- Testea **comportamiento externo**, no detalles de implementación.
- La mayoría de los tests van a la costura del **Engine**: puros, sin base de datos ni HTTP.
- Backend: **xUnit**. Front: Jest + Testing Library (o Vitest), con tests de la outbox/sync.
- Un bug se arregla con un **test de regresión** primero.

## Estándares

- Backend: `.editorconfig` + analizadores Roslyn, con warnings como errores.
- Front: ESLint + Prettier, TypeScript en modo estricto.
- CI: formato + lint + tests **bloquean** el merge.

## Definition of done

- [ ] Criterios de aceptación del ticket cumplidos.
- [ ] Tests nuevos y existentes en verde.
- [ ] Formato y lint limpios.
- [ ] Ninguna regla de programación fuera del Engine.
- [ ] Vocabulario de dominio respetado.
- [ ] PR revisado y mergeado con squash.
