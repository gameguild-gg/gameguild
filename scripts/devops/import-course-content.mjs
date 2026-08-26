#!/usr/bin/env node
// Import course content from a local course folder into the GameGuild API.
//
// Usage:
//   node scripts/devops/import-course-content.mjs --slug <folder> [flags]
//
// Flags:
//   --slug <folder>         course folder under apps/web/src/data/courses (required)
//   --dry-run               default mode: plan only, no writes
//   --execute               write mode: upsert metadata + content via REST
//   --prune-stale           delete content whose slug is not in the imported set
//                           (only meaningful with --execute; dry-run lists candidates)
//   --skip-source-ids <csv> comma-separated sourceIds to exclude from the import
//   --self-check            offline parse-only assertions; no creds, no HTTP
//   --auth-probe            test-only: sign in with env creds, print token length
//                           + tenantId, exit; no API reads/writes beyond sign-in
//
// Env:
//   GG_API_URL      API base (default https://api.gameguild.gg)
//   GG_API_EMAIL    admin email (fallback GG_USER_EMAIL)
//   GG_API_PASSWORD admin password (fallback GG_USER_PASSWORD) — never printed
//
// Evidence: every HTTP call is appended as one JSONL line
// {ts,method,url,status,ok} to .omo/evidence/import-course-content/<mode>-<runTs>.jsonl

import { randomUUID } from 'node:crypto';
import { appendFileSync, existsSync, mkdirSync, readFileSync } from 'node:fs';
import path from 'node:path';

const REPO_ROOT = path.resolve(import.meta.dirname, '..', '..');
const COURSES_ROOT = path.join(REPO_ROOT, 'apps/web/src/data/courses');
const EVIDENCE_DIR = path.join(REPO_ROOT, '.omo/evidence/import-course-content');
const DEFAULT_API_URL = 'https://api.gameguild.gg';

function fail(message) {
  console.error(`[error] ${message}`);
  process.exit(1);
}

// ===== cli =====

function parseArgs(argv) {
  const args = {
    slug: null,
    dryRun: false,
    execute: false,
    pruneStale: false,
    skipSourceIds: [],
    selfCheck: false,
    authProbe: false,
  };

  for (let i = 0; i < argv.length; i += 1) {
    const flag = argv[i];
    switch (flag) {
      case '--slug': {
        const value = argv[i + 1];
        if (!value || value.startsWith('--')) fail('--slug requires a value');
        args.slug = value;
        i += 1;
        break;
      }
      case '--dry-run':
        args.dryRun = true;
        break;
      case '--execute':
        args.execute = true;
        break;
      case '--prune-stale':
        args.pruneStale = true;
        break;
      case '--skip-source-ids': {
        const value = argv[i + 1];
        if (!value || value.startsWith('--')) fail('--skip-source-ids requires a csv value');
        const ids = value.split(',').map((id) => id.trim()).filter(Boolean);
        for (const id of ids) {
          if (!/^[A-Za-z0-9][A-Za-z0-9_-]*$/.test(id)) {
            fail(`--skip-source-ids: invalid identifier "${id}"`);
          }
        }
        args.skipSourceIds = ids;
        i += 1;
        break;
      }
      case '--self-check':
        args.selfCheck = true;
        break;
      case '--auth-probe':
        args.authProbe = true;
        break;
      default:
        fail(`unknown flag: ${flag}`);
    }
  }

  if (!args.slug) fail('slug required');
  if (args.execute && args.selfCheck) fail('--execute and --self-check are mutually exclusive');
  if (args.authProbe && args.execute) fail('--auth-probe and --execute are mutually exclusive');
  if (args.authProbe && args.selfCheck) fail('--auth-probe and --self-check are mutually exclusive');

  if (args.authProbe) args.mode = 'authprobe';
  else if (args.selfCheck) args.mode = 'selfcheck';
  else if (args.execute) args.mode = 'execute';
  else args.mode = 'dryrun';

  return args;
}

// ===== env =====

