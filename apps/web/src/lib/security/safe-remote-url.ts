/**
 * SSRF guard for outbound fetch targets (Codacy `rule-node-ssrf` remediation).
 *
 * Same-origin relative paths pass through untouched. Absolute URLs must:
 * - use `https:`,
 * - carry no embedded credentials,
 * - resolve to a host on the configured allowlist — env
 *   `REMOTE_ASSET_ALLOWED_HOSTS` (comma/space separated; defaults to the
 *   hosts this codebase already talks to),
 * - not use a private or loopback address literal.
 *
 * `ALLOW_UNSAFE_REMOTE_URL=true` bypasses every check (local development only,
 * e.g. for a `http://localhost:8080` API base). Test runs (`NODE_ENV=test`)
 * bypass as well so fixtures using local HTTP bases keep working.
 */

const DEFAULT_ALLOWED_HOSTS = "cdn.gameguild.gg,gameguild.gg,api.gameguild.gg";

export class UnsafeRemoteUrlError extends Error {
  constructor(
    reason: string,
    readonly url: string,
  ) {
    super(`Unsafe remote URL rejected (${reason}): ${url}`);
    this.name = "UnsafeRemoteUrlError";
  }
}

function envValue(name: string): string | undefined {
  return (
    globalThis as { process?: { env?: Record<string, string | undefined> } }
  ).process?.env?.[name];
}

function allowedHosts(): Set<string> {
  const raw = envValue("REMOTE_ASSET_ALLOWED_HOSTS") ?? DEFAULT_ALLOWED_HOSTS;
  return new Set(
    raw
      .split(/[,\s]+/)
      .map((host) => host.trim().toLowerCase())
      .filter(Boolean),
  );
}

function isPrivateIpv4(host: string): boolean {
  const octets = host.split(".");
  if (octets.length !== 4 || !octets.every((part) => /^\d{1,3}$/.test(part)))
    return false;
  const [a, b] = octets.map(Number) as [number, number, number, number];
  if (octets.some((part) => Number(part) > 255)) return false;
  return (
    a === 127 || // 127.0.0.0/8 loopback
    a === 10 || // 10.0.0.0/8 private
    (a === 172 && b >= 16 && b <= 31) || // 172.16.0.0/12 private
    (a === 192 && b === 168) || // 192.168.0.0/16 private
    (a === 169 && b === 254) // 169.254.0.0/16 link-local / cloud metadata
  );
}

function isPrivateHost(hostname: string): boolean {
  const host = hostname.toLowerCase().replace(/^\[|\]$/g, "");
  if (host === "localhost" || host.endsWith(".localhost")) return true;
  if (host === "::1" || host === "0:0:0:0:0:0:0:1") return true;
  if (/^f[cd][0-9a-f]{2}:/.test(host)) return true; // fc00::/7 unique-local
  return isPrivateIpv4(host.startsWith("::ffff:") ? host.slice(7) : host);
}

function bypassEnabled(): boolean {
  const environment = envValue("NODE_ENV");
  return (
    environment === "test" ||
    (environment === "development" &&
      envValue("ALLOW_UNSAFE_REMOTE_URL") === "true")
  );
}

/**
 * Assert that `input` is a safe outbound request target and return it (the
 * parsed URL for absolute targets, the input verbatim otherwise). Relative
 * paths are same-origin by construction and always allowed.
 *
 * @throws {UnsafeRemoteUrlError} when an absolute URL violates the policy.
 */
export function assertSafeRemoteUrl(input: string | URL): string | URL {
  const raw = input.toString();
  if (bypassEnabled()) return input;
  // Protocol-relative URLs are cross-origin by construction.
  if (raw.startsWith("//"))
    throw new UnsafeRemoteUrlError("protocol-relative URL", raw);
  const hasScheme = /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(raw);
  if (!hasScheme) {
    // Parse against a sentinel solely to detect disguised absolute/network paths.
    // Preserve relative fetch targets so the caller's real origin is unchanged.
    const relativeBase = "https://relative.invalid";
    if (new URL(raw, relativeBase).origin !== relativeBase) {
      throw new UnsafeRemoteUrlError("disguised cross-origin URL", raw);
    }
    return input;
  }
  const url = new URL(raw);
  if (url.protocol !== "https:")
    throw new UnsafeRemoteUrlError("only https is allowed", raw);
  if (url.username || url.password)
    throw new UnsafeRemoteUrlError("embedded credentials", raw);
  if (isPrivateHost(url.hostname))
    throw new UnsafeRemoteUrlError("private or loopback address", raw);
  if (!allowedHosts().has(url.hostname.toLowerCase())) {
    throw new UnsafeRemoteUrlError(
      "host not on the allowlist (REMOTE_ASSET_ALLOWED_HOSTS)",
      raw,
    );
  }
  return url;
}
