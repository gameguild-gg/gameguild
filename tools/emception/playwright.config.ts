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
                launchOptions: {
                    // JSPI (WebAssembly.promising / Suspending) is required for
                    // subprocess dispatch — emcc calls clang, lld, etc. via JSPI.
                    args: [
                        '--enable-features=WebAssemblyJSPromiseIntegration',
                        '--js-flags=--experimental-wasm-jspi',
                    ],
                },
            },
        },
    ],
    webServer: {
        // Use `npm run dev` (not `npx next dev`) so the `predev` hook runs,
        // which syncs CDN assets (libSDL3.a, port markers, etc.) from
        // tools/emception/public/cdn/. Without this, SDL3 compilation fails
        // with FROZEN_CACHE because the cache-lib and port markers are missing.
        command: `npm run dev -- --port ${PORT}`,
        cwd: '../../demos/emception-next',
        url: `http://localhost:${PORT}`,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
        stdout: 'pipe',
        stderr: 'pipe',
    },
});
