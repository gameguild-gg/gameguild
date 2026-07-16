# Project Channel Lifecycle Implementation Report

## Status

DONE

- Worktree: `E:\repositories\game-guild\game-guild-project-channel-lifecycle`
- Branch: `fix/project-channel-lifecycle`
- Base: `1e3856551b5e2eda0bc6c99abdeac9bc3e70ddc4`
- Push/merge: not performed

## Implementation

- Scoped explicit project IDs and legacy title fallback in `CreateSimpleTestingRequestAsync` to active projects in the authenticated actor tenant; ambiguous active titles are rejected before creating rows.
- Added Projects-owned `IProjectLifecycleParticipant`, `IProjectLifecycleCoordinator`, and `IProjectLifecycleLock` contracts without reverse references to Testing Lab or Launch Pad.
- Added owning-module lifecycle participants for Store links, Testing Lab session links/testing requests, and Launch Plans. The coordinator owns one transaction and final save for participant changes and project deletion.
- Testing Lab recalculates affected `RegisteredProjectCount` values from remaining active, nondeleted links. Restore does not reactivate closed associations.
- Added a PostgreSQL transaction-scoped advisory lock, with a process-local semaphore fallback for non-PostgreSQL tests, and applied it through commit to deletion and all project channel link/create paths.
- Repaired legacy active `session_projects` duplicates deterministically by `CreatedAt, Id`, reconciled all session counts, and added a write-conflicting table lock held through unique-index creation.
- Changed Launch Plan uniqueness to active rows only, preserving deleted plan history while allowing one replacement after project restore.
- Added real PostgreSQL tests for migration repair/index enforcement/writer exclusion and both serialization orders for Store, SessionProject, Launch Plan, and project-backed Testing Request races.

## TDD Evidence

Each production change below was preceded by a focused failure for the intended behavior.

### Tenant-scoped request resolution

```powershell
dotnet test apps/api/tests/GameGuild.TestingLab.UnitTests/GameGuild.TestingLab.UnitTests.csproj --no-restore --filter "FullyQualifiedName~WithProjectId_ShouldNotResolveCrossTenantProject|FullyQualifiedName~WithProjectId_ShouldNotResolveSoftDeletedProject|FullyQualifiedName~WithLegacyTitle_ShouldResolveOnlyActiveActorTenantProject|FullyQualifiedName~WithAmbiguousLegacyTitle_ShouldRejectBeforeCreatingRows" --logger "console;verbosity=minimal"
```

- RED: 0 passed, 4 failed, 0 skipped, total 4.
- GREEN: 4 passed, 0 failed, 0 skipped, total 4.
- Broader GREEN: `--filter "FullyQualifiedName~TestingRequestOperationsServiceTests"` passed 11/11.

### Atomic channel cleanup and restore

```powershell
dotnet test apps/api/tests/GameGuild.Projects.UnitTests/GameGuild.Projects.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ProjectLifecycleTests.DeleteCommand_ShouldSoftDeleteProjectAndActiveStoreAssociations" --logger "console;verbosity=minimal"
```

- RED: 0 passed, 1 failed; active Store link remained undeleted.
- GREEN: 1 passed, 0 failed/skipped.

```powershell
dotnet test apps/api/tests/GameGuild.TestingLab.UnitTests/GameGuild.TestingLab.UnitTests.csproj --no-restore --filter "FullyQualifiedName~TestingLabProjectLifecycleTests.Delete_ShouldDeactivateProjectLinksAndRecalculateAffectedSessionCounts" --logger "console;verbosity=minimal"
```

- RED: 0 passed, 1 failed; target SessionProject links remained active.
- GREEN: 1 passed, 0 failed/skipped.

```powershell
dotnet test apps/api/tests/GameGuild.LaunchPad.UnitTests/GameGuild.LaunchPad.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ProjectDelete_ShouldSoftDeleteLaunchPlanAndPreserveLaunchHistory" --logger "console;verbosity=minimal"
```