function resolveEnv() {
  return {
    apiUrl: process.env.GG_API_URL || DEFAULT_API_URL,
    email: process.env.GG_API_EMAIL || process.env.GG_USER_EMAIL || null,
    password: process.env.GG_API_PASSWORD || process.env.GG_USER_PASSWORD || null,
  };
}

function validateEnv(env, mode) {
  if (mode === 'selfcheck') return;
  const missing = [];
  if (!env.email) missing.push('GG_API_EMAIL (or GG_USER_EMAIL)');
  if (!env.password) missing.push('GG_API_PASSWORD (or GG_USER_PASSWORD)');
  if (missing.length > 0) fail(`missing env: ${missing.join(', ')}`);
}

function maskSecret(value) {
  return value ? '***' : '(unset)';
}

// ===== evidence logger =====

const runTs = new Date().toISOString().replace(/[:.-]/g, '');
let evidenceFile = null;

function initEvidence(mode) {
  mkdirSync(EVIDENCE_DIR, { recursive: true });
  evidenceFile = path.join(EVIDENCE_DIR, `${mode}-${runTs}.jsonl`);
}

function appendEvidenceLine(obj) {
  if (!evidenceFile) return;
  appendFileSync(evidenceFile, `${JSON.stringify(obj)}\n`);
}

export function logHttp({ method, url, status, ok }) {
  appendEvidenceLine({ ts: new Date().toISOString(), method, url, status, ok });
}

export function logEvent(obj) {
  appendEvidenceLine({ ts: new Date().toISOString(), event: obj });
}

// ===== parser (task 2-3) =====
// Ported from SnapshotCourseSeeder.cs (ParseCourseDefinition :240-278, ParseContentDefinition
// :429-476, ResolveContentBody :456-476, ResolveMarkdownPath :503-509, extractors :539-602,
// regexes :966-973). ONE deviation from the C#, documented at the object regexes: `export` is
// optional because intro2gpro/game-publishing declare contents as plain `const`.

// C# MarkdownImportRegex (:966). 'g' because .Matches() scans all; C# default has no
// Singleline/Multiline, and the pattern needs neither.
const MarkdownImportRegex = /import\s+(?<name>[A-Za-z0-9_]+)\s+from\s+['"](?<path>\.[^'"]+\.md)['"];/g;

// C# :969/:972 use Singleline|Multiline → JS 'gms' (s = dot-matches-newline for C# Singleline,
// m = ^ per-line for C# Multiline). Deviation: C# anchors on `export const`; the regexes here
// accept `(?:export\s+)?const` so plain-`const` courses (intro2gpro) also parse. Type
// annotations still gate strictly (`: Program = {` / `: ProgramContent = {`), so Product and
// ProductProgram objects and the `*.programContents = [...]` wiring blocks never match.
const ProgramObjectRegex = /(?:export\s+)?const\s+\w+Program:\s*Program\s*=\s*\{(?<body>.*?)^\};/gms;
const ProgramContentObjectRegex = /(?:export\s+)?const\s+\w+Content:\s*ProgramContent\s*=\s*\{(?<body>.*?)^\};/gms;

function escapeRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// C# Regex.Unescape → JSON string unescape; on malformed input fall back to the raw text.
function unescapeString(raw) {
  try {
    return JSON.parse(`"${raw}"`);
  } catch {
    return raw;
  }
}

// C# extractors use RegexOptions.Singleline → JS 's' flag (dot matches newline). Field names
// are anchored with \b exactly as in the C# (`^\s*fieldName`-style anchors are NOT used).
function extractString(objectBody, fieldName) {
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*(?<quote>['"])(?<value>(?:\\\\.|(?!\\k<quote>).)*)\\k<quote>`,
    's',
  ).exec(objectBody);
  return match ? unescapeString(match.groups.value) : null;
}

function extractNullableString(objectBody, fieldName) {
  const nullMatch = new RegExp(`\\b${escapeRegex(fieldName)}\\s*:\\s*null\\b`, 's').exec(objectBody);
  return nullMatch ? null : extractString(objectBody, fieldName);
}

function extractNullableInt(objectBody, fieldName) {
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*(?<value>-?\\d+|null)\\b`,
    's',
  ).exec(objectBody);
  if (!match || match.groups.value === 'null') return null;
  return Number.parseInt(match.groups.value, 10);
}

function extractBoolean(objectBody, fieldName) {
  // C# adds IgnoreCase → 'i'.
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*(?<value>true|false)\\b`,
    'si',
  ).exec(objectBody);
  return match ? match.groups.value.toLowerCase() === 'true' : null;
}

