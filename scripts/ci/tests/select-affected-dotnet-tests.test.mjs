import assert from "node:assert/strict";
import test from "node:test";

import { selectAffectedDotnetTestNames } from "../select-affected-dotnet-tests.mjs";

const availableProjects = [
  "GameGuild.API.UnitTests",
  "GameGuild.Identity.Authorization.IntegrationTests",
  "GameGuild.Projects.UnitTests",
  "GameGuild.SharedKernel.UnitTests",
  "GameGuild.TestingLab.UnitTests",
];

test("selects the test project matching a changed API module", () => {
  assert.deepEqual(
    selectAffectedDotnetTestNames(
      ["apps/api/Source/Modules/GameGuild.TestingLab/TestingEvent.cs"],
      availableProjects,
    ),
    ["GameGuild.TestingLab.UnitTests"],
  );
});

test("selects multiple module tests without duplicates", () => {
  assert.deepEqual(
    selectAffectedDotnetTestNames(
      [
        "apps/api/Source/Modules/GameGuild.Projects/Project.cs",
        "apps/api/Source/Modules/GameGuild.Projects/ProjectVersion.cs",
        "apps/api/Source/Modules/GameGuild.TestingLab/TestingEvent.cs",
      ],
      availableProjects,
    ),
    ["GameGuild.Projects.UnitTests", "GameGuild.TestingLab.UnitTests"],
  );
});

test("falls back to core tests for API infrastructure changes", () => {
  assert.deepEqual(
    selectAffectedDotnetTestNames(
      ["apps/api/Source/GameGuild.API/Program.cs"],
      availableProjects,
    ),
    ["GameGuild.API.UnitTests", "GameGuild.SharedKernel.UnitTests"],
  );
});

test("selects the matching project when an API integration test changes", () => {
  assert.deepEqual(
    selectAffectedDotnetTestNames(
      [
        "apps/api/tests/GameGuild.Identity.Authorization.IntegrationTests/PermissionCacheRedisIntegrationTests.cs",
      ],
      availableProjects,
    ),
    ["GameGuild.Identity.Authorization.IntegrationTests"],
  );
});

test("selects the matching project when an API unit test changes", () => {
  assert.deepEqual(
    selectAffectedDotnetTestNames(
      ["apps/api/tests/GameGuild.Projects.UnitTests/ProjectTests.cs"],
      availableProjects,
    ),
    ["GameGuild.Projects.UnitTests"],
  );
});
