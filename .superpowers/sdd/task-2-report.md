# Task 2: Assignment Delivery And Grading Contracts Report

## Implementation Summary

Implemented assignment delivery and grading contracts in `GameGuild.Learning.Assessments` while leaving course delivery in `GameGuild.Learning.Courses`.

- Added explicit persisted `SubmissionModality` flags for text, file, URL, code, media, project, and structured-answer payloads, plus `AssessmentPresentationMode` for single-step and continuous delivery.
- Added assessment delivery schedule fields: availability window, due date, late-submission flag, and late deadline. The domain validates every schedule update and evaluates submission timing consistently.
- Added persisted submission payload columns, submitted modality flags, and late metadata. Payload validation rejects disallowed modalities, non-HTTP(S) URL/media payloads, empty payload sets, and invalid structured-answer JSON.
- Added `InteractiveVideoAssessmentCue` links owned by Assessments and keyed by Courses `ContentId`, with API/service create and list operations.
- Extended existing requests and DTOs with trailing optional parameters, preserving current API callers and no-body submit behavior.
- Added EF configuration, database constraints, and a backward-compatible migration.

## TDD Evidence

RED command:

```powershell
dotnet test apps\api\tests\GameGuild.Learning.Assessments.UnitTests\GameGuild.Learning.Assessments.UnitTests.csproj --filter FullyQualifiedName~AssignmentDeliveryContractTests --diag assignment-delivery-red.log
```

Result: failed as intended, 5/5 new tests failed because the enum contracts, payload fields, late metadata, cue entity, and availability validation did not exist.

GREEN command:

```powershell
dotnet test apps\api\tests\GameGuild.Learning.Assessments.UnitTests\GameGuild.Learning.Assessments.UnitTests.csproj --no-restore --filter FullyQualifiedName~AssignmentDeliveryContractTests --logger "console;verbosity=minimal"
```

Result: passed, 11/11 focused contract tests.

Self-review regression RED/GREEN:

```powershell
dotnet test apps\api\tests\GameGuild.Learning.Assessments.UnitTests\GameGuild.Learning.Assessments.UnitTests.csproj --no-restore --filter FullyQualifiedName~Update_WhenAvailabilityEndsBeforeItStarts_ShouldRejectIt --logger "console;verbosity=normal"
```

Result: first run failed because `Assessment.Update` bypassed schedule validation; after routing updates through `SetDeliverySchedule`, the same command passed, 1/1.

## Verification

```powershell
dotnet test apps\api\tests\GameGuild.Learning.Assessments.UnitTests\GameGuild.Learning.Assessments.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"
```

Result: passed, 67/67.

```powershell
dotnet test apps\api\tests\GameGuild.Learning.Courses.UnitTests\GameGuild.Learning.Courses.UnitTests.csproj --no-restore --logger "console;verbosity=minimal"
```

Result: passed, 207/207.

```powershell
dotnet build apps\api\Source\GameGuild.API\GameGuild.API.csproj --no-restore --verbosity minimal
```

Result: succeeded, 0 warnings and 0 errors.

## Files Changed

- `apps/api/Source/Modules/GameGuild.Learning.Assessments/Models/SubmissionModality.cs`
- `apps/api/Source/Modules/GameGuild.Learning.Assessments/Entities/Assessment.cs`
- `apps/api/Source/Modules/GameGuild.Learning.Assessments/Entities/InteractiveVideoAssessmentCue.cs`
- `apps/api/Source/Modules/GameGuild.Learning.Assessments/Configuration/AssessmentsModelConfiguration.cs`
- `apps/api/Source/Modules/GameGuild.Learning.Assessments/Services/IAssessmentService.cs`
- `apps/api/Source/Modules/GameGuild.Learning.Assessments/Services/AssessmentService.cs`
- `apps/api/Source/Modules/GameGuild.Learning.Assessments/Controllers/AssessmentsController.cs`
- `apps/api/Source/GameGuild.API/Database/Migrations/20260716010751_AddAssignmentDeliveryAndGradingContracts.cs`
- `apps/api/tests/GameGuild.Learning.Assessments.UnitTests/Entities/AssignmentDeliveryContractTests.cs`
- `.superpowers/sdd/task-2-report.md`

## Migration And Backward Compatibility

The migration adds nullable payload/date columns and defaults existing required values to legacy-safe values: `SubmissionModalities = 1` (Text), `PresentationMode = 0` (SingleStep), `AllowLateSubmissions = false`, `IsLate = false`, and `SubmittedModalities = 0`. Existing assessments therefore continue accepting text-style submissions, existing submission rows remain valid, and no data is rewritten or dropped. Database check constraints enforce the persisted enum masks and date-policy invariants. The cue table has a foreign key only to Assessments; `ContentId` remains a GUID contract with Courses to avoid cross-module persistence ownership.

## Self-Review Findings And Concerns

- Fixed during review: `Assessment.Update` originally bypassed delivery schedule validation; a red regression test now covers it and updates use the same atomic validator as creation.
- The checked-in `ApplicationDbContextModelSnapshot` predates multiple existing hand-authored migrations. `dotnet ef migrations add` incorrectly proposed unrelated table drops/creates. Those generated artifacts were removed, the snapshot was left unchanged, and the committed migration is deliberately scoped to this task's columns, constraints, indexes, and cue table. Future snapshot repair should be handled as a dedicated migration-infrastructure task before relying on EF scaffolding for broad schema diffs.
- No other task-blocking issues found. `git diff --check` completed without whitespace errors; the migration contains no unrelated `Up` drops.
