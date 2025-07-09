import { expect, APIRequestContext } from '@playwright/test';

export class APITestHelper {
  constructor(private request: APIRequestContext) {}

  async getPublishedPrograms() {
    const response = await this.request.get('/api/program/published');
    expect(response.ok()).toBeTruthy();
    return response.json();
  }

  async getProgramBySlug(slug: string) {
    const response = await this.request.get(`/api/program/slug/${slug}`);
    return { response, data: response.ok() ? await response.json() : null };
  }

  async getAllPrograms(authToken?: string) {
    const headers: Record<string, string> = {};
    if (authToken) {
      headers.Authorization = `Bearer ${authToken}`;
    }
    const response = await this.request.get('/api/program', { headers });
    return { response, data: response.ok() ? await response.json() : null };
  }

  async authenticateUser(email: string, password: string) {
    const response = await this.request.post('/api/auth/login', {
      data: {
        email,
        password,
      },
    });
    
    if (response.ok()) {
      const data = await response.json();
      return data.accessToken;
    }
    return null;
  }

  async createTestProgram(authToken: string, programData: Record<string, unknown>) {
    const response = await this.request.post('/api/program', {
      headers: {
        Authorization: `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      },
      data: programData,
    });
    return { response, data: response.ok() ? await response.json() : null };
  }

  validateProgramSlug(slug: string) {
    // Validate slug format (URL-safe, lowercase, hyphens only)
    const slugRegex = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
    expect(slug).toMatch(slugRegex);
    expect(slug).not.toContain(' ');
    expect(slug).not.toContain('_');
    expect(slug).not.toContain('&');
    expect(slug).not.toContain('#');
  }

  validateProgramStructure(program: Record<string, unknown>) {
    expect(program).toHaveProperty('id');
    expect(program).toHaveProperty('slug');
    expect(program).toHaveProperty('title');
    expect(program).toHaveProperty('description');
    expect(program).toHaveProperty('category');
    expect(program).toHaveProperty('difficulty');
    expect(program).toHaveProperty('status');
    expect(program).toHaveProperty('visibility');
    
    this.validateProgramSlug(program.slug as string);
  }
}

export const mockProgramData = {
  basicProgram: {
    title: 'E2E Test Program',
    slug: 'e2e-test-program',
    description: 'A program created for end-to-end testing',
    category: 'Programming',
    difficulty: 'Beginner',
    status: 'Published',
    visibility: 'Public',
  },
  advancedProgram: {
    title: 'Advanced Unity Game Development',
    slug: 'advanced-unity-game-development',
    description: 'Learn advanced Unity techniques for professional game development',
    category: 'GameDevelopment',
    difficulty: 'Advanced',
    status: 'Published',
    visibility: 'Public',
  },
};
