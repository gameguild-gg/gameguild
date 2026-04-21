import { defineConfig, devices } from '@playwright/test';
import net from 'node:net';

const DEFAULT_PORT = Number(process.env.PORT ?? 3099);

async function isPortFree(port: number): Promise<boolean> {
    return new Promise((resolve) => {
        const server = net.createServer();

        server.once('error', () => resolve(false));
        server.once('listening', () => {
            server.close(() => resolve(true));
        });

        // Probe the port on the default interface set (IPv4/IPv6), not just
        // 127.0.0.1. This catches cases where another process is bound on
        // "::" and avoids false "free" readings.
        server.listen(port);
    });
}

async function resolvePlaywrightPort(preferredPort: number): Promise<number> {
    if (await isPortFree(preferredPort)) {
        return preferredPort;
    }

    // Probe a small deterministic range to avoid local port conflicts.
    for (let port = preferredPort + 1; port <= preferredPort + 25; port += 1) {
        if (await isPortFree(port)) {
            return port;
        }
    }

    throw new Error(`No available port found in range ${preferredPort}-${preferredPort + 25} for Playwright webServer.`);
}

const PORT = await resolvePlaywrightPort(DEFAULT_PORT);
const BASE_URL = `http://localhost:${PORT}`;

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
        baseURL: BASE_URL,
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
        command: `npm run dev -- --port ${PORT}`,
        cwd: '../../demos/emception-next',
        url: BASE_URL,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
        stdout: 'pipe',
        stderr: 'pipe',
    },
});