function extractIdentifier(objectBody, fieldName) {
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*(?<value>[A-Za-z_][A-Za-z0-9_]*)\\b`,
    's',
  ).exec(objectBody);
  return match ? match.groups.value : null;
}

function extractTrailingComment(objectBody, fieldName) {
  const match = new RegExp(
    `\\b${escapeRegex(fieldName)}\\s*:\\s*[^\\r\\n,]+,\\s*//\\s*(?<comment>[^\\r\\n]+)`,
    's',
  ).exec(objectBody);
  return match ? match.groups.comment.trim() : null;
}

// C# NormalizeLabel (:956-964). Exported — task 3 (inference) consumes it.
export function normalizeLabel(value) {
  if (value === null || value === undefined || value.trim() === '') return '';
  return value.replace(/[^A-Za-z0-9]+/g, '').toLowerCase();
}

// C# ResolveMarkdownPath (:503-509): strip every "./" occurrence, swap '/' for the platform
// separator, then combine against the course dir.
function resolveMarkdownPath(courseDirectory, relativeMarkdownPath) {
  const normalized = relativeMarkdownPath.split('./').join('').split('/').join(path.sep);
  return path.resolve(courseDirectory, normalized);
}

function parseMarkdownImports(fileText) {
  const imports = new Map();
  for (const match of fileText.matchAll(MarkdownImportRegex)) {
    imports.set(match.groups.name, match.groups.path);
  }
  return imports;
}

function extractFirstProgramObject(fileText) {
  const match = fileText.matchAll(ProgramObjectRegex).next().value;
  if (!match) {
    throw new Error('Unable to locate the top-level Program export in the snapshot course definition.');
  }
  return match.groups.body;
}

function extractProgramContentObjects(fileText) {
  return [...fileText.matchAll(ProgramContentObjectRegex)].map((match) => match.groups.body);
}

function resolveContentBody(courseDirectory, markdownImports, contentBody, sourceId, bodyImportName) {
  if (bodyImportName !== null && bodyImportName.trim() !== '' && markdownImports.has(bodyImportName)) {
    return readFileSync(resolveMarkdownPath(courseDirectory, markdownImports.get(bodyImportName)), 'utf8');
  }

  const inlineBody = extractString(contentBody, 'body');
  if (inlineBody !== null) {
    return inlineBody;
  }

  throw new Error(`ProgramContent '${sourceId}' does not declare a markdown import body or inline string body.`);
}

function parseContentDefinition(courseDirectory, markdownImports, contentBody) {
  const sourceId = extractString(contentBody, 'id') ?? randomUUID().replaceAll('-', '');
  const title = extractString(contentBody, 'title') ?? sourceId;
  const description = extractString(contentBody, 'description') ?? '';
  const bodyImportName = extractIdentifier(contentBody, 'body');
  const body = resolveContentBody(courseDirectory, markdownImports, contentBody, sourceId, bodyImportName);
  const typeComment = extractTrailingComment(contentBody, 'type');
  // Only quoted parentId values match; `parentId: undefined` yields null.
  const parentSourceId = extractString(contentBody, 'parentId');

  return {
    sourceId,
    parentSourceId,
    title,
    description,
    rawType: extractNullableInt(contentBody, 'type'),
    typeComment,
    bodyImportName,
    bodyResolvedFromFile:
      bodyImportName !== null && bodyImportName.trim() !== '' && markdownImports.has(bodyImportName),
    body,
    sortOrder: extractNullableInt(contentBody, 'sortOrder') ?? 0,
    isRequired: extractBoolean(contentBody, 'isRequired') ?? true,
    estimatedMinutes: extractNullableInt(contentBody, 'estimatedMinutes'),
  };
}

