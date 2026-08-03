const criticalStatuses = new Set([404]);

export const learningViewports = [
  { name: "mobile", width: 390, height: 844 },
  { name: "tablet", width: 768, height: 1024 },
  { name: "desktop", width: 1440, height: 1000 },
  { name: "wide", width: 1920, height: 1080 },
];

function normalizeOrigin(value) {
  return new URL(value).origin;
}

export function resolveNodeHealthCheckUrl(value) {
  const url = new URL(value);
  if (url.hostname === "localhost" || url.hostname.endsWith(".localhost")) {
    url.hostname = "127.0.0.1";
  }
  return url.href.endsWith("/") ? url.href.slice(0, -1) : url.href;
}

export function classifyAppHttpFailure({ origins, resourceType, status, url }) {
  let parsed;
  try {
    parsed = new URL(url);
  } catch {
    return null;
  }

  const allowedOrigins = new Set(origins.map(normalizeOrigin));
  if (!allowedOrigins.has(parsed.origin)) return null;

  const isRsc = parsed.searchParams.has("_rsc");
  const isCritical =
    criticalStatuses.has(status) || status >= 500 || (isRsc && status >= 400);
  if (!isCritical) return null;

  const marker = isRsc ? " RSC" : "";
  return `${status}${marker} ${resourceType} ${parsed.origin}${parsed.pathname}${parsed.search}`;
}

export function trackAppHttpFailures(page, origins) {
  const failures = [];
  const listener = (response) => {
    const failure = classifyAppHttpFailure({
      origins,
      resourceType: response.request().resourceType(),
      status: response.status(),
      url: response.url(),
    });
    if (failure) failures.push(failure);
  };

  page.on("response", listener);

  return {
    assertNone(label) {
      const uniqueFailures = [...new Set(failures)];
      if (uniqueFailures.length > 0) {
        throw new Error(
          `${label} produced critical HTTP failures:\n${uniqueFailures.join("\n")}`,
        );
      }
    },
    failures,
    stop() {
      page.off("response", listener);
    },
  };
}

export function assertSharedAuthCookie(cookies, expectedDomain) {
  const authCookies = cookies.filter((cookie) => {
    const name = cookie.name.replace(/^__Secure-/, "");
    return (
      name === "gameguild.session-token" ||
      name.startsWith("gameguild.session-token.")
    );
  });

  if (authCookies.length === 0) {
    throw new Error(
      "The shared GameGuild authentication cookie was not created.",
    );
  }

  if (expectedDomain) {
    const normalizedDomain = expectedDomain.replace(/^\./, "").toLowerCase();
    const hasExpectedDomain = authCookies.some(
      (cookie) =>
        cookie.domain.replace(/^\./, "").toLowerCase() === normalizedDomain,
    );
    if (!hasExpectedDomain) {
      throw new Error(
        `The GameGuild authentication cookie is not shared with ${expectedDomain}. Actual domains: ${authCookies
          .map((cookie) => cookie.domain)
          .join(", ")}`,
      );
    }
  }

  if (
    authCookies.some((cookie) => !cookie.httpOnly || cookie.sameSite !== "Lax")
  ) {
    throw new Error(
      "The GameGuild authentication cookie must be HttpOnly and SameSite=Lax.",
    );
  }

  return authCookies;
}

export async function assertNoHorizontalOverflow(page, label) {
  const dimensions = await page.evaluate(() => ({
    documentWidth: Math.max(
      document.documentElement.scrollWidth,
      document.body?.scrollWidth ?? 0,
    ),
    viewportWidth: window.innerWidth,
  }));

  if (dimensions.documentWidth > dimensions.viewportWidth + 1) {
    throw new Error(
      `${label} overflows horizontally: ${dimensions.documentWidth}px document at ${dimensions.viewportWidth}px viewport.`,
    );
  }
}
