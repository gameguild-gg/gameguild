import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

const rulesetPath = new URL("../../../.github/rulesets/main.json", import.meta.url);
const releaseWorkflowPath = new URL("../../../.github/workflows/production-release.yml", import.meta.url);

test("main ruleset requires a current approved PR and the stable gate", async () => {
  const ruleset = JSON.parse(await readFile(rulesetPath, "utf8"));
  const pullRequest = ruleset.rules.find((rule) => rule.type === "pull_request");
  const checks = ruleset.rules.find((rule) => rule.type === "required_status_checks");

  assert.equal(ruleset.enforcement, "active");
  assert.deepEqual(ruleset.conditions.ref_name.include, ["refs/heads/main"]);
  assert.equal(pullRequest.parameters.required_approving_review_count, 1);
  assert.equal(pullRequest.parameters.required_review_thread_resolution, true);
  assert.equal(pullRequest.parameters.require_last_push_approval, true);
  assert.deepEqual(pullRequest.parameters.allowed_merge_methods, ["squash"]);
  assert.equal(checks.parameters.strict_required_status_checks_policy, true);
  assert.deepEqual(checks.parameters.required_status_checks, [{ context: "PR Required Gate" }]);
});

test("manual releases are an administrator-only audited hotfix path", async () => {
  const workflow = await readFile(releaseWorkflowPath, "utf8");

  assert.match(workflow, /workflow_dispatch:/);
  assert.match(workflow, /\^hotfix\/\.\+/);
  assert.match(workflow, /Only a repository administrator may promote a production hotfix/);
  assert.match(workflow, /The hotfix branch must contain the latest main commit/);
  assert.match(workflow, /--workflow pr-verify\.yml/);
  assert.match(workflow, /No successful PR Verify run exists/);
  assert.match(workflow, /DEPLOY HOTFIX/);
});
