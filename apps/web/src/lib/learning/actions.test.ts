import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  revalidatePath: vi.fn(),
  fetch: vi.fn(),
  resolveCourseId: vi.fn(),
  deleteCoursesContent: vi.fn(),
  getCourses1: vi.fn(),
  putCourses: vi.fn(),
  postCoursesUsers: vi.fn(),
  deleteCoursesUsers: vi.fn(),
  postApiLearningEnrollments: vi.fn(),
  clientRequest: vi.fn(),
  createServerClient: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningAssessmentsModule: class {},
    LearningCoursesProgramModule: class {
      getCourses1 = mocks.getCourses1;
      putCourses = mocks.putCourses;
      postCoursesUsers = mocks.postCoursesUsers;
      deleteCoursesUsers = mocks.deleteCoursesUsers;
    },
    LearningCoursesProgramcontentModule: class {
      deleteCoursesContent = mocks.deleteCoursesContent;
    },
    LearningCoursesProgramlifecycleModule: class {},
    LearningEnrollmentsModule: class {
      postApiLearningEnrollments = mocks.postApiLearningEnrollments;
    },
  },
}));

vi.mock('@/lib/learning/queries/course', () => ({
  resolveCourseId: mocks.resolveCourseId,
}));

const {
  createCertificateTemplate,
  updateCertificateTemplate,
  deleteCertificateTemplate,
  deleteContent,
  createCourseClass,
  updateCourseClass,
  updateCourseClassStatus,
  createCourseDiscussion,
  createDiscussionReply,
  addCourseSupportTicketMessage,
  resolveCourseSupportTicket,
  deleteAssessmentGroup,
  updateDiscussionPin,
  updateAssessmentGroup,
  resolveDiscussion,
  updateCourseNotificationSettings,
  updateCourseIntegrationSettings,
  updateCourseReviewModeration,
  manualEnrollStudent,
  removeCourseStudents,
  sendCourseStudentMessage,
  updateCourse,
} = await import('./actions');

