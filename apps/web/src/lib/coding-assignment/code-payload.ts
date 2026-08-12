/**
 * Serialize/deserialize code files to/from the JSON string stored in
 * `AssessmentSubmission.CodePayload`.
 *
 * v1 pinned shape (Metis #29): `Record<path, {content: string, encoding: 'text'}>`.
 * Shared contract between Task 9 (student submit, write side) and Task 11
 * (grader, read side). `codePayloadToFiles` is tolerant of the legacy v0
 * shape `Record<path, string>` so existing submissions still parse until the
 * grader is rewritten.
 */

export interface CodeFile {
  path: string;
  content: string;
}

function isRecord(v: unknown): v is Record<string, unknown> {
  return typeof v === 'object' && v !== null;
}

/** Serialize `{path, content}[]` → JSON `Record<path, {content, encoding: 'text'}>`. */
export function filesToCodePayload(files: CodeFile[]): string {
  return JSON.stringify(
    Object.fromEntries(
      files.map((f) => [f.path, { content: f.content, encoding: 'text' as const }]),
    ),
  );
}

/**
 * Deserialize JSON string → `{path, content}[]`.
 *
 * Accepts:
 *   - v1: `Record<path, {content: string, encoding: 'text'}>`
 *   - legacy v0: `Record<path, string>` (existing submissions pre-v1)
 *
 * Throws `Invalid code payload: …` on non-JSON or non-object input.
 */
export function codePayloadToFiles(payload: string): CodeFile[] {
  let parsed: unknown;
  try {
    parsed = JSON.parse(payload);
  } catch {
    throw new Error('Invalid code payload: not valid JSON');
  }

  if (!isRecord(parsed) || Array.isArray(parsed)) {
    throw new Error('Invalid code payload: expected a JSON object');
  }

  return Object.entries(parsed).map(([path, value]) => ({
    path,
    content:
      typeof value === 'string'
        ? value // legacy v0 shape
        : isRecord(value) && typeof value.content === 'string'
          ? value.content // v1 shape
          : '',
  }));
}
