import { beforeEach, describe, expect, it, vi } from 'vitest';

const redirects = vi.hoisted(() => ({
  redirect: vi.fn(),
  notFound: vi.fn(),
}));
const learning = vi.hoisted(() => ({ getAssessment: vi.fn() }));

vi.mock('@/i18n/navigation', () => ({ redirect: redirects.redirect }));
vi.mock('next/navigation', () => ({ notFound: redirects.notFound }));
vi.mock('@/lib/learning', () => ({ getAssessment: learning.getAssessment }));

import GradeSubmissionPage from './page';

function makePageProps(submissionId: string) {
  return {
    params: Promise.resolve({
      locale: 'en-US',
      course: 'course-slug',
      assessmentSlug: 'assessment-slug',
      submissionId,
    }),
  };
}

describe('legacy grade route redirect', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    redirects.redirect.mockImplementation((value: unknown) => {
      throw new Error(`NEXT_REDIRECT ${JSON.stringify(value)}`);
    });
    redirects.notFound.mockImplementation(() => {
      throw new Error('NEXT_NOT_FOUND');
    });
  });

  it('redirects the immutable submission directly to runtime SpeedGrader', async () => {
    learning.getAssessment.mockResolvedValue({ id: 'assessment-id' });

    await expect(
      GradeSubmissionPage(makePageProps('submission/id')),
    ).rejects.toThrow('NEXT_REDIRECT');

    expect(learning.getAssessment).toHaveBeenCalledWith(
      'course-slug',
      'assessment-slug',
    );
    expect(redirects.redirect).toHaveBeenCalledWith({
      href: '/speedgrader/assessments/assessment-id?course=course-slug&submission=submission%2Fid',
      locale: 'en-US',
    });
  });

  it('notFounds when the assessment does not exist', async () => {
    learning.getAssessment.mockResolvedValue(null);

    await expect(
      GradeSubmissionPage(makePageProps('missing')),
    ).rejects.toThrow('NEXT_NOT_FOUND');

    expect(redirects.notFound).toHaveBeenCalledOnce();
    expect(redirects.redirect).not.toHaveBeenCalled();
  });
});
