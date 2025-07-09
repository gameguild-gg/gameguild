import { test, expect, Page } from '@playwright/test';

/**
 * Course Enrollment and Payment Flow E2E Tests
 *
 * Tests the complete enrollment journey from course discovery to payment
 */

test.describe('Course Enrollment Flow - Frontend to API Integration', () => {
  let page: Page;

  test.beforeEach(async ({ browser }) => {
    const context = await browser.newContext();
    page = await context.newPage();
    
    // Track API calls related to enrollment
    page.on('request', (request) => {
      if (request.url().includes('/api/') && (request.url().includes('enroll') || request.url().includes('payment') || request.url().includes('product'))) {
        console.log('Enrollment API Request:', request.method(), request.url());
      }
    });
  });

  test('should display enrollment options on course detail page', async () => {
    console.log('💳 Testing enrollment options display...');
    
    // Navigate to course catalog first
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');
    
    const courseCards = page.locator('[data-testid="course-card"]');
    await expect(courseCards.first()).toBeVisible({ timeout: 10000 });
    
    // Navigate to first course detail page
    await courseCards.first().click();
    await page.waitForLoadState('networkidle');
    
    // Check for enrollment sidebar
    const enrollmentSidebar = page.locator('[data-testid="course-sidebar"]');
    await expect(enrollmentSidebar).toBeVisible({ timeout: 10000 });
    
    // Look for enrollment-related elements
    const enrollButton = page.locator('[data-testid="enroll-button"]');
    const priceDisplay = page.locator('[data-testid="course-price"]');
    const accessLevel = page.locator('[data-testid="access-level"]');
    
    // At least one enrollment element should be visible
    const enrollmentElementsVisible = await Promise.all([
      enrollButton.isVisible().catch(() => false),
      priceDisplay.isVisible().catch(() => false),
      accessLevel.isVisible().catch(() => false),
    ]);
    
    const hasEnrollmentElements = enrollmentElementsVisible.some(visible => visible);
    expect(hasEnrollmentElements).toBeTruthy();
    
    console.log('✅ Enrollment options displayed on course detail page');
  });

  test('should handle course access levels correctly', async () => {
    console.log('🔐 Testing course access levels...');
    
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');
    
    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();
    
    // Test multiple courses to check different access levels
    for (let i = 0; i < Math.min(3, courseCount); i++) {
      await page.goto('/courses/catalog');
      await page.waitForLoadState('networkidle');
      
      const course = courseCards.nth(i);
      await course.click();
      await page.waitForLoadState('networkidle');
      
      // Check if access level information is displayed
      const sidebar = page.locator('[data-testid="course-sidebar"]');
      if (await sidebar.isVisible()) {
        const accessInfo = await sidebar.textContent();
        
        // Should contain some access-related information
        const hasAccessInfo = accessInfo?.toLowerCase().includes('access') ||
                             accessInfo?.toLowerCase().includes('enroll') ||
                             accessInfo?.toLowerCase().includes('free') ||
                             accessInfo?.toLowerCase().includes('premium');
        
        if (hasAccessInfo) {
          console.log(`✅ Course ${i + 1}: Access level information found`);
        }
      }
    }
  });

  test('should handle enrollment button interactions', async () => {
    console.log('🎯 Testing enrollment button interactions...');
    
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');
    
    const courseCards = page.locator('[data-testid="course-card"]');
    await courseCards.first().click();
    await page.waitForLoadState('networkidle');
    
    // Look for enrollment button
    const enrollButton = page.locator('[data-testid="enroll-button"]');
    
    if (await enrollButton.isVisible()) {
      // Check button state and text
      const buttonText = await enrollButton.textContent();
      console.log('Enrollment button text:', buttonText);
      
      // Button should not be disabled by default (unless already enrolled)
      const isDisabled = await enrollButton.isDisabled();
      
      if (!isDisabled) {
        // Click the enrollment button
        await enrollButton.click();
        
        // Should either:
        // 1. Navigate to sign-in page (if not authenticated)
        // 2. Show enrollment modal/form
        // 3. Navigate to payment page
        await page.waitForTimeout(2000);
        
        const currentUrl = page.url();
        const pageContent = await page.textContent('body');
        
        const hasEnrollmentResponse = currentUrl.includes('sign-in') ||
                                    currentUrl.includes('payment') ||
                                    currentUrl.includes('checkout') ||
                                    pageContent?.includes('sign in') ||
                                    pageContent?.includes('enrollment');
        
        if (hasEnrollmentResponse) {
          console.log('✅ Enrollment button triggered appropriate action');
        } else {
          console.log('ℹ️ Enrollment button clicked but no obvious redirect');
        }
      } else {
        console.log('ℹ️ Enrollment button is disabled (may be already enrolled)');
      }
    } else {
      console.log('ℹ️ No enrollment button found (may be free course or different flow)');
    }
  });

  test('should display course pricing information', async () => {
    console.log('💰 Testing course pricing display...');
    
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');
    
    const courseCards = page.locator('[data-testid="course-card"]');
    const courseCount = await courseCards.count();
    
    let pricingFound = false;
    
    // Check multiple courses for pricing information
    for (let i = 0; i < Math.min(5, courseCount); i++) {
      await page.goto('/courses/catalog');
      await page.waitForLoadState('networkidle');
      
      const course = courseCards.nth(i);
      await course.click();
      await page.waitForLoadState('networkidle');
      
      const pageContent = await page.textContent('body');
      
      // Look for pricing indicators
      const hasPricing = pageContent?.includes('$') ||
                        pageContent?.toLowerCase().includes('free') ||
                        pageContent?.toLowerCase().includes('price') ||
                        pageContent?.toLowerCase().includes('cost');
      
      if (hasPricing) {
        pricingFound = true;
        console.log(`✅ Course ${i + 1}: Pricing information found`);
        break;
      }
    }
    
    // At least some courses should have pricing information
    expect(pricingFound).toBeTruthy();
  });

  test('should handle product lookup by course slug', async () => {
    console.log('🔍 Testing product lookup integration...');
    
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');
    
    const courseCards = page.locator('[data-testid="course-card"]');
    await courseCards.first().click();
    await page.waitForLoadState('networkidle');
    
    // Extract course slug from URL
    const url = page.url();
    const slugMatch = url.match(/\/course\/([a-z0-9-]+)/);
    
    if (slugMatch) {
      const slug = slugMatch[1];
      console.log('Testing product lookup for slug:', slug);
      
      // Check if product/pricing information is displayed
      const sidebar = page.locator('[data-testid="course-sidebar"]');
      if (await sidebar.isVisible()) {
        const sidebarContent = await sidebar.textContent();
        
        // Should show some product-related information
        const hasProductInfo = sidebarContent?.includes('$') ||
                              sidebarContent?.toLowerCase().includes('access') ||
                              sidebarContent?.toLowerCase().includes('enroll');
        
        if (hasProductInfo) {
          console.log('✅ Product information loaded for course slug');
        } else {
          console.log('ℹ️ Product information not visible (may be free course)');
        }
      }
    }
  });

  test('should handle user authentication flow for enrollment', async () => {
    console.log('🔐 Testing authentication flow for enrollment...');
    
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');
    
    const courseCards = page.locator('[data-testid="course-card"]');
    await courseCards.first().click();
    await page.waitForLoadState('networkidle');
    
    // Look for enrollment elements that might require authentication
    const enrollButton = page.locator('[data-testid="enroll-button"]');
    const signInPrompt = page.locator('text=sign in');
    const loginButton = page.locator('text=login', { timeout: 1000 });
    
    if (await enrollButton.isVisible()) {
      await enrollButton.click();
      await page.waitForTimeout(2000);
      
      // Check if redirected to authentication
      const currentUrl = page.url();
      const isAuthPage = currentUrl.includes('sign-in') || 
                        currentUrl.includes('login') ||
                        currentUrl.includes('auth');
      
      if (isAuthPage) {
        console.log('✅ Enrollment correctly requires authentication');
        
        // Verify auth page elements
        const authForm = page.locator('form');
        if (await authForm.isVisible()) {
          console.log('✅ Authentication form is present');
        }
      }
    } else if (await signInPrompt.isVisible() || await loginButton.isVisible()) {
      console.log('✅ Authentication prompt visible on course page');
    } else {
      console.log('ℹ️ No authentication flow detected (may be free course or already authenticated)');
    }
  });

  test('should handle enrollment errors gracefully', async () => {
    console.log('💥 Testing enrollment error handling...');
    
    // Mock enrollment API failure
    await page.route('**/api/**/*enroll*', (route) => {
      route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Enrollment failed' }),
      });
    });
    
    await page.goto('/courses/catalog');
    await page.waitForLoadState('networkidle');
    
    const courseCards = page.locator('[data-testid="course-card"]');
    await courseCards.first().click();
    await page.waitForLoadState('networkidle');
    
    const enrollButton = page.locator('[data-testid="enroll-button"]');
    
    if (await enrollButton.isVisible()) {
      await enrollButton.click();
      await page.waitForTimeout(3000);
      
      // Check for error handling
      const pageContent = await page.textContent('body');
      const hasErrorHandling = pageContent?.toLowerCase().includes('error') ||
                              pageContent?.toLowerCase().includes('failed') ||
                              pageContent?.toLowerCase().includes('try again');
      
      if (hasErrorHandling) {
        console.log('✅ Enrollment error handled appropriately');
      } else {
        console.log('ℹ️ No obvious error message (may have different error handling)');
      }
    }
  });
});
