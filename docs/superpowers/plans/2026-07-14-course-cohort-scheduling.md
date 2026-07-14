# Course Cohort Scheduling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a real multi-cohort professor workflow where each class has independent students, dates, meetings, content releases, and assessment timing while referencing one canonical course curriculum.

**Architecture:** `Program` remains the canonical curriculum. `Cohort` represents one class delivery. New `CohortSchedule` and `CohortScheduleItem` entities persist cohort-specific rules and dates. CQRS commands and queries expose side-effect-free preview plus atomic apply; the Next.js dashboard consumes the generated client and renders a class control center followed by a cohort workspace.

**Tech Stack:** .NET 10, ASP.NET Core, Entity Framework Core/PostgreSQL, GameGuild.CQRS, xUnit/FluentAssertions, Next.js 16, React 19, TypeScript, shadcn/Radix UI, Vitest, Playwright.

## Global Constraints

- `Program` owns canonical modules, lessons, assessments, ordering, and assets.
- `Cohort` owns one class delivery and never duplicates canonical course content.
- UI copy uses `Class`; code and API contracts use `Cohort`.
- A live class meeting is a schedule item, never the cohort itself.
- Schedule preview is side-effect free; apply is atomic.
- New API endpoints use GameGuild.CQRS and course-level permission checks.
- Existing cohort rows and legacy `MeetingSchedule` values remain readable.
- Persisted enums use explicit numeric values.
- Class creation uses a side sheet; no permanent creation form remains.
- `Syllabus` is the default view; Calendar and Timeline read the same schedule.
- Use existing `@game-guild/ui` components instead of adding dependencies.
- Follow TDD and commit after every independently testable task.

## File Structure

- `Learning.Cohorts/Entities`: schedule policy, items, and enums.
- `Learning.Cohorts/Scheduling`: deterministic generation and conflict detection.
- `Learning.Cohorts/Commands/Schedules`: apply, patch, shift, add, and remove.
- `Learning.Cohorts/Queries/Schedules`: schedule, preview, calendar, and availability.
- `apps/web/src/lib/learning/queries/cohorts.ts`: frontend cohort view models.
- `apps/web/src/lib/learning/actions/cohorts.ts`: focused cohort mutations.
- `classes/class-control-center.tsx`: operational list and filters.
- `classes/new-class-sheet.tsx`: cohort creation.
- `classes/[classId]`: persistent cohort workspace and schedule views.

---

### Task 1: Establish the Cohort Schedule Domain

**Files:**
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Entities/SchedulingEnums.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Entities/CohortSchedule.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Entities/CohortScheduleItem.cs`
- Create: `apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/CohortScheduleTests.cs`
- Create: `apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/CohortScheduleItemTests.cs`

**Interfaces:**
- Consumes: `EntityBase`, `Cohort.Id`, `ProgramContent.Id`, and assessment IDs.
- Produces: `CohortSchedule.Create`, `CohortSchedule.UpdateRules`, `CohortScheduleItem.Create`, `CohortScheduleItem.Shift`, and stable enums.

- [ ] **Step 1: Write failing enum and invariant tests**

```csharp
[Theory]
[InlineData(CohortPacingMode.OneModulePerWeek, 0)]
[InlineData(CohortPacingMode.OneLessonPerMeeting, 1)]
[InlineData(CohortPacingMode.FixedLessonsPerWeek, 2)]
[InlineData(CohortPacingMode.Manual, 3)]
public void PacingMode_HasStableValues(CohortPacingMode value, int expected) =>
    ((int)value).Should().Be(expected);

[Fact]
public void ScheduleItem_RequiresReferenceOrExceptionalTitle() =>
    FluentActions.Invoking(() => CohortScheduleItem.Create(
        Guid.NewGuid(), null, null, CohortScheduleItemType.LiveSession, null))
        .Should().Throw<ArgumentException>();