function parseCourse(courseDirectory) {
  const indexFilePath = path.join(courseDirectory, 'index.ts');
  if (!existsSync(indexFilePath)) {
    throw new Error(`Course definition file not found: ${indexFilePath}`);
  }

  const fileText = readFileSync(indexFilePath, 'utf8');
  const markdownImports = parseMarkdownImports(fileText);
  const programBody = extractFirstProgramObject(fileText);

  const slug = extractString(programBody, 'slug') ?? path.basename(courseDirectory);
  const program = {
    slug,
    title: extractString(programBody, 'title') ?? slug,
    description: extractString(programBody, 'description') ?? '',
    thumbnail: extractNullableString(programBody, 'thumbnail'),
    estimatedHours: extractNullableInt(programBody, 'estimatedHours'),
    rawCategory: extractNullableInt(programBody, 'category'),
    rawDifficulty: extractNullableInt(programBody, 'difficulty'),
    rawEnrollmentStatus: extractNullableInt(programBody, 'enrollmentStatus'),
  };

  const contents = extractProgramContentObjects(fileText).map((contentBody) =>
    parseContentDefinition(courseDirectory, markdownImports, contentBody),
  );

  return { program, contents };
}

// Test-only: GG_COURSES_ROOT lets --self-check (and negative tests) point at a scratch courses
// root (e.g. /tmp) without touching the repo default. Read at call time; not part of the
// import flow contract.
function parseCourseDir(slug) {
  const root = process.env.GG_COURSES_ROOT || COURSES_ROOT;
  return parseCourse(path.join(root, slug));
}

// ===== inference (task 3) =====
// Ported verbatim from SnapshotCourseSeeder.cs: ParseProgramContentType (:818-907),
// ParseProgramCategory + ByHeuristic (:604-738), ParseProgramDifficulty (:740-783),
// ParseEnrollmentStatus (:785-816). C# enum returns become API wire strings.
// Keyword-vs-raw combination semantics (as in the C#): comment/semantic keyword hits always WIN
// over the raw int; the raw int is only a fallback when no keyword matched — and raw 0 never
// has its own arm in the difficulty/enrollment switches, so it falls to the default
// (Beginner/Open).

// C# :818-907. Returns the pre-normalization ProgramContentType name ('Page'/'Challenge'
// included); toApiType() maps those to API-valid wire values.
function parseProgramContentType(typeComment, rawType, title, description, sourceId, bodyImportName) {
  const normalized = normalizeLabel(typeComment);
  const semanticHint = normalizeLabel(`${title} ${description} ${sourceId} ${bodyImportName}`);

  if (normalized.includes('assignment')) return 'Assignment';
  if (
    normalized.includes('questionnaire') || normalized.includes('quiz') || normalized.includes('test')
  ) {
    return 'Questionnaire';
  }
  if (normalized.includes('discussion')) return 'Discussion';
  if (normalized.includes('code')) return 'Code';
  if (normalized.includes('challenge')) return 'Challenge';
  if (normalized.includes('reflection')) return 'Reflection';
  if (normalized.includes('survey')) return 'Survey';
  if (normalized.includes('lesson')) return 'Lesson';
  if (
    semanticHint.includes('syllabus') || semanticHint.includes('lecture') || semanticHint.includes('readings')
    || semanticHint.includes('reveal') || semanticHint.includes('slides')
  ) {
    return 'Lesson';
  }
  if (normalized.includes('page')) return 'Page';
  if (semanticHint.includes('quiz')) return 'Questionnaire';
  if (
    semanticHint.includes('assignment') || semanticHint.includes('exercise') || semanticHint.includes('project')
    || semanticHint.includes('midterm') || semanticHint.includes('final')
  ) {
    return 'Assignment';
  }

  switch (rawType) {
    case 0: return 'Page';
    case 1: return 'Lesson';
    case 2: return 'Assignment';
    case 3: return 'Questionnaire';
    // C# `_` arm; also catches rawType null.
    default: return 'Page';
  }
}

