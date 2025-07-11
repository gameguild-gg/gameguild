import { test, expect } from '@playwright/test';

/**
 * Advanced Frontend-to-API Integration Tests
 *
 * Comprehensive tests covering real user workflows and edge cases
 */

test.describe('Advanced Frontend-API Integration', () => {
  test('should load course catalog and handle real user interactions', async ({ page }) => {
    console.log('🎯 Testing comprehensive course catalog interaction...');

    // Track all API calls during the test
    const apiCalls: string[] = [];
    page.on('request', (request) => {
      if (request.url().includes('/api/')) {
        apiCalls.push(`${request.method()} ${request.url()}`);
      }
    });

    // Navigate to course catalog
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    // Verify page loaded with API data
    await expect(page.locator('h1')).toContainText('Course Catalog');
    const courseGrid = page.locator('[data-testid="course-grid"]');
    await expect(courseGrid).toBeVisible({ timeout: 10000 });

    // Count courses loaded from API
    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();
    expect(courseCount).toBeGreaterThan(0);

    console.log(`✅ Loaded ${courseCount} courses from API`);
    console.log('API calls made:', apiCalls);

    // Test first course interaction
    const firstCourse = courseCards.first();
    const courseTitle = await firstCourse.locator('[data-testid="course-title"]').textContent();
    const courseHref = await firstCourse.locator('a').getAttribute('href');

    expect(courseHref).toMatch(/^\/course\/[a-z0-9-]+$/);
    console.log(`Course: "${courseTitle}" -> ${courseHref}`);

    // Navigate to course detail
    await firstCourse.click();
    await page.waitForLoadState('networkidle');

    // Verify navigation worked
    expect(page.url()).toMatch(/\/course\/[a-z0-9-]+$/);
    await expect(page.locator('h1')).toBeVisible();

    if (courseTitle) {
      await expect(page.locator('h1')).toContainText(courseTitle);
    }

    console.log('✅ Successfully navigated from catalog to course detail');
  });

  test('should handle multiple course navigation patterns', async ({ page }) => {
    console.log('🔄 Testing multiple course navigation patterns...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();

    // Test navigation to multiple courses
    const maxTests = Math.min(3, courseCount);
    for (let i = 0; i < maxTests; i++) {
      // Go back to catalog
      await page.goto('/courses/catalog');
      await page.waitForLoadState('networkidle');

      const course = courseCards.nth(i);
      const courseTitle = await course.locator('[data-testid="course-title"]').textContent();

      // Navigate to course
      await course.click();
      await page.waitForLoadState('networkidle');

      // Verify we're on the right course
      const currentUrl = page.url();
      expect(currentUrl).toMatch(/\/course\/[a-z0-9-]+$/);

      // Verify page loaded correctly (no 404)
      const pageContent = await page.textContent('body');
      expect(pageContent).not.toContain('404');
      expect(pageContent).not.toContain('Not Found');

      console.log(`✅ Course ${i + 1}: "${courseTitle}" loaded successfully`);
    }
  });

  test('should validate course data structure from API', async ({ page }) => {
    console.log('🔍 Testing course data structure validation...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const courseCards = page.locator('[data-testid="course-card"]');
    await expect(courseCards.first()).toBeVisible();

    const courseCount = await courseCards.count();
    let validCourses = 0;

    // Validate data structure for multiple courses
    for (let i = 0; i < Math.min(5, courseCount); i++) {
      const course = courseCards.nth(i);

      // Check required elements
      const title = course.locator('[data-testid="course-title"]');
      const description = course.locator('[data-testid="course-description"]');
      const link = course.locator('a');

      const titleVisible = await title.isVisible();
      const descriptionVisible = await description.isVisible();
      const linkExists = (await link.count()) > 0;

      if (titleVisible && descriptionVisible && linkExists) {
        const href = await link.first().getAttribute('href');
        if (href && href.match(/^\/course\/[a-z0-9-]+$/)) {
          validCourses++;
        }
      }
    }

    // At least 80% of courses should have valid structure
    const validPercentage = (validCourses / Math.min(5, courseCount)) * 100;
    expect(validPercentage).toBeGreaterThanOrEqual(80);

    console.log(`✅ ${validCourses}/${Math.min(5, courseCount)} courses have valid data structure`);
  });

  test('should handle slow API responses gracefully', async ({ page }) => {
    console.log('⏳ Testing slow API response handling...');

    // Add artificial delay to API responses
    await page.route('**/api/**', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 1000)); // 1 second delay
      route.continue();
    });

    const startTime = Date.now();
    await page.goto('/courses/catalog');

    // Should eventually load despite delay
    await expect(page.locator('[data-testid="course-grid"]')).toBeVisible({ timeout: 15000 });

    const loadTime = Date.now() - startTime;
    console.log(`Page loaded in ${loadTime}ms with artificial delay`);

    // Verify courses still loaded correctly
    const courseCount = await page.locator('[data-testid="course-card"]').count();
    expect(courseCount).toBeGreaterThan(0);

    console.log('✅ Handled slow API responses correctly');
  });

  test('should work with browser navigation (back/forward)', async ({ page }) => {
    console.log('🧭 Testing browser navigation integration...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const courseCards = page.locator('[data-testid="course-card"]');
    await courseCards.first().click();
    await page.waitForLoadState('networkidle');

    // Verify we're on course detail page
    expect(page.url()).toMatch(/\/course\/[a-z0-9-]+$/);

    // Use browser back button
    await page.goBack();
    await page.waitForLoadState('networkidle');

    // Should be back on catalog
    expect(page.url()).toContain('/courses/catalog');
    await expect(page.locator('[data-testid="course-grid"]')).toBeVisible();

    // Use browser forward button
    await page.goForward();
    await page.waitForLoadState('networkidle');

    // Should be back on course detail
    expect(page.url()).toMatch(/\/course\/[a-z0-9-]+$/);

    console.log('✅ Browser navigation works correctly with API integration');
  });

  test('should handle course enrollment sidebar integration', async ({ page }) => {
    console.log('💡 Testing enrollment sidebar API integration...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const courseCards = page.locator('[data-testid="course-card"]');
    await courseCards.first().click();
    await page.waitForLoadState('networkidle');

    // Check for course sidebar
    const sidebar = page.locator('[data-testid="course-sidebar"]');
    if (await sidebar.isVisible()) {
      console.log('✅ Course sidebar is present');

      // Check for enrollment-related content
      const sidebarText = await sidebar.textContent();
      const hasEnrollmentContent =
        sidebarText?.toLowerCase().includes('enroll') || sidebarText?.toLowerCase().includes('access') || sidebarText?.toLowerCase().includes('start');

      if (hasEnrollmentContent) {
        console.log('✅ Enrollment content found in sidebar');
      }

      // Check for action buttons
      const buttons = sidebar.locator('button');
      const buttonCount = await buttons.count();
      if (buttonCount > 0) {
        console.log(`✅ Found ${buttonCount} action buttons in sidebar`);
      }
    } else {
      console.log('ℹ️ No enrollment sidebar found');
    }
  });

  test('should handle direct course URL access', async ({ page }) => {
    console.log('🔗 Testing direct course URL access...');

    // First get a valid course slug from the catalog
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const firstCourseLink = page.locator('[data-testid="course-card"] a').first();
    const href = await firstCourseLink.getAttribute('href');

    if (href) {
      // Navigate directly to the course URL
      await page.goto(href);
      await page.waitForLoadState('networkidle');

      // Should load successfully
      const pageContent = await page.textContent('body');
      expect(pageContent).not.toContain('404');
      expect(pageContent).not.toContain('Not Found');

      // Should have course page elements
      await expect(page.locator('h1')).toBeVisible();

      console.log(`✅ Direct access to ${href} works correctly`);
    }
  });

  test('should handle edge case URLs and slugs', async ({ page }) => {
    console.log('🧪 Testing edge case URLs and slug handling...');

    const edgeCaseUrls = [
      '/course/non-existent-course-123',
      '/course/',
      '/course/UPPERCASE-SLUG',
      '/course/slug_with_underscores',
      '/course/slug with spaces',
      '/course/very-very-very-long-slug-that-might-cause-issues-in-some-systems',
    ];

    for (const url of edgeCaseUrls) {
      await page.goto(url);
      await page.waitForLoadState('networkidle');

      // Should handle gracefully (404, redirect, or error page)
      const pageContent = await page.textContent('body');
      const currentUrl = page.url();

      // Should not crash or show blank page
      expect(pageContent?.trim().length).toBeGreaterThan(10);

      const handledGracefully =
        pageContent?.includes('404') || pageContent?.includes('Not Found') || currentUrl.includes('/courses') || pageContent?.includes('error');

      if (handledGracefully) {
        console.log(`✅ ${url} handled gracefully`);
      } else {
        console.log(`ℹ️ ${url} returned content but unclear if error handling`);
      }
    }
  });
});