```

- [ ] **Step 2: Confirm the focused tests fail**

Run: `dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --filter "FullyQualifiedName~CohortSchedule" --no-restore`

Expected: compile failure because schedule types do not exist.

- [ ] **Step 3: Implement stable enums and aggregates**

```csharp
public enum CohortPacingMode { OneModulePerWeek = 0, OneLessonPerMeeting = 1, FixedLessonsPerWeek = 2, Manual = 3 }
public enum CohortReleasePolicy { Weekly = 0, BeforeMeeting = 1, Manual = 2, Immediately = 3 }
public enum CohortScheduleItemType { ContentRelease = 0, LiveSession = 1, AssessmentWindow = 2, Milestone = 3 }
public enum CohortScheduleItemStatus { Draft = 0, Scheduled = 1, Published = 2, Completed = 3, Cancelled = 4 }
public enum CohortVisibilityOverride { Inherited = 0, Hidden = 1, Visible = 2 }
public enum ScheduleShiftScope { Single = 0, Following = 1 }
public enum ScheduleConflictSeverity { Advisory = 0, Blocking = 1 }
```

`CohortSchedule` validates timezone, meeting days, duration, and units. `CohortScheduleItem.Shift(TimeSpan)` moves starts, ends, availability, and due dates while preserving nulls.

- [ ] **Step 4: Run all cohort tests**

Run: `dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --no-restore`

Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add apps/api/Source/Modules/GameGuild.Learning.Cohorts/Entities apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests
git commit -m "feat(learning): add cohort schedule domain"
```

---

### Task 2: Generate Deterministic Schedule Previews

**Files:**
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Scheduling/CohortScheduleGenerationModels.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Scheduling/CohortScheduleGenerator.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Scheduling/ScheduleConflictDetector.cs`
- Create: `apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/CohortScheduleGeneratorTests.cs`
- Create: `apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/ScheduleConflictDetectorTests.cs`

**Interfaces:**
- Consumes: Task 1 enums and flattened canonical content descriptors.
- Produces: `CohortSchedulePreview Generate(CohortScheduleGenerationRequest request)`.

- [ ] **Step 1: Write failing generation tests**

```csharp
[Fact]
public void OneModulePerWeek_ReleasesModulesSevenDaysApart()
{
    var preview = generator.Generate(Fixture.Request(
        firstDate: new DateOnly(2026, 8, 12), modules: Fixture.ThreeModules()));
    preview.Items.Where(x => x.Type == CohortScheduleItemType.ContentRelease)
        .Select(x => DateOnly.FromDateTime(x.AvailableFrom!.Value))
        .Should().Equal(new DateOnly(2026, 8, 12), new DateOnly(2026, 8, 19), new DateOnly(2026, 8, 26));
}
```

The same test file must contain these named cases with direct expected dates or conflict codes: `OneLessonPerMeeting_UsesEachMeetingDate`, `FixedLessonsPerWeek_RespectsUnitCount`, `ManualMode_ReturnsNoGeneratedItems`, `SkippedDate_MovesToNextMeetingDay`, `DstBoundary_PreservesLocalStartTime`, `AssessmentDueDate_UsesConfiguredOffset`, `InstructorOverlap_IsBlocking`, `ReleaseAfterDue_IsBlocking`, and `CohortEndOverflow_IsAdvisory`.

- [ ] **Step 2: Confirm generator tests fail**

Run: `dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --filter "FullyQualifiedName~ScheduleGenerator|FullyQualifiedName~ConflictDetector" --no-restore`

- [ ] **Step 3: Implement pure generation contracts**

```csharp
public sealed record CohortScheduleGenerationRequest(
    Guid CohortId, DateOnly FirstInstructionalDate, DateOnly CohortEndDate,
    string TimezoneId, IReadOnlyCollection<DayOfWeek> MeetingDays,
    TimeOnly MeetingStartTime, int MeetingDurationMinutes,
    CohortPacingMode PacingMode, int UnitsPerPeriod,
    CohortReleasePolicy ReleasePolicy, IReadOnlyCollection<DateOnly> SkippedDates,
    IReadOnlyCollection<CanonicalScheduleContent> Content);

public sealed record CohortSchedulePreview(
    IReadOnlyList<CohortSchedulePreviewItem> Items,
    IReadOnlyList<CohortScheduleConflict> Conflicts,
    DateOnly CalculatedEndDate)
{
    public bool HasBlockingConflicts => Conflicts.Any(x => x.Severity == ScheduleConflictSeverity.Blocking);
}

public sealed record CanonicalScheduleContent(
    Guid ContentId, Guid? AssessmentId, Guid? ParentId, string Title,
    ProgramContentType Type, int SortOrder, int? EstimatedMinutes);