// 'Page' and 'Challenge' are legacy: "normalized on read and not valid for new content"
// (types.gen.ts:7375-7385 — LearningCoursesProgramContentType lists neither). Map them to
// their modern equivalents before anything reaches the create/update API.
function toApiType(inferredType) {
  if (inferredType === 'Page') return 'Lesson';
  if (inferredType === 'Challenge') return 'Assignment';
  return inferredType;
}

// C# :604-638. categoryComment is always null at the call site: no courses/*/index.ts carries
// program-level trailing comments (verified intro2gpro) and the T2 parser keeps them
// unextracted; the slug map / heuristic / raw fallback decide in practice.
function parseProgramCategory(categoryComment, rawCategory, title, description, slug) {
  const normalized = normalizeLabel(categoryComment);
  const semanticHint = normalizeLabel(`${categoryComment} ${title} ${description} ${slug}`);
  const normalizedSlug = normalizeLabel(slug);

  if (normalizedSlug.length > 0) {
    switch (normalizedSlug) {
      case 'python':
      case 'dsa':
        return 'Programming';
      case 'portfolio':
        return 'Design';
      case 'intro2gpro':
      case 'networking':
        return 'GameDevelopment';
      case 'gamepublishing':
        return 'Business';
      case 'databases':
        return 'Database';
      case 'dataanalysis':
        return 'DataScience';
      case 'ai4games':
      case 'ai4games2':
        return 'AI';
      default:
        return parseProgramCategoryByHeuristic(normalized, semanticHint, rawCategory);
    }
  }

  return parseProgramCategoryByHeuristic(normalized, semanticHint, rawCategory);
}

// C# :631-738. 'WebDevelopment' is a valid wire value (types.gen.ts:8851-8867 ProgramCategory).
function parseProgramCategoryByHeuristic(normalized, semanticHint, rawCategory) {
  if (
    semanticHint.includes('database') || semanticHint.includes('sql') || semanticHint.includes('nosql')
  ) {
    return 'Database';
  }

  if (
    semanticHint.includes('datascience') || semanticHint.includes('dataanalysis')
    || semanticHint.includes('analytics') || semanticHint.includes('pandas')
  ) {
    return 'DataScience';
  }

  if (
    normalized.includes('design') || semanticHint.includes('portfolio')
    || semanticHint.includes('userexperience') || semanticHint.includes('userinterface')
    || semanticHint.includes('uidesign') || semanticHint.includes('uxdesign')
    || semanticHint.includes('visualdesign') || semanticHint.includes('interactiondesign')
  ) {
    return 'Design';
  }

  if (
    semanticHint.includes('artificialintelligence') || semanticHint.includes('machinelearning')
    || semanticHint.includes('pathfinding') || semanticHint.includes('behaviortree')
    || semanticHint.includes('minmax') || semanticHint.includes('mcts')
    || semanticHint.includes('gameai')
  ) {
    return 'AI';
  }

  if (
    semanticHint.includes('gamedevelopment') || semanticHint.includes('gameprogramming')
    || semanticHint.includes('unity') || semanticHint.includes('unreal')
    || (semanticHint.includes('game') && semanticHint.includes('dev'))
  ) {
    return 'GameDevelopment';
  }

  if (
    normalized.includes('gamedevelopment')
    || (normalized.includes('game') && normalized.includes('dev'))
  ) {
    return 'GameDevelopment';
  }

  if (normalized.includes('programming')) {
    return 'Programming';
  }

  if (normalized.includes('datascience') || normalized.includes('dataanalysis')) {
    return 'DataScience';
  }

  if (normalized.includes('database')) {
    return 'Database';
  }

  if (normalized.includes('business')) {
    return 'Business';
  }

  if (normalized.includes('design')) {
    return 'Design';
  }

  if (normalized.includes('webdevelopment') || normalized.includes('web')) {
    return 'WebDevelopment';
  }

  if (
    normalized.includes('ai') || normalized.includes('artificialintelligence')
    || normalized.includes('machinelearning')
  ) {
    return 'AI';
  }

  if (rawCategory !== null) {
    switch (rawCategory) {
      case 0: return 'Programming';
      case 1: return 'GameDevelopment';
      case 2: return 'Design';
      case 3: return 'Business';
      default: return 'Other';
    }
  }

  return 'Other';
}

