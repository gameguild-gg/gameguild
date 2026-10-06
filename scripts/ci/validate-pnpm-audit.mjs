import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const severityLevels = ["info", "low", "moderate", "high", "critical"];

export function validateAuditReport(
  report,
  exitCode,
  verifiedBracesPatch = false,
  verifiedSprintfPatch = false,
) {
  const counts = report?.metadata?.vulnerabilities;
  const advisories = report?.advisories;
  if (
    report?.error ||
    !counts ||
    !advisories ||
    typeof advisories !== "object" ||
    Array.isArray(advisories)
  ) {
    throw new Error("pnpm audit did not return a complete advisory report");
  }
  for (const level of severityLevels) {
    if (!Number.isSafeInteger(counts[level]) || counts[level] < 0) {
      throw new Error(`Invalid pnpm audit vulnerability count: ${level}`);
    }
  }
  const items = Object.values(advisories);
  const reportedCount = severityLevels.reduce(
    (sum, level) => sum + counts[level],
    0,
  );
  if (reportedCount !== items.length) {
    throw new Error("pnpm audit metadata and advisory details disagree");
  }
  if (exitCode !== (items.length === 0 ? 0 : 1)) {
    throw new Error(
      `pnpm audit failed or returned an inconsistent exit code: ${exitCode}`,
    );
  }
  for (const advisory of items) {
    const knownPatchedBracesAdvisory =
      verifiedBracesPatch &&
      advisory.module_name === "braces" &&
      advisory.github_advisory_id === "GHSA-vfj7-8cjw-p6xm" &&
      advisory.url === "https://github.com/advisories/GHSA-vfj7-8cjw-p6xm" &&
      Array.isArray(advisory.cves) &&
      advisory.cves.length === 1 &&
      advisory.cves[0] === "CVE-2026-93687" &&
      Array.isArray(advisory.findings) &&
      advisory.findings.length > 0 &&
      advisory.findings.every((finding) => finding.version === "3.0.3");
    const knownPatchedSprintfAdvisory =
      verifiedSprintfPatch &&
      advisory.module_name === "sprintf-js" &&
      advisory.github_advisory_id === "GHSA-hp3w-g68c-fv3c" &&
      advisory.url === "https://github.com/advisories/GHSA-hp3w-g68c-fv3c" &&
      Array.isArray(advisory.cves) &&
      advisory.cves.length === 1 &&
      advisory.cves[0] === "CVE-2026-97058" &&
      Array.isArray(advisory.findings) &&
      advisory.findings.length > 0 &&
      advisory.findings.every((finding) => finding.version === "1.1.3");
    if (!knownPatchedBracesAdvisory && !knownPatchedSprintfAdvisory) {
      throw new Error(
        `Unmitigated dependency advisory: ${advisory.github_advisory_id ?? advisory.id ?? "unknown"}`,
      );
    }
  }
  return { advisories: items.length, verifiedLocalPatches: items.length };
}

const invokedDirectly =
  process.argv[1] &&
  resolve(process.argv[1]) === fileURLToPath(import.meta.url);
if (invokedDirectly) {
  try {
    const report = JSON.parse(readFileSync(process.argv[2], "utf8"));
    const patchCheck = spawnSync(
      process.execPath,
      [
        "--test",
        resolve(
          dirname(fileURLToPath(import.meta.url)),
          "tests/braces-security.test.mjs",
        ),
      ],
      { stdio: "inherit" },
    );
    if (patchCheck.error || patchCheck.status !== 0)
      throw new Error("Installed braces security patch verification failed");
    const sprintfPatchCheck = spawnSync(
      process.execPath,
      [
        "--test",
        resolve(
          dirname(fileURLToPath(import.meta.url)),
          "tests/sprintf-security.test.mjs",
        ),
      ],
      { stdio: "inherit" },
    );
    if (sprintfPatchCheck.error || sprintfPatchCheck.status !== 0)
      throw new Error(
        "Installed sprintf-js security patch verification failed",
      );
    const result = validateAuditReport(
      report,
      Number(process.argv[3]),
      true,
      true,
    );
    console.log(
      `Dependency audit passed: ${result.advisories} reported advisories, ${result.verifiedLocalPatches} verified local patches`,
    );
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