public sealed record CohortSchedulePreviewItem(
    Guid? ProgramContentId, Guid? AssessmentId, CohortScheduleItemType Type,
    int InstructionalWeek, int SortOrder, DateTime? StartsAt, DateTime? EndsAt,
    DateTime? AvailableFrom, DateTime? AvailableUntil, DateTime? DueAt, string Title);

public sealed record CohortScheduleConflict(
    string Code, ScheduleConflictSeverity Severity, string Message,
    Guid? ProgramContentId, Guid? AssessmentId);
```

The generator must be deterministic and must not depend on `IApplicationDbContext`.

- [ ] **Step 4: Run tests and build the module**

```powershell
dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --no-restore
dotnet build apps/api/Source/Modules/GameGuild.Learning.Cohorts/GameGuild.Learning.Cohorts.csproj --no-restore
```

- [ ] **Step 5: Commit**

```powershell
git add apps/api/Source/Modules/GameGuild.Learning.Cohorts/Scheduling apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests
git commit -m "feat(learning): generate cohort schedule previews"
```

---

### Task 3: Persist Schedule Policies and Items

**Files:**
- Modify: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Configuration/CohortsModelConfiguration.cs`
- Create: `apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/CohortsModelConfigurationTests.cs`
- Create: `apps/api/Source/GameGuild.API/Database/Migrations/20260714120000_AddCohortSchedules.cs`
- Create: `apps/api/Source/GameGuild.API/Database/Migrations/20260714120000_AddCohortSchedules.Designer.cs`
- Modify: `apps/api/Source/GameGuild.API/Database/Migrations/ApplicationDbContextModelSnapshot.cs`

**Interfaces:**
- Consumes: Task 1 entities.
- Produces: `learning_cohort_schedules` and `learning_cohort_schedule_items` with one schedule per cohort.

- [ ] **Step 1: Write a failing EF model test**

```csharp
[Fact]
public void Model_HasOneSchedulePerCohort()
{
    var entity = model.FindEntityType(typeof(CohortSchedule))!;
    entity.GetIndexes().Should().Contain(index => index.IsUnique &&
        index.Properties.Select(p => p.Name).SequenceEqual([nameof(CohortSchedule.CohortId)]));
}
```

- [ ] **Step 2: Confirm the model test fails**

Run: `dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --filter "FullyQualifiedName~ModelConfiguration" --no-restore`

- [ ] **Step 3: Add EF mappings**

```csharp
modelBuilder.Entity<CohortSchedule>(entity =>
{
    entity.ToTable("learning_cohort_schedules");
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.CohortId).IsUnique();
    entity.Property(x => x.TimezoneId).HasMaxLength(100).IsRequired();
    entity.Property(x => x.PacingMode).HasConversion<int>();
    entity.Property(x => x.ReleasePolicy).HasConversion<int>();
});
```

Map schedule items with indexes on `(CohortId, InstructionalWeek, SortOrder)`, `ProgramContentId`, and `AssessmentId`. Configure cascade from schedule to items and restrict deletion of referenced canonical content.

- [ ] **Step 4: Generate and validate the migration**

```powershell
dotnet ef migrations add AddCohortSchedules --project apps/api/Source/GameGuild.API/GameGuild.API.csproj --startup-project apps/api/Source/GameGuild.API/GameGuild.API.csproj --context ApplicationDbContext --output-dir Database/Migrations
dotnet build apps/api/Source/GameGuild.API/GameGuild.API.csproj --no-restore /clp:ErrorsOnly
```

Expected: only new schedule tables and indexes are created; existing cohort rows are not rewritten.

- [ ] **Step 5: Run tests and commit**

```powershell
dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --no-restore
git add apps/api/Source/Modules/GameGuild.Learning.Cohorts apps/api/Source/GameGuild.API/Database/Migrations apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests
git commit -m "feat(learning): persist cohort schedules"
```

---

### Task 4: Expose CQRS Schedule APIs

