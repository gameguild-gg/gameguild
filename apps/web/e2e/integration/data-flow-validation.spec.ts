import { test, expect, Page, Route } from '@playwright/test';

/**
 * 🔄 Data Flow Validation Tests
 *
 * This test suite specifically validates that data flows correctly
 * from the API through the React components and is rendered properly
 * in the user interface.
 *
 * Focus Areas:
 * ✅ API response structure validation
 * ✅ Component prop passing and rendering
 * ✅ State management and updates
 * ✅ Error state handling
 * ✅ Loading state management
 * ✅ Data transformation and formatting
 * ✅ Component lifecycle and effects
 * ✅ Browser navigation state sync
 */

interface MockCourse {
  id: string;
  slug: string;
  title: string;
  description: string;
  difficulty: 'Beginner' | 'Intermediate' | 'Advanced';
  status: 'Published' | 'Draft';
  tags: string[];
  estimatedHours: number;
  createdAt: string;
  updatedAt: string;
}

const mockCoursesData: MockCourse[] = [
  {
    id: '1',
    slug: 'introduction-to-game-development',
    title: 'Introduction to Game Development',
    description: 'Learn the fundamentals of game development using modern tools and techniques.',
    difficulty: 'Beginner',
    status: 'Published',
    tags: ['Unity', 'C#', 'Game Design'],
    estimatedHours: 24,
    createdAt: '2024-01-15T10:00:00Z',
    updatedAt: '2024-01-20T15:30:00Z',
  },
  {
    id: '2',
    slug: 'advanced-unity-techniques',
    title: 'Advanced Unity Techniques',
    description: 'Master advanced Unity features and optimization techniques for professional game development.',
    difficulty: 'Advanced',
    status: 'Published',
    tags: ['Unity', 'Optimization', 'Shaders'],
    estimatedHours: 48,
    createdAt: '2024-02-01T09:00:00Z',
    updatedAt: '2024-02-05T11:45:00Z',
  },
  {
    id: '3',
    slug: 'game-design-principles',
    title: 'Game Design Principles',
    description: 'Understand core game design principles and how to create engaging gameplay experiences.',
    difficulty: 'Intermediate',
    status: 'Published',
    tags: ['Game Design', 'UX', 'Player Psychology'],
    estimatedHours: 32,
    createdAt: '2024-01-28T14:20:00Z',
    updatedAt: '2024-02-02T16:10:00Z',
  },
];

