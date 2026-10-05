using Xunit;

// Los tests funcionales comparten una base SQLite en fichero: se arranca el API (y se aplican
// las migraciones) por clase, así que se serializan para no migrar en paralelo.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