**Files:**
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Queries/Schedules/GetCohortScheduleQuery.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Queries/Schedules/PreviewCohortScheduleQuery.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Queries/Schedules/GetCourseCohortCalendarQuery.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Commands/Schedules/ApplyCohortScheduleCommand.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Commands/Schedules/UpdateCohortScheduleItemCommand.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Commands/Schedules/ShiftCohortScheduleItemsCommand.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/DTOs/CohortScheduleDtos.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Controllers/CohortSchedulesController.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Controllers/CohortsController.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Courses/Abstractions/IProgramContentScheduleGuard.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Services/ProgramContentScheduleGuard.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Learning.Courses/Commands/RemoveProgramContent/RemoveProgramContentCommandHandler.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/CohortsModule.cs`
- Create: `apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/CohortScheduleHandlerTests.cs`
- Create: `apps/api/Tests/GameGuild.Learning.Courses.UnitTests/ProgramContentScheduleProtectionTests.cs`

**Interfaces:**
- Consumes: Tasks 1-3, actor tenant, and course permissions.
- Produces: schedule DTOs and preview/apply/edit/shift/calendar endpoints.

- [ ] **Step 1: Write failing handler tests**

```csharp
[Fact]
public async Task Preview_DoesNotPersistSchedule()
{
    await previewHandler.Handle(request, CancellationToken.None);
    context.Set<CohortSchedule>().Should().BeEmpty();
    context.SaveChangesCalls.Should().Be(0);
}

[Fact]
public async Task Apply_RejectsBlockingConflicts() =>
    await FluentActions.Invoking(() => applyHandler.Handle(blocked, CancellationToken.None))
        .Should().ThrowAsync<ValidationException>();
```

- [ ] **Step 2: Confirm handler tests fail**

Run: `dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --filter "FullyQualifiedName~ScheduleHandler" --no-restore`

- [ ] **Step 3: Implement CQRS contracts and handlers**

```csharp
public sealed record PreviewCohortScheduleQuery(Guid CourseId, Guid CohortId,
    PreviewCohortScheduleRequest Request) : IQuery<CohortSchedulePreviewDto>;

public sealed record ApplyCohortScheduleCommand(Guid CourseId, Guid CohortId,
    int ExpectedVersion, CohortSchedulePreviewDto Preview) : ICommand<CohortScheduleDto>;

public sealed record ShiftCohortScheduleItemsCommand(Guid CourseId, Guid CohortId,
    Guid ItemId, int Days, ScheduleShiftScope Scope) : ICommand<CohortScheduleDto>;

public sealed record CohortScheduleDto(Guid Id, Guid CohortId, int Version,
    string TimezoneId, CohortPacingMode PacingMode, CohortReleasePolicy ReleasePolicy,
    IReadOnlyList<CohortScheduleItemDto> Items, IReadOnlyList<Guid> UnscheduledContentIds);

public sealed record CohortSchedulePreviewDto(IReadOnlyList<CohortSchedulePreviewItemDto> Items,
    IReadOnlyList<CohortScheduleConflictDto> Conflicts, DateOnly CalculatedEndDate,
    bool HasBlockingConflicts);

public sealed record CohortScheduleItemDto(Guid Id, Guid? ProgramContentId,
    Guid? AssessmentId, CohortScheduleItemType Type, int InstructionalWeek,
    int SortOrder, DateTime? StartsAt, DateTime? EndsAt, DateTime? AvailableFrom,
    DateTime? AvailableUntil, DateTime? DueAt, string Title,
    CohortScheduleItemStatus Status);

public sealed record CohortSchedulePreviewItemDto(Guid? ProgramContentId,
    Guid? AssessmentId, CohortScheduleItemType Type, int InstructionalWeek,
    int SortOrder, DateTime? StartsAt, DateTime? EndsAt, DateTime? AvailableFrom,
    DateTime? AvailableUntil, DateTime? DueAt, string Title);

public sealed record CohortScheduleConflictDto(string Code,
    ScheduleConflictSeverity Severity, string Message, Guid? ProgramContentId,
    Guid? AssessmentId);
```

Apply uses one transaction and increments version. A version mismatch maps to HTTP 409.

- [ ] **Step 4: Implement a thin, permission-aware controller**

```csharp
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/courses/{courseId:guid}/cohorts/{cohortId:guid}/schedule")]
[Authorize]
public sealed class CohortSchedulesController(ISender sender) : BaseApiController
{
    [HttpPost("preview")]
    [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit, "courseId")]
    public Task<CohortSchedulePreviewDto> Preview(Guid courseId, Guid cohortId,
        PreviewCohortScheduleRequest request, CancellationToken ct) =>
        sender.Send(new PreviewCohortScheduleQuery(courseId, cohortId, request), ct);
}

