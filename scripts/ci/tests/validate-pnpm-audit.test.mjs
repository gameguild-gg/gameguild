import assert from "node:assert/strict";
import { test } from "node:test";
import { validateAuditReport } from "../validate-pnpm-audit.mjs";

const report = () => ({
  advisories: {},
  metadata: {
    vulnerabilities: { info: 0, low: 0, moderate: 0, high: 0, critical: 0 },
  },
});
const patchedReport = () => {
  const result = report();
  result.metadata.vulnerabilities.high = 1;
  result.advisories["1240992"] = {
    module_name: "braces",
    github_advisory_id: "GHSA-vfj7-8cjw-p6xm",
    url: "https://github.com/advisories/GHSA-vfj7-8cjw-p6xm",
    cves: ["CVE-2026-93687"],
    findings: [{ version: "3.0.3" }],
  };
  return result;
};

test("clean audit reports pass", () => {
  assert.deepEqual(validateAuditReport(report(), 0), {
    advisories: 0,
    verifiedLocalPatches: 0,
  });
});
test("only the exact advisory with verified installed patches passes", () => {
  assert.deepEqual(validateAuditReport(patchedReport(), 1, true), {
    advisories: 1,
    verifiedLocalPatches: 1,
  });
  assert.throws(() => validateAuditReport(patchedReport(), 1), /Unmitigated/);
});
test("missing metadata, registry errors, and inconsistent exit codes fail", () => {
  for (const invalid of [
    {},
    { error: { code: "ENETUNREACH" } },
    { advisories: {}, metadata: {} },
  ]) {
    assert.throws(
      () => validateAuditReport(invalid, 1, true),
      /complete advisory report/,
    );
  }
  for (const exitCode of [1, 2, NaN])
    assert.throws(
      () => validateAuditReport(report(), exitCode, true),
      /exit code/,
    );
  assert.throws(
    () => validateAuditReport(patchedReport(), 0, true),
    /exit code/,
  );
});
test("filtered or malformed reports cannot hide an advisory", () => {
  for (const advisories of [[], "not an object", 42]) {
    assert.throws(
      () => validateAuditReport({ ...report(), advisories }, 0, true),
      /complete advisory report/,
    );
  }
  const filtered = report();
  filtered.metadata.vulnerabilities.high = 1;
  assert.throws(() => validateAuditReport(filtered, 1, true), /disagree/);
  for (const count of [undefined, -1, 0.5, "1"]) {
    const invalid = report();
    invalid.metadata.vulnerabilities.high = count;
    assert.throws(
      () => validateAuditReport(invalid, 1, true),
      /Invalid.*count/,
    );
  }
});
test("different packages, advisories, CVEs, and unpatched versions fail", () => {
  for (const mutation of [
    { module_name: "another-package" },
    { github_advisory_id: "GHSA-another-advisory" },
    { url: "https://github.com/advisories/GHSA-another-advisory" },
    { cves: ["CVE-2026-93687", "CVE-another"] },
    { cves: [] },
    { findings: [{ version: "3.0.2" }] },
    { findings: [] },
    { findings: [{ version: "3.0.3" }, { version: "3.0.2" }] },
  ]) {
    const invalid = patchedReport();
    Object.assign(invalid.advisories["1240992"], mutation);
    assert.throws(() => validateAuditReport(invalid, 1, true), /Unmitigated/);
  }
});
test("an additional advisory fails even when the braces patch is verified", () => {
  const invalid = patchedReport();
  invalid.advisories.other = {
    module_name: "other",
    github_advisory_id: "GHSA-other",
  };
  invalid.metadata.vulnerabilities.low = 1;
  assert.throws(
    () => validateAuditReport(invalid, 1, true),
    /Unmitigated.*GHSA-other/,
  );
});

test("sprintf mitigation passes only with exact identity, version and installed patch proof", () => {
  const mitigated = report();
  mitigated.metadata.vulnerabilities.moderate = 1;
  mitigated.advisories["1241202"] = {
    module_name: "sprintf-js",
    github_advisory_id: "GHSA-hp3w-g68c-fv3c",
    url: "https://github.com/advisories/GHSA-hp3w-g68c-fv3c",
    cves: ["CVE-2026-97058"],
    findings: [{ version: "1.1.3" }],
  };
  assert.throws(() => validateAuditReport(mitigated, 1, true), /Unmitigated/);
  assert.deepEqual(validateAuditReport(mitigated, 1, false, true), {
    advisories: 1,
    verifiedLocalPatches: 1,
  });
  for (const mutation of [
    { module_name: "another-package" },
    { github_advisory_id: "GHSA-other" },
    { url: "https://github.com/advisories/GHSA-other" },
    { cves: [] },
    { cves: ["CVE-other"] },
    { findings: [] },
    { findings: [{ version: "1.0.3" }] },
    { findings: [{ version: "1.1.3" }, { version: "1.0.3" }] },
  ]) {
    const invalid = structuredClone(mitigated);
    Object.assign(invalid.advisories["1241202"], mutation);
    assert.throws(
      () => validateAuditReport(invalid, 1, true, true),
      /Unmitigated/,
    );
  }
});