// C# :740-783. semanticHint (comment+title+description+slug) keywords outrank everything:
// advanced|mastery → Advanced, then intermediate → Intermediate; then typeComment keywords;
// raw int only decides when nothing matched — raw 0 falls to the default arm → Beginner.
function parseProgramDifficulty(difficultyComment, rawDifficulty, title, description, slug) {
  const normalized = normalizeLabel(difficultyComment);
  const semanticHint = normalizeLabel(`${difficultyComment} ${title} ${description} ${slug}`);

  if (semanticHint.includes('advanced') || semanticHint.includes('mastery')) {
    return 'Advanced';
  }

  if (semanticHint.includes('intermediate')) {
    return 'Intermediate';
  }

  if (normalized.includes('intermediate')) {
    return 'Intermediate';
  }

  if (normalized.includes('advanced')) {
    return 'Advanced';
  }

  if (normalized.includes('expert')) {
    return 'Expert';
  }

  if (rawDifficulty !== null) {
    switch (rawDifficulty) {
      case 1: return 'Intermediate';
      case 2: return 'Advanced';
      case 3: return 'Expert';
      default: return 'Beginner';
    }
  }

  return 'Beginner';
}

// C# :785-816. Comment keywords only (no title/description hint here); raw fallback 1→Closed,
// 2→InviteOnly, 3→Waitlist, else (incl. 0) → Open.
function parseEnrollmentStatus(enrollmentStatusComment, rawEnrollmentStatus) {
  const normalized = normalizeLabel(enrollmentStatusComment);

  if (normalized.includes('closed')) return 'Closed';
  if (normalized.includes('inviteonly')) return 'InviteOnly';
  if (normalized.includes('waitlist')) return 'Waitlist';

  if (rawEnrollmentStatus !== null) {
    switch (rawEnrollmentStatus) {
      case 1: return 'Closed';
      case 2: return 'InviteOnly';
      case 3: return 'Waitlist';
      default: return 'Open';
    }
  }

  return 'Open';
}

// Ported from apps/web/src/lib/slugify.ts (:14-29), which mirrors backend
// StringExtensions.ToSlugCase: lowercase; whitespace/underscore/dot runs → single hyphen;
// chars outside [a-z0-9-] dropped; hyphen runs collapsed; hyphens-only → ''.
function slugifyValue(value) {
  const slug = value
    .toLowerCase()
    .replace(/[\s_.]+/g, '-')
    .replace(/[^a-z0-9-]/g, '')
    .replace(/-+/g, '-');
  return /^-+$/.test(slug) ? '' : slug;
}

// Submit-time normalization (slugify.ts :27-29): slugify plus leading/trailing hyphen
// removal, matching backend Trim('-'). Idempotent.
function normalizeSlug(value) {
  return slugifyValue(value).replace(/^-+|-+$/g, '');
}

// Filters parsed contents by the skip list and projects everything onto API wire shapes.
// Throws when a kept item's parent was skipped — silent reparenting would move subtrees.
function buildImportModel(parsed, skipSourceIds) {
  const skipSet = new Set(skipSourceIds);
  const kept = parsed.contents.filter((content) => !skipSet.has(content.sourceId));

  for (const content of kept) {
    if (content.parentSourceId !== null && skipSet.has(content.parentSourceId)) {
      throw new Error(
        `skip-source-ids orphaned ${content.sourceId} (parent ${content.parentSourceId} skipped)`,
      );
    }
  }

  const program = parsed.program;
  return {
    program: {
      slug: program.slug,
      title: program.title,
      description: program.description,
      thumbnail: program.thumbnail,
      estimatedHours: program.estimatedHours,
      category: parseProgramCategory(null, program.rawCategory, program.title, program.description, program.slug),
      difficulty: parseProgramDifficulty(null, program.rawDifficulty, program.title, program.description, program.slug),
      enrollmentStatus: parseEnrollmentStatus(null, program.rawEnrollmentStatus),
    },
    items: kept.map((content) => ({
      sourceId: content.sourceId,
      parentSourceId: content.parentSourceId,
      title: content.title,
      description: content.description,
      type: toApiType(
        parseProgramContentType(
          content.typeComment,
          content.rawType,
          content.title,
          content.description,
          content.sourceId,
          content.bodyImportName,
        ),
      ),
      lessonFormat: 'Markdown',
      sortOrder: content.sortOrder,
      isRequired: content.isRequired,
      estimatedMinutes: content.estimatedMinutes,
      body: content.body,
      predictedSlug: normalizeSlug(content.title),
    })),
  };
}

