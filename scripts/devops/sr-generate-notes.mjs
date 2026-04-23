#!/usr/bin/env node
/**
 * semantic-release generateNotesCmd implementation.
 *
 * Produces compact, conventional-commits-style release notes from
 * `<lastRelease.gitTag>..HEAD` and truncates to fit GitHub's 125 KB
 * release-body cap (https://docs.github.com/rest/releases/releases).
 *
 * Why this exists: the default `@semantic-release/release-notes-generator`
 * has no upper bound. When a release covers thousands of commits (e.g. the
 * v2.55.0 → v3.0.0 jump that pulled in a long-lived feature branch via a
 * single merge), the resulting body exceeds GitHub's limit and the publish
 * step fails with `422 body is too long`.
 *
 * semantic-release passes the full context as a JSON env var
 * `SEMANTIC_RELEASE_CONTEXT` is NOT a thing — instead it interpolates
 * `${nextRelease.gitTag}` etc. into the configured cmd string. We use the
 * git CLI to read the same data directly so this script stays self-contained.
 *
 * Inputs (env, all set by `@semantic-release/exec`):
 *   - LAST_GIT_TAG       e.g. "v2.55.0" (empty for first release)
 *   - NEXT_GIT_TAG       e.g. "v3.0.0"
 *   - NEXT_VERSION       e.g. "3.0.0"
 *   - REPO_URL           e.g. "https://github.com/gameguild-gg/gameguild"
 *   - RELEASE_DATE       e.g. "2026-04-23"
 *
 * Output: markdown to stdout. Logs to stderr only.
 */

import { execFileSync } from 'node:child_process';

// GitHub hard limit is 125_000 chars. Leave headroom for the truncation footer.
const MAX_BODY_CHARS = 120_000;

const lastTag = (process.env.LAST_GIT_TAG || '').trim();
const nextTag = (process.env.NEXT_GIT_TAG || '').trim();
const nextVersion = (process.env.NEXT_VERSION || '').trim();
const repoUrl = (process.env.REPO_URL || '').trim().replace(/\.git$/, '').replace(/\/$/, '');
const releaseDate = (process.env.RELEASE_DATE || new Date().toISOString().slice(0, 10)).trim();

if (!nextTag || !nextVersion || !repoUrl) {
  console.error('[sr-generate-notes] Missing required env (NEXT_GIT_TAG, NEXT_VERSION, REPO_URL).');
  process.exit(1);
}

const range = lastTag ? `${lastTag}..HEAD` : 'HEAD';
const compareUrl = lastTag ? `${repoUrl}/compare/${lastTag}...${nextTag}` : `${repoUrl}/commits/${nextTag}`;

// %x1f = unit separator, %x1e = record separator. Avoids fragile newline parsing.
const FMT = '%H%x1f%s%x1f%b%x1e';
let raw = '';
try {
  raw = execFileSync('git', ['log', `--format=${FMT}`, range], { encoding: 'utf8', maxBuffer: 256 * 1024 * 1024 });
} catch (err) {
  console.error(`[sr-generate-notes] git log failed: ${err.message}`);
  process.exit(1);
}

/**
 * Parse Conventional Commits header: `type(scope)?!?: subject`.
 * Returns { type, scope, breaking, subject } or null if not conventional.
 */
function parseHeader(header) {
  const m = /^(\w+)(?:\(([^)]+)\))?(!)?:\s*(.+)$/.exec(header);
  if (!m) return null;
  return { type: m[1].toLowerCase(), scope: m[2] || '', breaking: !!m[3], subject: m[4].trim() };
}

const SECTIONS = [
  { key: 'breaking', title: 'BREAKING CHANGES' },
  { key: 'feat', title: 'Features' },
  { key: 'fix', title: 'Bug Fixes' },
  { key: 'perf', title: 'Performance Improvements' },
  { key: 'revert', title: 'Reverts' },
];

const buckets = Object.fromEntries(SECTIONS.map((s) => [s.key, []]));
const breakingNotes = []; // collected from BREAKING CHANGE: footers regardless of type

const records = raw.split('\x1e').map((r) => r.trim()).filter(Boolean);
for (const rec of records) {
  const [sha, header = '', body = ''] = rec.split('\x1f');
  if (!sha || !header) continue;
  // Skip merge commit subjects ("Merge ..." with no conventional prefix).
  if (/^Merge\b/i.test(header) && !/^merge[(:]/i.test(header)) continue;
  const parsed = parseHeader(header);
  if (!parsed) continue;
  const shortSha = sha.slice(0, 7);
  const link = `[${shortSha}](${repoUrl}/commit/${sha})`;
  const scopePrefix = parsed.scope ? `**${parsed.scope}:** ` : '';
  const line = `* ${scopePrefix}${parsed.subject} (${link})`;

  if (buckets[parsed.type]) buckets[parsed.type].push(line);

  // Surface breaking changes (header bang OR `BREAKING CHANGE:` footer in body).
  const breakingFooter = /^BREAKING[ -]CHANGE:\s*([\s\S]+?)(?=\n\w+:|\n*$)/im.exec(body);
  if (parsed.breaking || breakingFooter) {
    const note = breakingFooter ? breakingFooter[1].trim().replace(/\s+/g, ' ') : parsed.subject;
    breakingNotes.push(`* ${scopePrefix}${note} (${link})`);
  }
}
buckets.breaking = breakingNotes;

const heading = lastTag
  ? `# [${nextVersion}](${compareUrl}) (${releaseDate})`
  : `# ${nextVersion} (${releaseDate})`;

function render(maxChars) {
  const parts = [heading, ''];
  for (const { key, title } of SECTIONS) {
    const lines = buckets[key];
    if (!lines.length) continue;
    parts.push(`### ${title}`, '');
    parts.push(...lines, '');
  }
  let out = parts.join('\n').trimEnd() + '\n';
  if (out.length <= maxChars) return out;

  // Truncate: keep heading + as many full lines as fit, then a footer.
  const footer =
    `\n\n_Release notes truncated to fit GitHub's 125 KB body limit. ` +
    `See the full commit list: ${compareUrl}._\n`;
  const budget = maxChars - footer.length;
  let kept = '';
  outer: for (const { key, title } of SECTIONS) {
    const lines = buckets[key];
    if (!lines.length) continue;
    const sectionHead = `### ${title}\n\n`;
    if (kept.length + sectionHead.length > budget) break;
    kept += sectionHead;
    for (const line of lines) {
      if (kept.length + line.length + 1 > budget) break outer;
      kept += line + '\n';
    }
    kept += '\n';
  }
  return `${heading}\n\n${kept.trimEnd()}${footer}`;
}

const body = render(MAX_BODY_CHARS);
console.error(`[sr-generate-notes] generated ${body.length} chars (cap ${MAX_BODY_CHARS}) for ${nextTag}`);
process.stdout.write(body);
