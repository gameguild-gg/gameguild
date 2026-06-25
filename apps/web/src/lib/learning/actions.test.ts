import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  revalidatePath: vi.fn(),
  fetch: vi.fn(),
  resolveCourseId: vi.fn(),
  deleteCoursesContent: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(),
  GeneratedApi: {
    LearningAssessmentsModule: class {},
    LearningCoursesProgramModule: class {},
    LearningCoursesProgramcontentModule: class {
      deleteCoursesContent = mocks.deleteCoursesContent;
    },
    LearningCoursesProgramlifecycleModule: class {},
    LearningEnrollmentsModule: class {},
  },
}));

vi.mock('@/lib/learning/queries/course', () => ({
  resolveCourseId: mocks.resolveCourseId,
}));

const {
  createCertificateTemplate,
  deleteCertificateTemplate,
  deleteContent,
  createCourseClass,
  updateCourseClass,
  updateCourseClassStatus,
  createCourseDiscussion,
  createDiscussionReply,
  deleteAssessmentGroup,
  updateDiscussionPin,
  updateAssessmentGroup,
  resolveDiscussion,
} = await import('./actions');

describe('learning server actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.resolveCourseId.mockImplementation(async (courseId: string) => courseId);
    mocks.deleteCoursesContent.mockResolvedValue({ ok: true, data: undefined });
    vi.stubGlobal('fetch', mocks.fetch);
  });

  it('resolves dashboard course slugs before deleting course content', async () => {
    mocks.resolveCourseId.mockResolvedValue('1caa16bb-6810-4e53-bb0d-91f0d5702333');

    const result = await deleteContent('creature-design-by-admin', '9ec3b854-89ca-4757-83fb-cfc823da1a5e');

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.resolveCourseId).toHaveBeenCalledWith('creature-design-by-admin');
    expect(mocks.deleteCoursesContent).toHaveBeenCalledWith(
      '1caa16bb-6810-4e53-bb0d-91f0d5702333',
      '9ec3b854-89ca-4757-83fb-cfc823da1a5e',
    );
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/creature-design-by-admin');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/1caa16bb-6810-4e53-bb0d-91f0d5702333');
  });

  it('creates certificate templates through the Learning.Certificates API', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'template-1', courseId: 'course-1', name: 'Completion' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await createCertificateTemplate({
      courseId: 'course-1',
      name: 'Completion',
      templateHtml: '<section>{{recipientName}}</section>',
    });

    expect(result).toEqual({ success: true, data: { id: 'template-1' } });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/certificates/templates', {
      method: 'POST',
      body: JSON.stringify({
        courseId: 'course-1',
        name: 'Completion',
        templateHtml: '<section>{{recipientName}}</section>',
      }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/certificates');
  });

  it('deletes certificate templates and refreshes the certificate page', async () => {
    mocks.fetch.mockResolvedValue(new Response(null, { status: 204 }));

    const result = await deleteCertificateTemplate('course-1', 'template-1');

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/certificates/templates/template-1', {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/certificates');
  });

  it('creates course classes through the Learning.Cohorts API', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'class-1', courseId: 'course-1' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await createCourseClass({
      courseId: 'course-1',
      name: 'Production Cohort',
      description: 'Live feedback sessions.',
      startDate: '2026-07-01T13:00',
      endDate: '2026-07-01T15:00',
      maxCapacity: 20,
      meetingSchedule: 'https://meet.example/session',
    });

    expect(result).toEqual({ success: true, data: { id: 'class-1' } });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/cohorts', {
      method: 'POST',
      body: JSON.stringify({
        courseId: 'course-1',
        name: 'Production Cohort',
        description: 'Live feedback sessions.',
        startDate: new Date('2026-07-01T13:00').toISOString(),
        endDate: new Date('2026-07-01T15:00').toISOString(),
        maxCapacity: 20,
        instructorId: null,
        meetingSchedule: 'https://meet.example/session',
      }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/classes');
  });

  it('updates course class details through the cohort update endpoint', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'class-1', courseId: 'course-1' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await updateCourseClass({
      courseId: 'course-1',
      classId: 'class-1',
      name: 'Updated Cohort',
      description: 'Updated schedule.',
      startDate: '2026-07-02T13:00',
      endDate: '2026-07-02T16:00',
      maxCapacity: 18,
      meetingSchedule: 'Room 302',
    });

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/cohorts/class-1', {
      method: 'PUT',
      body: JSON.stringify({
        name: 'Updated Cohort',
        description: 'Updated schedule.',
        startDate: new Date('2026-07-02T13:00').toISOString(),
        endDate: new Date('2026-07-02T16:00').toISOString(),
        maxCapacity: 18,
        instructorId: null,
        meetingSchedule: 'Room 302',
      }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/classes');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/classes/class-1');
  });

  it('updates class lifecycle status through the cohort status endpoint', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'class-1', courseId: 'course-1' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await updateCourseClassStatus('course-1', 'class-1', 'open');

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/cohorts/class-1/open', {
      method: 'POST',
      body: JSON.stringify({}),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/classes');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/classes/class-1');
  });

  it('creates course discussions through the Learning Social API', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'thread-1', courseId: 'course-1' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await createCourseDiscussion({
      courseId: 'course-1',
      title: 'Milestone review question',
      content: 'Can I submit a revised prototype after review?',
    });

    expect(result).toEqual({ success: true, data: { id: 'thread-1' } });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/social/discussions', {
      method: 'POST',
      body: JSON.stringify({
        courseId: 'course-1',
        title: 'Milestone review question',
        content: 'Can I submit a revised prototype after review?',
        contentId: null,
      }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/support/discussions');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/support/discussions/thread-1');
  });

  it('posts discussion replies and refreshes support routes', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'reply-1', discussionId: 'thread-1' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await createDiscussionReply({
      courseId: 'course-1',
      discussionId: 'thread-1',
      content: 'Yes, submit the revision before the checkpoint closes.',
    });

    expect(result).toEqual({ success: true, data: { id: 'reply-1' } });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/social/discussions/thread-1/replies', {
      method: 'POST',
      body: JSON.stringify({
        discussionId: 'thread-1',
        content: 'Yes, submit the revision before the checkpoint closes.',
        parentReplyId: null,
      }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/support/tickets/thread-1');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/support/discussions/thread-1');
  });

  it('pins and resolves discussion threads through social moderation endpoints', async () => {
    const okResponse = () =>
      new Response(JSON.stringify({ id: 'thread-1', courseId: 'course-1' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    mocks.fetch.mockResolvedValueOnce(okResponse()).mockResolvedValueOnce(okResponse());

    expect(await updateDiscussionPin('course-1', 'thread-1', true)).toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenLastCalledWith('http://localhost:5295/api/social/discussions/thread-1/pin', {
      method: 'POST',
      body: JSON.stringify({}),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });

    expect(await resolveDiscussion('course-1', 'thread-1')).toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenLastCalledWith('http://localhost:5295/api/social/discussions/thread-1/resolve', {
      method: 'POST',
      body: JSON.stringify({}),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
  });

  it('updates assessment groups through the weighted grading endpoint', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'group-1', courseId: 'course-1' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await updateAssessmentGroup({
      courseId: 'course-1',
      groupId: 'group-1',
      name: 'Weekly quizzes',
      description: 'Weekly knowledge checks.',
      weightPercent: 25,
      order: 2,
    });

    expect(result).toEqual({ success: true, data: { id: 'group-1' } });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/v1/assessments/groups/group-1', {
      method: 'PUT',
      body: JSON.stringify({
        name: 'Weekly quizzes',
        description: 'Weekly knowledge checks.',
        weightPercent: 25,
        order: 2,
      }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/assessments');
  });

  it('rejects invalid assessment group weights before calling the API', async () => {
    const result = await updateAssessmentGroup({
      courseId: 'course-1',
      groupId: 'group-1',
      name: 'Weekly quizzes',
      weightPercent: 120,
    });

    expect(result).toEqual({ success: false, error: 'Weight must be between 0 and 100.' });
    expect(mocks.fetch).not.toHaveBeenCalled();
  });

  it('deletes assessment groups and refreshes the assessment hub', async () => {
    mocks.fetch.mockResolvedValue(new Response(null, { status: 204 }));

    const result = await deleteAssessmentGroup('course-1', 'group-1');

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/v1/assessments/groups/group-1', {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/assessments');
  });
});
