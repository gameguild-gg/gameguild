#!/usr/bin/env node
/**
 * semantic-release prepareCmd implementation.
 *
 * Updates the version field in the root package.json and every npm workspace
 * manifest declared by the repository. This intentionally avoids broad
 * `find . -name package.json` or `**\/package.json` patterns, which can capture
 * dependency manifests under node_modules or build output.
 */

import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const repoRoot = process.cwd();
const nextVersion = (process.argv[2] || '').trim();

if (!nextVersion) {
    console.error('[sr-prepare] Missing required version argument.');
    process.exit(1);
}

const rootPackageJsonPath = path.join(repoRoot, 'package.json');
const rootPackage = JSON.parse(fs.readFileSync(rootPackageJsonPath, 'utf8'));
const workspaces = Array.isArray(rootPackage.workspaces) ? rootPackage.workspaces : [];

function updateManifest(manifestPath) {
    if (!fs.existsSync(manifestPath)) return;

    const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
    manifest.version = nextVersion;
    fs.writeFileSync(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`);
}

function expandWorkspacePattern(pattern) {
    const normalizedPattern = pattern.replace(/\\/g, '/');
    const segments = normalizedPattern.split('/');
    const wildcardIndex = segments.indexOf('*');

    if (wildcardIndex === -1) {
        return [path.join(repoRoot, normalizedPattern, 'package.json')];
    }

    const prefixSegments = segments.slice(0, wildcardIndex);
    const suffixSegments = segments.slice(wildcardIndex + 1);
    const parentDir = path.join(repoRoot, ...prefixSegments);

    if (!fs.existsSync(parentDir)) return [];

    return fs
        .readdirSync(parentDir, { withFileTypes: true })
        .filter((entry) => entry.isDirectory())
        .map((entry) => path.join(parentDir, entry.name, ...suffixSegments, 'package.json'));
}

updateManifest(rootPackageJsonPath);

const manifestPaths = new Set();
for (const pattern of workspaces) {
    for (const manifestPath of expandWorkspacePattern(pattern)) {
        manifestPaths.add(manifestPath);
    }
}

for (const manifestPath of [...manifestPaths].sort()) {
    updateManifest(manifestPath);
}

console.error(`[sr-prepare] Updated ${manifestPaths.size + 1} package.json files to ${nextVersion}.`);
