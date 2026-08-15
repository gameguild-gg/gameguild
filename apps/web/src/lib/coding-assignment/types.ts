/**
 * Wire types + runtime guards for the v1 coding-assignment content payload.
 *
 * The codegen output collapses the C# `Test` polymorphic base record (with the
 * lowercase `kind` discriminator) to its base fields only — the generated
 * `LearningCoursesTest` surfaces as `{ weight?, name? }` and the
 * variant-specific payload (`Stdin`/`Stdout` for `StandardTest`,
 * `Function`/`Result` for `FunctionalTest`) is dropped by the generated Zod
 * schema (`z.object` strips unknown keys by default). These types + guards
 * restore the typed shape; the wrappers in `client.ts` apply them.
 *
 * Wire shape mirrors the C# DTOs under
 * apps/api/Source/Modules/GameGuild.Learning.Courses/Models/CodingAssignmentContent/
 * — PascalCase field names with a lowercase `kind` discriminator
 * (values `"standard"`/`"functional"`) and lowercase `FunctionParameterType`
 * enum values (`"string"`/`"boolean"`/`"integer"`/`"float"`).
 */

// ---------------------------------------------------------------------------
// Wire types
// ---------------------------------------------------------------------------

export type FunctionParameterType = 'string' | 'boolean' | 'integer' | 'float';
export type TestKind = 'standard' | 'functional';
export type FileVisibility = 'Public' | 'Private';
export type FileEncoding = 'text' | 'base64';

export const TEST_KIND = {
  Standard: 'standard',
  Functional: 'functional',
} as const satisfies Record<string, TestKind>;

/** `FunctionParameter.Content` is `JsonElement` in C# — string | number | boolean on the wire. */
export type FunctionParameterValue = string | number | boolean;

export interface FunctionParameter {
  readonly Type: FunctionParameterType;
  readonly Content: FunctionParameterValue;
}

export interface FunctionParameterWithName {
  readonly Type: FunctionParameterType;
  readonly Name: string;
}

export interface TestFunctionData {
  readonly FunctionName: string;
  readonly Parameters: readonly FunctionParameterWithName[];
  readonly ReturnType: { readonly Type: FunctionParameterType };
}

interface TestBase {
  readonly Weight?: number;
  readonly Name?: string | null;
}

export interface StandardTest extends TestBase {
  readonly kind: typeof TEST_KIND.Standard;
  readonly Stdin?: string | null;
  readonly Stdout: string;
  readonly Stderr?: string | null;
  readonly ExitCode?: number | null;
}

export interface FunctionalTestCase {
  readonly Inputs: readonly FunctionParameter[];
  readonly Expected: FunctionParameter;
}

export interface FunctionalTestGroup extends TestBase {
  readonly kind: typeof TEST_KIND.Functional;
  readonly Function: TestFunctionData;
  readonly Cases: readonly FunctionalTestCase[];
}

export type Test = StandardTest | FunctionalTestGroup;

export interface BundleFileMeta {
  readonly Content: string;
  readonly Encoding: FileEncoding;
  readonly Visibility: FileVisibility;
  readonly Modifiable: boolean;
}

export interface CodingEnvironment {
  readonly Language: string;
  readonly Tools: string;
  readonly LibBundle?: string | null;
  readonly AllowStudentCreateFiles: boolean;
}

export interface WorkspaceData {
  readonly Files: Readonly<Record<string, BundleFileMeta>>;
}

export interface TestSuite {
  readonly Public: readonly Test[];
  readonly Private: readonly Test[];
}

export interface GradingConfig {
  readonly MaxScore: number;
}

export interface CodingAssignmentContent {
  readonly Type: 'coding-assignment';
  readonly Version: 1;
  readonly Environment: CodingEnvironment;
  readonly Data: WorkspaceData;
  readonly Tests: TestSuite;
  readonly Grading: GradingConfig;
}

// ---------------------------------------------------------------------------
// Runtime guards
// ---------------------------------------------------------------------------

const PARAM_TYPES: readonly FunctionParameterType[] = [
  'string',
  'boolean',
  'integer',
  'float',
];

export function isRecord(v: unknown): v is Record<string, unknown> {
  return typeof v === 'object' && v !== null;
}

function isFunctionParameterType(v: unknown): v is FunctionParameterType {
  return typeof v === 'string' && (PARAM_TYPES as readonly string[]).includes(v);
}

function isFunctionParameterValue(v: unknown): v is FunctionParameterValue {
  const t = typeof v;
  return t === 'string' || t === 'number' || t === 'boolean';
}

export function isFunctionParameter(v: unknown): v is FunctionParameter {
  if (!isRecord(v)) return false;
  return isFunctionParameterType(v.Type) && isFunctionParameterValue(v.Content);
}

export function isFunctionParameterWithName(
  v: unknown,
): v is FunctionParameterWithName {
  if (!isRecord(v)) return false;
  return (
    isFunctionParameterType(v.Type) &&
    typeof (v as unknown as Record<string, unknown>).Name === 'string'
  );
}

