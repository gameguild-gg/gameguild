import { defineConfig, devices } from '@playwright/test';

const PORT = process.env.PORT ?? '3099';

export default defineConfig({
    testDir: './e2e',
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    workers: 1,
    reporter: 'html',
    timeout: 5 * 60 * 1000, // 5 min — WASM boot + compile is slow
    expect: {
        timeout: 2 * 60 * 1000, // 2 min for assertions
    },
    use: {
        baseURL: `http://localhost:${PORT}`,
        trace: 'on-first-retry',
        screenshot: 'only-on-failure',
    },
    projects: [
        {
            name: 'chromium',
            use: {
                ...devices['Desktop Chrome'],
            },
        },
        {
            name: 'firefox',
            use: {
                ...devices['Desktop Firefox'],
            },
        },
        {
            name: 'webkit',
            use: {
                ...devices['Desktop Safari'],
            },
        },
    ],
    webServer: {
        // Use `npm run dev` (not `npx next dev`) so the `predev` hook runs,
        // which syncs CDN assets (libSDL3.a, port markers, etc.) from
        // tools/emception/public/cdn/. Without this, SDL3 compilation fails
        // with FROZEN_CACHE because the cache-lib and port markers are missing.
        command: `PORT=${PORT} npm run dev`,
        cwd: '../../demos/emception-ide-next',
        url: `http://localhost:${PORT}`,
        // Keep deterministic test runs: stale dev servers can keep old bundled
        // workspace presets in memory and mask source-level fixes.
        reuseExistingServer: !process.env.CI,
        // Predev performs multi-package rebuilds before Next starts; allow a
        // longer startup window so Playwright doesn't kill the server early.
        timeout: 60_000,
        stdout: 'pipe',
        stderr: 'pipe',
    },
});
