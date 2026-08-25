/**
 * Slugify a string into URL-friendly form: lowercase letters, digits, hyphens.
 * Mirrors the backend `StringExtensions.ToSlugCase` (GameGuild.Learning.Courses)
 * so client-generated slugs match what the API would derive from a title:
 * - lowercase
 * - whitespace, underscores, and dots collapse to single hyphens
 * - anything outside [a-z0-9-] is dropped
 *
 * Edge hyphens are intentionally kept: while a user types, a trailing space
 * must surface as a trailing hyphen or words fuse together ("my c" -> "myc").
 * A value reduced to hyphens only (e.g. a lone space) stays empty instead of
 * showing a lone "-". Strip the edges on field blur with `normalizeSlug`.
 */
export function slugify(value: string): string {
  const slug = value
    .toLowerCase()
    .replace(/[\s_.]+/g, "-")
    .replace(/[^a-z0-9-]/g, "")
    .replace(/-+/g, "-");
  return /^-+$/.test(slug) ? "" : slug;
}

/**
 * Submit-time slug normalization: `slugify` plus leading/trailing hyphen
 * removal, matching the backend's `Trim('-')` exactly. Idempotent.
 */
export function normalizeSlug(value: string): string {
  return slugify(value).replace(/^-+|-+$/g, "");
}
