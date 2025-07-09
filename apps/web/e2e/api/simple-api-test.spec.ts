import { test, expect } from '@playwright/test';

test.describe('Simple API Tests', () => {
  test('should get published programs', async ({ request }) => {
    const response = await request.get('/api/program/published');
    expect(response.ok()).toBeTruthy();
    
    const programs = await response.json();
    expect(Array.isArray(programs)).toBeTruthy();
    
    if (programs.length > 0) {
      const firstProgram = programs[0];
      expect(firstProgram).toHaveProperty('id');
      expect(firstProgram).toHaveProperty('slug');
      expect(firstProgram).toHaveProperty('title');
      expect(firstProgram).toHaveProperty('description');
      
      // Validate slug format
      expect(firstProgram.slug).toMatch(/^[a-z0-9-]+$/);
    }
  });

  test('should get program by valid slug', async ({ request }) => {
    // First get a valid slug
    const publishedResponse = await request.get('/api/program/published');
    const programs = await publishedResponse.json();
    
    if (programs.length > 0) {
      const slug = programs[0].slug;
      
      // Get program by slug
      const response = await request.get(`/api/program/slug/${slug}`);
      expect(response.ok()).toBeTruthy();
      
      const program = await response.json();
      expect(program.slug).toBe(slug);
    } else {
      test.skip(true, 'No programs available for testing');
    }
  });

  test('should return 404 for invalid slug', async ({ request }) => {
    const response = await request.get('/api/program/slug/non-existent-slug-12345');
    expect(response.status()).toBe(404);
  });
});
