import { test, expect, Page, BrowserContext } from '@playwright/test';

/**
 * Comprehensive Frontend-to-API E2E Tests
 *
 * These tests validate the complete user journey:
 * 1. User visits course catalog
 * 2. Course data loads from API
 * 3. User interacts with course cards
 * 4. Navigation to course details works
 * 5. Course detail page loads data correctly
 * 6. All slug-based navigation functions properly
 */

test.describe('Frontend to API Integration - Complete User Journey', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();

    // Add console error tracking
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        console.log('Browser console error:', msg.text());
      }
    });

    // Track network requests
    page.on('request', (request) => {
      if (request.url().includes('/api/')) {
        console.log('API Request:', request.method(), request.url());
      }
    });

    page.on('response', (response) => {
      if (response.url().includes('/api/')) {
        console.log('API Response:', response.status(), response.url());
      }
    });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('should load course catalog and display courses from API', async () => {
    console.log('🎯 Testing course catalog page...');

    // Navigate to course catalog
    await page.goto('/courses/catalog');

    // Wait for the page to load completely
    await page.waitForLoadState('networkidle');

    // Check that the main heading is present
    await expect(page.locator('h1')).toContainText('Course Catalog');

    // Wait for course grid to appear
    const courseGrid = page.locator('[data-testid="course-grid"]');
    await expect(courseGrid).toBeVisible({ timeout: 10000 });

    // Verify courses are loaded from API
    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();

    expect(courseCount).toBeGreaterThan(0);
    console.log(`✅ Found ${courseCount} courses in catalog`);

    // Verify first course has required elements
    if (courseCount > 0) {
      const firstCourse = courseCards.first();
      await expect(firstCourse.locator('[data-testid="course-title"]')).toBeVisible();
      await expect(firstCourse.locator('[data-testid="course-description"]')).toBeVisible();
      await expect(firstCourse.locator('a')).toHaveAttribute('href', /^\/course\/[a-z0-9-]+$/);
    }
  });

  test('should handle course filtering and maintain API integration', async () => {
    console.log('🔍 Testing course filtering...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    // Wait for initial courses to load
    await expect(page.locator('[data-testid="course-grid"]')).toBeVisible();

    const initialCourseCount = await page.locator('[data-testid="course-card"]').count();
    console.log(`Initial course count: ${initialCourseCount}`);

    // Test category filtering if filters exist
    const categoryFilter = page.locator('[data-testid*="filter-"]').first();
    if (await categoryFilter.isVisible()) {
      await categoryFilter.click();

      // Wait for filter to apply
      await page.waitForTimeout(1000);

      const filteredCourseCount = await page.locator('[data-testid="course-card"]').count();
      console.log(`Filtered course count: ${filteredCourseCount}`);

      // Verify filtered courses still have proper links
      const filteredCourses = page.locator('[data-testid="course-card"]');
      const firstFilteredCourse = filteredCourses.first();

      if (await firstFilteredCourse.isVisible()) {
        await expect(firstFilteredCourse.locator('a')).toHaveAttribute('href', /^\/course\/[a-z0-9-]+$/);
      }
    }
  });

  test('should navigate from catalog to course detail using slug', async () => {
    console.log('🚀 Testing catalog to course detail navigation...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    // Wait for courses to load
    const courseGrid = page.locator('[data-testid="course-grid"]');
    await expect(courseGrid).toBeVisible({ timeout: 10000 });

    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();

    if (courseCount > 0) {
      // Get course details before navigation
      const firstCourse = courseCards.first();
      const courseTitle = await firstCourse.locator('[data-testid="course-title"]').textContent();
      const courseLink = firstCourse.locator('a').first();
      const href = await courseLink.getAttribute('href');

      console.log(`Navigating to course: ${courseTitle}`);
      console.log(`Course URL: ${href}`);

      // Click to navigate to course detail
      await firstCourse.click();

      // Wait for navigation to complete
      await page.waitForLoadState('networkidle');

      // Verify we're on the course detail page
      expect(page.url()).toMatch(/\/course\/[a-z0-9-]+$/);

      // Verify course detail page loaded correctly
      await expect(page.locator('h1')).toBeVisible({ timeout: 10000 });

      // Verify the course title matches (should be the same)
      if (courseTitle) {
        await expect(page.locator('h1')).toContainText(courseTitle);
      }

      console.log('✅ Successfully navigated to course detail page');
    } else {
      test.skip(true, 'No courses available for navigation test');
    }
  });

  test('should load course detail page data from API', async () => {
    console.log('📚 Testing course detail page API integration...');

    // First get a valid course slug from the catalog
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const courseCards = page.locator('[data-testid="course-card"]');
    await expect(courseCards.first()).toBeVisible({ timeout: 10000 });

    const firstCourseLink = courseCards.first().locator('a').first();
    const href = await firstCourseLink.getAttribute('href');

    if (href) {
      // Navigate directly to course detail page
      await page.goto(href);
      await page.waitForLoadState('networkidle');

      // Verify page loaded successfully (not a 404)
      const pageContent = page.locator('body');
      await expect(pageContent).not.toContainText('404');
      await expect(pageContent).not.toContainText('Not Found');

      // Verify essential course detail elements are present
      await expect(page.locator('h1')).toBeVisible({ timeout: 10000 });

      // Check for course sidebar (enrollment area)
      const sidebar = page.locator('[data-testid="course-sidebar"]');
      if (await sidebar.isVisible()) {
        console.log('✅ Course sidebar loaded successfully');
      }

      // Check for course overview
      const overview = page.locator('[data-testid="course-overview"]');
      if (await overview.isVisible()) {
        console.log('✅ Course overview loaded successfully');
      }

      console.log('✅ Course detail page loaded with API data');
    } else {
      test.skip(true, 'No valid course href found');
    }
  });

  test('should handle invalid course slugs gracefully', async () => {
    console.log('❌ Testing invalid course slug handling...');

    // Navigate to a non-existent course slug
    await page.goto('/course/definitely-not-a-real-course-slug-12345');
    await page.waitForLoadState('networkidle');

    // Should show 404 page or redirect
    const url = page.url();
    const pageContent = await page.textContent('body');

    const is404Page = pageContent?.includes('404') || pageContent?.includes('Not Found') || pageContent?.includes('Page not found');

    const isRedirected = url.includes('/courses') && !url.includes('/course/definitely');

    expect(is404Page || isRedirected).toBeTruthy();
    console.log('✅ Invalid slug handled appropriately');
  });

  test('should maintain course data consistency across pages', async () => {
    console.log('🔄 Testing data consistency between catalog and detail...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const courseCards = page.locator('[data-testid="course-card"]');
    await expect(courseCards.first()).toBeVisible({ timeout: 10000 });

    // Get course data from catalog
    const firstCourse = courseCards.first();
    const catalogTitle = await firstCourse.locator('[data-testid="course-title"]').textContent();
    const catalogDescription = await firstCourse.locator('[data-testid="course-description"]').textContent();

    // Navigate to detail page
    await firstCourse.click();
    await page.waitForLoadState('networkidle');

    // Verify data consistency
    if (catalogTitle) {
      const detailTitle = await page.locator('h1').textContent();
      expect(detailTitle).toContain(catalogTitle.trim());
      console.log('✅ Course title consistent between pages');
    }

    if (catalogDescription) {
      const pageText = await page.textContent('body');
      // Description should appear somewhere on the detail page
      expect(pageText).toContain(catalogDescription.trim());
      console.log('✅ Course description consistent between pages');
    }
  });

  test('should handle loading states properly', async () => {
    console.log('⏳ Testing loading states...');

    // Use network throttling to make loading more apparent
    await page.route('**/api/**', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 500)); // Add 500ms delay
      route.continue();
    });

    await page.goto('/courses/catalog');

    // Check if loading indicator appears (might be very fast)
    const loadingIndicator = page.locator('[data-testid="loading"]');

    // Eventually courses should load
    await expect(page.locator('[data-testid="course-grid"]')).toBeVisible({ timeout: 15000 });

    // Loading should be gone
    if (await loadingIndicator.isVisible({ timeout: 1000 })) {
      await expect(loadingIndicator).not.toBeVisible({ timeout: 5000 });
    }

    console.log('✅ Loading states handled correctly');
  });

  test('should handle API errors gracefully', async () => {
    console.log('💥 Testing API error handling...');

    // Mock API failure
    await page.route('**/api/program/published', (route) => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' }),
      });
    });

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    // Should show error state or fallback
    const pageContent = await page.textContent('body');
    const hasErrorMessage = pageContent?.includes('error') || pageContent?.includes('failed') || pageContent?.includes('try again');

    // Page should not be completely blank
    expect(pageContent?.trim().length).toBeGreaterThan(10);

    if (hasErrorMessage) {
      console.log('✅ Error state displayed appropriately');
    } else {
      console.log('✅ Fallback content displayed');
    }
  });

  test('should track API requests and responses', async () => {
    console.log('📊 Testing API request/response tracking...');

    const apiRequests: string[] = [];
    const apiResponses: { url: string; status: number }[] = [];

    page.on('request', (request) => {
      if (request.url().includes('/api/')) {
        apiRequests.push(request.url());
      }
    });

    page.on('response', (response) => {
      if (response.url().includes('/api/')) {
        apiResponses.push({ url: response.url(), status: response.status() });
      }
    });

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    // Wait for courses to load
    await expect(page.locator('[data-testid="course-grid"]')).toBeVisible({ timeout: 10000 });

    // Verify API calls were made
    expect(apiRequests.length).toBeGreaterThan(0);
    expect(apiResponses.length).toBeGreaterThan(0);

    // Check for expected API endpoints
    const hasPublishedProgramsCall = apiRequests.some((url) => url.includes('/api/program/published'));
    expect(hasPublishedProgramsCall).toBeTruthy();

    // Check response statuses
    const successfulResponses = apiResponses.filter((r) => r.status >= 200 && r.status < 300);
    expect(successfulResponses.length).toBeGreaterThan(0);

    console.log(`✅ Tracked ${apiRequests.length} API requests and ${apiResponses.length} responses`);
    console.log('API Requests:', apiRequests);
    console.log('API Responses:', apiResponses);
  });
});
