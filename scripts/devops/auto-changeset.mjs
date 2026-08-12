#!/usr/bin/env node
// Auto-generate a changeset from conventional commits since the last release tag.
//
// Usage:
//   node scripts/devops/auto-changeset.mjs            # write .changeset/auto-<ts>.md
//   node scripts/devops/auto-changeset.mjs --dry-run  # print what would be written
//   node scripts/devops/auto-changeset.mjs --apply    # bump package.json versions directly
//
// Conventional-commit mapping (https://www.conventionalcommits.org/en/v1.0.0/):
//   `BREAKING CHANGE:` footer OR `feat(scope)!:` / `fix!:` etc.  → major
//   `feat:` / `feat(scope):`                                      → minor
//   `fix:` / `perf:` / `refactor:` / `revert:`                    → patch
//   anything else (chore, docs, style, test, ci, build)           → skipped
//
// `--apply` mode skips the .changeset machinery entirely and rewrites the
// `version` field of every fixed-group package.json in lockstep. Used by
// the Emception publish-packages CI job so it can ship a new version
// without running `changeset version` (no @changesets/cli install needed).
//
// ponytail: fixed group in .changeset/config.json handles cross-package
// propagation, so we only need to declare the bump for one canonical
// package. We declare it for all 6 publishable emception packages for
// human-readable changeset output and resilience if the fixed group is
// ever loosened.

import { execSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';

const ROOT = path.resolve(import.meta.dirname, '..', '..');
const DRY_RUN = process.argv.includes('--dry-run');
const APPLY_MODE = process.argv.includes('--apply');

// Fixed group: declaring a bump for any one of these propagates to all.
const PACKAGES = [
    'emception',
    '@gameguild/emception-browser',
    '@gameguild/emception-xterm',
    '@gameguild/emception-react',
    '@gameguild/emception-webcomponent',
    '@gameguild/emception-ide',
];

// Lockstep monorepo: all emception packages share one version.
// Mapping from package name (above) to its package.json path.
const PKG_PATHS = {
    'emception': 'tools/emception/packages/core/package.json',
    '@gameguild/emception-browser': 'tools/emception/packages/browser/package.json',
    '@gameguild/emception-xterm': 'tools/emception/packages/xterm/package.json',
    '@gameguild/emception-react': 'tools/emception/packages/react/package.json',
    '@gameguild/emception-webcomponent': 'tools/emception/packages/webcomponent/package.json',
    '@gameguild/emception-ide': 'tools/emception/packages/ide/package.json',
};

function bumpSemver(version, bump) {
    const [major, minor, patch] = version.split('.').map((n) => parseInt(n, 10));
    if (bump === 'major') return `${major + 1}.0.0`;
    if (bump === 'minor') return `${major}.${minor + 1}.0`;
    return `${major}.${minor}.${patch + 1}`;
}

function applyBump(bump) {
    // Read canonical version from core, bump once, write to all packages.
    const corePath = path.join(ROOT, PKG_PATHS['emception']);
    const corePkg = JSON.parse(readFileSync(corePath, 'utf8'));
    const current = corePkg.version;
    const next = bumpSemver(current, bump);

    let written = 0;
    for (const [name, relPath] of Object.entries(PKG_PATHS)) {
        const fullPath = path.join(ROOT, relPath);
        const pkg = JSON.parse(readFileSync(fullPath, 'utf8'));
        if (pkg.version !== next) {
            pkg.version = next;
            writeFileSync(fullPath, JSON.stringify(pkg, null, 2) + '\n');
            written++;
            console.log(`  ${name}: ${relPath}  ${current} → ${next}`);
        }
    }
    console.log(`[auto-changeset] Applied ${bump} bump: ${current} → ${next} (${written} files updated).`);
}

function git(args) {
    return execSync(`git ${args}`, { cwd: ROOT, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
}

function lastTag() {
    try {
        // `git describe --tags --abbrev=0` requires the tag to be an ancestor
        // of HEAD, which fails on divergent histories (e.g. post-migration
        // main). Use date-sorted tag list instead — returns the most recent
        // tag regardless of ancestry, which is what "what was last released"
        // actually means.
        const list = git('tag --sort=-creatordate');
        const top = list.split('\n').map((t) => t.trim()).filter(Boolean)[0];
        return top ?? null;
    } catch {
        return null;
    }
}

function commitsSince(tag) {
    const range = tag ? `${tag}..HEAD` : 'HEAD';
    // %H sha, %s subject, %b body, NUL-separated records.
    const raw = git(`log ${range} --no-merges --pretty=format:'%H%x1f%s%x1f%b%x1e'`);
    if (!raw) return [];
    return raw
        .split('\x1e')
        .map((r) => r.trim())
        .filter(Boolean)
        .map((r) => {
            const [sha, subject, body] = r.split('\x1f').map((s) => (s ?? '').trim());
            return { sha, subject, body };
        });
}

function classify({ subject, body }) {
    // Breaking: explicit footer OR `!:` marker in subject.
    const breaking = /BREAKING CHANGE:/i.test(body) || /^[a-z]+(\([^)]+\))?!:/.test(subject);
    if (breaking) return 'major';

    if (/^(feat|feature)(\([^)]+\))?:/.test(subject)) return 'minor';
    if (/^(fix|perf|refactor|revert)(\([^)]+\))?:/.test(subject)) return 'patch';

    return null; // skip: chore, docs, style, test, ci, build, merge, etc.
}

function pickBump(commits) {
    let bump = null;
    const featured = [];
    for (const c of commits) {
        const type = classify(c);
        if (!type) continue;
        featured.push(c);
        if (type === 'major') bump = 'major';
        else if (type === 'minor' && bump !== 'major') bump = 'minor';
        else if (type === 'patch' && bump === null) bump = 'patch';
    }
    return { bump, featured };
}

function buildChangeset(bump, featured, tag) {
    const frontMatter = PACKAGES.map((p) => `"${p}": ${bump}`).join('\n');
    const limit = 30;
    const notes = featured
        .slice(-limit)
        .map((c) => `- ${c.subject}`)
        .join('\n');
    const more = featured.length > limit ? `\n\n_…and ${featured.length - limit} more commits_` : '';
    const since = tag ? `since \`${tag}\`` : 'since the start of history';
    return `---\n${frontMatter}\n---\n\nAuto-generated from ${featured.length} release-relevant commit(s) ${since}.\n\n${notes}${more}\n`;
}

function main() {
    const tag = lastTag();
    const commits = commitsSince(tag);
    const { bump, featured } = pickBump(commits);

    if (!bump) {
        console.log(`[auto-changeset] No release-relevant commits ${tag ? `since ${tag}` : 'in history'}. Skipping.`);
        return;
    }

    if (APPLY_MODE) {
        console.log(`[auto-changeset] Applying ${bump} bump from ${featured.length} release-relevant commit(s) ${tag ? `since ${tag}` : 'in history'}.`);
        applyBump(bump);
        return;
    }

    const content = buildChangeset(bump, featured, tag);
    const filename = `.changeset/auto-${Date.now()}.md`;
    const filepath = path.join(ROOT, filename);

    if (DRY_RUN) {
        console.log(`[auto-changeset] DRY RUN — would write ${filename} (${bump} bump, ${featured.length} commits):`);
        console.log('---');
        console.log(content);
        return;
    }

    mkdirSync(path.join(ROOT, '.changeset'), { recursive: true });
    writeFileSync(filepath, content);
    console.log(`[auto-changeset] Wrote ${filename} (${bump} bump, ${featured.length} commits).`);
}

main();
