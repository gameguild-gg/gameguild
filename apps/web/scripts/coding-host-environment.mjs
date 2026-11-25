// @ts-check

/**
 * @param {Readonly<Record<string, string | undefined>>} environment
 * @returns {{ readonly webBaseUrl: string; readonly apiBaseUrl: string }}
 */
export function resolveCodingHostEnvironment(environment) {
  if (environment.E2E_RUN !== '1') throw new TypeError('E2E_RUN=1 is required for a coding host proof.');
  return {
    webBaseUrl: localOrigin(environment.PLAYWRIGHT_WEB_BASE_URL ?? 'http://localhost:3000'),
    apiBaseUrl: localOrigin(environment.API_BASE_URL ?? 'http://localhost:8080'),
  };
}

/** @param {string} value */
function localOrigin(value) {
  const url = new URL(value);
  if (
    !['http:', 'https:'].includes(url.protocol) ||
    !(url.hostname === 'localhost' || url.hostname.endsWith('.localhost') || url.hostname === '127.0.0.1' || url.hostname === '[::1]') ||
    url.username ||
    url.password ||
    url.pathname !== '/' ||
    url.search ||
    url.hash
  ) {
    throw new TypeError('The coding proof requires an uncredentialed loopback origin.');
  }
  return url.origin;
}
