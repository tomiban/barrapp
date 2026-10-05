# Offline-first con almacén local y sincronización

La app funciona sin conexión para **leer el plan y registrar sesiones**: el cliente mantiene un almacén local (**SQLite en el dispositivo**, `expo-sqlite`) y una **cola de sincronización** que sube los cambios al backend cuando vuelve la red. La **generación** de plan y de sesión suelta, al depender del motor server-side (ADR-0002), **requiere conexión** en el MVP. La sincronización usa **last-write-wins**, válido porque el MVP es mono-usuario; el backend sigue siendo la fuente de verdad para el futuro multi-usuario (ADR-0001). Se descarta duplicar el motor en el cliente (offline total) por ser más trabajo y arriesgar la divergencia con el motor del servidor.

## Considered Options

- **Offline de generación (motor en el cliente, duplicado en TypeScript o WASM)**: independencia total de red, pero doble implementación del motor y riesgo de divergencia.
- **Local-first puro (motor en el cliente, backend solo sincroniza)**: contradice ADR-0002 y complica el multi-usuario.
- **Offline de lectura y registro** (elegida): cubre el caso real (gimnasio sin señal, planificación con wifi) con coste bajo.
