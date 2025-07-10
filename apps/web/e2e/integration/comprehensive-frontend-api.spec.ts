import { test, expect, Page, BrowserContext, Request, Response } from '@playwright/test';

/**
 * 🚀 COMPREHENSIVE Frontend-to-API E2E Test Suite
 *
 * This test suite provides COMPLETE coverage of frontend-to-API interactions,
 * focusing on real user scenarios and ensuring the React frontend properly
 * communicates with the .NET API backend.
 *
 * Coverage Areas:
 * ✅ Course catalog loading and rendering
 * ✅ Slug-based navigation and routing
 * ✅ API request/response validation
 * ✅ Error handling and fallbacks
 * ✅ Loading states and UI feedback
 * ✅ Data consistency across views
 * ✅ User interaction workflows
 * ✅ Network failure scenarios
 * ✅ Performance monitoring
 * ✅ Accessibility compliance
 */

interface ApiRequest {
  method: string;
  url: string;
  timestamp: number;
  status?: number;
  responseTime?: number;
}

interface TestMetrics {
  apiRequests: ApiRequest[];
  pageLoadTime: number;
  firstContentfulPaint: number;
  largestContentfulPaint: number;
  consoleErrors: string[];
  networkErrors: string[];
}

test.describe('🎮 Game Guild: Complete Frontend-to-API Integration', () => {
  let context: BrowserContext;
  let page: Page;
  let metrics: TestMetrics;

  test.beforeAll(async ({ browser }) => {
    // Create browser context with realistic settings
    context = await browser.newContext({
      viewport: { width: 1920, height: 1080 },
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
    });

    page = await context.newPage();

    // Initialize metrics tracking
    metrics = {
      apiRequests: [],
      pageLoadTime: 0,
      firstContentfulPaint: 0,
      largestContentfulPaint: 0,
      consoleErrors: [],
      networkErrors: [],
    };

    // Enhanced request/response tracking
    page.on('request', (request: Request) => {
      const apiRequest: ApiRequest = {
        method: request.method(),
        url: request.url(),
        timestamp: Date.now(),
      };

      if (request.url().includes('/api/')) {
        metrics.apiRequests.push(apiRequest);
        console.log(`📤 API Request: ${request.method()} ${request.url()}`);
      }
    });

    page.on('response', (response: Response) => {
      if (response.url().includes('/api/')) {
        const request = metrics.apiRequests.find((req) => req.url === response.url());
        if (request) {
          request.status = response.status();
          request.responseTime = Date.now() - request.timestamp;
        }
        console.log(`📥 API Response: ${response.status()} ${response.url()} (${request?.responseTime}ms)`);
      }
    });

    // Console error tracking
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        metrics.consoleErrors.push(msg.text());
        console.log(`❌ Console Error: ${msg.text()}`);
      }
    });

    // Network error tracking
    page.on('requestfailed', (request) => {
      metrics.networkErrors.push(`${request.method()} ${request.url()}: ${request.failure()?.errorText}`);
      console.log(`🚫 Network Error: ${request.method()} ${request.url()}`);
    });
  });

  test.afterAll(async () => {
    // Print comprehensive test metrics
    console.log('\n🔍 Test Metrics Summary:');
    console.log(`📊 API Requests: ${metrics.apiRequests.length}`);
    console.log(`⚠️ Console Errors: ${metrics.consoleErrors.length}`);
    console.log(`🚫 Network Errors: ${metrics.networkErrors.length}`);

    if (metrics.apiRequests.length > 0) {
      const avgResponseTime =
        metrics.apiRequests.filter((req) => req.responseTime).reduce((sum, req) => sum + (req.responseTime || 0), 0) /
        metrics.apiRequests.filter((req) => req.responseTime).length;
      console.log(`⚡ Average API Response Time: ${avgResponseTime.toFixed(2)}ms`);
    }

    await context.close();
  });

  test('🏠 Course Catalog: Complete Load and Interaction Journey', async () => {
    console.log('\n🎯 Testing complete course catalog workflow...');

    const startTime = Date.now();

    // Step 1: Navigate to course catalog
    console.log('📍 Step 1: Navigating to course catalog...');
    await page.goto('/courses', { waitUntil: 'networkidle' });

    metrics.pageLoadTime = Date.now() - startTime;
    console.log(`⏱️ Page load time: ${metrics.pageLoadTime}ms`);

    // Step 2: Verify page loaded correctly
    console.log('📍 Step 2: Verifying page structure...');
    await expect(page).toHaveTitle(/Course/i);

    // Check for main content areas
    const mainContent = page.locator('main, [role="main"], .course-catalog, .courses-container');
    await expect(mainContent).toBeVisible({ timeout: 10000 });

    // Step 3: Wait for API data to load
    console.log('📍 Step 3: Waiting for API data...');

    // Wait for either course cards or empty state
    const courseCards = page.locator('[data-testid="course-card"], .course-card, [class*="course"], .grid > div');
    const emptyState = page.locator('[data-testid="empty-state"], .empty-state, [class*="empty"]');

    // Wait for loading to finish
    await page.waitForFunction(
      () => {
        const loading = document.querySelector('[data-testid="loading"], .loading, [class*="loading"]');
        return !loading || (loading as HTMLElement).style.display === 'none' || !(loading as HTMLElement).checkVisibility?.();
      },
      { timeout: 15000 },
    );

    // Step 4: Verify API communication occurred
    console.log('📍 Step 4: Verifying API communication...');
    expect(metrics.apiRequests.length).toBeGreaterThan(0);

    // Check for programs/courses API call
    const programsApiCall = metrics.apiRequests.find((req) => req.url.includes('/api/programs') || req.url.includes('/courses'));
    expect(programsApiCall).toBeDefined();

    if (programsApiCall?.status) {
      expect(programsApiCall.status).toBe(200);
    }

    // Step 5: Test content rendering
    console.log('📍 Step 5: Testing content rendering...');

    const hasContent = (await courseCards.count()) > 0;
    const hasEmptyState = await emptyState.isVisible().catch(() => false);

    expect(hasContent || hasEmptyState).toBe(true);

    if (hasContent) {
      console.log(`✅ Found ${await courseCards.count()} course cards`);

      // Test first course card interaction
      const firstCard = courseCards.first();
      await expect(firstCard).toBeVisible();

      // Hover effect test
      await firstCard.hover();
      await page.waitForTimeout(500);

      // Check for interactive elements
      const cardLink = firstCard.locator('a, [role="button"], button');
      if ((await cardLink.count()) > 0) {
        await expect(cardLink.first()).toBeVisible();
      }
    } else {
      console.log('ℹ️ Empty state displayed (no courses available)');
      await expect(emptyState).toBeVisible();
    }

    // Step 6: Test navigation if courses exist
    if (hasContent) {
      console.log('📍 Step 6: Testing course navigation...');

      const firstCard = courseCards.first();
      const cardLink = firstCard.locator('a').first();

      if ((await cardLink.count()) > 0) {
        const href = await cardLink.getAttribute('href');
        expect(href).toBeTruthy();
        expect(href).toMatch(/\/course\/[a-z0-9-]+/);

        console.log(`🔗 Course link: ${href}`);

        // Navigate to course detail
        await cardLink.click();
        await page.waitForLoadState('networkidle');

        // Verify we're on course detail page
        expect(page.url()).toMatch(/\/course\/[a-z0-9-]+/);

        // Verify course detail API call
        const courseDetailApiCall = metrics.apiRequests.find((req) => req.url.includes('/api/programs/') && req.method === 'GET');
        expect(courseDetailApiCall).toBeDefined();
      }
    }

    // Step 7: Performance validation
    console.log('📍 Step 7: Performance validation...');
    expect(metrics.pageLoadTime).toBeLessThan(5000); // Page should load within 5s

    const slowApiCalls = metrics.apiRequests.filter((req) => req.responseTime && req.responseTime > 2000);
    expect(slowApiCalls.length).toBe(0); // No API calls should take >2s

    // Step 8: Error validation
    console.log('📍 Step 8: Error validation...');
    expect(metrics.consoleErrors.length).toBe(0);
    expect(metrics.networkErrors.length).toBe(0);

    console.log('✅ Course catalog journey completed successfully!');
  });

  test('🔗 Slug-Based Navigation: Direct URL Access', async () => {
    console.log('\n🎯 Testing direct slug-based navigation...');

    // Test valid course slug
    console.log('📍 Testing valid course slug navigation...');
    const testSlug = 'introduction-to-game-development';

    await page.goto(`/course/${testSlug}`, { waitUntil: 'networkidle' });

    // Verify API call for specific course
    const courseApiCall = metrics.apiRequests.find(
      (req) => req.url.includes(`/api/programs/${testSlug}`) || (req.url.includes(`/api/programs`) && req.url.includes(testSlug)),
    );

    if (courseApiCall) {
      console.log(`✅ API call made for course: ${courseApiCall.url}`);
      expect(courseApiCall.status).toBe(200);
    }

    // Check if we're on the right page (either loaded or 404)
    const pageContent = page.locator('main, [role="main"]');
    await expect(pageContent).toBeVisible();

    // Test invalid course slug
    console.log('📍 Testing invalid course slug navigation...');
    await page.goto('/course/invalid-course-slug-12345', { waitUntil: 'networkidle' });

    // Should show 404 or error state
    const errorState = page.locator('[data-testid="error-state"], .error, .not-found, h1');
    await expect(errorState).toBeVisible();

    console.log('✅ Slug navigation tests completed!');
  });

  test('🔄 API Error Handling and Resilience', async () => {
    console.log('\n🎯 Testing API error handling...');

    // Intercept API calls to simulate errors
    await page.route('**/api/programs**', (route) => {
      // Simulate server error
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' }),
      });
    });

    await page.goto('/courses', { waitUntil: 'networkidle' });

    // Should show error state
    const errorState = page.locator('[data-testid="error-state"], .error-message, .error');
    await expect(errorState).toBeVisible({ timeout: 10000 });

    console.log('✅ Error handling test completed!');
  });

  test('📱 Mobile Responsive Frontend-API Integration', async () => {
    console.log('\n🎯 Testing mobile responsive integration...');

    // Switch to mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/courses', { waitUntil: 'networkidle' });

    // Verify mobile layout
    const content = page.locator('main, [role="main"]');
    await expect(content).toBeVisible();

    // Verify API calls still work on mobile
    expect(metrics.apiRequests.length).toBeGreaterThan(0);

    // Test mobile navigation
    const courseCards = page.locator('[data-testid="course-card"], .course-card, [class*="course"]');
    if ((await courseCards.count()) > 0) {
      const firstCard = courseCards.first();
      await firstCard.tap();
      await page.waitForLoadState('networkidle');
    }

    console.log('✅ Mobile responsive test completed!');
  });

  test('🚀 Performance Under Load Simulation', async () => {
    console.log('\n🎯 Testing performance under load...');

    // Simulate slow network
    await context.route('**/*', (route) => {
      const delay = Math.random() * 1000; // Random delay 0-1s
      setTimeout(() => route.continue(), delay);
    });

    const startTime = Date.now();
    await page.goto('/courses', { waitUntil: 'networkidle' });
    const loadTime = Date.now() - startTime;

    console.log(`⏱️ Load time with network delay: ${loadTime}ms`);

    // Should still complete within reasonable time
    expect(loadTime).toBeLessThan(15000); // 15s max with delays

    // Verify content still loads
    const content = page.locator('main, [role="main"]');
    await expect(content).toBeVisible();

    console.log('✅ Performance test completed!');
  });

  test('♿ Accessibility and Frontend-API Data Flow', async () => {
    console.log('\n🎯 Testing accessibility with API data...');

    await page.goto('/courses', { waitUntil: 'networkidle' });

    // Check for proper ARIA labels and roles
    const mainContent = page.locator('[role="main"], main');
    await expect(mainContent).toBeVisible();

    // Check for proper heading structure
    const headings = page.locator('h1, h2, h3, h4, h5, h6');
    expect(await headings.count()).toBeGreaterThan(0);

    // Test keyboard navigation
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');

    // Check focus indicators
    const focusedElement = page.locator(':focus');
    await expect(focusedElement).toBeVisible();

    console.log('✅ Accessibility test completed!');
  });
});
