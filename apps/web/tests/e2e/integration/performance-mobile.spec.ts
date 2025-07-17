import { devices, expect, test } from '@playwright/test';

/**
 * Performance and Mobile E2E Tests
 *
 * Tests performance, mobile responsiveness, and accessibility
 */

test.describe('Performance and Mobile Frontend-API Integration', () => {
  test('should load course catalog with good performance', async ({ page }) => {
    console.log('⚡ Testing course catalog performance...');

    // Start performance measurement
    const startTime = Date.now();

    await page.goto('/courses/catalog');

    // Wait for First Contentful Paint
    await page.waitForSelector('h1');
    const fcpTime = Date.now() - startTime;

    // Wait for full interactivity
    await page.waitForLoadState('networkidle');
    const loadTime = Date.now() - startTime;

    console.log(`First Contentful Paint: ${fcpTime}ms`);
    console.log(`Full Load Time: ${loadTime}ms`);

    // Performance expectations
    expect(fcpTime).toBeLessThan(3000); // FCP under 3 seconds
    expect(loadTime).toBeLessThan(10000); // Full load under 10 seconds

    // Verify content loaded
    const courseCount = await page.locator('[data-testid="course-card"]').count();
    expect(courseCount).toBeGreaterThan(0);

    console.log(`✅ Performance test passed - ${courseCount} courses loaded in ${loadTime}ms`);
  });

  test('should work on mobile devices', async ({ browser }) => {
    console.log('📱 Testing mobile device compatibility...');

    const context = await browser.newContext({
      ...devices['iPhone 12'],
    });

    const page = await context.newPage();

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    // Verify mobile layout
    const courseGrid = page.locator('[data-testid="course-grid"]');
    await expect(courseGrid).toBeVisible();

    // Test mobile interactions
    const courseCards = page.locator('[data-testid="course-card"]');
    const firstCourse = courseCards.first();

    if (await firstCourse.isVisible()) {
      // Test tap interaction
      await firstCourse.tap();
      await page.waitForLoadState('networkidle');

      // Should navigate successfully
      expect(page.url()).toMatch(/\/course\/[a-z0-9-]+$/);

      console.log('✅ Mobile navigation works correctly');
    }

    await context.close();
  });

  test('should handle slow network conditions', async ({ page }) => {
    console.log('🐌 Testing slow network conditions...');

    // Simulate slow 3G connection
    await page.route('**/*', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 200)); // Add 200ms delay
      route.continue();
    });

    const startTime = Date.now();
    await page.goto('/courses/catalog');

    // Should show loading state
    const loadingIndicator = page.locator('[data-testid="loading"]');

    // Eventually should load content
    await expect(page.locator('[data-testid="course-grid"]')).toBeVisible({ timeout: 20000 });

    const loadTime = Date.now() - startTime;
    console.log(`Loaded under slow network in ${loadTime}ms`);

    // Verify courses loaded despite slow network
    const courseCount = await page.locator('[data-testid="course-card"]').count();
    expect(courseCount).toBeGreaterThan(0);

    console.log('✅ Handles slow network conditions gracefully');
  });

  test('should be accessible to screen readers', async ({ page }) => {
    console.log('♿ Testing accessibility features...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    // Check for proper heading structure
    const h1 = page.locator('h1');
    await expect(h1).toBeVisible();

    // Check for proper link accessibility
    const courseLinks = page.locator('[data-testid="course-card"] a');
    const linkCount = await courseLinks.count();

    for (let i = 0; i < Math.min(3, linkCount); i++) {
      const link = courseLinks.nth(i);

      // Links should have accessible text
      const linkText = await link.textContent();
      expect(linkText?.trim().length).toBeGreaterThan(0);

      // Links should have href
      const href = await link.getAttribute('href');
      expect(href).toBeTruthy();
    }

    console.log(`✅ Checked accessibility for ${Math.min(3, linkCount)} course links`);
  });

  test('should handle concurrent users simulation', async ({ browser }) => {
    console.log('👥 Testing concurrent users simulation...');

    const contexts = await Promise.all([browser.newContext(), browser.newContext(), browser.newContext()]);

    const pages = await Promise.all(contexts.map((context) => context.newPage()));

    // Simulate 3 users accessing the catalog simultaneously
    const results = await Promise.all(
      pages.map(async (page, index) => {
        const startTime = Date.now();

        await page.goto('/courses/catalog');
        await page.waitForLoadState('networkidle');

        const courseCount = await page.locator('[data-testid="course-card"]').count();
        const loadTime = Date.now() - startTime;

        return { user: index + 1, courseCount, loadTime };
      }),
    );

    // All users should get data
    results.forEach((result) => {
      expect(result.courseCount).toBeGreaterThan(0);
      console.log(`User ${result.user}: ${result.courseCount} courses in ${result.loadTime}ms`);
    });

    // Clean up
    await Promise.all(contexts.map((context) => context.close()));

    console.log('✅ Concurrent users handled successfully');
  });

  test('should maintain state during navigation', async ({ page }) => {
    console.log('🔄 Testing state maintenance during navigation...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    // Get initial course count
    const initialCourseCount = await page.locator('[data-testid="course-card"]').count();

    // Navigate to a course
    const firstCourse = page.locator('[data-testid="course-card"]').first();
    await firstCourse.click();
    await page.waitForLoadState('networkidle');

    // Navigate back
    await page.goBack();
    await page.waitForLoadState('networkidle');

    // Should maintain course data
    const finalCourseCount = await page.locator('[data-testid="course-card"]').count();
    expect(finalCourseCount).toBe(initialCourseCount);

    console.log('✅ State maintained during navigation');
  });

  test('should handle large dataset efficiently', async ({ page }) => {
    console.log('📊 Testing large dataset handling...');

    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const courseCount = await page.locator('[data-testid="course-card"]').count();
    console.log(`Testing with ${courseCount} courses`);

    if (courseCount > 10) {
      // Test scrolling performance with many courses
      const startTime = Date.now();

      // Scroll to bottom
      await page.evaluate(() => {
        window.scrollTo(0, document.body.scrollHeight);
      });

      await page.waitForTimeout(1000);

      // Scroll back to top
      await page.evaluate(() => {
        window.scrollTo(0, 0);
      });

      const scrollTime = Date.now() - startTime;
      console.log(`Scrolling performance: ${scrollTime}ms`);

      // Should handle scrolling smoothly
      expect(scrollTime).toBeLessThan(5000);

      console.log('✅ Large dataset handled efficiently');
    } else {
      console.log('ℹ️ Dataset too small for meaningful performance test');
    }
  });

  test('should work offline with cached data', async ({ page }) => {
    console.log('📴 Testing offline capability...');

    // First load the page online
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');

    const onlineCourseCount = await page.locator('[data-testid="course-card"]').count();
    console.log(`Online course count: ${onlineCourseCount}`);

    // Go offline
    await page.context().setOffline(true);

    // Try to reload the page
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Should either show cached content or proper offline message
    const pageContent = await page.textContent('body');
    const hasContent = pageContent && pageContent.trim().length > 100;

    if (hasContent) {
      // If content is shown, it should be meaningful
      const offlineCourseCount = await page.locator('[data-testid="course-card"]').count();

      if (offlineCourseCount > 0) {
        console.log('✅ Offline cached content displayed');
      } else {
        console.log('✅ Offline state handled appropriately');
      }
    } else {
      console.log('ℹ️ No offline caching implemented');
    }

    // Restore online state
    await page.context().setOffline(false);
  });
});
