#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { pathToFileURL } from "node:url";

export function integrationTestFilter(groups, shard) {
  const explicit = ["api-integration-1", "api-integration-2", "api-integration-3", "api-integration-4"];
  if (!groups || Object.keys(groups).sort().join() !== explicit.join()) {
    throw new TypeError("Integration shards require four explicit groups plus their complement");
  }
  if (explicit.some((key) => !Array.isArray(groups[key]) || groups[key].length === 0)) {
    throw new TypeError("An explicit integration shard is empty or invalid");
  }
  const names = explicit.flatMap((key) => groups[key]);
  if (new Set(names).size !== names.length || names.some((name) =>
    typeof name !== "string" || !/^GameGuild\.API\.IntegrationTests\.[A-Za-z0-9_]+$/u.test(name))) {
    throw new TypeError("Integration shard classes must be valid and disjoint");
  }
  // The complement is a partition of its own: one testhost per shard keeps each process
  // small enough for a 16 GB runner (issue #1410). New classes land here by default.
  if (shard === "api-integration-5") {
    return names.map((name) => `FullyQualifiedName!~${name}.`).join("&");
  }
  if (!explicit.includes(shard)) throw new TypeError("Unknown integration shard");
  return groups[shard].map((name) => `FullyQualifiedName~${name}.`).join("|");
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) {
  const groups = JSON.parse(readFileSync(new URL("economy-integration-shards.json", import.meta.url), "utf8"));
  process.stdout.write(integrationTestFilter(groups, process.argv[2]) + "\n");
}
