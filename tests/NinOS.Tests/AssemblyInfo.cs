using Xunit;

// Las pruebas comparten estado del proceso (la variable NINOS_DB_CONNECTION, archivos del
// directorio de salida y la base de testing), asi que no pueden correr en paralelo: una
// clase pisaba la configuracion que otra estaba probando. La suite completa tarda segundos,
// asi que serializarla no cuesta nada y evita fallos intermitentes.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
