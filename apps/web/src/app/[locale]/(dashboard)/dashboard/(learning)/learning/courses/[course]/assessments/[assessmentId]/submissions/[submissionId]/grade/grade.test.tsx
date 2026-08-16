import '@testing-library/jest-dom/vitest';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import type { LearningAssessmentsGradingQueueItem } from '@game-guild/client';

const redirects = vi.hoisted(() => ({
  redirect: vi.fn(),
  notFound: vi.fn(),
}));

// Real redirect()/notFound() throw to halt rendering — the mocks mirror that
// so the page body actually stops at the redirect call.
vi.mock('@/i18n/navigation', () => ({
  redirect: redirects.redirect,
}));

vi.mock('next/navigation', () => ({
  notFound: redirects.notFound,
}));

const clientMock = vi.hoisted(() => ({
  request: vi.fn(),
  queue: vi.fn(),
}));

vi.mock('@game-guild/client', () => {
  const GeneratedApi = {
    LearningAssessmentsModule: class {
      getAssessmentsGradingQueue = clientMock.queue;
    },
  };
  return {
    GeneratedApi,
    createServerClient: () => ({
      request: clientMock.request,
    }),
  };
});

vi.mock('@/auth', () => ({
  getToken: vi.fn(),
}));

import GradeSubmissionPage from './page';
import { resolveNavIndex } from './resolve-nav-index';

const individualItems = [
  {
    submissionId: 'sub-1',
    canonicalSubmissionId: 'sub-1',
    userId: 'user-1',
    displayName: 'Ada Lovelace',
    attemptNumber: 1,
    isGroup: false,
  },
  {
    submissionId: 'sub-2',
    canonicalSubmissionId: 'sub-2',
    userId: 'user-2',
    displayName: 'Grace Hopper',
    attemptNumber: 2,
    isGroup: false,
  },
] satisfies LearningAssessmentsGradingQueueItem[];

const groupItems = [
  {
    submissionId: 'sub-canonical',
    canonicalSubmissionId: 'sub-canonical',
    userId: 'user-1',
    displayName: 'Ada Lovelace',
    groupId: 'group-1',
    groupName: 'Team Rocket',
    memberNames: ['Ada Lovelace', 'Grace Hopper'],
    attemptNumber: 2,
    isGroup: true,
  },
  {
    submissionId: 'sub-other',
    canonicalSubmissionId: 'sub-other',
    userId: 'user-3',
    displayName: 'Alan Turing',
    attemptNumber: 1,
    isGroup: false,
  },
] satisfies LearningAssessmentsGradingQueueItem[];

function makePageProps(submissionId: string) {
  return {
    params: Promise.resolve({
      locale: 'en',
      course: 'course-1',
      assessmentId: 'assessment-1',
      submissionId,
    }),
  };
}

function mockSubmission(overrides: Record<string, unknown> = {}) {
  clientMock.request.mockResolvedValue({
    ok: true,
    data: {
      id: 'sub-1',
      assessmentId: 'assessment-1',
      userId: 'user-1',
      attemptNumber: 1,
      status: 'Submitted',
      ...overrides,
    },
  });
}

describe('legacy grade route redirect', () => {
  beforeEach(() => {
    redirects.redirect.mockReset();
    redirects.notFound.mockReset();
    // Real redirect()/notFound() throw to halt rendering — mirror that so the
    // page body actually stops at the redirect call.
    redirects.redirect.mockImplementation((url: string) => {
      throw new Error(`NEXT_REDIRECT ${url}`);
    });
    redirects.notFound.mockImplementation(() => {
      throw new Error('NEXT_NOT_FOUND');
    });
    clientMock.request.mockReset();
    clientMock.queue.mockReset();
  });

  it('redirects to speedgrader with the resolved index and course slug', async () => {
    mockSubmission({ id: 'sub-2', userId: 'user-2', attemptNumber: 2 });
    clientMock.queue.mockResolvedValue({
      ok: true,
      data: { items: individualItems },
    });

    await expect(GradeSubmissionPage(makePageProps('sub-2'))).rejects.toThrow('NEXT_REDIRECT');

    expect(redirects.redirect).toHaveBeenCalledWith({
      href: '/speedgrader/assessments/assessment-1?course=course-1&nav=1',
      locale: 'en',
    });
    expect(redirects.notFound).not.toHaveBeenCalled();
  });

  it('notFounds when the submission cannot be loaded', async () => {
    clientMock.request.mockResolvedValue({ ok: false });

    await expect(GradeSubmissionPage(makePageProps('missing'))).rejects.toThrow('NEXT_NOT_FOUND');

    expect(redirects.notFound).toHaveBeenCalled();
    expect(redirects.redirect).not.toHaveBeenCalled();
  });

  it('resolves a group member row to the group queue item by attempt', async () => {
    // A member's (non-canonical) row: submissionId matches nothing, but the
    // single group attempt at attempt 2 resolves it.
    mockSubmission({
      id: 'sub-member-copy',
      userId: 'user-2',
      attemptNumber: 2,
    });
    clientMock.queue.mockResolvedValue({
      ok: true,
      data: { items: groupItems },
    });

    await expect(GradeSubmissionPage(makePageProps('sub-member-copy'))).rejects.toThrow('NEXT_REDIRECT');

    expect(redirects.redirect).toHaveBeenCalledWith({
      href: '/speedgrader/assessments/assessment-1?course=course-1&nav=0',
      locale: 'en',
    });
  });
});

describe('resolveNavIndex', () => {
  it('matches by canonical submission id', () => {
    expect(
      resolveNavIndex(groupItems, {
        submissionId: 'sub-canonical',
        attemptNumber: 2,
      }),
    ).toBe(0);
  });

  it('falls back to user + attempt', () => {
    expect(
      resolveNavIndex(groupItems, {
        submissionId: 'sub-unknown',
        userId: 'user-3',
        attemptNumber: 1,
      }),
    ).toBe(1);
  });

  it('defaults to 0 when nothing matches', () => {
    expect(
      resolveNavIndex(groupItems, {
        submissionId: 'sub-unknown',
        userId: 'user-x',
        attemptNumber: 9,
      }),
    ).toBe(0);
  });
});
