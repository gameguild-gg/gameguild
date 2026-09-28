#!/usr/bin/env node

import { execFileSync } from "node:child_process";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";

function normalizePath(filePath) {
  return filePath.trim().replaceAll("\\", "/").replace(/^\.\//, "");
}

export function selectAffectedDotnetTests(filePaths, availableProjects) {
  const selected = new Map();
  let requiresCoreFallback = false;

  function selectProject(name) {
    selected.set(name, { name, filters: null });
  }

  function selectTestFile(name, testName) {
    const current = selected.get(name);
    if (current?.filters === null) return;

    const filters = current?.filters ?? new Set();
    filters.add(`FullyQualifiedName~${testName}`);
    selected.set(name, { name, filters });
  }

  for (const rawPath of filePaths) {
    const filePath = normalizePath(rawPath);
    const moduleMatch = filePath.match(
      /^apps\/api\/Source\/Modules\/(GameGuild\.[^/]+)\//u,
    );
    const testProjectMatch = filePath.match(
      /^apps\/api\/tests\/(GameGuild\.[^/]+\.(?:UnitTests|IntegrationTests))\//u,
    );

    if (moduleMatch?.[1]) {
      const testName = `${moduleMatch[1]}.UnitTests`;
      if (availableProjects.includes(testName)) selectProject(testName);
      continue;
    }

    if (testProjectMatch?.[1] && availableProjects.includes(testProjectMatch[1])) {
      const testFile = filePath.match(/\/([^/]+)\.cs$/u)?.[1];
      if (testFile && /(?:Test|Tests|Spec|Specs)$/u.test(testFile)) {
        selectTestFile(testProjectMatch[1], testFile);
      } else {
        selectProject(testProjectMatch[1]);
      }
      continue;
    }

    if (filePath.startsWith("apps/api/Source/")) requiresCoreFallback = true;
  }

  if (requiresCoreFallback) {
    for (const fallback of ["GameGuild.API.UnitTests", "GameGuild.SharedKernel.UnitTests"]) {
      if (availableProjects.includes(fallback)) selectProject(fallback);
    }
  }

  return [...selected.values()]
    .map(({ name, filters }) => ({
      name,
      filter: filters ? [...filters].sort().join("|") : null,
    }))
    .sort((left, right) => left.name.localeCompare(right.name));
}

function parseArguments(argv) {
  const options = { base: "", head: "HEAD", filesFrom: "" };

  for (let index = 0; index < argv.length; index += 1) {
    const argument = argv[index];
    const value = argv[index + 1];
    if (argument === "--base" && value) options.base = value;
    else if (argument === "--head" && value) options.head = value;
    else if (argument === "--files-from" && value) options.filesFrom = value;
    else throw new TypeError(`Unknown or incomplete argument: ${argument ?? ""}`);
    index += 1;
  }

  return options;
}

function changedFiles(options) {
  if (options.filesFrom) {
    return readFileSync(options.filesFrom, "utf8").split(/\r?\n/u).filter(Boolean);
  }
  if (!options.base) throw new TypeError("--base is required when --files-from is not provided");

  return execFileSync(
    "git",
    ["diff", "--name-only", "--diff-filter=ACMR", `${options.base}...${options.head}`],
    { encoding: "utf8" },
  )
    .split(/\r?\n/u)
    .filter(Boolean);
}

function availableTestProjects(testRoot) {
  return readdirSync(testRoot, { withFileTypes: true })
    .filter((entry) =>
      entry.isDirectory() &&
      (entry.name.endsWith(".UnitTests") || entry.name.endsWith(".IntegrationTests")),
    )
    .map((entry) => entry.name)
    .sort();
}

function main() {
  const options = parseArguments(process.argv.slice(2));
  const testRoot = join(process.cwd(), "apps", "api", "tests");
  const selected = selectAffectedDotnetTests(
    changedFiles(options),
    availableTestProjects(testRoot),
  );

  for (const { name, filter } of selected) {
    process.stdout.write(`apps/api/tests/${name}/${name}.csproj\t${filter ?? ""}\n`);
  }
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) {
  main();
}
