export function canonicalizeJson(value: unknown): string {
  return serialize(value);
}

export async function sha256Jcs(value: unknown): Promise<string> {
  const bytes = new TextEncoder().encode(canonicalizeJson(value));
  const digest = await crypto.subtle.digest("SHA-256", bytes);
  return Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, "0")).join("");
}

export const hashAssessmentAuthoringSource = sha256Jcs;
export const hashAssessmentExecutionSnapshot = sha256Jcs;
export const hashAssessmentExecutionDelivery = sha256Jcs;

function serialize(value: unknown): string {
  if (value === null) return "null";
  if (typeof value === "string" || typeof value === "boolean") return JSON.stringify(value);
  if (typeof value === "number") {
    if (!Number.isFinite(value)) throw new TypeError("JCS does not support non-finite numbers.");
    return JSON.stringify(value);
  }
  if (Array.isArray(value)) return `[${value.map(serialize).join(",")}]`;
  if (typeof value === "object") {
    const record = value as Record<string, unknown>;
    const fields = Object.keys(record)
      .sort(compareUtf16)
      .flatMap((key) => record[key] === undefined ? [] : [`${JSON.stringify(key)}:${serialize(record[key])}`]);
    return `{${fields.join(",")}}`;
  }
  throw new TypeError(`JCS cannot serialize ${typeof value}.`);
}

function compareUtf16(left: string, right: string): number {
  return left < right ? -1 : left > right ? 1 : 0;
}