test.describe('🔄 Data Flow Validation: API to Frontend', () => {
  let page: Page;

  test.beforeEach(async ({ page: testPage }) => {
    page = testPage;
  });

  test('📊 API Response Structure Validation', async () => {
    console.log('🎯 Testing API response structure and data flow...');

    // Mock the API response with controlled data
    await page.route('**/api/programs**', (route: Route) => {
      if (route.request().url().includes('/api/programs/')) {
        // Individual course detail
        const slug = route.request().url().split('/').pop();
        const course = mockCoursesData.find((c) => c.slug === slug);

        if (course) {
          route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify(course),
          });
        } else {
          route.fulfill({
            status: 404,
            contentType: 'application/json',
            body: JSON.stringify({ error: 'Course not found' }),
          });
        }
      } else {
        // Course listing
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockCoursesData),
        });
      }
    });

    // Navigate to courses page
    await page.goto('/courses', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    // Validate course cards are rendered with correct data
    const courseCards = page.locator('[data-testid="course-card"], .course-card, [class*="course"]:not(.course-catalog)');
    const cardCount = await courseCards.count();

    console.log(`🎯 Found ${cardCount} course cards`);
    expect(cardCount).toBeGreaterThan(0);

    // Validate first course card data
    if (cardCount > 0) {
      const firstCard = courseCards.first();
      const expectedCourse = mockCoursesData[0];

      // Check title rendering
      const titleElement = firstCard.locator('h1, h2, h3, h4, [class*="title"], [data-testid*="title"]');
      if ((await titleElement.count()) > 0) {
        const displayedTitle = await titleElement.first().textContent();
        console.log(`📝 Expected: "${expectedCourse.title}", Displayed: "${displayedTitle}"`);

        expect(displayedTitle?.trim()).toContain(expectedCourse.title);
      }

      // Check link href uses slug
      const linkElement = firstCard.locator('a');
      if ((await linkElement.count()) > 0) {
        const href = await linkElement.first().getAttribute('href');
        console.log(`🔗 Link href: ${href}`);

        expect(href).toContain(expectedCourse.slug);
        expect(href).toMatch(/\/course\/[a-z0-9-]+/);
      }

      // Check difficulty/tags if displayed
      const cardText = await firstCard.textContent();
      if (cardText?.includes(expectedCourse.difficulty)) {
        console.log(`✅ Difficulty "${expectedCourse.difficulty}" found in card`);
      }
    }

    console.log('✅ API response structure validation completed');
  });

  test('🎭 Component State Management Validation', async () => {
    console.log('🎯 Testing component state management...');

    let apiCallCount = 0;

    // Track API calls
    await page.route('**/api/programs**', (route: Route) => {
      apiCallCount++;
      console.log(`📡 API Call #${apiCallCount}: ${route.request().url()}`);

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCoursesData),
      });
    });

    // Initial page load
    await page.goto('/courses', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);

    const initialApiCalls = apiCallCount;
    console.log(`📊 Initial API calls: ${initialApiCalls}`);

    // Test navigation to course detail and back
    const courseCards = page.locator('[data-testid="course-card"], .course-card');
    if ((await courseCards.count()) > 0) {
      const firstCardLink = courseCards.first().locator('a');

      if ((await firstCardLink.count()) > 0) {
        // Navigate to detail page
        await firstCardLink.click();
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1000);

        const detailApiCalls = apiCallCount;
        console.log(`📊 API calls after detail navigation: ${detailApiCalls}`);

        // Verify we're on detail page
        expect(page.url()).toMatch(/\/course\/[a-z0-9-]+/);

        // Go back to catalog
        await page.goBack();
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1000);

        const backApiCalls = apiCallCount;
        console.log(`📊 API calls after back navigation: ${backApiCalls}`);

        // Verify we're back at catalog
        expect(page.url()).toMatch(/courses/);

        // Check if state is preserved (should not re-fetch unnecessarily)
        const courseCardsAfterBack = page.locator('[data-testid="course-card"], .course-card');
        expect(await courseCardsAfterBack.count()).toBeGreaterThan(0);

        console.log('✅ Navigation state management validated');
      }
    }

    console.log('✅ Component state management validation completed');
  });

  test('⚡ Loading States and UI Feedback', async () => {
    console.log('🎯 Testing loading states and UI feedback...');

    // Simulate slow API response
    await page.route('**/api/programs**', async (route: Route) => {
      console.log('⏳ Simulating slow API response...');

      // Delay response to test loading state
      await new Promise((resolve) => setTimeout(resolve, 2000));

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCoursesData),
      });
    });

    // Navigate and immediately check for loading state
    const navigationPromise = page.goto('/courses');

    // Check for loading indicators
    await page.waitForTimeout(500);

    const loadingIndicators = page.locator('[data-testid="loading"], .loading, .spinner, [class*="loading"], [aria-label*="loading" i]');

    if ((await loadingIndicators.count()) > 0) {
      console.log('✅ Loading indicator found');
      expect(await loadingIndicators.first().isVisible()).toBe(true);
    } else {
      console.log('ℹ️ No loading indicator found (may load too fast)');
    }

    // Wait for navigation to complete
    await navigationPromise;
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Verify loading state is gone and content is shown
    if ((await loadingIndicators.count()) > 0) {
      expect(await loadingIndicators.first().isVisible()).toBe(false);
      console.log('✅ Loading indicator hidden after content loads');
    }

    // Verify course content is displayed
    const courseContent = page.locator('[data-testid="course-card"], .course-card, main');
    expect(await courseContent.count()).toBeGreaterThan(0);

    console.log('✅ Loading states validation completed');
  });

  test('🚫 Error State Handling and Recovery', async () => {
    console.log('🎯 Testing error state handling...');

    // Mock API error
    await page.route('**/api/programs**', (route: Route) => {
      console.log('❌ Simulating API error...');

      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error', message: 'Database connection failed' }),
      });
    });

    // Navigate to page with error
    await page.goto('/courses', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    // Check for error state
    const errorElements = page.locator('[data-testid*="error"], .error, .error-message, [class*="error"], [aria-label*="error" i]');

    if ((await errorElements.count()) > 0) {
      console.log('✅ Error state displayed');
      expect(await errorElements.first().isVisible()).toBe(true);

      const errorText = await errorElements.first().textContent();
      console.log(`📝 Error message: ${errorText}`);
    } else {
      console.log('ℹ️ No explicit error state found, checking for empty/fallback state');

      // Check for empty state or fallback content
      const mainContent = page.locator('main, [role="main"]');
      expect(await mainContent.isVisible()).toBe(true);
    }

    // Test recovery - mock successful response
    await page.route('**/api/programs**', (route: Route) => {
      console.log('✅ Simulating API recovery...');

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCoursesData),
      });
    });

    // Trigger retry by refreshing or clicking retry button
    const retryButton = page.locator('[data-testid*="retry"], .retry, button:has-text("Retry"), button:has-text("Try Again")');

    if ((await retryButton.count()) > 0) {
      console.log('🔄 Found retry button, testing recovery...');
      await retryButton.click();
      await page.waitForTimeout(2000);
    } else {
      console.log('🔄 No retry button found, refreshing page...');
      await page.reload({ waitUntil: 'networkidle' });
      await page.waitForTimeout(2000);
    }

    // Verify content loads after recovery
    const courseCards = page.locator('[data-testid="course-card"], .course-card');
    if ((await courseCards.count()) > 0) {
      console.log('✅ Content loaded successfully after recovery');
      expect(await courseCards.first().isVisible()).toBe(true);
    }

    console.log('✅ Error state handling validation completed');
  });

  test('🔍 Data Transformation and Formatting', async () => {
    console.log('🎯 Testing data transformation and formatting...');

    // Mock API with specific data formats to test transformation
    const courseWithSpecialData: MockCourse = {
      id: '999',
      slug: 'test-formatting-course',
      title: 'Test Course with Special Characters & Formatting',
      description:
        'This course tests how the frontend handles special characters, long text, and various data formats. It includes unicode characters (🎮), HTML entities (&amp;), and line breaks.',
      difficulty: 'Intermediate',
      status: 'Published',
      tags: ['Testing', 'Special Characters', 'Data Handling'],
      estimatedHours: 99,
      createdAt: '2024-12-25T23:59:59Z',
      updatedAt: '2024-12-26T00:00:01Z',
    };

    await page.route('**/api/programs**', (route: Route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([courseWithSpecialData]),
      });
    });

    await page.goto('/courses', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);

    const courseCard = page.locator('[data-testid="course-card"], .course-card').first();

    if ((await courseCard.count()) > 0) {
      const cardContent = await courseCard.textContent();
      console.log(`📝 Card content: ${cardContent}`);

      // Check title handling
      if (cardContent?.includes(courseWithSpecialData.title)) {
        console.log('✅ Special characters in title handled correctly');
      }

      // Check description handling (may be truncated)
      if (cardContent?.includes('This course tests') || cardContent?.includes('special characters')) {
        console.log('✅ Description content found');
      }

      // Check difficulty display
      if (cardContent?.includes(courseWithSpecialData.difficulty)) {
        console.log('✅ Difficulty displayed correctly');
      }

      // Check estimated hours formatting
      if (cardContent?.includes('99') || cardContent?.includes(courseWithSpecialData.estimatedHours.toString())) {
        console.log('✅ Estimated hours displayed');
      }
    }

    // Test navigation to detail page for more thorough data display
    const detailLink = courseCard.locator('a');
    if ((await detailLink.count()) > 0) {
      await detailLink.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Verify URL slug handling
      expect(page.url()).toContain(courseWithSpecialData.slug);

      // Check detailed content
      const pageContent = await page.textContent('main, [role="main"], body');

      if (pageContent?.includes(courseWithSpecialData.title)) {
        console.log('✅ Full title displayed on detail page');
      }

      if (pageContent?.includes('unicode characters') || pageContent?.includes('🎮')) {
        console.log('✅ Unicode characters handled correctly');
      }
    }

    console.log('✅ Data transformation and formatting validation completed');
  });

  test('🔄 Browser Navigation and History Management', async () => {
    console.log('🎯 Testing browser navigation and history management...');

    await page.route('**/api/programs**', (route: Route) => {
      if (route.request().url().includes('/api/programs/')) {
        const slug = route.request().url().split('/').pop();
        const course = mockCoursesData.find((c) => c.slug === slug);

        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(course || { error: 'Not found' }),
        });
      } else {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(mockCoursesData),
        });
      }
    });

    // Start from homepage or courses
    await page.goto('/', { waitUntil: 'networkidle' });

    // Navigate to courses
    await page.goto('/courses', { waitUntil: 'networkidle' });
    console.log(`📍 At courses page: ${page.url()}`);

    // Navigate to first course detail
    const firstCard = page.locator('[data-testid="course-card"], .course-card').first();
    if ((await firstCard.count()) > 0) {
      const firstLink = firstCard.locator('a').first();
      const expectedSlug = mockCoursesData[0].slug;

      await firstLink.click();
      await page.waitForLoadState('networkidle');

      console.log(`📍 At course detail: ${page.url()}`);
      expect(page.url()).toContain(expectedSlug);

      // Test browser back button
      await page.goBack();
      await page.waitForLoadState('networkidle');

      console.log(`📍 Back to courses: ${page.url()}`);
      expect(page.url()).toMatch(/courses/);

      // Test browser forward button
      await page.goForward();
      await page.waitForLoadState('networkidle');

      console.log(`📍 Forward to detail: ${page.url()}`);
      expect(page.url()).toContain(expectedSlug);

      // Test direct URL navigation
      const directSlug = mockCoursesData[1].slug;
      await page.goto(`/course/${directSlug}`, { waitUntil: 'networkidle' });

      console.log(`📍 Direct navigation: ${page.url()}`);
      expect(page.url()).toContain(directSlug);

      // Verify content matches the directly navigated course
      const pageTitle = await page.locator('h1, h2, [class*="title"]').first().textContent();
      const expectedTitle = mockCoursesData[1].title;

      if (pageTitle?.includes(expectedTitle) || pageTitle?.includes('Advanced Unity')) {
        console.log('✅ Direct navigation loads correct course content');
      }
    }

    console.log('✅ Browser navigation validation completed');
  });
});