public sealed record PreviewCohortScheduleRequest(
    DateOnly FirstInstructionalDate, DateOnly CohortEndDate, string TimezoneId,
    IReadOnlyCollection<DayOfWeek> MeetingDays, TimeOnly MeetingStartTime,
    int MeetingDurationMinutes, CohortPacingMode PacingMode, int UnitsPerPeriod,
    CohortReleasePolicy ReleasePolicy, IReadOnlyCollection<DateOnly> SkippedDates);
```

Keep `/api/cohorts` compatibility endpoints and enrich list DTOs with `NextMeetingAt`, `ConflictCount`, and schedule summary.

Add `IProgramContentScheduleGuard.HasActiveScheduleReference(Guid contentId, CancellationToken)` in the Courses module and implement it in Cohorts. `RemoveProgramContentCommandHandler` must throw `ValidationException` when the guard finds a published or active cohort reference. The schedule query compares canonical content IDs with scheduled IDs and returns the difference as `UnscheduledContentIds`.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --no-restore
dotnet test apps/api/Tests/GameGuild.Learning.Courses.UnitTests/GameGuild.Learning.Courses.UnitTests.csproj --filter "FullyQualifiedName~ProgramContentScheduleProtection" --no-restore
dotnet build apps/api/Source/GameGuild.API/GameGuild.API.csproj --no-restore /clp:ErrorsOnly
git add apps/api/Source/Modules/GameGuild.Learning.Cohorts apps/api/Source/Modules/GameGuild.Learning.Courses apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests apps/api/Tests/GameGuild.Learning.Courses.UnitTests
git commit -m "feat(learning): expose cohort schedule APIs"
```

---

### Task 5: Regenerate the Client and Correct Frontend Semantics

**Files:**
- Regenerate: `packages/infrastructure/client/src/generated/**`
- Create: `apps/web/src/lib/learning/queries/cohorts.ts`
- Create: `apps/web/src/lib/learning/queries/cohorts.test.ts`
- Create: `apps/web/src/lib/learning/actions/cohorts.ts`
- Create: `apps/web/src/lib/learning/actions/cohorts.test.ts`
- Modify: `apps/web/src/lib/learning/queries/index.ts`
- Modify: `apps/web/src/lib/learning/actions.ts`
- Modify: `apps/web/src/lib/learning/queries/course.ts`

**Interfaces:**
- Consumes: Task 4 OpenAPI contracts.
- Produces: explicit cohort models and schedule query/action adapters.

- [ ] **Step 1: Write a failing semantic mapping test**

```typescript
it('maps a cohort period without deriving a session duration', () => {
  const cohort = mapCohort(dto({ startDate: '2026-08-12T00:00:00Z', endDate: '2026-12-18T00:00:00Z' }));
  expect(cohort.period).toEqual({ startsAt: '2026-08-12T00:00:00Z', endsAt: '2026-12-18T00:00:00Z' });
  expect(cohort).not.toHaveProperty('duration');
  expect(cohort).not.toHaveProperty('scheduledAt');
});
```

- [ ] **Step 2: Regenerate and verify the client**

```powershell
pnpm --filter @game-guild/client generate
pnpm --filter @game-guild/client test
pnpm --filter @game-guild/client build
```

- [ ] **Step 3: Implement focused web contracts**

```typescript
export interface CourseCohortSummary {
  id: string;
  courseId: string;
  name: string;
  instructor: { id: string; name: string } | null;
  period: { startsAt: string; endsAt: string };
  meetingPattern: string | null;
  enrollment: { current: number; capacity: number | null };
  nextMeetingAt: string | null;
  conflictCount: number;
  status: 'scheduled' | 'active' | 'completed' | 'cancelled';
}
```

Remove `mapCohortToClass`. Re-export old action names only for routes not migrated in this plan.

- [ ] **Step 4: Run tests and typecheck**

```powershell
pnpm --filter @game-guild/web test -- cohorts.test.ts
pnpm --filter @game-guild/web exec tsc --noEmit
```

- [ ] **Step 5: Commit**

```powershell
git add packages/infrastructure/client apps/web/src/lib/learning
git commit -m "refactor(learning): model classes as cohorts"
```

---

### Task 6: Replace Classes With the Class Control Center

**Files:**
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/class-control-center.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/class-control-center.test.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/new-class-sheet.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/new-class-sheet.test.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/calendar/page.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/calendar/general-cohort-calendar.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/calendar/general-cohort-calendar.test.tsx`
- Modify: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/page.tsx`
- Replace: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/course-classes-manager.tsx`
- Modify: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/loading.tsx`