describe('learning server actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.resolveCourseId.mockImplementation(async (courseId: string) => courseId);
    mocks.deleteCoursesContent.mockResolvedValue({ ok: true, data: undefined });
    mocks.getCourses1.mockResolvedValue({
      ok: true,
      data: { id: 'course-1', metadata: JSON.stringify({ landingFaq: [{ question: 'Existing' }] }) },
    });
    mocks.putCourses.mockResolvedValue({ ok: true, data: {} });
    mocks.createServerClient.mockReturnValue({ request: mocks.clientRequest });
    mocks.clientRequest.mockResolvedValue({
      ok: true,
      data: { items: [{ id: 'user-1', email: 'student@example.com', username: 'student', name: 'Student' }] },
    });
    mocks.postCoursesUsers.mockResolvedValue({ ok: true, data: { enrollmentId: 'program-user-1' } });
    mocks.deleteCoursesUsers.mockResolvedValue({ ok: true, data: undefined });
    mocks.postApiLearningEnrollments.mockResolvedValue({ ok: true, data: { id: 'cohort-enrollment-1' } });
    vi.stubGlobal('fetch', mocks.fetch);
  });

  it('uses explicit clear flags for nullable enrollment controls', async () => {
    const result = await updateCourse({
      courseId: 'course-1',
      maxEnrollments: null,
      enrollmentDeadline: null,
    });

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.putCourses).toHaveBeenCalledWith('course-1', {
      clearMaxEnrollments: true,
      clearEnrollmentDeadline: true,
    });
  });

  it('preserves finite enrollment controls without clear flags', async () => {
    const result = await updateCourse({
      courseId: 'course-1',
      maxEnrollments: 25,
      enrollmentDeadline: '2026-09-01T12:00:00.000Z',
    });

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.putCourses).toHaveBeenCalledWith('course-1', {
      maxEnrollments: 25,
      enrollmentDeadline: '2026-09-01T12:00:00.000Z',
    });
  });

  it('resolves canonical course routes before updating the API resource', async () => {
    mocks.resolveCourseId.mockResolvedValueOnce('resolved-course-id');

    const result = await updateCourse({
      courseId: 'boss-ai-by-instructor-one',
      title: 'Resolved course',
    });

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.resolveCourseId).toHaveBeenCalledWith('boss-ai-by-instructor-one');
    expect(mocks.putCourses).toHaveBeenCalledWith('resolved-course-id', { title: 'Resolved course' });
  });

  it('persists notification settings while preserving unrelated course metadata', async () => {
    const result = await updateCourseNotificationSettings('course-1', {
      studentNotifications: {
        enrollmentConfirmation: true,
        courseUpdates: false,
        newContent: true,
        upcomingClasses: true,
        classReminders: [120, -1, 15, 120],
        assignmentDue: true,
        assessmentResults: true,
        certificateReady: true,
        discussionReplies: false,
      },
      instructorNotifications: {
        newEnrollment: true,
        newReview: true,
        supportTicket: true,
        discussionMention: false,
        lowRating: true,
        lowRatingThreshold: 9,
      },
      templates: [{ id: 'updates', type: 'course-update', subject: '  Course update  ', enabled: true }],
    });

    expect(result).toEqual({ success: true, data: null });
    const metadata = JSON.parse(mocks.putCourses.mock.calls[0][1].metadata);
    expect(metadata.landingFaq).toEqual([{ question: 'Existing' }]);
    expect(metadata.notificationSettings.studentNotifications.classReminders).toEqual([120, 15]);
    expect(metadata.notificationSettings.instructorNotifications.lowRatingThreshold).toBe(5);
    expect(metadata.notificationSettings.templates[0].subject).toBe('Course update');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/settings/notifications');
  });

  it('persists course integrations and rejects invalid webhook URLs', async () => {
    const invalid = await updateCourseIntegrationSettings('course-1', {
      integrations: [],
      webhooks: [{ id: 'hook-1', url: 'javascript:alert(1)', events: ['course.updated'], enabled: true }],
    });
    expect(invalid).toEqual({ success: false, error: 'Webhook URLs must use http or https.' });

    const result = await updateCourseIntegrationSettings('course-1', {
      integrations: [
        { id: 'discord', type: 'discord', name: ' Class Discord ', enabled: true, status: 'connected', config: { inviteUrl: 'https://discord.gg/gameguild' } },
      ],
      webhooks: [{ id: 'hook-1', url: 'https://example.com/events', events: ['course.updated', '', 'course.updated'], enabled: true }],
    });

    expect(result).toEqual({ success: true, data: null });
    const metadata = JSON.parse(mocks.putCourses.mock.calls.at(-1)?.[1].metadata);
    expect(metadata.integrationSettings.integrations[0].name).toBe('Class Discord');
    expect(metadata.integrationSettings.webhooks[0].events).toEqual(['course.updated']);
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/settings/integrations');
  });

  it('updates testimonial approval and featured state through the moderation endpoint', async () => {
    mocks.fetch.mockResolvedValue(new Response(JSON.stringify({ id: 'review-1', isApproved: true, isFeatured: false }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }));

    const result = await updateCourseReviewModeration('course-1', 'review-1', true, false);

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/social/reviews/review-1/moderation', {
      method: 'PATCH',
      body: JSON.stringify({ isApproved: true, isFeatured: false }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/listing/testimonials');
  });

  it('enrolls through the canonical course roster and synchronizes an optional cohort', async () => {
    const result = await manualEnrollStudent({
      courseId: 'course-slug',
      userId: 'student@example.com',
      cohortId: 'cohort-1',
    });

    expect(result).toEqual({ success: true, data: { id: 'program-user-1' } });
    expect(mocks.postCoursesUsers).toHaveBeenCalledWith('course-slug', 'user-1');
    expect(mocks.postApiLearningEnrollments).toHaveBeenCalledWith({
      courseId: 'course-slug',
      userId: 'user-1',
      cohortId: 'cohort-1',
    });
  });

  it('rolls back the canonical roster when cohort synchronization fails', async () => {
    mocks.postApiLearningEnrollments.mockResolvedValueOnce({ ok: false, error: { detail: 'Cohort is full.' } });

    const result = await manualEnrollStudent({
      courseId: 'course-1',
      userId: 'student@example.com',
      cohortId: 'cohort-full',
    });

    expect(result).toEqual({ success: false, error: 'Cohort is full.' });
    expect(mocks.deleteCoursesUsers).toHaveBeenCalledWith('course-1', 'user-1');
  });

  it('removes selected users from the canonical course roster', async () => {
    const result = await removeCourseStudents('course-1', ['user-1', 'user-2', 'user-1']);

    expect(result).toEqual({ success: true, data: { removed: 2 } });
    expect(mocks.deleteCoursesUsers).toHaveBeenCalledTimes(2);
    expect(mocks.deleteCoursesUsers).toHaveBeenCalledWith('course-1', 'user-1');
    expect(mocks.deleteCoursesUsers).toHaveBeenCalledWith('course-1', 'user-2');
  });

  it('sends a course message to selected enrolled users', async () => {
    mocks.fetch.mockResolvedValue(new Response(JSON.stringify({ sent: 2 }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }));

    const result = await sendCourseStudentMessage({
      courseId: 'course-1',
      userIds: ['user-1', 'user-2'],
      subject: ' Milestone update ',
      message: ' The critique session moved to Friday. ',
    });

    expect(result).toEqual({ success: true, data: { sent: 2 } });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/v1/courses/course-1/students/message', {
      method: 'POST',
      body: JSON.stringify({
        userIds: ['user-1', 'user-2'],
        subject: 'Milestone update',
        message: 'The critique session moved to Friday.',
      }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
  });

  it('replies to and resolves persisted course support tickets', async () => {
    mocks.fetch.mockImplementation(async () => new Response(JSON.stringify({ id: 'ticket-1' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }));

    await expect(addCourseSupportTicketMessage({
      courseId: 'course-1',
      ticketId: 'ticket-1',
      message: ' Please retry now. ',
    })).resolves.toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenLastCalledWith('http://localhost:5295/v1/courses/course-1/support/tickets/ticket-1/messages', {
      method: 'POST',
      body: JSON.stringify({ message: 'Please retry now.' }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });

    await expect(resolveCourseSupportTicket({
      courseId: 'course-1',
      ticketId: 'ticket-1',
      summary: ' Entitlement refreshed. ',
    })).resolves.toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenLastCalledWith('http://localhost:5295/v1/courses/course-1/support/tickets/ticket-1:resolve', {
      method: 'POST',
      body: JSON.stringify({ summary: 'Entitlement refreshed.' }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
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

  it('updates certificate templates and refreshes the list and editor', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ id: 'template-1', courseId: 'course-1', name: 'Completion' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const result = await updateCertificateTemplate({
      courseId: 'course-1',
      templateId: 'template-1',
      name: ' Completion ',
      description: ' Course credential ',
      templateHtml: '<main>{{recipientName}}</main>',
      templateStyles: ' main { color: navy; } ',
      isDefault: true,
      isActive: true,
    });

    expect(result).toEqual({ success: true, data: null });
    expect(mocks.fetch).toHaveBeenCalledWith('http://localhost:5295/api/certificates/templates/template-1', {
      method: 'PUT',
      body: JSON.stringify({
        name: 'Completion',
        description: 'Course credential',
        templateHtml: '<main>{{recipientName}}</main>',
        templateStyles: 'main { color: navy; }',
        isDefault: true,
        isActive: true,
      }),
      headers: {
        'Content-Type': 'application/json',
        Authorization: 'Bearer access-token',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/certificates');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/learning/courses/course-1/certificates/template-1');
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