- RED: 0 passed, 1 failed; Launch Plan remained active.
- GREEN: 1 passed, 0 failed/skipped.
- Restore characterization GREEN: `--filter "FullyQualifiedName~ProjectLifecycleTests"` passed 2/2 and proved associations remain closed.

```powershell
dotnet test apps/api/tests/GameGuild.TestingLab.UnitTests/GameGuild.TestingLab.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Delete_ShouldSoftDeleteActiveProjectTestingRequestsOnly" --logger "console;verbosity=minimal"
```

- RED: 0 passed, 1 failed; active project-backed request retained null `DeletedAt`.
- GREEN: 1 passed, 0 failed/skipped.

### Lifecycle serialization

```powershell
dotnet test apps/api/tests/GameGuild.Projects.UnitTests/GameGuild.Projects.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ConcurrentDeleteAndStoreLink_ShouldNotLeaveActiveLinkOnDeletedProject" --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.TestingLab.UnitTests/GameGuild.TestingLab.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ConcurrentDeleteAndSessionLink_ShouldNotLeaveActiveLinkOnDeletedProject" --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.LaunchPad.UnitTests/GameGuild.LaunchPad.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ConcurrentDeleteAndLaunchPlanCreate_ShouldNotLeaveActivePlanOnDeletedProject" --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.TestingLab.UnitTests/GameGuild.TestingLab.UnitTests.csproj --no-restore --filter "FullyQualifiedName~CreateSimpleTestingRequestAsync_ShouldHoldProjectLifecycleLockThroughCommit" --logger "console;verbosity=minimal"
```

- RED: each command passed 0/1; respectively an active Store link, SessionProject, or Launch Plan survived, and the request path never acquired the recording lock.
- GREEN: each command passed 1/1 with 0 failures/skips.

Additional real PostgreSQL GREEN coverage after the lock implementation:

```powershell
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ProjectChannelPostgreSqlRaceTests&FullyQualifiedName~DeleteFirst" --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ConcurrentDeleteAndTestingRequestCreate_CreateFirstClosesRequest" --logger "console;verbosity=minimal"
```

- Delete-first ordering: 4 passed, 0 failed/skipped.
- Testing Request create-first ordering: 1 passed, 0 failed/skipped.

### Deployment-safe migration and Launch Plan uniqueness

```powershell
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ProjectChannelPostgreSqlMigrationTests.Up_Repairs_Active_Session_Project_Duplicates_Reconciles_Counts_And_Enforces_Uniqueness" --logger "console;verbosity=minimal"
```

- RED: 0 passed, 1 failed with PostgreSQL SQLSTATE 23505 from the aborting duplicate preflight.
- GREEN: 1 passed, 0 failed/skipped; earliest deterministic survivor retained, duplicates closed, counts reconciled, unique index enforced.

```powershell
dotnet test apps/api/tests/GameGuild.LaunchPad.UnitTests/GameGuild.LaunchPad.UnitTests.csproj --no-restore --filter "FullyQualifiedName~LaunchPlan_Model_ShouldEnforceUniquenessForActivePlansOnly" --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Up_AllowsReplacementForSoftDeletedLaunchPlanAndRejectsSecondActivePlan" --logger "console;verbosity=minimal"
```

- RED: model test 0/1 because the index filter was null; PostgreSQL test 0/1 with SQLSTATE 23505 when inserting a replacement after a deleted plan.
- GREEN: each command passed 1/1 with 0 failures/skips.

```powershell
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Up_AcquiresWriteConflictingSessionProjectLockBeforeRepair" --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj --no-restore --filter "FullyQualifiedName~Up_HoldsWriteConflictingLockThroughRepairAndUniqueIndexCreation" --logger "console;verbosity=minimal"
```

- RED: migration contract 0/1 because no `LOCK TABLE session_projects` operation existed before repair.
- GREEN: migration contract 1/1; real PostgreSQL writer-exclusion test 1/1 with 0 skips.

