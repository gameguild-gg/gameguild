using Xunit;

// Docker Desktop on Windows serves Testcontainers through one named-pipe endpoint.
// Serializing this assembly keeps database integration tests deterministic while
// leaving parallelism available to the other test assemblies.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
