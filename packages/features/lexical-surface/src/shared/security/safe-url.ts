const SAFE_URL_PROTOCOLS = new Set([
  "http:",
  "https:",
  "mailto:",
  "sms:",
  "tel:",
]);

export function sanitizeUrl(url: string): string {
  const value = url.trim();
  if (!value) return "about:blank";

  try {
    const base =
      typeof window === "undefined"
        ? "https://lexical-surface.invalid"
        : window.location.href;
    const parsed = new URL(value, base);
    return SAFE_URL_PROTOCOLS.has(parsed.protocol) ? value : "about:blank";
  } catch {
    return "about:blank";
  }
}

export function openSafeUrl(url: string): Window | null {
  const safeUrl = sanitizeUrl(url);
  if (safeUrl === "about:blank") return null;

  const opened = window.open(safeUrl, "_blank", "noopener,noreferrer");
  if (opened) opened.opener = null;
  return opened;
}
