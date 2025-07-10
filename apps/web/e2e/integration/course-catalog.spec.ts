import { test, expect } from '@playwright/test';

test.describe('Course Catalog - Slug-based Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the courses page
    await page.goto('/courses');
  });

  test('should display course catalog with slug-based URLs', async ({ page }) => {
    // Wait for courses to load
    await page.waitForSelector('[data-testid="course-grid"]', { timeout: 10000 });

    // Check if courses are displayed
    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();

    if (courseCount > 0) {
      // Verify first course card has proper slug-based link
      const firstCourseCard = courseCards.first();
      const courseLink = firstCourseCard.locator('a').first();
      const href = await courseLink.getAttribute('href');

      expect(href).toBeTruthy();
      expect(href).toMatch(/^\/course\/[a-z0-9-]+$/);
      expect(href).not.toContain(' ');
      expect(href).not.toContain('_');
    }
  });

  test('should navigate to course detail using slug', async ({ page }) => {
    // Wait for courses to load
    await page.waitForSelector('[data-testid="course-grid"]', { timeout: 10000 });

    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();

    if (courseCount > 0) {
      // Get the first course title and click it
      const firstCourseCard = courseCards.first();
      const courseTitle = await firstCourseCard.locator('[data-testid="course-title"]').textContent();

      await firstCourseCard.click();

      // Should navigate to course detail page with slug in URL
      await expect(page).toHaveURL(/\/course\/[a-z0-9-]+$/);

      // Verify course title is displayed on detail page
      if (courseTitle) {
        await expect(page.locator('h1')).toContainText(courseTitle);
      }
    } else {
      test.skip(true, 'No courses available for testing');
    }
  });

  test('should handle course filtering while maintaining slug navigation', async ({ page }) => {
    // Wait for courses to load
    await page.waitForSelector('[data-testid="course-grid"]', { timeout: 10000 });

    // Check if filters are available
    const filterButtons = page.locator('[data-testid="filter-button"]');
    const filterCount = await filterButtons.count();

    if (filterCount > 0) {
      // Click on a filter (e.g., programming)
      const programmingFilter = page.locator('[data-testid="filter-programming"]').first();
      if (await programmingFilter.isVisible()) {
        await programmingFilter.click();

        // Wait for filtered results
        await page.waitForTimeout(1000);

        // Verify filtered courses still have slug-based URLs
        const filteredCourseCards = page.locator('[data-testid="course-card"]');
        const filteredCount = await filteredCourseCards.count();

        if (filteredCount > 0) {
          const courseLink = filteredCourseCards.first().locator('a').first();
          const href = await courseLink.getAttribute('href');

          expect(href).toBeTruthy();
          expect(href).toMatch(/^\/course\/[a-z0-9-]+$/);
        }
      }
    }
  });

  test('should display course information correctly from API', async ({ page }) => {
    // Wait for courses to load
    await page.waitForSelector('[data-testid="course-grid"]', { timeout: 10000 });

    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();

    if (courseCount > 0) {
      const firstCourseCard = courseCards.first();

      // Verify course information is displayed
      await expect(firstCourseCard.locator('[data-testid="course-title"]')).toBeVisible();
      await expect(firstCourseCard.locator('[data-testid="course-description"]')).toBeVisible();

      // Check for difficulty badge
      const difficultyBadge = firstCourseCard.locator('[data-testid="difficulty-badge"]');
      if (await difficultyBadge.isVisible()) {
        const difficultyText = await difficultyBadge.textContent();
        expect(['Beginner', 'Intermediate', 'Advanced', 'Expert']).toContain(difficultyText);
      }
    }
  });

  test('should handle 404 for invalid course slug', async ({ page }) => {
    // Navigate to a non-existent course slug
    await page.goto('/course/non-existent-course-slug-12345');

    // Should show 404 or redirect to courses page
    await page.waitForLoadState('networkidle');

    // Check if we're on a 404 page or courses page
    const currentUrl = page.url();
    const isOnCoursesPage = currentUrl.includes('/courses');
    const isOn404Page = currentUrl.includes('/404') || page.locator('text=404').isVisible() || page.locator('text=Not Found').isVisible();

    expect(isOnCoursesPage || isOn404Page).toBeTruthy();
  });

  test('should maintain consistent course data between catalog and detail', async ({ page }) => {
    // Wait for courses to load
    await page.waitForSelector('[data-testid="course-grid"]', { timeout: 10000 });

    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();

    if (courseCount > 0) {
      const firstCourseCard = courseCards.first();

      // Get course information from catalog
      const catalogTitle = await firstCourseCard.locator('[data-testid="course-title"]').textContent();
      const catalogDescription = await firstCourseCard.locator('[data-testid="course-description"]').textContent();

      // Navigate to course detail
      await firstCourseCard.click();
      await page.waitForLoadState('networkidle');

      // Verify same information is displayed on detail page
      if (catalogTitle) {
        await expect(page.locator('h1')).toContainText(catalogTitle);
      }

      if (catalogDescription) {
        await expect(page.locator('[data-testid="course-detail-description"]')).toContainText(catalogDescription);
      }
    }
  });

  test('should show proper loading states', async ({ page }) => {
    // Navigate to courses page
    await page.goto('/courses');

    // Should show loading state initially
    const loadingIndicator = page.locator('[data-testid="loading"]');

    // Loading might be very fast, so we don't require it to be visible
    // but if it is visible, it should have proper content
    if (await loadingIndicator.isVisible({ timeout: 1000 })) {
      await expect(loadingIndicator).toContainText('Loading');
    }

    // Eventually courses should load
    await page.waitForSelector('[data-testid="course-grid"]', { timeout: 10000 });

    // Loading should be gone
    await expect(loadingIndicator).not.toBeVisible();
  });

  test('should handle empty course catalog gracefully', async ({ page }) => {
    // Mock an empty response (if possible with request interception)
    await page.route('**/api/program/published', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/courses');
    await page.waitForLoadState('networkidle');

    // Should show empty state
    const emptyState = page.locator('[data-testid="empty-state"]');
    await expect(emptyState).toBeVisible();
    await expect(emptyState).toContainText('No courses found');
  });
});
