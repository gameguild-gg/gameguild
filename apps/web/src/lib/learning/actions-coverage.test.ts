import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => {
  const apiMethods = [
    'postCoursesContent', 'putCoursesContent', 'deleteCoursesContent', 'postCoursesContentReorder', 'postCoursesContentMove',
    'postCourses', 'putCourses', 'deleteCourses', 'getCoursesForGetCoursesById', 'postCoursesUsersEnroll', 'deleteCoursesUsers',
    'postCoursesPublish', 'postCoursesUnpublish', 'postCoursesRestore', 'postCoursesArchive', 'postCoursesClone',
    'postCoursesDisableMonetization', 'postCoursesMonetize', 'postCoursesStudentsMessage',
    'postCoursesSupportTicketsMessages', 'postCoursesSupportTicketsResolve',
    'postAssessments', 'putAssessments', 'deleteAssessments', 'postAssessmentsRestore',
    'postAssessmentsGroups', 'putAssessmentsGroups', 'deleteAssessmentsGroups',
    'postCoursesGroupSets', 'postCoursesGroupSetsGroups', 'postCoursesGroupSetsGroupsMembers',
    'deleteCoursesGroupSetsGroupsMembers', 'postCoursesGroupSetsGroupsJoin', 'deleteCoursesGroupSetsGroupsMembership',
    'putAssessmentsRubric', 'deleteAssessmentsRubric',
    'postApiLearningEnrollments', 'postApiCertificatesTemplates', 'putApiCertificatesTemplates', 'deleteApiCertificatesTemplates',
    'patchApiSocialReviewsModeration', 'postApiSocialDiscussions', 'deleteApiSocialDiscussions',
    'postApiSocialDiscussionsPin', 'postApiSocialDiscussionsUnpin', 'postApiSocialDiscussionsResolve',
    'postApiSocialDiscussionsReplies', 'postApiSocialRepliesAccept', 'postApiSocialRepliesUpvote', 'getUsersForGetUsers',
  ] as const;
  const methods = Object.fromEntries(apiMethods.map((name) => [name, vi.fn()])) as Record<(typeof apiMethods)[number], ReturnType<typeof vi.fn>>;
  return {
    ...methods,
    getToken: vi.fn(),
    createServerClient: vi.fn(),
    revalidatePath: vi.fn(),
    resolveCourseId: vi.fn(),
    getCourse: vi.fn(),
    getCourseContent: vi.fn(),
    deriveCourseLaunchSummary: vi.fn(),
    createEmptyQuiz: vi.fn(),
  };
});

vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));
vi.mock('@/lib/learning/queries/course', () => ({
  resolveCourseId: mocks.resolveCourseId,
  getCourse: mocks.getCourse,
  getCourseContent: mocks.getCourseContent,
}));
vi.mock('@/lib/learning/course-launch', () => ({ deriveCourseLaunchSummary: mocks.deriveCourseLaunchSummary }));
vi.mock('@game-guild/quiz-content', () => ({ createEmptyQuizContentDocument: mocks.createEmptyQuiz }));
vi.mock('@game-guild/client', () => {
  class ApiModule {
    postCoursesContent = mocks.postCoursesContent;
    putCoursesContent = mocks.putCoursesContent;
    deleteCoursesContent = mocks.deleteCoursesContent;
    postCoursesContentReorder = mocks.postCoursesContentReorder;
    postCoursesContentMove = mocks.postCoursesContentMove;
    postCourses = mocks.postCourses;
    putCourses = mocks.putCourses;
    deleteCourses = mocks.deleteCourses;
    getCoursesForGetCoursesById = mocks.getCoursesForGetCoursesById;
    postCoursesUsersEnroll = mocks.postCoursesUsersEnroll;
    deleteCoursesUsers = mocks.deleteCoursesUsers;
    postCoursesPublish = mocks.postCoursesPublish;
    postCoursesUnpublish = mocks.postCoursesUnpublish;
    postCoursesRestore = mocks.postCoursesRestore;
    postCoursesArchive = mocks.postCoursesArchive;
    postCoursesClone = mocks.postCoursesClone;
    postCoursesDisableMonetization = mocks.postCoursesDisableMonetization;
    postCoursesMonetize = mocks.postCoursesMonetize;
    postCoursesStudentsMessage = mocks.postCoursesStudentsMessage;
    postCoursesSupportTicketsMessages = mocks.postCoursesSupportTicketsMessages;
    postCoursesSupportTicketsResolve = mocks.postCoursesSupportTicketsResolve;
    postAssessments = mocks.postAssessments;
    putAssessments = mocks.putAssessments;
    deleteAssessments = mocks.deleteAssessments;
    postAssessmentsRestore = mocks.postAssessmentsRestore;
    postAssessmentsGroups = mocks.postAssessmentsGroups;
    putAssessmentsGroups = mocks.putAssessmentsGroups;
    deleteAssessmentsGroups = mocks.deleteAssessmentsGroups;
    postCoursesGroupSets = mocks.postCoursesGroupSets;
    postCoursesGroupSetsGroups = mocks.postCoursesGroupSetsGroups;
    postCoursesGroupSetsGroupsMembers = mocks.postCoursesGroupSetsGroupsMembers;
    deleteCoursesGroupSetsGroupsMembers = mocks.deleteCoursesGroupSetsGroupsMembers;
    postCoursesGroupSetsGroupsJoin = mocks.postCoursesGroupSetsGroupsJoin;
    deleteCoursesGroupSetsGroupsMembership = mocks.deleteCoursesGroupSetsGroupsMembership;
    putAssessmentsRubric = mocks.putAssessmentsRubric;
    deleteAssessmentsRubric = mocks.deleteAssessmentsRubric;
    postApiLearningEnrollments = mocks.postApiLearningEnrollments;
    postApiCertificatesTemplates = mocks.postApiCertificatesTemplates;
    putApiCertificatesTemplates = mocks.putApiCertificatesTemplates;
    deleteApiCertificatesTemplates = mocks.deleteApiCertificatesTemplates;
    patchApiSocialReviewsModeration = mocks.patchApiSocialReviewsModeration;
    postApiSocialDiscussions = mocks.postApiSocialDiscussions;
    deleteApiSocialDiscussions = mocks.deleteApiSocialDiscussions;
    postApiSocialDiscussionsPin = mocks.postApiSocialDiscussionsPin;
    postApiSocialDiscussionsUnpin = mocks.postApiSocialDiscussionsUnpin;
    postApiSocialDiscussionsResolve = mocks.postApiSocialDiscussionsResolve;
    postApiSocialDiscussionsReplies = mocks.postApiSocialDiscussionsReplies;
    postApiSocialRepliesAccept = mocks.postApiSocialRepliesAccept;
    postApiSocialRepliesUpvote = mocks.postApiSocialRepliesUpvote;
    getUsersForGetUsers = mocks.getUsersForGetUsers;
  }

  return {
    createServerClient: mocks.createServerClient,
    GeneratedApi: {
      LearningAssessmentsModule: ApiModule,
      LearningCoursesProgramModule: ApiModule,
      LearningCoursesProgramContentModule: ApiModule,
      LearningCoursesProgramLifecycleModule: ApiModule,
      LearningEnrollmentsModule: ApiModule,
      LearningCoursesStudentsModule: ApiModule,
      LearningCoursesSupportTicketsModule: ApiModule,
      LearningCertificatesModule: ApiModule,
      LearningExperienceSocialDiscussionsModule: ApiModule,
      LearningExperienceSocialRepliesModule: ApiModule,
      LearningExperienceSocialReviewsModule: ApiModule,
      UsersModule: ApiModule,
      LearningAssessmentsGroupSetsModule: ApiModule,
      LearningAssessmentsRubricsModule: ApiModule,
    },
  };
});

import * as actions from './actions';

type ServerAction = (...args: readonly unknown[]) => Promise<unknown>;

