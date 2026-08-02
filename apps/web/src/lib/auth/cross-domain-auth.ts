interface SharedAuthCookieEnvironment {
  authCookieDomain?: string;
  authCookieSecure?: boolean | string;
  nodeEnv?: string;
}

interface SharedAuthCookieConfig {
  domain?: string;
  httpOnly: true;
  name: "gameguild";
  path: "/";
  sameSite: "lax";
  secure: boolean;
}

interface AllowedAuthRedirectOptions {
  fallback?: string;
  learningOrigin: string;
  webOrigin: string;
}

export function createSharedAuthCookieConfig({
  authCookieDomain,
  authCookieSecure,
  nodeEnv,
}: SharedAuthCookieEnvironment): SharedAuthCookieConfig {
  const explicitSecure =
    typeof authCookieSecure === "boolean"
      ? authCookieSecure
      : authCookieSecure?.trim().toLowerCase();

  return {
    name: "gameguild",
    secure:
      explicitSecure === true || explicitSecure === "true"
        ? true
        : explicitSecure === false || explicitSecure === "false"
          ? false
          : nodeEnv === "production",
    sameSite: "lax",
    path: "/",
    domain: authCookieDomain?.trim() || undefined,
    httpOnly: true,
  };
}

export function resolveAllowedAuthRedirect(
  value: unknown,
  {
    fallback = "/dashboard",
    learningOrigin,
    webOrigin,
  }: AllowedAuthRedirectOptions,
): string {
  const redirectTo = typeof value === "string" ? value.trim() : "";
  if (!redirectTo) {
    return fallback;
  }

  if (redirectTo.startsWith("/") && !redirectTo.startsWith("//")) {
    return redirectTo;
  }

  try {
    const target = new URL(redirectTo);
    const allowedOrigins = new Set([
      new URL(learningOrigin).origin,
      new URL(webOrigin).origin,
    ]);

    if (
      !target.username &&
      !target.password &&
      allowedOrigins.has(target.origin)
    ) {
      return target.toString();
    }
  } catch {
    // Invalid and non-relative values fall back to a local safe route.
  }

  return fallback;
}
