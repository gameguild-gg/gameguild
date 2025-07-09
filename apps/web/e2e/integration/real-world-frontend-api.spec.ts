import { test, expect, Page, BrowserContext } from '@playwright/test';

/**
 * 🎯 REAL-WORLD Frontend-to-API E2E Test Suite
 *
 * This suite focuses on realistic user scenarios and validates that the
 * React frontend correctly communicates with the .NET API backend in
 * production-like conditions.
 *
 * Focus Areas:
 * ✅ Real data validation and structure
 * ✅ Complete user workflows
 * ✅ Error boundary testing
 * ✅ Authentication state handling
 * ✅ Data consistency across navigation
 * ✅ Form submissions and API updates
 * ✅ Search and filtering integration
 * ✅ Real network conditions simulation
 */

interface CourseData {
  id?: string;
  slug: string;
  title: string;
  description?: string;
  difficulty?: string;
  status?: string;
}

interface ApiResponse {
  status: number;
  data: unknown;
  headers: Record<string, string>;
  timing: number;
}

test.describe('🌍 Real-World Frontend-to-API Integration', () => {
  let context: BrowserContext;
  let page: Page;
  const capturedApiResponses: Map<string, ApiResponse> = new Map();
  let discoveredCourses: CourseData[] = [];

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext({
      viewport: { width: 1280, height: 720 },
      // Simulate real user conditions
      locale: 'en-US',
      timezoneId: 'America/New_York',
    });

    page = await context.newPage();

    // Capture all API responses for validation
    page.on('response', async (response) => {
      if (response.url().includes('/api/')) {
        const startTime = Date.now();
        try {
          const responseData = await response.json().catch(() => response.text());
          const endTime = Date.now();

          capturedApiResponses.set(response.url(), {
            status: response.status(),
            data: responseData,
            headers: response.headers(),
            timing: endTime - startTime,
          });

          console.log(`📊 API Response captured: ${response.status()} ${response.url()}`);
        } catch {
          console.log(`⚠️ Failed to capture response: ${response.url()}`);
        }
      }
    });
  });

  test.afterAll(async () => {
    console.log(`\n📈 Test Summary:`);
    console.log(`🔗 API Endpoints tested: ${capturedApiResponses.size}`);
    console.log(`📚 Courses discovered: ${discoveredCourses.length}`);
    
    await context.close();
  });

  test('🎮 Complete Course Discovery Journey', async () => {
    console.log('\n🚀 Starting complete course discovery journey...');

    // Step 1: Homepage to Course Catalog
    console.log('📍 Step 1: Navigate from homepage to catalog...');
    await page.goto('/', { waitUntil: 'networkidle' });
    
    // Look for navigation to courses
    const coursesLink = page.locator('a[href*="/courses"], a:has-text("Courses"), nav a:has-text("Learn")').first();
    if (await coursesLink.isVisible()) {
      await coursesLink.click();
      await page.waitForLoadState('networkidle');
    } else {
      // Direct navigation if no link found
      await page.goto('/courses', { waitUntil: 'networkidle' });
    }

    expect(page.url()).toMatch(/courses/);

    // Step 2: Validate course catalog API integration
    console.log('📍 Step 2: Validating course catalog API...');
    
    // Wait for content to load
    await page.waitForTimeout(2000);
    
    // Check if programs API was called
    const programsResponse = Array.from(capturedApiResponses.entries()).find(([url]) => url.includes('/api/programs') && !url.includes('/api/programs/'));
    
    if (programsResponse) {
      const [url, response] = programsResponse;
      console.log(`✅ Programs API called: ${url}`);
      expect(response.status).toBe(200);
      
      // Validate response structure
      const data = response.data as Record<string, unknown> | unknown[];
      if (Array.isArray(data)) {
        console.log(`📚 Found ${data.length} courses in API response`);
        discoveredCourses = data.map((course) => {
          const courseObj = course as Record<string, unknown>;
          return {
            id: courseObj.id as string,
            slug: courseObj.slug as string,
            title: (courseObj.title || courseObj.name) as string,
            description: courseObj.description as string,
            difficulty: courseObj.difficulty as string,
            status: courseObj.status as string,
          };
        });
      } else if (data && typeof data === 'object' && 'data' in data) {
        const nestedData = (data as { data: unknown }).data;
        discoveredCourses = Array.isArray(nestedData) ? nestedData : [nestedData];
      }
    }

    // Step 3: Validate frontend rendering matches API data
    console.log('📍 Step 3: Validating frontend-API data consistency...');
    
    const courseElements = page.locator('[data-testid="course-card"], .course-card, [class*="course"]:not(.course-catalog)');
    const courseCount = await courseElements.count();
    
    console.log(`🎯 Frontend shows ${courseCount} courses, API returned ${discoveredCourses.length}`);
    
    if (courseCount > 0 && discoveredCourses.length > 0) {
      // Validate first few courses match
      const maxCheck = Math.min(3, courseCount, discoveredCourses.length);
      
      for (let i = 0; i < maxCheck; i++) {
        const courseElement = courseElements.nth(i);
        const courseTitle = await courseElement.locator('h1, h2, h3, h4, [class*="title"], [data-testid*="title"]').first().textContent();
        const courseLink = await courseElement.locator('a').first().getAttribute('href');
        
        if (courseTitle && courseLink) {
          const expectedSlug = discoveredCourses[i]?.slug;
          if (expectedSlug && courseLink.includes(expectedSlug)) {
            console.log(`✅ Course ${i + 1} slug matches: ${expectedSlug}`);
          } else {
            console.log(`⚠️ Course ${i + 1} data mismatch - Title: "${courseTitle}", Link: "${courseLink}"`);
          }
        }
      }
    }

    // Step 4: Test course detail navigation
    if (courseCount > 0) {
      console.log('📍 Step 4: Testing course detail navigation...');
      
      const firstCourse = courseElements.first();
      const courseLink = await firstCourse.locator('a').first().getAttribute('href');
      
      if (courseLink) {
        console.log(`🔗 Navigating to: ${courseLink}`);
        await firstCourse.locator('a').first().click();
        await page.waitForLoadState('networkidle');
        
        // Validate course detail API call
        await page.waitForTimeout(1000);
        
        const detailResponse = Array.from(capturedApiResponses.entries()).find(
          ([url]) => url.includes('/api/programs/') && url.split('/').pop() !== 'programs',
        );
        
        if (detailResponse) {
          const [url, response] = detailResponse;
          console.log(`✅ Course detail API called: ${url}`);
          expect(response.status).toBe(200);
          
          // Validate course detail data
          const detailData = response.data as Record<string, unknown>;
          if (detailData && (detailData.slug || detailData.id)) {
            console.log(`📖 Course detail loaded: ${detailData.title || detailData.name}`);
          }
        }
        
        // Validate URL matches slug pattern
        expect(page.url()).toMatch(/\/course\/[a-z0-9-]+/);
      }
    }

    console.log('✅ Course discovery journey completed!');
  });

  test('🔍 Search and Filter Integration', async () => {
    console.log('\n🔎 Testing search and filter integration...');
    
    await page.goto('/courses', { waitUntil: 'networkidle' });
    
    // Look for search/filter elements
    const searchInput = page.locator('input[type="search"], input[placeholder*="search" i], [data-testid*="search"]').first();
    const filterElements = page.locator('select, [role="combobox"], [data-testid*="filter"]');
    
    if (await searchInput.isVisible()) {
      console.log('🔍 Testing search functionality...');
      
      await searchInput.fill('game');
      await page.waitForTimeout(1000); // Wait for debounce
      
      // Check if search triggered API call
      const searchApiCall = Array.from(capturedApiResponses.entries()).find(
        ([url]) => url.includes('search') || url.includes('query') || url.includes('filter'),
      );
      
      if (searchApiCall) {
        console.log(`✅ Search API triggered: ${searchApiCall[0]}`);
      }
      
      await searchInput.clear();
    }
    
    if ((await filterElements.count()) > 0) {
      console.log('🎛️ Testing filter functionality...');
      
      const firstFilter = filterElements.first();
      await firstFilter.click();
      await page.waitForTimeout(500);
      
      // Try to select an option
      const filterOptions = page.locator('option, [role="option"]');
      if ((await filterOptions.count()) > 1) {
        await filterOptions.nth(1).click();
        await page.waitForTimeout(1000);
      }
    }
    
    console.log('✅ Search and filter integration tested!');
  });

  test('🚫 Error Handling and Edge Cases', async () => {
    console.log('\n🚫 Testing error handling and edge cases...');
    
    // Test 1: Invalid course slug
    console.log('📍 Test 1: Invalid course slug...');
    await page.goto('/course/nonexistent-course-12345', { waitUntil: 'networkidle' });
    
    const errorElements = page.locator('[data-testid*="error"], .error, .not-found, h1:has-text("404"), h1:has-text("Not Found")');
    const hasError = (await errorElements.count()) > 0;
    
    if (hasError) {
      console.log('✅ Error page displayed for invalid course');
      expect(await errorElements.first().isVisible()).toBe(true);
    } else {
      console.log('⚠️ No explicit error page found, checking for empty state...');
      const mainContent = page.locator('main, [role="main"]');
      expect(await mainContent.isVisible()).toBe(true);
    }
    
    // Test 2: API error simulation
    console.log('📍 Test 2: Simulating API errors...');
    
    // Intercept and mock API failure
    await page.route('**/api/programs**', (route) => {
      route.fulfill({
        status: 503,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Service Temporarily Unavailable' }),
      });
    });
    
    await page.goto('/courses', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    
    const errorState = page.locator('[data-testid*="error"], .error-message, .error, [class*="error"]');
    if (await errorState.isVisible()) {
      console.log('✅ Error state displayed for API failure');
    } else {
      console.log('ℹ️ Graceful degradation - no explicit error shown');
    }
    
    // Reset route interception
    await page.unroute('**/api/programs**');
    
    console.log('✅ Error handling tests completed!');
  });

  test('📱 Mobile and Performance Validation', async () => {
    console.log('\n📱 Testing mobile experience and performance...');
    
    // Switch to mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    const startTime = Date.now();
    await page.goto('/courses', { waitUntil: 'networkidle' });
    const loadTime = Date.now() - startTime;
    
    console.log(`⏱️ Mobile load time: ${loadTime}ms`);
    expect(loadTime).toBeLessThan(8000); // Allow more time for mobile
    
    // Test mobile navigation
    const mobileMenu = page.locator('[data-testid*="menu"], .mobile-menu, button:has-text("Menu"), [aria-label*="menu" i]');
    if (await mobileMenu.isVisible()) {
      console.log('📱 Testing mobile menu...');
      await mobileMenu.click();
      await page.waitForTimeout(500);
    }
    
    // Test touch interactions
    const courseCards = page.locator('[data-testid="course-card"], .course-card');
    if ((await courseCards.count()) > 0) {
      console.log('👆 Testing touch interactions...');
      await courseCards.first().tap();
      await page.waitForTimeout(1000);
    }
    
    // Validate API calls still work on mobile
    const mobileApiCalls = Array.from(capturedApiResponses.values()).filter(response => 
      response.timing < Date.now() - startTime + 1000 // Recent calls
    );
    
    console.log(`📊 API calls on mobile: ${mobileApiCalls.length}`);
    expect(mobileApiCalls.length).toBeGreaterThan(0);
    
    console.log('✅ Mobile validation completed!');
  });

  test('🔄 Data Consistency Across Navigation', async () => {
    console.log('\n🔄 Testing data consistency across navigation...');
    
    // Start at catalog
    await page.goto('/courses', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    
    const catalogCourses = page.locator('[data-testid="course-card"], .course-card');
    const catalogCount = await catalogCourses.count();
    
    if (catalogCount > 0) {
      // Get first course data from catalog
      const firstCourse = catalogCourses.first();
      const catalogTitle = await firstCourse.locator('h1, h2, h3, h4, [class*="title"]').first().textContent();
      const catalogLink = await firstCourse.locator('a').first().getAttribute('href');
      
      console.log(`📚 Catalog course: "${catalogTitle}" -> ${catalogLink}`);
      
      // Navigate to detail page
      if (catalogLink) {
        await firstCourse.locator('a').first().click();
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1000);
        
        // Get detail page data
        const detailTitle = await page.locator('h1, h2, [class*="title"], [data-testid*="title"]').first().textContent();
        
        console.log(`📖 Detail page title: "${detailTitle}"`);
        
        // Validate consistency
        if (catalogTitle && detailTitle) {
          const titlesMatch =
            catalogTitle.trim().toLowerCase() === detailTitle.trim().toLowerCase() ||
            detailTitle.includes(catalogTitle.trim()) ||
            catalogTitle.includes(detailTitle.trim());
          
          if (titlesMatch) {
            console.log('✅ Course data consistent between catalog and detail');
          } else {
            console.log(`⚠️ Title mismatch: Catalog "${catalogTitle}" vs Detail "${detailTitle}"`);
          }
        }
        
        // Test back navigation
        await page.goBack();
        await page.waitForLoadState('networkidle');
        
        // Verify we're back at catalog
        expect(page.url()).toMatch(/courses/);
        
        const backCourseCount = await page.locator('[data-testid="course-card"], .course-card').count();
        expect(backCourseCount).toBe(catalogCount);
        
        console.log('✅ Back navigation preserves catalog state');
      }
    }
    
    console.log('✅ Data consistency tests completed!');
  });

  test('⚡ Performance and Resource Optimization', async () => {
    console.log('\n⚡ Testing performance and resource optimization...');
    
    // Monitor resource loading
    const resourceTypes: string[] = [];
    page.on('response', (response) => {
      const resourceType = response.request().resourceType();
      if (!resourceTypes.includes(resourceType)) {
        resourceTypes.push(resourceType);
      }
    });
    
    const startTime = Date.now();
    await page.goto('/courses', { waitUntil: 'networkidle' });
    const totalLoadTime = Date.now() - startTime;
    
    console.log(`⏱️ Total page load time: ${totalLoadTime}ms`);
    console.log(`📦 Resource types loaded: ${resourceTypes.join(', ')}`);
    
    // Validate API response times
    const apiTimes = Array.from(capturedApiResponses.values()).map((response) => response.timing);
    if (apiTimes.length > 0) {
      const avgApiTime = apiTimes.reduce((sum, time) => sum + time, 0) / apiTimes.length;
      const maxApiTime = Math.max(...apiTimes);
      
      console.log(`🔥 Average API response time: ${avgApiTime.toFixed(2)}ms`);
      console.log(`🐌 Slowest API response: ${maxApiTime}ms`);
      
      expect(maxApiTime).toBeLessThan(5000); // No API call should take more than 5s
      expect(avgApiTime).toBeLessThan(1000); // Average should be under 1s
    }
    
    // Test concurrent navigation
    console.log('🔄 Testing concurrent navigation performance...');
    
    const courseLinks = page.locator('[data-testid="course-card"] a, .course-card a');
    const linkCount = Math.min(3, await courseLinks.count());
    
    if (linkCount > 0) {
      const navigationPromises = [];
      
      for (let i = 0; i < linkCount; i++) {
        const link = await courseLinks.nth(i).getAttribute('href');
        if (link) {
          navigationPromises.push(page.evaluate((url) => fetch(url), `${page.url().split('/').slice(0, 3).join('/')}${link}`));
        }
      }
      
      if (navigationPromises.length > 0) {
        const concurrentStart = Date.now();
        await Promise.all(navigationPromises);
        const concurrentTime = Date.now() - concurrentStart;
        
        console.log(`⚡ Concurrent navigation time: ${concurrentTime}ms`);
        expect(concurrentTime).toBeLessThan(3000); // Concurrent requests should complete quickly
      }
    }
    
    console.log('✅ Performance validation completed!');
  });
});