// ===== self-check (tasks 2-3) =====

function assertSelfCheck(name, ok, detail) {
  if (ok) {
    console.log(`[self-check] ok ${name}`);
  } else {
    console.log(`[self-check] FAIL ${name}: ${detail}`);
    process.exit(1);
  }
}

function runSelfCheck(args) {
  let parsed;
  try {
    parsed = parseCourseDir(args.slug);
  } catch (err) {
    console.log(`[self-check] FAIL parse: ${err.message}`);
    process.exit(1);
  }
  const { program, contents } = parsed;

  const fromFile = contents.filter((content) => content.bodyResolvedFromFile);
  const inline = contents.filter((content) => !content.bodyResolvedFromFile);

  for (const content of contents) {
    console.log(
      `[self-check] parsed ${content.sourceId} body=${content.bodyResolvedFromFile ? 'file' : 'inline'}` +
        ` type=${content.rawType} parent=${content.parentSourceId ?? 'null'}`,
    );
  }

  assertSelfCheck(
    'program',
    program.slug === 'intro2gpro' && program.title === 'Introduction to Game Programming',
    `slug=${program.slug} title=${JSON.stringify(program.title)}`,
  );
  assertSelfCheck('contents-16', contents.length === 16, `contents.length=${contents.length}`);
  assertSelfCheck(
    'body-kinds',
    fromFile.length === 13 && inline.length === 3,
    `file=${fromFile.length} inline=${inline.length}`,
  );
  assertSelfCheck(
    'md-bodies-nonempty',
    fromFile.every((content) => content.body.length > 0),
    `empty=${fromFile.filter((content) => content.body.length === 0).map((content) => content.sourceId).join(',') || 'none'}`,
  );

  // Task 3: inference + import model.
  const skipIds = [
    'intro2gpro-technical-challenges',
    'intro2gpro-design-challenges',
    'intro2gpro-business-challenges',
  ];

  let model;
  let modelAll;
  try {
    model = buildImportModel(parsed, skipIds);
    modelAll = buildImportModel(parsed, []);
  } catch (err) {
    console.log(`[self-check] FAIL model: ${err.message}`);
    process.exit(1);
  }

  for (const item of model.items) {
    console.log(`[self-check] predicted-slug ${item.predictedSlug}`);
  }

  // Verbatim-port note: 'Week 01: Interview a Game Developer' has description
  // 'Assignment to interview a game programmer…' → semanticHint 'assignment' → Assignment.
  // The plan's "other 11 → Lesson" expectation missed that keyword; this assert matches the
  // C# chain as ported (syllabus→Lesson, assignment→Assignment, interview→Assignment, rest
  // →Lesson via Page→Lesson).
  const expectedTypes = Object.fromEntries(model.items.map((item) => [item.sourceId, 'Lesson']));
  expectedTypes['intro2gpro-assignment'] = 'Assignment';
  expectedTypes['intro2gpro-interview'] = 'Assignment';
  const actualTypes = Object.fromEntries(model.items.map((item) => [item.sourceId, item.type]));
  assertSelfCheck(
    'type-map',
    JSON.stringify(actualTypes) === JSON.stringify(expectedTypes),
    `types=${JSON.stringify(actualTypes)}`,
  );

  const slugs = model.items.map((item) => item.predictedSlug);
  assertSelfCheck(
    'slugs-unique',
    new Set(slugs).size === slugs.length,
    `distinct=${new Set(slugs).size}/${slugs.length}`,
  );
  assertSelfCheck(
    'no-stub-collision',
    !slugs.includes('course-overview'),
    'a predictedSlug equals the prod stub slug course-overview',
  );

  assertSelfCheck(
    'program-fields',
    model.program.category === 'GameDevelopment'
      && model.program.difficulty === 'Beginner'
      && model.program.enrollmentStatus === 'Open'
      && model.program.estimatedHours === 45
      && model.program.description.startsWith('Students will be introduced'),
    `program=${JSON.stringify(model.program)}`,
  );

  assertSelfCheck(
    'skip-filter',
    model.items.length === 13 && modelAll.items.length === 16,
    `skipped=${model.items.length} all=${modelAll.items.length}`,
  );
}

