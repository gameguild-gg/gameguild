/**
 * Serialize/deserialize code files to/from the JSON string format stored
 * in the AssessmentSubmission.CodePayload column. Shape: `{path: content}`.
 */

export interface CodeFile {
  path: string;
  content: string;
}

/** Serialize `{path, content}[]` → JSON string `{path: content}`. */
export function filesToCodePayload(files: CodeFile[]): string {
  return JSON.stringify(Object.fromEntries(files.map((f) => [f.path, f.content])));
}

/** Deserialize JSON string `{path: content}` → `{path, content}[]`. */
export function codePayloadToFiles(payload: string): CodeFile[] {
  let parsed: unknown;
  try {
    parsed = JSON.parse(payload);
  } catch {
    throw new Error(`Invalid code payload: not valid JSON`);
  }

  if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
    throw new Error('Invalid code payload: expected a JSON object');
  }

  return Object.entries(parsed as Record<string, unknown>).map(([path, content]) => ({
    path,
    content: String(content),
  }));
}
