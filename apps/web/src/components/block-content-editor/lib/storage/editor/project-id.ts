/**
 * Generates a globally-unique project id.
 *
 * Prefers `crypto.randomUUID` when available; falls back to a time + random
 * suffix encoded in base-36 for non-secure contexts (older browsers, SSR).
 */
export function generateProjectId(): string {
  if (typeof crypto !== "undefined" && crypto.randomUUID) {
    return crypto.randomUUID()
  }
  return "proj_" + Date.now().toString(36) + "_" + Math.random().toString(36).substr(2, 9)
}
