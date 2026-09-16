import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  postGrade: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningAssessmentsModule: class {
      postAssessmentsSubmissionsGrade = mocks.postGrade;
    },
  },
}));

import { gradeSubmission } from './grade-action';

describe('grade submission action', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', 'https://api.gameguild.test');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public-api.gameguild.test');
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
  });

  afterEach(() => vi.unstubAllEnvs());

  it('posts a rubric grade without accepting a client-supplied grader identity', async () => {
    mocks.postGrade.mockResolvedValue({ ok: true, data: {} });
    await expect(gradeSubmission({
      submissionId: 'submission-1', score: 85, feedback: 'Good work.', rubricScores: '{"quality":{"points":5}}',
    })).resolves.toEqual({ success: true, data: { submissionId: 'submission-1' } });
    expect(mocks.postGrade).toHaveBeenCalledWith('submission-1', {
      score: 85, feedback: 'Good work.', rubricScores: '{"quality":{"points":5}}',
    });
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'https://api.gameguild.test',
      auth: { getAccessToken: expect.any(Function) },
    });
    await expect(mocks.createServerClient.mock.calls[0]![0].auth.getAccessToken()).resolves.toBe('access-token');
  });

  it('normalizes an optional rubric and API failure messages', async () => {
    mocks.postGrade.mockResolvedValueOnce({ ok: false, error: { message: 'Score exceeds maximum.' } });
    await expect(gradeSubmission({ submissionId: 'submission-1', score: 101, feedback: '' })).resolves.toEqual({
      success: false, error: 'Score exceeds maximum.',
    });
    expect(mocks.postGrade).toHaveBeenCalledWith('submission-1', {
      score: 101, feedback: '', rubricScores: null,
    });

    mocks.postGrade.mockResolvedValueOnce({ ok: false, error: undefined });
    await expect(gradeSubmission({ submissionId: 'submission-2', score: 50, feedback: '' })).resolves.toEqual({
      success: false, error: 'Failed to post grade.',
    });
  });

  it('uses public and local API URL fallbacks', async () => {
    mocks.postGrade.mockResolvedValue({ ok: true, data: {} });
    vi.stubEnv('API_URL', '');
    await gradeSubmission({ submissionId: 'submission-1', score: 50, feedback: '' });
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({
      baseUrl: 'https://public-api.gameguild.test',
    }));

    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    await gradeSubmission({ submissionId: 'submission-1', score: 50, feedback: '' });
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({
      baseUrl: 'http://localhost:8080',
    }));
  });
});