## Final Verification

```powershell
dotnet test apps/api/tests/GameGuild.Projects.UnitTests/GameGuild.Projects.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.TestingLab.UnitTests/GameGuild.TestingLab.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.LaunchPad.UnitTests/GameGuild.LaunchPad.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"
dotnet test apps/api/tests/GameGuild.Commerce.Products.UnitTests/GameGuild.Commerce.Products.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"
```

- Projects: 157 passed, 0 failed, 0 skipped, total 157.
- Testing Lab: 84 passed, 0 failed, 0 skipped, total 84.
- Launch Pad: 24 passed, 0 failed, 0 skipped, total 24.
- Products: 554 passed, 0 failed, 0 skipped, total 554.

Real PostgreSQL zero-skip command (12 container-backed tests; the non-container migration-operation contract is deliberately excluded):

```powershell
$filter = 'FullyQualifiedName~ProjectChannelPostgreSqlRaceTests|FullyQualifiedName~Up_Repairs_Active_Session_Project_Duplicates_Reconciles_Counts_And_Enforces_Uniqueness|FullyQualifiedName~Up_Enforces_Active_Pair_Uniqueness_And_Project_Product_Foreign_Keys|FullyQualifiedName~Up_AllowsReplacementForSoftDeletedLaunchPlanAndRejectsSecondActivePlan|FullyQualifiedName~Up_HoldsWriteConflictingLockThroughRepairAndUniqueIndexCreation'
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj --no-restore --filter $filter --logger "console;verbosity=minimal"
```

- PostgreSQL: 12 passed, 0 failed, 0 skipped, total 12.

```powershell
dotnet build apps/api/Source/GameGuild.API/GameGuild.API.csproj --no-restore --warnaserror
```

- Build succeeded with 0 warnings and 0 errors.

Architecture check:

```powershell
rg -n "GameGuild\.TestingLab|GameGuild\.LaunchPad" apps/api/Source/Modules/GameGuild.Projects -g "*.cs" -g "*.csproj"
```

- No matches: Projects has no reverse Testing Lab or Launch Pad references.
- Public controller/command contracts were unchanged.

## Commits

- `3f99e09a15137060142f8edfe3fdf5c9ace9ac57` - scope project request resolution
- `d518a5c6b24cad2886801013835640f09fcd9798` - close channel participants on delete
- `69790d9aba94476eceb280acc4f20413b3dd8b61` - serialize project channel lifecycle
- `857ced0e3074fb10b704d2a1e8e659017de56f64` - repair project channel duplicates
- `bad50e281ce09457b54c1951df511076af2fa004` - prove PostgreSQL project channel race safety
- `bfb573a5bcdb2ca29b941da6f69dc0b957ebe677` - close project-backed Testing Requests
- `b8c4274ec926a55003544278a41e0957a32391ce` - allow Launch Plan replacement after restore
- `c90f55d74ccde9a0b634e150674452ce1051bb59` - lock migration repair against writers
- `dba6bfce14cac69a7980e0e1e46f853e4480f938` - cover both project race orderings

## Review and Residual Concerns

- Independent review found four issues: project-backed requests surviving deletion, migration writers racing repair, unfiltered Launch Plan uniqueness, and missing delete-first race coverage. All four were corrected and verified before final suite execution.
- The non-PostgreSQL lifecycle lock fallback is process-local and its per-project semaphore entries remain for process lifetime. It is intended only for in-memory/non-PostgreSQL execution; PostgreSQL uses transaction-scoped advisory locks and is the authoritative concurrency path.
- `LOCK TABLE ... IN SHARE ROW EXCLUSIVE MODE` intentionally blocks `session_projects` writers while repair and index creation run. Deployment duration therefore scales with legacy table size and should be scheduled with that write pause in mind.
- Branch remains local and unmerged as required.