**Interfaces:**
- Consumes: `CourseCohortSummary[]` and create-cohort action.
- Produces: responsive list, filters, general-calendar command, and side-sheet creation.

- [ ] **Step 1: Write failing UI tests**

```tsx
it('shows independent morning and evening classes', () => {
  render(<ClassControlCenter courseId="course-1" cohorts={[morning, evening]} />);
  expect(screen.getByText('2026.2 · Morning')).toBeVisible();
  expect(screen.getByText('2026.2 · Evening')).toBeVisible();
  expect(screen.getByText('Mon/Wed · 09:00')).toBeVisible();
  expect(screen.getByText('Tue/Thu · 19:00')).toBeVisible();
});

it('opens class creation in a sheet', async () => {
  render(<ClassControlCenter courseId="course-1" cohorts={[]} />);
  expect(screen.queryByLabelText('Class name')).not.toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'New class' }));
  expect(screen.getByRole('dialog', { name: 'Create class' })).toBeVisible();
});

it('renders concurrent classes as separate calendar lanes', () => {
  render(<GeneralCohortCalendar cohorts={[morning, evening]} items={calendarItems} />);
  expect(screen.getByLabelText('2026.2 · Morning calendar lane')).toBeVisible();
  expect(screen.getByLabelText('2026.2 · Evening calendar lane')).toBeVisible();
});
```

- [ ] **Step 2: Confirm tests fail**

Run: `pnpm --filter @game-guild/web test -- class-control-center.test.tsx new-class-sheet.test.tsx general-cohort-calendar.test.tsx`

- [ ] **Step 3: Implement the approved control center**

Use `Table` on desktop and class cards on mobile. Keep four stable summary metrics, bounded search width, status/period filters, and full-row links. Use `Sheet`, `Select`, `Input`, and field-level errors for creation. The General calendar route assigns a stable color and accessible lane label per cohort, supports week/month modes, and links every event back to its cohort workspace.

```tsx
<Sheet open={open} onOpenChange={setOpen}>
  <SheetTrigger asChild><Button><Plus />New class</Button></SheetTrigger>
  <SheetContent className="w-full overflow-y-auto sm:max-w-xl">
    <SheetHeader><SheetTitle>Create class</SheetTitle></SheetHeader>
    <NewClassForm courseId={courseId} onCreated={handleCreated} />
  </SheetContent>
</Sheet>
```

- [ ] **Step 4: Verify tests and typecheck**

```powershell
pnpm --filter @game-guild/web test -- class-control-center.test.tsx new-class-sheet.test.tsx general-cohort-calendar.test.tsx
pnpm --filter @game-guild/web exec tsc --noEmit
```

- [ ] **Step 5: Commit**

```powershell
git add "apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes"
git commit -m "feat(learning): add class control center"
```

---

### Task 7: Build the Persistent Cohort Workspace

**Files:**
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/layout.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/cohort-workspace-nav.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/cohort-workspace-nav.test.tsx`
- Replace: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/page.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/overview/page.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/page.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/students/page.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/assessments/page.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/gradebook/page.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/settings/page.tsx`

**Interfaces:**
- Consumes: cohort detail and course cohort list.
- Produces: persistent selector and six workspace sections; base detail redirects to `schedule`.

- [ ] **Step 1: Write a failing context-switch test**

```tsx
it('switches classes without losing the course route', async () => {
  render(<CohortWorkspaceNav courseRoute="advanced-game-ai-by-gameguild" cohort={evening} cohorts={[morning, evening]} />);
  await user.click(screen.getByRole('button', { name: 'Switch class' }));
  await user.click(screen.getByRole('menuitem', { name: '2026.2 · Morning' }));
  expect(push).toHaveBeenCalledWith(expect.stringContaining(`/classes/${morning.id}/schedule`));
});
```

- [ ] **Step 2: Confirm the test fails**

Run: `pnpm --filter @game-guild/web test -- cohort-workspace-nav.test.tsx`

- [ ] **Step 3: Implement layout and routes**

```typescript
const sections = [
  ['overview', 'Overview'], ['schedule', 'Schedule & content'],
  ['students', 'Students'], ['assessments', 'Assessments'],
  ['gradebook', 'Gradebook'], ['settings', 'Settings'],
] as const;
```