async function expectApiFailureAndThrow(action: ServerAction, apiMock: ReturnType<typeof vi.fn>, args: readonly unknown[]) {
  apiMock.mockResolvedValueOnce({ ok: false, error: { detail: 'Operation denied' } });
  await expect(action(...args)).resolves.toEqual({ success: false, error: 'Operation denied' });
  apiMock.mockRejectedValueOnce(new Error('Transport failed'));
  await expect(action(...args)).resolves.toEqual({ success: false, error: 'Unexpected error: Transport failed' });
}

describe('learning action coverage', () => {
  beforeEach(() => {
    Object.values(mocks).forEach((mock) => mock.mockReset());
    vi.stubEnv('API_URL', '');
    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    mocks.createServerClient.mockReturnValue({});
    mocks.getToken.mockResolvedValue('token');
    mocks.resolveCourseId.mockImplementation(async (id: string) => id);
    mocks.getCourse.mockResolvedValue({ id: 'course-1', metadata: null });
    mocks.getCourseContent.mockResolvedValue({ items: [], total: 0 });
    mocks.deriveCourseLaunchSummary.mockReturnValue({ blockers: [] });
    mocks.createEmptyQuiz.mockReturnValue({ type: 'quiz', questions: [] });
  });

  it('configures authenticated clients with API URL precedence and revalidates canonical routes', async () => {
    vi.stubEnv('API_URL', 'https://internal.example');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public.example');
    mocks.resolveCourseId.mockResolvedValue('course-id');
    mocks.putCoursesContent.mockResolvedValue({ ok: true, data: {} });
    await actions.updateContent({ courseId: 'course-slug', contentId: 'content-1' });
    let options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('https://internal.example');
    await expect(options.auth.getAccessToken()).resolves.toBe('token');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/workspace/learning/courses/course-id/content');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/console/learning/courses/course-id/overview');

    vi.stubEnv('API_URL', '');
    mocks.putCoursesContent.mockResolvedValue({ ok: true, data: {} });
    await actions.updateContent({ courseId: 'course-id', contentId: 'content-1' });
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('https://public.example');

    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    mocks.putCoursesContent.mockResolvedValue({ ok: true, data: {} });
    await actions.updateContent({ courseId: 'course-id', contentId: 'content-1' });
    options = mocks.createServerClient.mock.calls.at(-1)?.[0];
    expect(options.baseUrl).toBe('http://localhost:8080');
  });

  it('validates content creation and maps all optional content fields', async () => {
    await expect(actions.addContent({ courseId: 'course-1', title: '', type: 'Lesson' })).resolves.toEqual({ success: false, error: 'Title is required.' });
    await expect(actions.addContent({ courseId: 'course-1', title: ' ', type: 'Lesson' })).resolves.toEqual({ success: false, error: 'Title is required.' });

    mocks.postCoursesContent.mockResolvedValueOnce({ ok: true, data: { id: 'questionnaire-1' } });
    await actions.addContent({
      courseId: 'course-1', parentId: 'module-1', title: ' Questionnaire ', description: undefined,
      slug: ' Questionnaire Slug ', type: 'Questionnaire', sortOrder: 3,
    });
    expect(mocks.postCoursesContent).toHaveBeenLastCalledWith('course-1', expect.objectContaining({
      parentId: 'module-1', title: 'Questionnaire', description: '', slug: 'questionnaire-slug',
      jsonBody: { type: 'quiz', questions: [] }, sortOrder: 3,
    }));

    mocks.postCoursesContent.mockResolvedValueOnce({ ok: true, data: { id: 'lesson-1' } });
    await actions.addContent({ courseId: 'course-1', title: 'Lesson', description: ' Text ', slug: '***', type: 'Lesson', lessonFormat: 'Video' });
    expect(mocks.postCoursesContent).toHaveBeenLastCalledWith('course-1', expect.objectContaining({ description: 'Text', lessonFormat: 'Video', sortOrder: 0 }));
    expect(mocks.postCoursesContent.mock.calls.at(-1)?.[1]).not.toHaveProperty('slug');
  });

  it('handles content creation response failures and every unexpected error shape', async () => {
    mocks.postCoursesContent.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(actions.addContent({ courseId: 'course-1', title: 'Lesson', type: 'Lesson' })).resolves.toEqual({
      success: false, error: 'Content was created, but the API did not return its ID.',
    });
    mocks.postCoursesContent.mockResolvedValueOnce({ ok: false, error: { message: 'Create failed' } });
    await expect(actions.addContent({ courseId: 'course-1', title: 'Lesson', type: 'Lesson' })).resolves.toEqual({ success: false, error: 'Create failed' });
    mocks.resolveCourseId.mockRejectedValueOnce(new Error('Error failure'));
    await expect(actions.addContent({ courseId: 'course-1', title: 'Lesson', type: 'Lesson' })).resolves.toEqual({ success: false, error: 'Unexpected error: Error failure' });
    mocks.resolveCourseId.mockRejectedValueOnce('string failure');
    await expect(actions.updateContent({ courseId: 'course-1', contentId: 'content-1' })).resolves.toEqual({ success: false, error: 'Unexpected error: string failure' });
    mocks.resolveCourseId.mockRejectedValueOnce({ reason: 'object failure' });
    await expect(actions.updateContent({ courseId: 'course-1', contentId: 'content-1' })).resolves.toEqual({ success: false, error: 'Unexpected error: {"reason":"object failure"}' });
    const circular: { self?: unknown } = {};
    circular.self = circular;
    mocks.resolveCourseId.mockRejectedValueOnce(circular);
    await expect(actions.updateContent({ courseId: 'course-1', contentId: 'content-1' })).resolves.toEqual({ success: false, error: 'Unexpected error: [object Object]' });
  });

  it('creates code content without bypassing the content-owned assessment workflow', async () => {
    mocks.postCoursesContent.mockResolvedValue({ ok: true, data: { id: 'code-1' } });
    await expect(actions.addContent({ courseId: 'course-1', title: 'Code task', type: 'Code' })).resolves.toEqual({
      success: true,
      data: { id: 'code-1' },
    });
    expect(mocks.postAssessments).not.toHaveBeenCalled();
  });

  it('covers content update, delete, reorder, and move failures and exceptions', async () => {
    await expectApiFailureAndThrow(actions.deleteContent as ServerAction, mocks.deleteCoursesContent, ['course-1', 'content-1']);
    await expectApiFailureAndThrow(actions.updateContent as ServerAction, mocks.putCoursesContent, [{ courseId: 'course-1', contentId: 'content-1', title: 'Updated' }]);
    await expectApiFailureAndThrow(actions.reorderContent as ServerAction, mocks.postCoursesContentReorder, ['course-1', ['one']]);
    await expectApiFailureAndThrow(actions.moveContent as ServerAction, mocks.postCoursesContentMove, ['course-1', 'one', null, 0]);
  });

  it.each([
    [{ title: '', description: 'Long description', slug: 'slug' }, 'Title must be at least 3 characters.'],
    [{ title: 'ab', description: 'Long description', slug: 'slug' }, 'Title must be at least 3 characters.'],
    [{ title: 'Title', description: '', slug: 'slug' }, 'Description must be at least 10 characters.'],
    [{ title: 'Title', description: 'short', slug: 'slug' }, 'Description must be at least 10 characters.'],
    [{ title: 'Title', description: 'Long description', slug: '' }, 'Slug is required.'],
    [{ title: 'Title', description: 'Long description', slug: ' ' }, 'Slug is required.'],
  ])('validates course creation input', async (input, error) => {
    await expect(actions.createCourse(input)).resolves.toEqual({ success: false, error });
  });

  it('creates courses with canonical and fallback slugs', async () => {
    mocks.postCourses
      .mockResolvedValueOnce({ ok: true, data: { id: 'course-1', slug: ' canonical ', creatorId: 'teacher-1' } })
      .mockResolvedValueOnce({ ok: true, data: { id: 'course-2', slug: ' ', creatorId: null } });
    await expect(actions.createCourse({ title: ' Course ', description: ' Long description ', slug: 'requested', passingScore: 80 })).resolves.toMatchObject({
      success: true, data: { id: 'course-1', slug: 'canonical' },
    });
    await expect(actions.createCourse({ title: 'Course', description: 'Long description', slug: 'fallback' })).resolves.toMatchObject({
      success: true, data: { id: 'course-2', slug: 'fallback' },
    });
  });

  it('handles course CRUD and lifecycle failures and exceptions', async () => {
    await expectApiFailureAndThrow(actions.createCourse as ServerAction, mocks.postCourses, [{ title: 'Course', description: 'Long description', slug: 'course' }]);
    await expectApiFailureAndThrow(actions.updateCourse as ServerAction, mocks.putCourses, [{ courseId: 'course-1', title: 'Updated' }]);
    await expectApiFailureAndThrow(actions.unpublishCourse as ServerAction, mocks.postCoursesUnpublish, ['course-1']);
    await expectApiFailureAndThrow(actions.restoreCourse as ServerAction, mocks.postCoursesRestore, ['course-1']);
    await expectApiFailureAndThrow(actions.archiveCourse as ServerAction, mocks.postCoursesArchive, ['course-1']);
    await expectApiFailureAndThrow(actions.deleteCourse as ServerAction, mocks.deleteCourses, ['course-1']);
  });

  it('handles every publication preflight and API result', async () => {
    mocks.getCourse.mockResolvedValueOnce(null);
    await expect(actions.publishCourse('course-1')).resolves.toEqual({ success: false, error: 'Course could not be loaded before publishing.' });
    mocks.getCourse.mockResolvedValueOnce({ id: 'course-1' });
    mocks.deriveCourseLaunchSummary.mockReturnValueOnce({ blockers: ['Add content', 'Set title'] });
    await expect(actions.publishCourse('course-1')).resolves.toEqual({ success: false, error: 'Course cannot be published until readiness is complete: Add content, Set title.' });
    mocks.getCourse.mockResolvedValue({ id: 'course-1' });
    mocks.postCoursesPublish.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(actions.publishCourse('course-1')).resolves.toEqual({ success: false, error: 'An unexpected error occurred.' });
    mocks.resolveCourseId.mockRejectedValueOnce('publish failed');
    await expect(actions.publishCourse('course-1')).resolves.toEqual({ success: false, error: 'Unexpected error: publish failed' });
  });

  it('covers remaining lifecycle and assessment success paths', async () => {
    mocks.postCoursesUnpublish.mockResolvedValue({ ok: true, data: {} });
    mocks.postCoursesArchive.mockResolvedValue({ ok: true, data: {} });
    mocks.deleteCourses.mockResolvedValue({ ok: true, data: {} });
    mocks.deleteAssessments.mockResolvedValue({ ok: true, data: {} });
    mocks.postAssessmentsRestore.mockResolvedValue({ ok: true, data: {} });
    await expect(actions.unpublishCourse('course-1')).resolves.toEqual({ success: true, data: null });
    await expect(actions.archiveCourse('course-1')).resolves.toEqual({ success: true, data: null });
    await expect(actions.deleteCourse('course-1')).resolves.toEqual({ success: true, data: null });
    await expect(actions.deleteAssessment('course-1', 'assessment-1')).resolves.toEqual({ success: true, data: null });
    await expect(actions.restoreAssessment('course-1', 'assessment-1')).resolves.toEqual({ success: true, data: null });
  });

  it('validates ownership transfer and handles user resolution and API failures', async () => {
    await expect(actions.transferCourseOwnership('course-1', ' ')).resolves.toEqual({ success: false, error: 'New owner email, name, or user ID is required.' });
    mocks.getUsersForGetUsers.mockResolvedValueOnce({ ok: false, error: { message: 'Directory failed' } });
    await expect(actions.transferCourseOwnership('course-1', 'person')).resolves.toEqual({ success: false, error: 'Directory failed' });
    mocks.getUsersForGetUsers.mockResolvedValueOnce({ ok: true, data: { items: [] } });
    await expect(actions.transferCourseOwnership('course-1', 'person')).resolves.toEqual({ success: false, error: 'No user matched that email, name, or user ID.' });
    mocks.putCourses.mockResolvedValueOnce({ ok: false, error: { message: 'Transfer denied' } });
    await expect(actions.transferCourseOwnership('course-1', '08691da8-245e-4d9e-b729-83c9023ba061')).resolves.toEqual({ success: false, error: 'Transfer denied' });
    mocks.resolveCourseId.mockRejectedValueOnce(new Error('Transfer failed'));
    await expect(actions.transferCourseOwnership('course-1', 'person')).resolves.toEqual({ success: false, error: 'Unexpected error: Transfer failed' });
  });

  it('resolves enrollment users by email, name, first match, and GUID', async () => {
    mocks.getUsersForGetUsers
      .mockResolvedValueOnce({ ok: true, data: { items: [{ id: 'first' }, { id: 'email', email: 'student@example.com' }] } })
      .mockResolvedValueOnce({ ok: true, data: { items: [{ id: 'name', name: 'Ada' }] } })
      .mockResolvedValueOnce({ ok: true, data: { items: [{ id: 'fallback' }] } });
    mocks.putCourses.mockResolvedValue({ ok: true, data: {} });
    await actions.transferCourseOwnership('course-1', 'STUDENT@EXAMPLE.COM');
    expect(mocks.putCourses).toHaveBeenLastCalledWith('course-1', { creatorId: 'email' });
    await actions.transferCourseOwnership('course-1', 'ada');
    expect(mocks.putCourses).toHaveBeenLastCalledWith('course-1', { creatorId: 'name' });
    await actions.transferCourseOwnership('course-1', 'unknown');
    expect(mocks.putCourses).toHaveBeenLastCalledWith('course-1', { creatorId: 'fallback' });
    await actions.transferCourseOwnership('course-1', '08691da8-245e-4d9e-b729-83c9023ba061');
    expect(mocks.putCourses).toHaveBeenLastCalledWith('course-1', { creatorId: '08691da8-245e-4d9e-b729-83c9023ba061' });
    expect(mocks.getUsersForGetUsers).toHaveBeenCalledWith({ email: 'STUDENT@EXAMPLE.COM', limit: 5 });
    expect(mocks.getUsersForGetUsers).toHaveBeenCalledWith({ q: 'ada', limit: 5 });
  });

  it('handles a directory response with no item collection', async () => {
    mocks.getUsersForGetUsers.mockResolvedValue({ ok: true, data: { items: null } });
    await expect(actions.transferCourseOwnership('course-1', 'unknown')).resolves.toEqual({
      success: false,
      error: 'No user matched that email, name, or user ID.',
    });
  });

  it.each([
    [{ courseId: '', userId: 'user' }, 'Course is required.'],
    [{ courseId: 'course-1', userId: '' }, 'Student email, name, or user ID is required.'],
  ])('validates manual enrollment', async (input, error) => {
    await expect(actions.manualEnrollStudent(input)).resolves.toEqual({ success: false, error });
  });

  it('handles manual enrollment failures, missing user identity, and null enrollment IDs', async () => {
    mocks.postCoursesUsersEnroll.mockResolvedValueOnce({ ok: false, error: { message: 'Enrollment denied' } });
    await expect(actions.manualEnrollStudent({ courseId: 'course-1', userId: 'student' })).resolves.toEqual({ success: false, error: 'Enrollment denied' });
    mocks.postCoursesUsersEnroll.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(actions.manualEnrollStudent({ courseId: 'course-1', userId: 'student' })).resolves.toEqual({ success: false, error: 'Enrollment succeeded without identifying the enrolled student.' });
    mocks.postCoursesUsersEnroll.mockResolvedValueOnce({ ok: true, data: { userId: 'user-1', enrollmentId: null } });
    await expect(actions.manualEnrollStudent({ courseId: 'course-1', userId: 'student', cohortId: ' ' })).resolves.toEqual({ success: true, data: { id: null } });
    mocks.resolveCourseId.mockRejectedValueOnce('enrollment failed');
    await expect(actions.manualEnrollStudent({ courseId: 'course-1', userId: 'student' })).resolves.toEqual({ success: false, error: 'Unexpected error: enrollment failed' });
  });

  it('validates and handles remove-student operations', async () => {
    await expect(actions.removeCourseStudents('course-1', [' ', ''])).resolves.toEqual({ success: false, error: 'Select at least one student to remove.' });
    mocks.deleteCoursesUsers.mockResolvedValueOnce({ ok: true, data: {} }).mockResolvedValueOnce({ ok: false, error: { message: 'Remove failed' } });
    await expect(actions.removeCourseStudents('course-1', [' user-1 ', 'user-1', 'user-2'])).resolves.toEqual({ success: false, error: 'Remove failed' });
    mocks.resolveCourseId.mockRejectedValueOnce(new Error('Roster failed'));
    await expect(actions.removeCourseStudents('course-1', ['user-1'])).resolves.toEqual({ success: false, error: 'Unexpected error: Roster failed' });
  });

  it('validates course messages and handles defaults, API failures, and exceptions', async () => {
    await expect(actions.sendCourseStudentMessage({ courseId: 'course-1', userIds: [], subject: 'Subject', message: 'Message' })).resolves.toEqual({ success: false, error: 'Select at least one student.' });
    await expect(actions.sendCourseStudentMessage({ courseId: 'course-1', userIds: ['user'], subject: 'x', message: 'Message' })).resolves.toEqual({ success: false, error: 'Subject must be at least 3 characters.' });
    await expect(actions.sendCourseStudentMessage({ courseId: 'course-1', userIds: ['user'], subject: 'Subject', message: ' ' })).resolves.toEqual({ success: false, error: 'Message is required.' });
    mocks.postCoursesStudentsMessage.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(actions.sendCourseStudentMessage({ courseId: 'course-1', userIds: [' user ', 'user'], subject: ' Subject ', message: ' Message ' })).resolves.toEqual({ success: true, data: { sent: 0 } });
    await expectApiFailureAndThrow(actions.sendCourseStudentMessage as ServerAction, mocks.postCoursesStudentsMessage, [{ courseId: 'course-1', userIds: ['user'], subject: 'Subject', message: 'Message' }]);
  });

  it('normalizes notification settings and covers metadata parsing outcomes', async () => {
    const input = {
      studentNotifications: { classReminders: [10, 10, -1, 10081, Number.NaN], announcements: true },
      instructorNotifications: { lowRatingThreshold: 9, enrollments: true },
      templates: [
        { id: ' one ', type: ' email ', subject: ' Subject ', enabled: 1 },
        { id: '', type: 'email', subject: 'ignored', enabled: true },
        ...Array.from({ length: 21 }, (_, index) => ({ id: `id-${index}`, type: 'email', subject: 'Subject', enabled: false })),
      ],
    };
    mocks.getCoursesForGetCoursesById
      .mockResolvedValueOnce({ ok: true, data: { metadata: JSON.stringify({ preserved: true }) } })
      .mockResolvedValueOnce({ ok: true, data: { metadata: 'invalid' } })
      .mockResolvedValueOnce({ ok: true, data: { metadata: '[]' } })
      .mockResolvedValueOnce({ ok: true, data: { metadata: 'null' } })
      .mockResolvedValueOnce({ ok: true, data: { metadata: null } });
    mocks.putCourses.mockResolvedValue({ ok: true, data: {} });

    await actions.updateCourseNotificationSettings('course-1', input as never);
    let metadata = JSON.parse(mocks.putCourses.mock.calls.at(-1)?.[1].metadata);
    expect(metadata.preserved).toBe(true);
    expect(metadata.notificationSettings.studentNotifications.classReminders).toEqual([10]);
    expect(metadata.notificationSettings.instructorNotifications.lowRatingThreshold).toBe(5);
    expect(metadata.notificationSettings.templates).toHaveLength(20);
    await actions.updateCourseNotificationSettings('course-1', { ...input, instructorNotifications: { ...input.instructorNotifications, lowRatingThreshold: -2 } } as never);
    metadata = JSON.parse(mocks.putCourses.mock.calls.at(-1)?.[1].metadata);
    expect(metadata.notificationSettings.instructorNotifications.lowRatingThreshold).toBe(1);
    await actions.updateCourseNotificationSettings('course-1', input as never);
    await actions.updateCourseNotificationSettings('course-1', input as never);
    await actions.updateCourseNotificationSettings('course-1', input as never);
  });

  it('returns metadata read, update, and transport failures', async () => {
    const input = { studentNotifications: { classReminders: [] }, instructorNotifications: { lowRatingThreshold: 3 }, templates: [] };
    mocks.getCoursesForGetCoursesById.mockResolvedValueOnce({ ok: false, error: { message: 'Read failed' } });
    await expect(actions.updateCourseNotificationSettings('course-1', input as never)).resolves.toEqual({ success: false, error: 'Read failed' });
    mocks.getCoursesForGetCoursesById.mockResolvedValueOnce({ ok: true, data: { metadata: null } });
    mocks.putCourses.mockResolvedValueOnce({ ok: false, error: { message: 'Write failed' } });
    await expect(actions.updateCourseNotificationSettings('course-1', input as never)).resolves.toEqual({ success: false, error: 'Write failed' });
    mocks.resolveCourseId.mockRejectedValueOnce('Settings failed');
    await expect(actions.updateCourseNotificationSettings('course-1', input as never)).resolves.toEqual({ success: false, error: 'Unexpected error: Settings failed' });
  });

  it('validates and normalizes integration settings', async () => {
    const invalid = { integrations: [], webhooks: [{ id: 'one', url: 'ftp://example.com', events: [], enabled: true }] };
    await expect(actions.updateCourseIntegrationSettings('course-1', invalid as never)).resolves.toEqual({ success: false, error: 'Webhook URLs must use http or https.' });
    await expect(actions.updateCourseIntegrationSettings('course-1', { integrations: [], webhooks: [{ ...invalid.webhooks[0], url: 'not-url' }] } as never)).resolves.toEqual({ success: false, error: 'Webhook URLs must use http or https.' });

    mocks.getCoursesForGetCoursesById.mockResolvedValue({ ok: true, data: { metadata: null } });
    mocks.putCourses.mockResolvedValue({ ok: true, data: {} });
    await actions.updateCourseIntegrationSettings('course-1', {
      integrations: [
        { id: ' one ', name: ' Integration ', enabled: false, status: 'connected' },
        { id: 'two', name: 'Second', enabled: true, status: 'connected' },
        { id: '', name: 'Invalid', enabled: true, status: 'connected' },
        ...Array.from({ length: 21 }, (_, index) => ({ id: `id-${index}`, name: 'Extra', enabled: true, status: 'connected' })),
      ],
      webhooks: [{ id: ' hook ', url: ' https://example.com/hook ', events: [' created ', '', 'created'], enabled: 1 }],
    } as never);
    const metadata = JSON.parse(mocks.putCourses.mock.calls.at(-1)?.[1].metadata);
    expect(metadata.integrationSettings.integrations).toHaveLength(19);
    expect(metadata.integrationSettings.integrations[0]).toMatchObject({ id: 'one', name: 'Integration', enabled: false, status: 'disconnected' });
    expect(metadata.integrationSettings.integrations[1]).toMatchObject({ enabled: true, status: 'connected' });
    expect(metadata.integrationSettings.webhooks[0]).toEqual({ id: 'hook', url: 'https://example.com/hook', events: ['created'], enabled: true });
  });

  it('updates FAQs and landing projects with sanitization and lookup failures', async () => {
    mocks.getCourse.mockResolvedValue({ id: 'course-1', metadata: '{}' });
    mocks.putCourses.mockResolvedValue({ ok: true, data: {} });
    await actions.updateCourseFaq('course-1', [
      { question: ' Question ', answer: ' Answer ', category: ' ' },
      { question: '', answer: 'ignored' },
      ...Array.from({ length: 13 }, (_, index) => ({ question: `Q${index}`, answer: 'A', category: 'Custom' })),
    ]);
    let update = mocks.putCourses.mock.calls.at(-1)?.[1];
    expect(JSON.parse(update.metadata).landingFaq).toHaveLength(12);
    expect(JSON.parse(update.metadata).landingFaq[0]).toEqual({ question: 'Question', answer: 'Answer', category: 'Course details' });

    await actions.updateCourseLandingProjects('course-1', [
      { title: ' Project ', summary: ' Summary ', image: ' ', skills: 'C++; AI\nTesting', deliverable: ' Build ', moduleLabel: '' },
      { title: '', summary: 'invalid', deliverable: 'invalid' },
      { title: 'Array', summary: 'Summary', skills: [' C# ', ''], deliverable: 'Ship', moduleLabel: ' Label ' },
      ...Array.from({ length: 7 }, (_, index) => ({ title: `P${index}`, summary: 'S', deliverable: 'D' })),
    ]);
    update = mocks.putCourses.mock.calls.at(-1)?.[1];
    const projects = JSON.parse(update.metadata).landingProjects;
    expect(projects).toHaveLength(6);
    expect(projects[0]).toMatchObject({ title: 'Project', image: null, skills: ['C++', 'AI', 'Testing'], moduleLabel: 'Project 01' });
    expect(projects[1]).toMatchObject({ skills: ['C#'], moduleLabel: 'Label' });

    mocks.getCourse.mockResolvedValueOnce(null);
    await expect(actions.updateCourseFaq('missing', [])).resolves.toEqual({ success: false, error: 'Course not found.' });
    mocks.getCourse.mockResolvedValueOnce(null);
    await expect(actions.updateCourseLandingProjects('missing', [])).resolves.toEqual({ success: false, error: 'Course not found.' });
    mocks.getCourse.mockRejectedValueOnce(new Error('FAQ failed'));
    await expect(actions.updateCourseFaq('course-1', [])).resolves.toEqual({ success: false, error: 'Unexpected error: FAQ failed' });
    mocks.getCourse.mockRejectedValueOnce('Projects failed');
    await expect(actions.updateCourseLandingProjects('course-1', [])).resolves.toEqual({ success: false, error: 'Unexpected error: Projects failed' });
  });

  it('handles review moderation success, failure, and exceptions', async () => {
    mocks.patchApiSocialReviewsModeration.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(actions.updateCourseReviewModeration('course-1', 'review-1', true, false)).resolves.toEqual({ success: true, data: null });
    mocks.patchApiSocialReviewsModeration.mockResolvedValueOnce({ ok: false, error: { message: 'Review failed' } });
    await expect(actions.updateCourseReviewModeration('course-1', 'review-1', false, false)).resolves.toEqual({ success: false, error: 'Review failed' });
    mocks.patchApiSocialReviewsModeration.mockRejectedValueOnce(new Error('Review transport failed'));
    await expect(actions.updateCourseReviewModeration('course-1', 'review-1', false, false)).resolves.toEqual({ success: false, error: 'Unexpected error: Review transport failed' });
    mocks.patchApiSocialReviewsModeration.mockRejectedValueOnce('Review unavailable');
    await expect(actions.updateCourseReviewModeration('course-1', 'review-1', false, false)).resolves.toEqual({ success: false, error: 'Unexpected error: Review unavailable' });
  });

  it.each([
    [{ isMonetizationEnabled: true, price: Number.NaN }, 'Price must be zero or greater.'],
    [{ isMonetizationEnabled: true, price: -1 }, 'Price must be zero or greater.'],
  ])('validates pricing', async (partial, error) => {
    await expect(actions.updateCoursePricing({ courseId: 'course-1', currency: 'usd', isSubscription: false, ...partial })).resolves.toEqual({ success: false, error });
  });

  it('handles disabled and enabled pricing variants', async () => {
    mocks.postCoursesDisableMonetization.mockResolvedValueOnce({ ok: true, data: {} }).mockResolvedValueOnce({ ok: false, error: { message: 'Disable failed' } });
    await expect(actions.updateCoursePricing({ courseId: 'course-1', isMonetizationEnabled: false, price: 0, currency: 'USD', isSubscription: false })).resolves.toEqual({ success: true, data: null });
    await expect(actions.updateCoursePricing({ courseId: 'course-1', isMonetizationEnabled: false, price: 0, currency: 'USD', isSubscription: false })).resolves.toEqual({ success: false, error: 'Disable failed' });
    mocks.postCoursesMonetize
      .mockResolvedValueOnce({ ok: true, data: {} })
      .mockResolvedValueOnce({ ok: true, data: {} })
      .mockResolvedValueOnce({ ok: false, error: { message: 'Price failed' } });
    await actions.updateCoursePricing({ courseId: 'course-1', isMonetizationEnabled: true, price: 10, currency: ' ', isSubscription: true });
    expect(mocks.postCoursesMonetize).toHaveBeenLastCalledWith('course-1', expect.objectContaining({ currency: 'USD', subscriptionDurationDays: null }));
    await actions.updateCoursePricing({ courseId: 'course-1', isMonetizationEnabled: true, price: 10, currency: ' brl ', isSubscription: false, subscriptionDurationDays: 30 });
    expect(mocks.postCoursesMonetize).toHaveBeenLastCalledWith('course-1', expect.objectContaining({ currency: 'BRL', subscriptionDurationDays: null }));
    await expect(actions.updateCoursePricing({ courseId: 'course-1', isMonetizationEnabled: true, price: 10, currency: 'USD', isSubscription: true, subscriptionDurationDays: 30 })).resolves.toEqual({ success: false, error: 'Price failed' });
    mocks.resolveCourseId.mockRejectedValueOnce(new Error('Pricing failed'));
    await expect(actions.updateCoursePricing({ courseId: 'course-1', isMonetizationEnabled: false, price: 0, currency: 'USD', isSubscription: false })).resolves.toEqual({ success: false, error: 'Unexpected error: Pricing failed' });
  });

  it('handles clone success, API failure, and exception', async () => {
    mocks.postCoursesClone.mockResolvedValueOnce({ ok: true, data: { id: 'clone-1' } });
    await expect(actions.cloneCourse('course-1', 'Clone')).resolves.toEqual({ success: true, data: { id: 'clone-1' } });
    await expectApiFailureAndThrow(actions.cloneCourse as ServerAction, mocks.postCoursesClone, ['course-1', 'Clone']);
  });

  it('validates assessment creation and maps default and explicit fields', async () => {
    await expect(actions.createAssessment({ courseId: 'course-1', title: '', type: 'Quiz' })).resolves.toEqual({ success: false, error: 'Title is required.' });
    await expect(actions.createAssessment({ courseId: 'course-1', title: ' ', type: 'Quiz' })).resolves.toEqual({ success: false, error: 'Title is required.' });
    mocks.postAssessments
      .mockResolvedValueOnce({ ok: true, data: { id: 'assessment-1' } })
      .mockResolvedValueOnce({ ok: true, data: { id: 'assessment-2' } });
    await actions.createAssessment({ courseId: 'course-1', title: ' Quiz ', type: 'Quiz' });
    expect(mocks.postAssessments).toHaveBeenLastCalledWith(expect.objectContaining({
      courseId: 'course-1', title: 'Quiz', description: null, assessmentGroupId: null,
      maxScore: 10_000, passingScore: 7_000, timeLimitMinutes: null,
      maxAttempts: 1, isRequired: true, availableFrom: null, availableUntil: null,
      presentationMode: 'Continuous', contentId: null, reviewMethods: 8, slug: null,
    }));
    await actions.createAssessment({
      courseId: 'course-1', title: 'Assignment', description: ' Description ', type: 'Assignment', assessmentGroupId: 'group-1',
      maxScore: 50, passingScore: 40, timeLimitMinutes: 10, maxAttempts: 2, isRequired: false,
      availableFrom: 'from', availableUntil: 'until', presentationMode: 'SingleStep', contentId: 'content-1',
      submissionModalities: 'Text', reviewMethods: 8, slug: ' My Assessment ',
    });
    expect(mocks.postAssessments).toHaveBeenLastCalledWith(expect.objectContaining({
      description: 'Description', assessmentGroupId: 'group-1', maxScore: 5_000, passingScore: 4_000,
      timeLimitMinutes: 10, maxAttempts: 2, isRequired: false, slug: 'my-assessment',
    }));
  });

  it('handles assessment CRUD API failures and exceptions', async () => {
    await expectApiFailureAndThrow(actions.createAssessment as ServerAction, mocks.postAssessments, [{ courseId: 'course-1', title: 'Assessment', type: 'Assignment' }]);
    await expectApiFailureAndThrow(actions.updateAssessment as ServerAction, mocks.putAssessments, [{ courseId: 'course-1', assessmentId: 'assessment-1' }]);
    await expectApiFailureAndThrow(actions.deleteAssessment as ServerAction, mocks.deleteAssessments, ['course-1', 'assessment-1']);
    await expectApiFailureAndThrow(actions.restoreAssessment as ServerAction, mocks.postAssessmentsRestore, ['course-1', 'assessment-1']);
  });

  it('maps explicit and default assessment updates', async () => {
    mocks.putAssessments.mockResolvedValue({ ok: true, data: { version: 2 } });
    await actions.updateAssessment({ courseId: 'course-1', assessmentId: 'assessment-1', expectedVersion: 1 });
    expect(mocks.putAssessments).toHaveBeenLastCalledWith('assessment-1', expect.objectContaining({
      expectedVersion: 1, title: null, description: null, clearDescription: false,
      maxScore: undefined, passingScore: undefined, timeLimitMinutes: null, clearTimeLimitMinutes: false, maxAttempts: null,
      isRequired: null, contentId: null, clearContentId: false, assessmentGroupId: null, clearAssessmentGroupId: false,
      reviewMethods: undefined, groupSetId: null, clearGroupSetId: false, slug: null,
    }));
    await actions.updateAssessment({
      courseId: 'course-1', assessmentId: 'assessment-1', expectedVersion: 2,
      title: ' Title ', description: ' Description ', maxScore: 20,
      passingScore: 10, timeLimitMinutes: 5, maxAttempts: 1, isRequired: false, availableFrom: 'from', availableUntil: 'until',
      contentId: 'content', clearContentId: true, assessmentGroupId: 'group', clearAssessmentGroupId: true,
      presentationMode: 'Continuous', reviewMethods: 4, groupSetId: 'set', clearGroupSetId: true, slug: ' Slug ',
    });
    expect(mocks.putAssessments).toHaveBeenLastCalledWith('assessment-1', expect.objectContaining({
      expectedVersion: 2, title: 'Title', description: 'Description', maxScore: 2_000,
      passingScore: 1_000, reviewMethods: 4, slug: 'slug',
    }));
  });

  it.each([
    [{ name: '', weightPercent: 10 }, 'Group name is required.'],
    [{ name: 'Group', weightPercent: Number.NaN }, 'Weight must be between 0 and 100.'],
    [{ name: 'Group', weightPercent: -1 }, 'Weight must be between 0 and 100.'],
    [{ name: 'Group', weightPercent: 101 }, 'Weight must be between 0 and 100.'],
  ])('validates assessment group creation', async (partial, error) => {
    await expect(actions.createAssessmentGroup({ courseId: 'course-1', ...partial })).resolves.toEqual({ success: false, error });
  });

  it('covers assessment group defaults, required IDs, API failures, and exceptions', async () => {
    mocks.postAssessmentsGroups.mockResolvedValueOnce({ ok: true, data: { id: 'group-1' } });
    await actions.createAssessmentGroup({ courseId: 'course-1', name: ' Group ', weightPercent: 50 });
    expect(mocks.postAssessmentsGroups).toHaveBeenLastCalledWith({ courseId: 'course-1', name: 'Group', weightPercent: 5_000, order: 0, description: null });
    await expectApiFailureAndThrow(actions.createAssessmentGroup as ServerAction, mocks.postAssessmentsGroups, [{ courseId: 'course-1', name: 'Group', weightPercent: 50, order: 2, description: ' Description ' }]);
    await expect(actions.updateAssessmentGroup({ courseId: 'course-1', groupId: ' ', name: 'Group', weightPercent: 50 })).resolves.toEqual({ success: false, error: 'Group id is required.' });
    mocks.putAssessmentsGroups.mockResolvedValueOnce({ ok: true, data: { id: 'group-1' } });
    await actions.updateAssessmentGroup({ courseId: 'course-1', groupId: 'group-1', name: ' Group ', weightPercent: 50 });
    expect(mocks.putAssessmentsGroups).toHaveBeenLastCalledWith('group-1', expect.objectContaining({ order: 0, description: null }));
    await expectApiFailureAndThrow(actions.updateAssessmentGroup as ServerAction, mocks.putAssessmentsGroups, [{ courseId: 'course-1', groupId: 'group-1', name: 'Group', weightPercent: 50 }]);
    await expect(actions.deleteAssessmentGroup('course-1', ' ')).resolves.toEqual({ success: false, error: 'Group id is required.' });
    await expectApiFailureAndThrow(actions.deleteAssessmentGroup as ServerAction, mocks.deleteAssessmentsGroups, ['course-1', 'group-1']);
  });

  it('covers group-set validation and all group membership operations', async () => {
    await expect(actions.createGroupSet('course-1', ' ')).resolves.toEqual({ success: false, error: 'Group set name is required.' });
    await expect(actions.createCourseGroup({ courseId: 'course-1', setId: 'set', name: ' ', capacity: 2 })).resolves.toEqual({ success: false, error: 'Group name is required.' });
    await expect(actions.createCourseGroup({ courseId: 'course-1', setId: 'set', name: 'Group', capacity: 1.5 })).resolves.toEqual({ success: false, error: 'Capacity must be at least 2.' });
    await expect(actions.createCourseGroup({ courseId: 'course-1', setId: 'set', name: 'Group', capacity: 1 })).resolves.toEqual({ success: false, error: 'Capacity must be at least 2.' });
    await expect(actions.addGroupMember({ courseId: 'course-1', groupId: 'group', userReference: ' ' })).resolves.toEqual({ success: false, error: 'User email or ID is required.' });

    const cases: Array<[ServerAction, ReturnType<typeof vi.fn>, readonly unknown[]]> = [
      [actions.createGroupSet as ServerAction, mocks.postCoursesGroupSets, ['course-1', 'Set']],
      [actions.createCourseGroup as ServerAction, mocks.postCoursesGroupSetsGroups, [{ courseId: 'course-1', setId: 'set', name: 'Group', capacity: 2 }]],
      [actions.addGroupMember as ServerAction, mocks.postCoursesGroupSetsGroupsMembers, [{ courseId: 'course-1', groupId: 'group', userReference: '08691da8-245e-4d9e-b729-83c9023ba061' }]],
      [actions.removeGroupMember as ServerAction, mocks.deleteCoursesGroupSetsGroupsMembers, [{ courseId: 'course-1', groupId: 'group', userId: 'user' }]],
      [actions.joinGroup as ServerAction, mocks.postCoursesGroupSetsGroupsJoin, ['course-1', 'group']],
      [actions.leaveGroup as ServerAction, mocks.deleteCoursesGroupSetsGroupsMembership, ['course-1', 'group']],
    ];
    for (const [action, apiMock, args] of cases) {
      apiMock.mockResolvedValueOnce({ ok: true, data: {} });
      await expect(action(...args)).resolves.toEqual({ success: true, data: null });
      await expectApiFailureAndThrow(action, apiMock, args);
    }

    mocks.getUsersForGetUsers.mockResolvedValue({ ok: false, error: { message: 'No user' } });
    await expect(actions.addGroupMember({ courseId: 'course-1', groupId: 'group', userReference: 'unknown' })).resolves.toEqual({ success: false, error: 'No user' });
  });

  it('covers rubric success, failure, exceptions, and criterion mapping', async () => {
    mocks.putAssessmentsRubric.mockResolvedValueOnce({ ok: true, data: {} });
    await actions.saveRubric({ assessmentId: 'assessment-1', title: 'Rubric', criteria: [{ description: 'Quality', points: 10, order: 1 }] });
    expect(mocks.putAssessmentsRubric).toHaveBeenCalledWith('assessment-1', { title: 'Rubric', criteria: [{ description: 'Quality', points: 1_000, order: 1 }] });
    await expectApiFailureAndThrow(actions.saveRubric as ServerAction, mocks.putAssessmentsRubric, [{ assessmentId: 'assessment-1', title: 'Rubric', criteria: [] }]);
    mocks.deleteAssessmentsRubric.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(actions.deleteRubric('assessment-1')).resolves.toEqual({ success: true, data: null });
    await expectApiFailureAndThrow(actions.deleteRubric as ServerAction, mocks.deleteAssessmentsRubric, ['assessment-1']);
  });

  it('validates certificate templates and covers API failures and exceptions', async () => {
    await expect(actions.createCertificateTemplate({ courseId: 'course-1', name: 'x' })).resolves.toEqual({ success: false, error: 'Template name must be at least 3 characters.' });
    await expect(actions.createCertificateTemplate({ courseId: 'course-1', name: 'Name', templateHtml: 'short' })).resolves.toEqual({ success: false, error: 'Template HTML must be at least 20 characters.' });
    mocks.postApiCertificatesTemplates.mockResolvedValueOnce({ ok: true, data: { id: 'template-1' } });
    await actions.createCertificateTemplate({ courseId: 'course-1', name: ' Name ' });
    expect(mocks.postApiCertificatesTemplates.mock.calls.at(-1)?.[0].templateHtml).toContain('{{recipientName}}');
    await expectApiFailureAndThrow(actions.createCertificateTemplate as ServerAction, mocks.postApiCertificatesTemplates, [{ courseId: 'course-1', name: 'Name' }]);

    const update = { courseId: 'course-1', templateId: 'template-1', name: 'Name', templateHtml: '<section>Long enough template</section>', isDefault: false, isActive: true };
    await expect(actions.updateCertificateTemplate({ ...update, name: 'x' })).resolves.toEqual({ success: false, error: 'Template name must be at least 3 characters.' });
    await expect(actions.updateCertificateTemplate({ ...update, templateHtml: 'short' })).resolves.toEqual({ success: false, error: 'Template HTML must be at least 20 characters.' });
    mocks.putApiCertificatesTemplates.mockResolvedValueOnce({ ok: true, data: {} });
    await actions.updateCertificateTemplate({ ...update, description: ' ', templateStyles: ' ' });
    expect(mocks.putApiCertificatesTemplates).toHaveBeenLastCalledWith('template-1', expect.objectContaining({ description: null, templateStyles: null }));
    await expectApiFailureAndThrow(actions.updateCertificateTemplate as ServerAction, mocks.putApiCertificatesTemplates, [update]);
    mocks.deleteApiCertificatesTemplates.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(actions.deleteCertificateTemplate('course-1', 'template-1')).resolves.toEqual({ success: true, data: null });
    await expectApiFailureAndThrow(actions.deleteCertificateTemplate as ServerAction, mocks.deleteApiCertificatesTemplates, ['course-1', 'template-1']);
  });

  it('validates support and discussion inputs', async () => {
    await expect(actions.addCourseSupportTicketMessage({ courseId: 'course-1', ticketId: 'ticket', message: ' ' })).resolves.toEqual({ success: false, error: 'Reply is required.' });
    await expect(actions.resolveCourseSupportTicket({ courseId: 'course-1', ticketId: 'ticket', summary: 'x' })).resolves.toEqual({ success: false, error: 'Resolution summary must be at least 3 characters.' });
    await expect(actions.createCourseDiscussion({ courseId: 'course-1', title: 'x', content: 'Long enough content' })).resolves.toEqual({ success: false, error: 'Discussion title must be at least 5 characters.' });
    await expect(actions.createCourseDiscussion({ courseId: 'course-1', title: 'Title', content: 'short' })).resolves.toEqual({ success: false, error: 'Discussion content must be at least 10 characters.' });
    await expect(actions.createDiscussionReply({ courseId: 'course-1', discussionId: 'thread', content: ' ' })).resolves.toEqual({ success: false, error: 'Reply content is required.' });
  });

  it('covers support and discussion success, API failure, and exceptions', async () => {
    const cases: Array<[ServerAction, ReturnType<typeof vi.fn>, readonly unknown[], unknown]> = [
      [actions.addCourseSupportTicketMessage as ServerAction, mocks.postCoursesSupportTicketsMessages, [{ courseId: 'course-1', ticketId: 'ticket', message: ' Reply ' }], { success: true, data: null }],
      [actions.resolveCourseSupportTicket as ServerAction, mocks.postCoursesSupportTicketsResolve, [{ courseId: 'course-1', ticketId: 'ticket', summary: ' Done ' }], { success: true, data: null }],
      [actions.createCourseDiscussion as ServerAction, mocks.postApiSocialDiscussions, [{ courseId: 'course-1', title: ' Title ', content: ' Long enough content ', contentId: ' ' }], { success: true, data: { id: 'thread-1' } }],
      [actions.createDiscussionReply as ServerAction, mocks.postApiSocialDiscussionsReplies, [{ courseId: 'course-1', discussionId: 'thread', content: ' Reply ', parentReplyId: ' ' }], { success: true, data: { id: 'reply-1' } }],
      [actions.updateDiscussionPin as ServerAction, mocks.postApiSocialDiscussionsPin, ['course-1', 'thread', true], { success: true, data: null }],
      [actions.updateDiscussionPin as ServerAction, mocks.postApiSocialDiscussionsUnpin, ['course-1', 'thread', false], { success: true, data: null }],
      [actions.resolveDiscussion as ServerAction, mocks.postApiSocialDiscussionsResolve, ['course-1', 'thread'], { success: true, data: null }],
      [actions.deleteDiscussion as ServerAction, mocks.deleteApiSocialDiscussions, ['course-1', 'thread'], { success: true, data: null }],
      [actions.acceptDiscussionReply as ServerAction, mocks.postApiSocialRepliesAccept, ['course-1', 'thread', 'reply'], { success: true, data: null }],
      [actions.upvoteDiscussionReply as ServerAction, mocks.postApiSocialRepliesUpvote, ['course-1', 'thread', 'reply'], { success: true, data: null }],
    ];
    for (const [action, apiMock, args, success] of cases) {
      const data = success && typeof success === 'object' && 'data' in success && success.data && typeof success.data === 'object' && 'id' in success.data
        ? success.data
        : {};
      apiMock.mockResolvedValueOnce({ ok: true, data });
      await expect(action(...args)).resolves.toEqual(success);
      await expectApiFailureAndThrow(action, apiMock, args);
    }
  });

  it('revalidates only support collection routes when no discussion identifier is available', async () => {
    mocks.postCoursesSupportTicketsMessages.mockResolvedValue({ ok: true, data: {} });
    await actions.addCourseSupportTicketMessage({ courseId: 'course-1', ticketId: '', message: 'Reply' });
    expect(mocks.revalidatePath).toHaveBeenCalledTimes(3);
  });

  it('formats Error exceptions at publication, enrollment, and metadata boundaries', async () => {
    mocks.postCoursesPublish.mockRejectedValueOnce(new Error('Publish transport failed'));
    await expect(actions.publishCourse('course-1')).resolves.toEqual({ success: false, error: 'Unexpected error: Publish transport failed' });

    mocks.postCoursesUsersEnroll.mockRejectedValueOnce(new Error('Enrollment transport failed'));
    await expect(actions.manualEnrollStudent({ courseId: 'course-1', userId: 'student' })).resolves.toEqual({ success: false, error: 'Unexpected error: Enrollment transport failed' });

    const notificationInput = {
      studentNotifications: { classReminders: [] },
      instructorNotifications: { lowRatingThreshold: 3 },
      templates: [],
    };
    mocks.getCoursesForGetCoursesById.mockRejectedValueOnce(new Error('Metadata transport failed'));
    await expect(actions.updateCourseNotificationSettings('course-1', notificationInput as never)).resolves.toEqual({ success: false, error: 'Unexpected error: Metadata transport failed' });
  });

  it('formats non-Error exceptions consistently at every server action boundary', async () => {
    const notificationInput = {
      studentNotifications: { classReminders: [] },
      instructorNotifications: { lowRatingThreshold: 3 },
      templates: [],
    };
    const integrationInput = { integrations: [], webhooks: [] };
    const certificateUpdate = {
      courseId: 'course-1', templateId: 'template-1', name: 'Template',
      templateHtml: '<section>Long enough template</section>', isDefault: false, isActive: true,
    };
    const cases: Array<[ServerAction, ReturnType<typeof vi.fn>, readonly unknown[]]> = [
      [actions.deleteContent as ServerAction, mocks.deleteCoursesContent, ['course-1', 'content-1']],
      [actions.reorderContent as ServerAction, mocks.postCoursesContentReorder, ['course-1', ['content-1']]],
      [actions.moveContent as ServerAction, mocks.postCoursesContentMove, ['course-1', 'content-1', null, 0]],
      [actions.createCourse as ServerAction, mocks.postCourses, [{ title: 'Course', description: 'Long description', slug: 'course' }]],
      [actions.updateCourse as ServerAction, mocks.putCourses, [{ courseId: 'course-1', title: 'Course' }]],
      [actions.publishCourse as ServerAction, mocks.postCoursesPublish, ['course-1']],
      [actions.unpublishCourse as ServerAction, mocks.postCoursesUnpublish, ['course-1']],
      [actions.restoreCourse as ServerAction, mocks.postCoursesRestore, ['course-1']],
      [actions.transferCourseOwnership as ServerAction, mocks.putCourses, ['course-1', '08691da8-245e-4d9e-b729-83c9023ba061']],
      [actions.archiveCourse as ServerAction, mocks.postCoursesArchive, ['course-1']],
      [actions.deleteCourse as ServerAction, mocks.deleteCourses, ['course-1']],
      [actions.manualEnrollStudent as ServerAction, mocks.postCoursesUsersEnroll, [{ courseId: 'course-1', userId: 'student' }]],
      [actions.removeCourseStudents as ServerAction, mocks.deleteCoursesUsers, ['course-1', ['user-1']]],
      [actions.sendCourseStudentMessage as ServerAction, mocks.postCoursesStudentsMessage, [{ courseId: 'course-1', userIds: ['user-1'], subject: 'Subject', message: 'Message' }]],
      [actions.updateCourseNotificationSettings as ServerAction, mocks.getCoursesForGetCoursesById, ['course-1', notificationInput]],
      [actions.updateCourseIntegrationSettings as ServerAction, mocks.getCoursesForGetCoursesById, ['course-1', integrationInput]],
      [actions.updateCoursePricing as ServerAction, mocks.postCoursesDisableMonetization, [{ courseId: 'course-1', isMonetizationEnabled: false, price: 0, currency: 'USD', isSubscription: false }]],
      [actions.cloneCourse as ServerAction, mocks.postCoursesClone, ['course-1', 'Clone']],
      [actions.createAssessment as ServerAction, mocks.postAssessments, [{ courseId: 'course-1', title: 'Assessment', type: 'Assignment' }]],
      [actions.createAssessmentGroup as ServerAction, mocks.postAssessmentsGroups, [{ courseId: 'course-1', name: 'Group', weightPercent: 50 }]],
      [actions.updateAssessmentGroup as ServerAction, mocks.putAssessmentsGroups, [{ courseId: 'course-1', groupId: 'group-1', name: 'Group', weightPercent: 50 }]],
      [actions.deleteAssessmentGroup as ServerAction, mocks.deleteAssessmentsGroups, ['course-1', 'group-1']],
      [actions.updateAssessment as ServerAction, mocks.putAssessments, [{ courseId: 'course-1', assessmentId: 'assessment-1' }]],
      [actions.deleteAssessment as ServerAction, mocks.deleteAssessments, ['course-1', 'assessment-1']],
      [actions.restoreAssessment as ServerAction, mocks.postAssessmentsRestore, ['course-1', 'assessment-1']],
      [actions.createGroupSet as ServerAction, mocks.postCoursesGroupSets, ['course-1', 'Set']],
      [actions.createCourseGroup as ServerAction, mocks.postCoursesGroupSetsGroups, [{ courseId: 'course-1', setId: 'set', name: 'Group', capacity: 2 }]],
      [actions.addGroupMember as ServerAction, mocks.postCoursesGroupSetsGroupsMembers, [{ courseId: 'course-1', groupId: 'group', userReference: '08691da8-245e-4d9e-b729-83c9023ba061' }]],
      [actions.removeGroupMember as ServerAction, mocks.deleteCoursesGroupSetsGroupsMembers, [{ courseId: 'course-1', groupId: 'group', userId: 'user-1' }]],
      [actions.joinGroup as ServerAction, mocks.postCoursesGroupSetsGroupsJoin, ['course-1', 'group']],
      [actions.leaveGroup as ServerAction, mocks.deleteCoursesGroupSetsGroupsMembership, ['course-1', 'group']],
      [actions.saveRubric as ServerAction, mocks.putAssessmentsRubric, [{ assessmentId: 'assessment-1', title: 'Rubric', criteria: [] }]],
      [actions.deleteRubric as ServerAction, mocks.deleteAssessmentsRubric, ['assessment-1']],
      [actions.createCertificateTemplate as ServerAction, mocks.postApiCertificatesTemplates, [{ courseId: 'course-1', name: 'Template' }]],
      [actions.updateCertificateTemplate as ServerAction, mocks.putApiCertificatesTemplates, [certificateUpdate]],
      [actions.deleteCertificateTemplate as ServerAction, mocks.deleteApiCertificatesTemplates, ['course-1', 'template-1']],
      [actions.addCourseSupportTicketMessage as ServerAction, mocks.postCoursesSupportTicketsMessages, [{ courseId: 'course-1', ticketId: 'ticket', message: 'Reply' }]],
      [actions.resolveCourseSupportTicket as ServerAction, mocks.postCoursesSupportTicketsResolve, [{ courseId: 'course-1', ticketId: 'ticket', summary: 'Done' }]],
      [actions.createCourseDiscussion as ServerAction, mocks.postApiSocialDiscussions, [{ courseId: 'course-1', title: 'Title', content: 'Long enough content' }]],
      [actions.createDiscussionReply as ServerAction, mocks.postApiSocialDiscussionsReplies, [{ courseId: 'course-1', discussionId: 'thread', content: 'Reply' }]],
      [actions.updateDiscussionPin as ServerAction, mocks.postApiSocialDiscussionsPin, ['course-1', 'thread', true]],
      [actions.resolveDiscussion as ServerAction, mocks.postApiSocialDiscussionsResolve, ['course-1', 'thread']],
      [actions.deleteDiscussion as ServerAction, mocks.deleteApiSocialDiscussions, ['course-1', 'thread']],
      [actions.acceptDiscussionReply as ServerAction, mocks.postApiSocialRepliesAccept, ['course-1', 'thread', 'reply']],
      [actions.upvoteDiscussionReply as ServerAction, mocks.postApiSocialRepliesUpvote, ['course-1', 'thread', 'reply']],
    ];

    for (const [action, apiMock, args] of cases) {
      apiMock.mockRejectedValueOnce('String failure');
      await expect(action(...args)).resolves.toEqual({ success: false, error: 'Unexpected error: String failure' });
    }

    mocks.getCourse.mockRejectedValueOnce('FAQ failure');
    await expect(actions.updateCourseFaq('course-1', [])).resolves.toEqual({ success: false, error: 'Unexpected error: FAQ failure' });
    mocks.getCourse.mockRejectedValueOnce(new Error('Project failure'));
    await expect(actions.updateCourseLandingProjects('course-1', [])).resolves.toEqual({ success: false, error: 'Unexpected error: Project failure' });
  });

  it('fetches courses through the server query wrapper', async () => {
    mocks.getCourse.mockResolvedValue({ id: 'course-1' });
    await expect(actions.fetchCourse('course-1')).resolves.toEqual({ id: 'course-1' });
    expect(mocks.getCourse).toHaveBeenCalledWith('course-1');
  });
});
