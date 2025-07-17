import { expect, test } from '@playwright/test';

/**
 * Enhanced API E2E Tests for Game Guild Program Slug Endpoints
 *
 * These tests validate:
 * - Public API endpoints work correctly
 * - Slug-based navigation is properly implemented
 * - Error handling for invalid slugs
 * - Response structure and data consistency
 */

test.describe('Game Guild API - Enhanced Slug Tests', () => {
  let validSlugs: string[] = [];
  let sampleProgram: Record<string, unknown> | null = null;

  test.beforeAll(async ({ request }) => {
    // Get published programs to use for testing
    const response = await request.get('/api/program/published');
    expect(response.ok()).toBeTruthy();

    const programs = await response.json();
    expect(Array.isArray(programs)).toBeTruthy();

    if (programs.length > 0) {
      validSlugs = programs.slice(0, 3).map((p: Record<string, unknown>) => p.slug as string);
      sampleProgram = programs[0];
    }
  });

  test('should fetch published programs successfully', async ({ request }) => {
    const response = await request.get('/api/program/published');

    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);

    const programs = await response.json();
    expect(Array.isArray(programs)).toBeTruthy();

    if (programs.length > 0) {
      // Validate structure of first program
      const program = programs[0];
      expect(program).toHaveProperty('id');
      expect(program).toHaveProperty('slug');
      expect(program).toHaveProperty('title');
      expect(program).toHaveProperty('description');
      expect(program).toHaveProperty('category');
      expect(program).toHaveProperty('difficulty');

      // Validate slug format (URL-safe)
      expect(program.slug).toMatch(/^[a-z0-9-]+$/);
      expect(program.slug).not.toContain(' ');
      expect(program.slug).not.toContain('_');
    }
  });

  test('should validate all program slugs have correct format', async ({ request }) => {
    const response = await request.get('/api/program/published');
    const programs = await response.json();

    const slugRegex = /^[a-z0-9-]+$/;
    const invalidSlugs: string[] = [];

    programs.forEach((program: Record<string, unknown>) => {
      const slug = program.slug as string;
      if (!slugRegex.test(slug)) {
        invalidSlugs.push(slug);
      }
    });

    expect(invalidSlugs.length).toBe(0);

    if (invalidSlugs.length > 0) {
      console.log('Invalid slugs found:', invalidSlugs);
    }
  });

  test('should handle individual slug endpoint appropriately', async ({ request }) => {
    test.skip(validSlugs.length === 0, 'No valid slugs available for testing');

    const slug = validSlugs[0];
    const response = await request.get(`/api/program/slug/${slug}`);

    // The endpoint might require authentication (401) or be public (200)
    // Both are acceptable for this test
    expect([200, 401]).toContain(response.status());

    if (response.status() === 200) {
      const program = await response.json();
      expect(program.slug).toBe(slug);
      expect(program).toHaveProperty('id');
      expect(program).toHaveProperty('title');
    }
  });

  test('should return appropriate error for invalid slug', async ({ request }) => {
    const response = await request.get('/api/program/slug/non-existent-slug-12345');

    // Should return either 404 (not found) or 401 (unauthorized)
    expect([401, 404]).toContain(response.status());
  });

  test('should handle special characters in slugs correctly', async ({ request }) => {
    const testSlugs = ['unity-3d-game-development', 'c-sharp-programming', 'game-dev-portfolio', 'javascript-game-development'];

    for (const slug of testSlugs) {
      const response = await request.get(`/api/program/slug/${slug}`);

      // Should return 200 (found), 401 (auth required), or 404 (not found)
      // but NOT 400 (bad request) which would indicate slug format issues
      expect([200, 401, 404]).toContain(response.status());

      if (response.status() === 400) {
        throw new Error(`Slug format issue detected for: ${slug}`);
      }
    }
  });

  test('should maintain consistent data structure across endpoints', async ({ request }) => {
    test.skip(!sampleProgram, 'No sample program available for comparison');

    if (!sampleProgram) return;

    const publishedResponse = await request.get('/api/program/published');
    const programs = await publishedResponse.json();

    const program = programs.find((p: Record<string, unknown>) => p.slug === sampleProgram!.slug);
    expect(program).toBeTruthy();

    // Test the individual slug endpoint
    const slugResponse = await request.get(`/api/program/slug/${sampleProgram.slug}`);

    if (slugResponse.status() === 200) {
      const individualProgram = await slugResponse.json();

      // Compare key fields between endpoints
      expect(individualProgram.id).toBe(program.id);
      expect(individualProgram.slug).toBe(program.slug);
      expect(individualProgram.title).toBe(program.title);
    } else if (slugResponse.status() === 401) {
      // Authentication required - this is acceptable
      console.log('Individual slug endpoint requires authentication');
    }
  });

  test('should handle concurrent requests gracefully', async ({ request }) => {
    test.skip(validSlugs.length < 2, 'Need at least 2 slugs for concurrent testing');

    const promises = validSlugs.slice(0, 3).map((slug) => request.get(`/api/program/slug/${slug}`));

    const responses = await Promise.all(promises);

    // All responses should be consistent (either all 200, all 401, or mixed but no errors)
    responses.forEach((response, index) => {
      expect([200, 401, 404]).toContain(response.status());
      console.log(`Slug ${validSlugs[index]}: HTTP ${response.status()}`);
    });
  });

  test('should return consistent response times', async ({ request }) => {
    test.skip(validSlugs.length === 0, 'No valid slugs available for testing');

    const slug = validSlugs[0];
    const startTime = Date.now();

    await request.get(`/api/program/slug/${slug}`);

    const responseTime = Date.now() - startTime;

    // Response time should be reasonable (less than 5 seconds)
    expect(responseTime).toBeLessThan(5000);

    console.log(`Response time for slug '${slug}': ${responseTime}ms`);
  });

  test('should validate program categories and difficulties', async ({ request }) => {
    const response = await request.get('/api/program/published');
    const programs = await response.json();

    programs.forEach((program: Record<string, unknown>) => {
      // Category should be a number (enum)
      expect(typeof program.category).toBe('number');

      // Difficulty should be a number (enum)
      expect(typeof program.difficulty).toBe('number');

      // Title and description should be strings
      expect(typeof program.title).toBe('string');
      expect(typeof program.description).toBe('string');

      // Slug should be a string
      expect(typeof program.slug).toBe('string');
    });
  });
});