Show `Course / Class`, status, period, and `Switch class` persistently. Use vertical navigation on desktop and a compact selector on mobile.

- [ ] **Step 4: Run route tests and typecheck**

```powershell
pnpm --filter @game-guild/web test -- cohort-workspace-nav.test.tsx page.test.tsx route-pages.test.tsx
pnpm --filter @game-guild/web exec tsc --noEmit
```

- [ ] **Step 5: Commit**

```powershell
git add "apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]"
git commit -m "feat(learning): add cohort workspace"
```

---

### Task 8: Implement Schedule Views and the Builder

**Files:**
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/cohort-schedule-workspace.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/syllabus-view.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/calendar-view.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/timeline-view.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/schedule-builder-sheet.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/cohort-schedule-workspace.test.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/syllabus-view.test.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/calendar-view.test.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/timeline-view.test.tsx`
- Create: `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule/schedule-builder-sheet.test.tsx`

**Interfaces:**
- Consumes: schedule DTOs and preview/apply/shift actions.
- Produces: three synchronized views and a Rules → Preview editing workflow.

- [ ] **Step 1: Write failing schedule tests**

```tsx
it('groups items into instructional weeks', () => {
  render(<SyllabusView schedule={fixtureSchedule} />);
  expect(screen.getByRole('heading', { name: 'Week 1 · Foundations' })).toBeVisible();
  expect(screen.getByText('Available Aug 12, 08:00')).toBeVisible();
});

it('does not apply before confirmation', async () => {
  render(<ScheduleBuilderSheet cohort={cohort} content={content} />);
  await user.click(screen.getByRole('button', { name: 'Generate preview' }));
  expect(previewAction).toHaveBeenCalledOnce();
  expect(applyAction).not.toHaveBeenCalled();
});
```

The test files must include `BlockingConflict_DisablesApply`, `AdvisoryConflict_RequiresConfirmation`, `FailedPreview_PreservesRules`, `CompletedCohort_IsReadOnly`, `ShiftSingle_ChangesOneItem`, `ShiftFollowing_ChangesSelectedAndLaterItems`, and `Mobile_DefaultsToTimeline`, each asserting the corresponding button state, action payload, or selected tab.

- [ ] **Step 2: Confirm component tests fail**

Run: `pnpm --filter @game-guild/web test -- syllabus-view.test.tsx calendar-view.test.tsx timeline-view.test.tsx schedule-builder-sheet.test.tsx`

- [ ] **Step 3: Implement synchronized views**

```tsx
<Tabs defaultValue="syllabus">
  <TabsList>
    <TabsTrigger value="syllabus">Syllabus</TabsTrigger>
    <TabsTrigger value="calendar">Calendar</TabsTrigger>
    <TabsTrigger value="timeline">Timeline</TabsTrigger>
  </TabsList>
  <TabsContent value="syllabus"><SyllabusView schedule={schedule} /></TabsContent>
  <TabsContent value="calendar"><CalendarView schedule={schedule} /></TabsContent>
  <TabsContent value="timeline"><TimelineView schedule={schedule} /></TabsContent>
</Tabs>
```

- [ ] **Step 4: Implement Rules → Preview**

Capture timezone, first date, meeting days/time, duration, pacing, release policy, skipped dates, and due offset. Blocking conflicts disable `Apply schedule`; advisory conflicts require confirmation. Editing and shifts use dialogs/sheets, never expanded page forms.

- [ ] **Step 5: Verify and commit**

```powershell
pnpm --filter @game-guild/web test -- syllabus-view.test.tsx calendar-view.test.tsx timeline-view.test.tsx schedule-builder-sheet.test.tsx
pnpm --filter @game-guild/web exec tsc --noEmit
pnpm --filter @game-guild/web build
git add "apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/classes/[classId]/schedule"
git commit -m "feat(learning): add cohort schedule workspace"
```

---

### Task 9: Enforce Student Availability and Complete E2E Coverage

**Files:**
- Create: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Queries/Schedules/GetAvailableCohortContentQuery.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Learning.Cohorts/Controllers/CohortSchedulesController.cs`
- Create: `apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/CohortContentAvailabilityTests.cs`
- Modify: `apps/web/src/lib/__tests__/e2e/courses.e2e.test.ts`
- Modify: `apps/web/scripts/learning-professor-browser-e2e.mjs`

