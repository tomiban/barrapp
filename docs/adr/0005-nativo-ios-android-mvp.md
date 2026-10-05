# Nativo iOS/Android como objetivo del MVP

La v1 se entrega como **app nativa para iOS y Android** construida con **Expo / React Native** (base tecnológica de Expo), no como PWA. El motivo original para elegir Expo fue el roadmap nativo; al decidir hacer el nativo **desde el inicio**, la PWA sale del alcance del MVP. Se descarta la PWA en la v1 por ser un objetivo de plataforma distinto que distraería del producto real y del objetivo de aprendizaje; si vuelve, será un objetivo posterior reutilizando el mismo código. Esto refuerza ADR-0004: en nativo el almacén local para offline es aún más natural.

## Considered Options

- **PWA primero**: instalación sin tienda y un solo despliegue, pero no es el producto nativo que ahora se quiere.
- **Nativo primero y web después**: válido, pero añade alcance; se pospone la web hasta que el nativo funcione.