// ===== api client (task 4) =====
// Sign-in is rate-limited (10/min) — signIn() is called ONCE per run. Token values and
// passwords never reach stdout or evidence: logHttp lines carry method/url/status/ok only,
// and urls stay relative (purge-courses JSONL precedent).

async function signIn({ apiUrl, email, password }) {
  const response = await fetch(`${apiUrl}/v1/auth/sign-in`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  const bodyText = await response.text();
  logHttp({ method: 'POST', url: '/v1/auth/sign-in', status: response.status, ok: response.ok });
  if (!response.ok) {
    throw new Error(`POST /v1/auth/sign-in -> ${response.status}: ${bodyText}`);
  }
  const data = JSON.parse(bodyText);
  if (typeof data.accessToken !== 'string' || data.accessToken.length === 0) {
    throw new Error(
      `sign-in response has no accessToken (top-level keys: ${Object.keys(data).join(', ')})`,
    );
  }
  return { accessToken: data.accessToken, tenantId: data.tenantId ?? null };
}

async function request(method, path, { token, body, apiUrl }) {
  const response = await fetch(`${apiUrl}${path}`, {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(body ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const bodyText = await response.text();
  logHttp({ method, url: path, status: response.status, ok: response.ok });
  if (!response.ok) {
    throw new Error(`${method} ${path} -> ${response.status}: ${bodyText}`);
  }
  if (bodyText === '') return null;
  return JSON.parse(bodyText);
}

// ===== upsert (task 5-7) =====

// ===== orchestration (task 8) =====

async function main() {
  const args = parseArgs(process.argv.slice(2));

  const courseDir = path.join(COURSES_ROOT, args.slug);
  const indexPath = path.join(courseDir, 'index.ts');
  // Self-check resolves the course dir itself (parseCourseDir, GG_COURSES_ROOT-aware) and
  // reports a missing index.ts as a self-check failure instead of an early exit.
  // Auth-probe is network-only and never reads the course folder.
  if (args.mode !== 'selfcheck' && args.mode !== 'authprobe' && !existsSync(indexPath)) {
    fail(`index.ts not found: ${indexPath}`);
  }

  const env = resolveEnv();
  validateEnv(env, args.mode);

  initEvidence(args.mode);

  console.log(`[boot] mode=${args.mode} slug=${args.slug} courseDir=${courseDir} api=${env.apiUrl}`);
  logEvent({ type: 'boot', mode: args.mode, slug: args.slug, args: { ...args, mode: undefined } });

  if (args.mode === 'selfcheck') {
    runSelfCheck(args);
    process.exit(0);
  }

  // Creds are read but only consumed by the API client (task 4); masked here
  // so the value never reaches stdout or evidence logs.
  logEvent({ type: 'creds', email: env.email, password: maskSecret(env.password) });

  // Test-only smoke path: one sign-in POST, token LENGTH only, then exit. Dry-run/execute
  // flows pick up signIn in task 8 (orchestration).
  if (args.mode === 'authprobe') {
    const session = await signIn({ apiUrl: env.apiUrl, email: env.email, password: env.password });
    console.log(
      `[auth] ok tokenLength=${session.accessToken.length} tenantId=${session.tenantId ?? 'null'}`,
    );
    process.exit(0);
  }

  console.log('[todo] scaffold complete — parser/API/import stages land in tasks 2-8');
  process.exit(0);
}

main().catch((err) => fail(err.message));
