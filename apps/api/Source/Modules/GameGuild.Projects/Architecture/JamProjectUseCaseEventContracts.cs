using GameGuild;

// Hand-maintained use-case event contract for the jam project factory (issue #84),
// following the ProjectWork module's hand-maintained contract file pattern. The
// committed UseCaseEventContracts.g.cs is produced by external tooling; new commands
// that postdate it declare their contract here until the next regeneration.
[assembly: UseCaseEventContract(typeof(GameGuild.Projects.CreateJamProjectCommand), "projects.create-jam-project", NoDomainEventReason = "Jam project creation is observed through the durable generic operation event.")]
