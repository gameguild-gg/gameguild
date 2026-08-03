export interface LearningHostRoutingConfig {
  defaultLocale: string;
  locales: readonly string[];
  learningOrigin: string;
  webOrigin: string;
}

export type HostRouteDecision =
  | { action: "next" }
  | { action: "rewrite"; url: string }
  | { action: "redirect"; status: 307 | 308; url: string };

interface ResolveLearningHostRouteInput {
  config: LearningHostRoutingConfig;
  hostname: string;
  requiresAuthentication?: boolean;
  url: URL;
}

function normalizeHostname(value: string | null): string {
  const firstValue = value?.split(",")[0]?.trim();
  if (!firstValue) {
    return "";
  }

  try {
    return new URL(`http://${firstValue}`).hostname.toLowerCase();
  } catch {
    return firstValue.replace(/:\d+$/, "").toLowerCase();
  }
}

function getOriginHostname(origin: string): string {
  return new URL(origin).hostname.toLowerCase();
}

function getLocalizedPath(
  pathname: string,
  config: Pick<LearningHostRoutingConfig, "defaultLocale" | "locales">,
): { locale: string; pathname: string; localeWasExplicit: boolean } {
  const segments = pathname.split("/").filter(Boolean);
  const localeWasExplicit = config.locales.includes(segments[0] ?? "");
  const locale = localeWasExplicit ? segments.shift()! : config.defaultLocale;

  return {
    locale,
    localeWasExplicit,
    pathname: segments.length > 0 ? `/${segments.join("/")}` : "/",
  };
}

function joinOrigin(origin: string, pathname: string, search: string): string {
  const target = new URL(origin);
  target.pathname = pathname;
  target.search = search;
  return target.toString();
}

export function getRequestHostname(headers: Headers): string {
  return normalizeHostname(
    headers.get("x-forwarded-host") ?? headers.get("host"),
  );
}

export function hasGameGuildSessionCookie(
  cookieHeader: string | null,
): boolean {
  if (!cookieHeader) return false;

  return cookieHeader.split(";").some((entry) => {
    const rawName = entry.split("=", 1)[0]?.trim() ?? "";
    const name = rawName.replace(/^__Secure-/, "");

    return (
      name === "gameguild.session-token" ||
      name.startsWith("gameguild.session-token.")
    );
  });
}

export function resolveLearningHostRoute({
  config,
  hostname,
  requiresAuthentication = false,
  url,
}: ResolveLearningHostRouteInput): HostRouteDecision {
  const requestHostname = normalizeHostname(hostname);
  const learningHostname = getOriginHostname(config.learningOrigin);
  const webHostname = getOriginHostname(config.webOrigin);
  const localized = getLocalizedPath(url.pathname, config);

  if (requestHostname === learningHostname) {
    if (localized.pathname === "/catalog") {
      return {
        action: "redirect",
        status: 308,
        url: joinOrigin(config.webOrigin, "/courses", url.search),
      };
    }

    if (requiresAuthentication) {
      const visiblePath = localized.localeWasExplicit
        ? `/${localized.locale}${localized.pathname === "/" ? "" : localized.pathname}`
        : localized.pathname;
      const returnUrl = joinOrigin(
        config.learningOrigin,
        visiblePath,
        url.search,
      );
      const signInUrl = new URL("/sign-in", config.webOrigin);
      signInUrl.searchParams.set("redirectTo", returnUrl);

      return { action: "redirect", status: 307, url: signInUrl.toString() };
    }

    const legacyAssignmentsPath = localized.pathname.replace(
      /(^|\/)assignments(?=\/|$)/,
      "$1activities",
    );
    if (legacyAssignmentsPath !== localized.pathname) {
      const visiblePath = localized.localeWasExplicit
        ? `/${localized.locale}${legacyAssignmentsPath}`
        : legacyAssignmentsPath;

      return {
        action: "redirect",
        status: 308,
        url: joinOrigin(config.learningOrigin, visiblePath, url.search),
      };
    }

    if (
      localized.pathname === "/learn" ||
      localized.pathname.startsWith("/learn/")
    ) {
      return { action: "next" };
    }

    const internalPath = `/${localized.locale}/learn${
      localized.pathname === "/" ? "" : localized.pathname
    }`;

    return {
      action: "rewrite",
      url: joinOrigin(config.learningOrigin, internalPath, url.search),
    };
  }

  if (
    requestHostname === webHostname &&
    (localized.pathname === "/learn" ||
      localized.pathname.startsWith("/learn/"))
  ) {
    const visiblePath = localized.pathname.slice("/learn".length) || "/";
    const localizedVisiblePath = localized.localeWasExplicit
      ? `/${localized.locale}${visiblePath === "/" ? "" : visiblePath}`
      : visiblePath;

    return {
      action: "redirect",
      status: 308,
      url: joinOrigin(config.learningOrigin, localizedVisiblePath, url.search),
    };
  }

  return { action: "next" };
}