export function isTestFunctionData(v: unknown): v is TestFunctionData {
  if (!isRecord(v)) return false;
  return (
    typeof v.FunctionName === 'string' &&
    Array.isArray(v.Parameters) &&
    v.Parameters.every(isFunctionParameterWithName) &&
    isRecord(v.ReturnType) &&
    isFunctionParameterType(v.ReturnType.Type)
  );
}

export function isFunctionalTestCase(v: unknown): v is FunctionalTestCase {
  if (!isRecord(v)) return false;
  return (
    Array.isArray(v.Inputs) &&
    v.Inputs.every(isFunctionParameter) &&
    isFunctionParameter(v.Expected)
  );
}

export function isStandardTest(v: unknown): v is StandardTest {
  if (!isRecord(v)) return false;
  return v.kind === TEST_KIND.Standard && typeof v.Stdout === 'string';
}

export function isFunctionalTestGroup(v: unknown): v is FunctionalTestGroup {
  if (!isRecord(v)) return false;
  return (
    v.kind === TEST_KIND.Functional &&
    isTestFunctionData(v.Function) &&
    Array.isArray(v.Cases) &&
    v.Cases.every(isFunctionalTestCase)
  );
}

export function isTest(v: unknown): v is Test {
  return isStandardTest(v) || isFunctionalTestGroup(v);
}

function narrowTest(v: unknown): Test | null {
  if (isStandardTest(v)) return v;
  if (isFunctionalTestGroup(v)) return v;
  return null;
}

function narrowSuite(raw: unknown): TestSuite | null {
  if (!isRecord(raw)) return null;
  const pub = Array.isArray(raw.Public)
    ? raw.Public.map(narrowTest).filter((t): t is Test => t !== null)
    : [];
  const priv = Array.isArray(raw.Private)
    ? raw.Private.map(narrowTest).filter((t): t is Test => t !== null)
    : [];
  return { Public: pub, Private: priv };
}

// ---------------------------------------------------------------------------
// Wire-casing normalization (camelCase API wire → PascalCase guard shape)
// ---------------------------------------------------------------------------

/**
 * The API serializes via AddJsonOptions web defaults (camelCase keys); the
 * guards above expect the C# PascalCase shape. Fixed known-keys map — never
 * rename generically: `Data.Files` is keyed by file paths (`/user/main.cpp`)
 * which must survive untouched. `kind` is lowercase in both casings and is
 * deliberately absent (passes through).
 */
const WIRE_KEY_MAP: Record<string, string> = {
  type: 'Type', version: 'Version',
  environment: 'Environment', data: 'Data', tests: 'Tests', grading: 'Grading',
  files: 'Files',
  language: 'Language', tools: 'Tools', libBundle: 'LibBundle', allowStudentCreateFiles: 'AllowStudentCreateFiles',
  content: 'Content', encoding: 'Encoding', visibility: 'Visibility', modifiable: 'Modifiable',
  public: 'Public', private: 'Private',
  name: 'Name', weight: 'Weight', stdin: 'Stdin', stdout: 'Stdout', stderr: 'Stderr', exitCode: 'ExitCode',
  function: 'Function', cases: 'Cases', functionName: 'FunctionName', parameters: 'Parameters', returnType: 'ReturnType',
  inputs: 'Inputs', expected: 'Expected',
  maxScore: 'MaxScore',
};

/**
 * Deep-walk renaming camelCase wire keys to PascalCase. Rules:
 * - Object already carrying `Type` → assumed PascalCase, returned as-is
 *   (fast path; also makes the walk idempotent on PascalCase subtrees).
 * - Own keys renamed via {@link WIRE_KEY_MAP}; unknown keys pass through.
 * - The value of `Files`/`files` is a map keyed by file paths — its keys are
 *   never renamed; only the per-file metadata values are walked.
 * - Arrays and all other object values recurse normally.
 */
function normalizeWireCasing(node: unknown): unknown {
  if (Array.isArray(node)) return node.map(normalizeWireCasing);
  if (!isRecord(node)) return node;
  if ('Type' in node) return node;
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(node)) {
    const renamed = WIRE_KEY_MAP[key] ?? key;
    if (renamed !== 'Files') {
      out[renamed] = normalizeWireCasing(value);
      continue;
    }
    const files: Record<string, unknown> = {};
    const map = isRecord(value) ? value : {};
    for (const [path, meta] of Object.entries(map)) {
      files[path] = normalizeWireCasing(meta);
    }
    out.Files = files;
  }
  return out;
}

/**
 * Narrow the raw HTTP payload into a typed {@link CodingAssignmentContent}.
 * camelCase API payloads are normalized to PascalCase first; the single seam
 * that fixes both the student and instructor read paths.
 * Returns `null` if the shape is wrong (caller should treat as "no content").
 */
export function narrowCodingAssignmentContent(
  raw: unknown,
): CodingAssignmentContent | null {
  if (!isRecord(raw)) return null;
  const source = normalizeWireCasing(raw) as Record<string, unknown>;
  if (source.Type !== 'coding-assignment') return null;
  if (source.Version !== 1) return null;
  const tests = narrowSuite(source.Tests);
  if (!tests) return null;
  // Environment / Data / Grading are not polymorphic — trust the wire shape,
  // the server validates via FluentValidation.
  return { ...(source as unknown as CodingAssignmentContent), Tests: tests };
}
