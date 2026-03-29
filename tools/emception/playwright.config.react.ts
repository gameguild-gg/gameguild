import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config targeting demos/emception-react (Vite).
 *
 * Use:  npx playwright test --config=playwright.config.react.ts
 * Or:   npm run e2e:react   (from tools/emception/)
 */

const PORT = process.env.PORT ?? '3098';

export default defineConfig({
    testDir: './e2e',
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    workers: 1,
    reporter: [['html', { outputFolder: 'playwright-report-react' }], ['list']],
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
                launchOptions: {
                    // JSPI is required for subprocess dispatch (emcc → clang/lld via JSPI).
                    args: [
                        '--enable-features=WebAssemblyJSPromiseIntegration',
                        '--js-flags=--experimental-wasm-jspi',
                    ],
                },
            },
        },
    ],
    webServer: {
        // Run `vite --port PORT` directly; the `predev` CDN-sync step is a
        // one-time setup that must have been run beforehand (npm run dev once,
        // or `node ../../scripts/sync-emception-cdn.mjs demos/emception-react`).
        command: `npx vite --port ${PORT}`,
        cwd: '../../demos/emception-react',
        url: `http://localhost:${PORT}`,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
        stdout: 'pipe',
        stderr: 'pipe',
    },
});
