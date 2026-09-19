import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(),
  getToken: vi.fn(),
  claim: vi.fn(),
  getWorkspace: vi.fn(),
  submit: vi.fn(),
  getReceived: vi.fn(),
}));

vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningAssessmentsPeerReviewsModule: class {
      postAssessmentsPeerReviewsClaim = mocks.claim;
      getAssessmentsPeerReviews = mocks.getWorkspace;
      postAssessmentsPeerReviewsSubmit = mocks.submit;
      getAssessmentsSubmissionsReceivedPeerReviews = mocks.getReceived;
    },
  },
}));

import {
  claimPeerReview,
  fetchPeerReviewWorkspace,
  fetchReceivedPeerReviews,
  submitPeerReview,
} from './actions-peer-review';

describe('peer review actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', 'https://api.gameguild.test');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public-api.gameguild.test');
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('access-token');
  });

  afterEach(() => vi.unstubAllEnvs());

  it('claims a review and configures the authenticated server client', async () => {
    mocks.claim.mockResolvedValue({ ok: true, data: { reviewId: 'review-1' } });

    await expect(claimPeerReview('assessment-1')).resolves.toEqual({
      success: true,
      data: { reviewId: 'review-1' },
    });
    expect(mocks.claim).toHaveBeenCalledWith('assessment-1');
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'https://api.gameguild.test',
      auth: { getAccessToken: expect.any(Function) },
    });
    await expect(mocks.createServerClient.mock.calls[0]![0].auth.getAccessToken()).resolves.toBe('access-token');
  });

  it.each([
    [{ detail: 'No eligible submissions.' }, 'No eligible submissions.'],
    [{ message: 'Claim failed.' }, 'Claim failed.'],
    [undefined, 'Request failed.'],
  ])('returns the most useful claim API error', async (error, expected) => {
    mocks.claim.mockResolvedValue({ ok: false, error });
    await expect(claimPeerReview('assessment-1')).resolves.toEqual({ success: false, error: expected });
  });

  it('rejects a claim response without an id', async () => {
    mocks.claim.mockResolvedValue({ ok: true, data: {} });
    await expect(claimPeerReview('assessment-1')).resolves.toEqual({
      success: false,
      error: 'Failed to claim a peer review.',
    });
  });

  it.each([
    [new Error('network down'), 'Unexpected error: network down'],
    ['offline', 'Unexpected error: offline'],
  ])('normalizes thrown claim errors', async (reason, expected) => {
    mocks.claim.mockRejectedValue(reason);
    await expect(claimPeerReview('assessment-1')).resolves.toEqual({ success: false, error: expected });
  });

  it('loads an anonymous review workspace', async () => {
    const review = { id: 'review-1', assessmentId: 'assessment-1' };
    mocks.getWorkspace.mockResolvedValue({ ok: true, data: review });
    await expect(fetchPeerReviewWorkspace('review-1')).resolves.toEqual({ ok: true, review });
    expect(mocks.getWorkspace).toHaveBeenCalledWith('review-1');
  });

  it.each([false, true])('uses a privacy-safe workspace loading failure', async (throws) => {
    if (throws) mocks.getWorkspace.mockRejectedValue(new Error('offline'));
    else mocks.getWorkspace.mockResolvedValue({ ok: false, error: { message: 'ignored' } });
    await expect(fetchPeerReviewWorkspace('review-1')).resolves.toEqual({
      ok: false,
      error: 'Failed to load the review.',
    });
  });

  it('validates, trims, and submits peer review feedback', async () => {
    await expect(submitPeerReview('review-1', { feedback: '   ' })).resolves.toEqual({
      success: false,
      error: 'Feedback comment is required',
    });
    expect(mocks.submit).not.toHaveBeenCalled();

    mocks.submit.mockResolvedValue({ ok: true, data: null });
    await expect(
      submitPeerReview('review-1', { score: 90, feedback: '  Useful feedback.  ', rubricScores: '{"quality":{"points":5}}' }),
    ).resolves.toEqual({ success: true, data: null });
    expect(mocks.submit).toHaveBeenCalledWith('review-1', {
      score: 9000,
      feedback: 'Useful feedback.',
      rubricScores: '{"quality":{"points":500}}',
    });
  });

  it('submits optional score and rubric values as null', async () => {
    mocks.submit.mockResolvedValue({ ok: true, data: null });
    await submitPeerReview('review-1', { feedback: 'Good work.' });
    expect(mocks.submit).toHaveBeenCalledWith('review-1', {
      score: undefined,
      feedback: 'Good work.',
      rubricScores: null,
    });
  });

  it.each([
    [{ detail: 'Review is closed.' }, 'Review is closed.'],
    [{ message: 'Submission failed.' }, 'Submission failed.'],
    [undefined, 'Request failed.'],
  ])('returns the most useful submit API error', async (error, expected) => {
    mocks.submit.mockResolvedValue({ ok: false, error });
    await expect(submitPeerReview('review-1', { feedback: 'Good work.' })).resolves.toEqual({
      success: false,
      error: expected,
    });
  });

  it.each([
    [new Error('network down'), 'Unexpected error: network down'],
    [503, 'Unexpected error: 503'],
  ])('normalizes thrown submit errors', async (reason, expected) => {
    mocks.submit.mockRejectedValue(reason);
    await expect(submitPeerReview('review-1', { feedback: 'Good work.' })).resolves.toEqual({
      success: false,
      error: expected,
    });
  });

  it('loads received reviews and normalizes an absent collection', async () => {
    const reviews = [{ id: 'review-1', feedback: 'Clear solution.' }];
    mocks.getReceived.mockResolvedValueOnce({ ok: true, data: reviews }).mockResolvedValueOnce({ ok: true, data: null });

    await expect(fetchReceivedPeerReviews('submission-1')).resolves.toEqual({ ok: true, reviews });
    await expect(fetchReceivedPeerReviews('submission-2')).resolves.toEqual({ ok: true, reviews: [] });
    expect(mocks.getReceived).toHaveBeenNthCalledWith(1, 'submission-1');
  });

  it.each([false, true])('uses a stable received-review loading failure', async (throws) => {
    if (throws) mocks.getReceived.mockRejectedValue(new Error('offline'));
    else mocks.getReceived.mockResolvedValue({ ok: false, error: { message: 'ignored' } });
    await expect(fetchReceivedPeerReviews('submission-1')).resolves.toEqual({
      ok: false,
      error: 'Failed to load received reviews.',
    });
  });

  it('uses the public and local API URL fallbacks', async () => {
    vi.stubEnv('API_URL', '');
    mocks.claim.mockResolvedValue({ ok: true, data: { reviewId: 'review-1' } });
    await claimPeerReview('assessment-1');
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({
      baseUrl: 'https://public-api.gameguild.test',
    }));

    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    await claimPeerReview('assessment-1');
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({
      baseUrl: 'http://localhost:8080',
    }));
  });
});
