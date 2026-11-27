import assert from "node:assert/strict";
import { readFileSync, readdirSync } from "node:fs";
import test from "node:test";
import { integrationTestFilter } from "../api-integration-filter.mjs";

const groups = JSON.parse(readFileSync(new URL("../economy-integration-shards.json", import.meta.url), "utf8"));
const shards = ["api-integration-1", "api-integration-2", "api-integration-3", "api-integration-4", "api-integration-5"];

// Evaluate VSTest's substring terms independently of the filter generator.
function selected(filter, name) {
  return filter.split("|").some((alternative) => alternative.split("&").every((term) => {
    const match = /^FullyQualifiedName(!?~)(.+)$/u.exec(term);
    assert.ok(match, "Unexpected test-filter grammar");
    return match[1] === "~" ? name.includes(match[2]) : !name.includes(match[2]);
  }));
}

function integrationTestClasses() {
  const directory = new URL("../../../apps/api/tests/GameGuild.API.IntegrationTests/", import.meta.url);
  return readdirSync(directory).filter((file) => file.endsWith(".cs")).flatMap((file) =>
    [...readFileSync(new URL(file, directory), "utf8").matchAll(/public\s+(?:sealed\s+)?class\s+([A-Za-z0-9_]+Tests?)/gu)]
      .map((match) => `GameGuild.API.IntegrationTests.${match[1]}`));
}

test("every configured class belongs to exactly its declared shard", () => {
  for (const [shard, names] of Object.entries(groups)) {
    for (const name of names) {
      assert.deepEqual(shards.filter((key) => selected(integrationTestFilter(groups, key), name + ".Case")), [shard]);
    }
  }
});

test("new classes and longer class names are automatically in the complement", () => {
  for (const name of ["GameGuild.API.IntegrationTests.FutureRegression.Case", ...Object.values(groups).flat().map((name) => name + "Extended.Case")]) {
    assert.deepEqual(shards.filter((key) => selected(integrationTestFilter(groups, key), name)), ["api-integration-5"]);
  }
});

test("no partition outgrows a 16 GB runner: every shard stays within the class budget", () => {
  // One testhost per partition is what keeps the API integration suite on a hosted runner
  // (issue #1410). The complement is measured against the test project itself, so a shard
  // that grows past the budget fails here, before a runner dies, and asks to be split.
  const declared = Object.values(groups).flat();
  const complement = integrationTestClasses().filter((name) => !declared.includes(name));
  const budget = 60;
  for (const [shard, names] of [...Object.entries(groups), ["api-integration-5", complement]]) {
    assert.ok(names.length <= budget, `${shard} holds ${names.length} test classes; split it before it outgrows the runner`);
  }
  assert.ok(complement.length > 0, "the complement must stay a real partition");
});

test("overlapping groups are rejected before any test process starts", () => {
  const copy = structuredClone(groups);
  copy["api-integration-2"].push(copy["api-integration-1"][0]);
  assert.throws(() => integrationTestFilter(copy, "api-integration-5"), /disjoint/u);
});

test("every partition list agrees with the filter", () => {
  // A new partition is easy to add here and miss elsewhere: #1411 added a fifth and the
  // regression gate still accepted four by range, so its shard exited before any test ran.
  const root = new URL("../../../", import.meta.url);
  const read = (file) => readFileSync(new URL(file, root), "utf8");
  const known = [...shards].sort();
  for (const [file, pattern] of [
    [".github/workflows/pr-verify.yml", /shard: \[([^\]]+)\]/u],
    ["scripts/ci/run-affected-dotnet-tests.sh", /partitions=\(([^)]*api-integration-1[^)]*)\)/u],
  ]) {
    const listed = [...read(file).match(pattern)[1].matchAll(/api-integration-\d/gu)].map((match) => match[0]).sort();
    assert.deepEqual(listed, known, `${file} must name every integration partition`);
  }
  const gate = read("scripts/ci/verify-economy.sh");
  const range = /api-integration-\[(\d)-(\d)\]/u.exec(gate);
  assert.ok(range, "the regression gate must accept integration partitions by range");
  assert.equal(range[2], String(shards.length), "the regression gate must accept the last integration partition");
});

test("missing, empty and unknown groups cannot silently drop tests", () => {
  for (const mutate of [
    (copy) => { delete copy["api-integration-1"]; },
    (copy) => { copy["api-integration-1"] = []; },
    (copy) => { copy["api-integration-5"] = ["GameGuild.API.IntegrationTests.Other"]; },
  ]) {
    const copy = structuredClone(groups);
    mutate(copy);
    assert.throws(() => integrationTestFilter(copy, "api-integration-5"), TypeError);
  }
  assert.throws(() => integrationTestFilter(groups, "api-integration-6"), /Unknown/u);
});

test("class names cannot introduce extra filter operators", () => {
  const copy = structuredClone(groups);
  copy["api-integration-1"].push("GameGuild.API.IntegrationTests.Other|FullyQualifiedName~Anything");
  assert.throws(() => integrationTestFilter(copy, "api-integration-1"), /valid/u);
});
