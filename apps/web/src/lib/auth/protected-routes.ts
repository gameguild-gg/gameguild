/**
 * Proxy-level protected-route policy (auth expiry UX): the `authorized`
 * callback bounces anonymous visitors off these route roots before the
 * first render, while everything else (public site, legal, auth pages,
 * block-content-editor, speedgrader, learn, api) stays reachable —
 * those surfaces own their own, finer-grained guards.
 */

/** Route roots that require an authenticated session. Mirrors the (social) and (dashboards) layout guards. */
export const PROTECTED_ROUTE_PREFIXES = [
  "/feed",
  "/workspace",
  "/console",
  "/dashboard",
  "/my",
  "/authoring",
  "/blog",
  "/blogs",
  "/testing-lab",
  "/community",
  "/explore",
  "/projects",
  "/launch-pad",
  "/social",
] as const;

/** Locales the proxy may serve with an explicit URL prefix (next-intl `as-needed`). */
export const LOCALE_PREFIXES = ["/en-US", "/pt-BR"] as const;

/**
 * Strips a leading locale prefix (`/en-US/...`, `/pt-BR/...`) so the
 * protected-route match sees the canonical unprefixed path. The default
 * locale is canonical without a prefix, so most paths pass through as-is.
 */
export function stripLocalePrefix(pathname: string): string {
  for (const prefix of LOCALE_PREFIXES) {
    if (pathname === prefix) return "/";
    if (pathname.startsWith(`${prefix}/`)) return pathname.slice(prefix.length) || "/";
  }
  return pathname;
}

/** True when the (locale-stripped) pathname sits under a protected route root. */
export function isProtectedPath(pathname: string): boolean {
  const unprefixed = stripLocalePrefix(pathname);
  return PROTECTED_ROUTE_PREFIXES.some(
    (prefix) => unprefixed === prefix || unprefixed.startsWith(`${prefix}/`),
  );
}
