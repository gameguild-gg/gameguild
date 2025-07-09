import { defineConfig } from '@playwright/test';

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './e2e',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* API testing configuration */
    extraHTTPHeaders: {
      Accept: 'application/json',
    },
  },

  /* Configure projects for major browsers */
  projects: [
    /* API testing project - no web servers */
    {
      name: 'api-only',
      testMatch: '**/api/simple-api-test.spec.ts',
      use: {
        baseURL: 'http://localhost:5001',
      },
    },
  ],
});
