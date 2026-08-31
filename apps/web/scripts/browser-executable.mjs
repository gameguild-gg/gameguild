/**
 * Resolve the real browser used by local browser E2E runs.
 *
 * CI intentionally leaves this undefined so Playwright uses its managed
 * Linux browser. Windows developers can opt in explicitly, while a normal
 * Chrome installation works after a Playwright cache cleanup.
 */
export function resolveChromiumExecutablePath({
  env = process.env,
  platform = process.platform,
  exists = () => false,
} = {}) {
  const configured = env.CODING_CYCLE_CHROMIUM_EXECUTABLE;
  if (configured) return configured;
  if (platform !== 'win32') return undefined;

  const candidates = [
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    env.LOCALAPPDATA ? `${env.LOCALAPPDATA}/Google/Chrome/Application/chrome.exe` : undefined,
  ];
  return candidates.find((candidate) => candidate && exists(candidate));
}
