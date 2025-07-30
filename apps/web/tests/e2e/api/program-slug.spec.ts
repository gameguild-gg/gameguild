import { expect, test } from '@playwright/test';
import { APITestHelper } from '../utils/api-helper';

test.describe('API - Program Slug Endpoints', () => {
  let apiHelper: APITestHelper;

  test.beforeEach(async ({ request }) => {
    apiHelper = new APITestHelper(request);
  });

  test('should get published programs', async () => {
    const programs = await apiHelper.getPublishedPrograms();

    expect(Array.isArray(programs)).toBeTruthy();

    if (programs.length > 0) {
      // Validate each program has the required structure
      programs.forEach((program: Record<string, unknown>) => {
        apiHelper.validateProgramStructure(program);
      });
    }
  });

  test('should get program by valid slug', async () => {
    // First get published programs to find a valid slug
    const programs = await apiHelper.getPublishedPrograms();

    if (programs.length > 0) {
      const firstProgram = programs[0];
      const slug = firstProgram.slug as string;

      const { response, data } = await apiHelper.getProgramBySlug(slug);

      expect(response.ok()).toBeTruthy();
      expect(data).toBeTruthy();
      apiHelper.validateProgramStructure(data);
      expect(data.slug).toBe(slug);
    } else {
      test.skip(true, 'No published programs available for testing');
    }
  });

  test('should return 404 for invalid slug', async () => {
    const { response } = await apiHelper.getProgramBySlug('non-existent-slug-12345');

    expect(response.status()).toBe(404);
  });

  test('should handle slug with special characters', async () => {
    const testSlugs = [
      'unity-3d-game-development-2024',
      'c-sharp-programming-fundamentals',
      'react-typescript-advanced-patterns',
      'blender-3d-modeling-complete-guide',
    ];

    for (const slug of testSlugs) {
      const { response } = await apiHelper.getProgramBySlug(slug);

      // Should either return 200 (if exists) or 404 (if doesn't exist)
      // but not 400 (bad request) which would indicate slug format issues
      expect([200, 404]).toContain(response.status());
    }
  });

  test('should validate slug format in response', async () => {
    const programs = await apiHelper.getPublishedPrograms();

    programs.forEach((program: Record<string, unknown>) => {
      const slug = program.slug as string;

      // Validate slug is URL-safe
      apiHelper.validateProgramSlug(slug);

      // Ensure slug matches the title in a sensible way
      expect(slug.length).toBeGreaterThan(0);
      expect(slug.length).toBeLessThan(100);
    });
  });

  test('should maintain consistent program data across endpoints', async () => {
    const publishedPrograms = await apiHelper.getPublishedPrograms();

    if (publishedPrograms.length > 0) {
      const program = publishedPrograms[0];
      const slug = program.slug as string;

      // Get the same program by slug
      const { data: programBySlug } = await apiHelper.getProgramBySlug(slug);

      // Compare key fields
      expect(programBySlug.id).toBe(program.id);
      expect(programBySlug.slug).toBe(program.slug);
      expect(programBySlug.title).toBe(program.title);
      expect(programBySlug.description).toBe(program.description);
    }
  });

  test('should handle concurrent slug requests', async () => {
    const programs = await apiHelper.getPublishedPrograms();

    if (programs.length > 0) {
      const slugs = programs.slice(0, 3).map((p: Record<string, unknown>) => p.slug as string);

      // Make concurrent requests
      const promises = slugs.map((slug: string) => apiHelper.getProgramBySlug(slug));
      const results = await Promise.all(promises);

      // All should succeed
      results.forEach(({ response, data }, index) => {
        expect(response.ok()).toBeTruthy();
        expect(data.slug).toBe(slugs[index]);
      });
    }
  });

  test('should handle case sensitivity correctly', async () => {
    const programs = await apiHelper.getPublishedPrograms();

    if (programs.length > 0) {
      const originalSlug = programs[0].slug as string;
      const upperCaseSlug = originalSlug.toUpperCase();
      const mixedCaseSlug = originalSlug
        .split('-')
        .map((word, index) => (index % 2 === 0 ? word.toUpperCase() : word.toLowerCase()))
        .join('-');

      // Original slug should work
      const { response: originalResponse } = await apiHelper.getProgramBySlug(originalSlug);
      expect(originalResponse.ok()).toBeTruthy();

      // Different case versions should return 404 (case-sensitive)
      const { response: upperResponse } = await apiHelper.getProgramBySlug(upperCaseSlug);
      const { response: mixedResponse } = await apiHelper.getProgramBySlug(mixedCaseSlug);

      expect(upperResponse.status()).toBe(404);
      expect(mixedResponse.status()).toBe(404);
    }
  });
});