**Interfaces:**
- Consumes: enrollment, canonical visibility, cohort override, and schedule windows.
- Produces: student-safe available content and E2E proof of independent cohorts.

- [ ] **Step 1: Write failing availability tests**

```csharp
[Theory]
[InlineData(true, true, true)]
[InlineData(false, true, false)]
[InlineData(true, false, false)]
public async Task Availability_RequiresEnrollmentAndRelease(bool enrolled, bool released, bool expected)
{
    var result = await handler.Handle(Fixture.Query(enrolled, released), CancellationToken.None);
    result.Any(x => x.ContentId == Fixture.ContentId).Should().Be(expected);
}
```

The availability test file must include `HiddenOverride_ExcludesContent`, `ExpiredWindow_ExcludesContent`, `DifferentCohort_ExcludesContent`, `DifferentTenant_ExcludesContent`, and `DeletedCanonicalContent_ExcludesContent`, each asserting that the fixture content ID is absent.

- [ ] **Step 2: Implement student-safe query**

```csharp
[HttpGet("available-content")]
public Task<IReadOnlyList<AvailableCohortContentDto>> GetAvailableContent(
    Guid courseId, Guid cohortId, CancellationToken ct) =>
    sender.Send<IReadOnlyList<AvailableCohortContentDto>>(
        new GetAvailableCohortContentQuery(courseId, cohortId), ct);
```

The handler derives user ID from actor context and never accepts an arbitrary student ID.

- [ ] **Step 3: Extend API E2E**

Create morning and evening cohorts with different first dates, apply different schedules, enroll different students, shift only evening, and assert morning dates are unchanged.

- [ ] **Step 4: Extend Playwright professor E2E**

```javascript
await page.getByRole('button', { name: 'New class' }).click();
await page.getByLabel('Class name').fill('2026.2 · Evening');
await page.getByRole('button', { name: 'Create class' }).click();
await page.getByRole('link', { name: /2026.2 · Evening/ }).click();
await page.getByRole('button', { name: 'Edit schedule' }).click();
await page.getByRole('button', { name: 'Generate preview' }).click();
await page.getByRole('button', { name: 'Apply schedule' }).click();
await page.getByRole('heading', { name: /Week 1/ }).waitFor();
```

Repeat for morning with another start week. Capture desktop, tablet, and mobile screenshots and assert no horizontal document overflow.

- [ ] **Step 5: Run the full acceptance gate**

```powershell
dotnet test apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests/GameGuild.Learning.Cohorts.UnitTests.csproj --no-restore
dotnet build apps/api/Source/GameGuild.API/GameGuild.API.csproj --no-restore /clp:ErrorsOnly
pnpm --filter @game-guild/client test
pnpm --filter @game-guild/client build
pnpm --filter @game-guild/web test
pnpm --filter @game-guild/web exec tsc --noEmit
pnpm --filter @game-guild/web build
pnpm --filter @game-guild/web test:e2e -- courses.e2e.test.ts
pnpm --filter @game-guild/web test:browser:learning-professor
```

Expected: every command passes with zero unexpected 4xx/5xx responses, browser errors, or overflow failures.

- [ ] **Step 6: Commit**

```powershell
git add apps/api/Source/Modules/GameGuild.Learning.Cohorts apps/api/Tests/GameGuild.Learning.Cohorts.UnitTests apps/web/src/lib/__tests__/e2e/courses.e2e.test.ts apps/web/scripts/learning-professor-browser-e2e.mjs
git commit -m "test(learning): verify independent cohort delivery"
```

## Final Acceptance Gate

- [ ] Morning and evening cohorts display as separate rows.
- [ ] Each cohort has independent period, cadence, students, next meeting, and status.
- [ ] Cohort identity remains visible while switching workspace sections.
- [ ] Syllabus, Calendar, and Timeline render one schedule source.
- [ ] Weekly and meeting-based previews generate correct dates.
- [ ] Blocking conflicts prevent apply; advisory conflicts require confirmation.
- [ ] Applying or shifting one cohort never changes another.
- [ ] Students see only content released to their enrolled cohort.
- [ ] Existing cohorts survive migration and show `Schedule not configured` until planned.
- [ ] Desktop, tablet, and mobile flows pass without clipping or horizontal overflow.
- [ ] API, client, web, integration, and browser verification commands all pass.
