import assert from "node:assert/strict";
import test from "node:test";

import { selectAffectedDotnetTests } from "../select-affected-dotnet-tests.mjs";

const availableProjects = [
  "GameGuild.API.UnitTests",
  "GameGuild.API.IntegrationTests",
  "GameGuild.Projects.UnitTests",
  "GameGuild.Identity.Authentication.IntegrationTests",
  "GameGuild.SharedKernel.UnitTests",
  "GameGuild.TestingLab.UnitTests",
];

test("selects the test project matching a changed API module", () => {
  assert.deepEqual(
    selectAffectedDotnetTests(
      ["apps/api/Source/Modules/GameGuild.TestingLab/TestingEvent.cs"],
      availableProjects,
    ),
    [{ name: "GameGuild.TestingLab.UnitTests", filter: null }],
  );
});

test("selects multiple module tests without duplicates", () => {
  assert.deepEqual(
    selectAffectedDotnetTests(
      [
        "apps/api/Source/Modules/GameGuild.Projects/Project.cs",
        "apps/api/Source/Modules/GameGuild.Projects/ProjectVersion.cs",
        "apps/api/Source/Modules/GameGuild.TestingLab/TestingEvent.cs",
      ],
      availableProjects,
    ),
    [
      { name: "GameGuild.Projects.UnitTests", filter: null },
      { name: "GameGuild.TestingLab.UnitTests", filter: null },
    ],
  );
});

test("falls back to core tests for API infrastructure changes", () => {
  assert.deepEqual(
    selectAffectedDotnetTests(
      ["apps/api/Source/GameGuild.API/Program.cs"],
      availableProjects,
    ),
    [
      { name: "GameGuild.API.UnitTests", filter: null },
      { name: "GameGuild.SharedKernel.UnitTests", filter: null },
    ],
  );
});

test("selects only the changed test class when API tests are the only changes", () => {
  assert.deepEqual(
    selectAffectedDotnetTests(
      ["apps/api/tests/GameGuild.Projects.UnitTests/ProjectTests.cs"],
      availableProjects,
    ),
    [{ name: "GameGuild.Projects.UnitTests", filter: "FullyQualifiedName~ProjectTests" }],
  );
});

test("selects the matching integration test class for PostgreSQL test changes", () => {
  assert.deepEqual(
    selectAffectedDotnetTests(
      ["apps/api/tests/GameGuild.API.IntegrationTests/BulkPermissionChecksPostgreSqlTests.cs"],
      availableProjects,
    ),
    [
      {
        name: "GameGuild.API.IntegrationTests",
        filter: "FullyQualifiedName~BulkPermissionChecksPostgreSqlTests",
      },
    ],
  );
});

test("combines changed test files while keeping production changes on the full project", () => {
  assert.deepEqual(
    selectAffectedDotnetTests(
      [
        "apps/api/tests/GameGuild.Projects.UnitTests/ProjectTests.cs",
        "apps/api/tests/GameGuild.Projects.UnitTests/ProjectVersionTests.cs",
      ],
      availableProjects,
    ),
    [
      {
        name: "GameGuild.Projects.UnitTests",
        filter: "FullyQualifiedName~ProjectTests|FullyQualifiedName~ProjectVersionTests",
      },
    ],
  );

  assert.deepEqual(
    selectAffectedDotnetTests(
      [
        "apps/api/tests/GameGuild.Projects.UnitTests/ProjectTests.cs",
        "apps/api/Source/Modules/GameGuild.Projects/Project.cs",
      ],
      availableProjects,
    ),
    [{ name: "GameGuild.Projects.UnitTests", filter: null }],
  );
});
