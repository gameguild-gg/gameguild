import { defineConfig, devices } from '@playwright/test';

const port = Number(process.env.EMCEPTION_SMOKE_PORT ?? 3097);

export default defineConfig({
    testDir: './e2e',
    testMatch: 'smoke-cpp.spec.ts',
    fullyParallel: false,
    forbidOnly: Boolean(process.env.CI),
    retries: process.env.CI ? 1 : 0,
    workers: 1,
    reporter: 'list',
    timeout: 5 * 60 * 1000,
    expect: { timeout: 2 * 60 * 1000 },
    use: {
        baseURL: `http://127.0.0.1:${port}`,
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure',
    },
    projects: [{ name: 'chromium', use: devices['Desktop Chrome'] }],
    webServer: {
        command: `pnpm exec vite --strictPort --host 127.0.0.1 --port ${port}`,
        cwd: './apps/run-react',
        url: `http://127.0.0.1:${port}`,
        reuseExistingServer: false,
        timeout: 120_000,
        stdout: 'pipe',
        stderr: 'pipe',
    },
});
