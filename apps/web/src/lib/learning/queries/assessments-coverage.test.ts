import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  createServerClient: vi.fn(), getToken: vi.fn(), resolveCourseId: vi.fn(), request: vi.fn(),
  getCourse: vi.fn(), getOne: vi.fn(), getGroups: vi.fn(), getAnalytics: vi.fn(), getDefinition: vi.fn(), getSubmissions: vi.fn(),
  getTemplates: vi.fn(), getIssued: vi.fn(), getTemplate: vi.fn(),
  getGroupSets: vi.fn(), getGroupSetGroups: vi.fn(), getRubric: vi.fn(),
}));

vi.mock('react', () => ({ cache: <T extends (...args: never[]) => unknown>(callback: T) => callback }));
vi.mock('@/auth', () => ({ getToken: mocks.getToken }));
vi.mock('./course', () => ({ resolveCourseId: mocks.resolveCourseId }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    LearningAssessmentsModule: class {
      getAssessmentsCourse = mocks.getCourse;
      getAssessments = mocks.getOne;
      getAssessmentsCourseGroups = mocks.getGroups;
      getAssessmentsCourseAnalytics = mocks.getAnalytics;
      getAssessmentsDefinition = mocks.getDefinition;
      getAssessmentsSubmissionsForGetAssessmentsByAssessmentIdSubmissions = mocks.getSubmissions;
    },
    LearningCertificatesModule: class {
      getApiCertificatesTemplatesCourse = mocks.getTemplates;
      getApiCertificatesCourse = mocks.getIssued;
      getApiCertificatesTemplates = mocks.getTemplate;
    },
    LearningAssessmentsGroupSetsModule: class {
      getCoursesGroupSets = mocks.getGroupSets;
      getCoursesGroupSetsGroups = mocks.getGroupSetGroups;
    },
    LearningAssessmentsRubricsModule: class {
      getAssessmentsRubric = mocks.getRubric;
    },
  },
}));

import {
  getAssessment,
  getAssessmentDefinition,
  getAssessmentRubric,
  getAssessmentSubmissions,
  getCertificateTemplate,
  getCodingDefinitionPublic,
  getCourseAssessmentAnalytics,
  getCourseAssessmentGroups,
  getCourseAssessments,
  getCourseCertificates,
  getCourseGroupSets,
  getCourseGroupSetViews,
  getGroupSetGroups,
} from './assessments';

const guid = '123e4567-e89b-42d3-a456-426614174000';

describe('assessment query coverage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubEnv('API_URL', 'https://api.gameguild.test');
    vi.stubEnv('NEXT_PUBLIC_API_URL', 'https://public-api.gameguild.test');
    mocks.createServerClient.mockReturnValue({ request: mocks.request });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.resolveCourseId.mockResolvedValue('course-1');
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllEnvs();
  });

  it('maps every assessment type and both explicit and default fields', async () => {
    const full = {
      id: 'assessment-1', slug: 'quiz-1', courseId: 'course-1', contentId: 'content-1',
      assessmentGroupId: 'group-1', assessmentGroupName: 'Quizzes', assessmentGroupWeightPercent: 30,
      assessmentGroupOrder: 2, title: 'Quiz', description: 'Description', type: 'Exam', maxScore: 50,
      passingScore: 35, timeLimitMinutes: 20, maxAttempts: 2, isRequired: false, order: 3,
      availableFrom: '2026-01-01', availableUntil: '2026-02-01', presentationMode: 'Continuous',
      dueAt: '2026-02-01', allowLateSubmissions: true, lateSubmissionDeadline: '2026-02-02',
      isAvailable: false, gradingMethods: 'AutoGraded', groupSetId: 'set-1', peerReviewsRequiredCount: 2,
    };
    const types = ['Quiz', 'Assignment', 'Project', 'PeerReview', 'SelfAssessment'];
    mocks.getCourse.mockResolvedValue({
      ok: true,
      data: [
        full,
        ...types.map((type, index) => ({ id: `type-${index}`, type })),
        { id: 'legacy-id', slug: null, type: 'Unknown' },
        { id: null, slug: null, courseId: null, type: null },
      ],
    });

    const result = await getCourseAssessments('course-slug');
    expect(mocks.resolveCourseId).toHaveBeenCalledWith('course-slug');
    expect(mocks.getCourse).toHaveBeenCalledWith('course-1');
    expect(result.total).toBe(8);
    expect(result.assessments[0]).toEqual({
      ...full,
      type: 'Quiz',
    });
    expect(result.assessments.slice(1, 6).map((item) => item.type)).toEqual(types);
    expect(result.assessments[6]).toMatchObject({ id: 'legacy-id', slug: 'legacy-id', type: 'Quiz' });
    expect(result.assessments[7]).toEqual({
      id: '', slug: '', courseId: '', contentId: null, assessmentGroupId: null, assessmentGroupName: null,
      assessmentGroupWeightPercent: null, assessmentGroupOrder: null, title: '', description: null,
      type: 'Quiz', maxScore: 100, passingScore: 70, timeLimitMinutes: null, maxAttempts: null,
      isRequired: true, order: 0, availableFrom: null, availableUntil: null, presentationMode: 'SingleStep',
      dueAt: null, allowLateSubmissions: false, lateSubmissionDeadline: null, isAvailable: true,
      gradingMethods: '', groupSetId: null, peerReviewsRequiredCount: 0,
    });
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'https://api.gameguild.test', auth: { getAccessToken: expect.any(Function) },
    });
    await expect(mocks.createServerClient.mock.calls[0]![0].auth.getAccessToken()).resolves.toBe('access-token');
  });

  it('handles absent, rejected, and failed course assessment responses', async () => {
    mocks.getCourse.mockResolvedValueOnce({ ok: true, data: null });
    await expect(getCourseAssessments('course-1')).resolves.toEqual({ assessments: [], total: 0 });
    mocks.getCourse.mockResolvedValueOnce({ ok: false, error: { message: 'denied' } });
    await expect(getCourseAssessments('course-1')).resolves.toEqual({ assessments: [], total: 0 });
    mocks.resolveCourseId.mockRejectedValueOnce(new Error('unavailable'));
    await expect(getCourseAssessments('course-1')).resolves.toEqual({ assessments: [], total: 0 });
  });

  it('maps assessment groups and their defaults', async () => {
    mocks.getGroups.mockResolvedValueOnce({ ok: true, data: [
      { id: 'group-1', courseId: 'course-1', name: 'Projects', description: 'Work', weightPercent: 40, order: 1 },
      { id: null, courseId: null, name: null, description: null, weightPercent: null, order: null },
    ] });
    await expect(getCourseAssessmentGroups('course-1')).resolves.toEqual([
      { id: 'group-1', courseId: 'course-1', name: 'Projects', description: 'Work', weightPercent: 40, order: 1 },
      { id: '', courseId: '', name: '', description: null, weightPercent: 0, order: 0 },
    ]);
    mocks.getGroups.mockResolvedValueOnce({ ok: true, data: null });
    await expect(getCourseAssessmentGroups('course-1')).resolves.toEqual([]);
    mocks.getGroups.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCourseAssessmentGroups('course-1')).resolves.toEqual([]);
    mocks.getGroups.mockRejectedValueOnce(new Error('offline'));
    await expect(getCourseAssessmentGroups('course-1')).resolves.toEqual([]);
  });

  it('maps analytics distributions and all missing numeric defaults', async () => {
    mocks.getAnalytics.mockResolvedValueOnce({
      ok: true,
      data: {
        courseId: null, assessmentCount: null, gradedCount: null, ungradedCount: null,
        averagePercent: null, passRate: null,
        distribution: [{ label: null, minPercent: null, maxPercent: null, count: null }],
        groups: [{
          groupId: null, groupName: null, weightPercent: null, assessmentCount: null, gradedCount: null,
          ungradedCount: null, averagePercent: null, passRate: null,
          distribution: [{ label: '90-100', minPercent: 90, maxPercent: 100, count: 2 }],
        }],
      },
    });
    await expect(getCourseAssessmentAnalytics('course-1')).resolves.toEqual({
      courseId: '', assessmentCount: 0, gradedCount: 0, ungradedCount: 0, averagePercent: 0, passRate: 0,
      distribution: [{ label: 'Unscored', minPercent: 0, maxPercent: 0, count: 0 }],
      groups: [{
        groupId: null, groupName: 'Ungrouped', weightPercent: null, assessmentCount: 0, gradedCount: 0,
        ungradedCount: 0, averagePercent: 0, passRate: 0,
        distribution: [{ label: '90-100', minPercent: 90, maxPercent: 100, count: 2 }],
      }],
    });

    mocks.getAnalytics.mockResolvedValueOnce({ ok: true, data: { distribution: null, groups: null } });
    await expect(getCourseAssessmentAnalytics('course-1')).resolves.toMatchObject({ distribution: [], groups: [] });
    mocks.getAnalytics.mockResolvedValueOnce({ ok: true, data: {
      distribution: [], groups: [{ groupName: 'No scores', distribution: null }],
    } });
    await expect(getCourseAssessmentAnalytics('course-1')).resolves.toMatchObject({
      groups: [expect.objectContaining({ groupName: 'No scores', distribution: [] })],
    });
    mocks.getAnalytics.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCourseAssessmentAnalytics('course-1')).resolves.toBeNull();
    mocks.getAnalytics.mockRejectedValueOnce(new Error('offline'));
    await expect(getCourseAssessmentAnalytics('course-1')).resolves.toBeNull();
  });

  it('loads an assessment by GUID or course-local slug', async () => {
    mocks.getOne.mockResolvedValueOnce({ ok: true, data: { id: guid, slug: 'quiz', type: 'Quiz' } });
    await expect(getAssessment('course-1', ` ${guid} `)).resolves.toMatchObject({ id: guid, slug: 'quiz' });
    mocks.getOne.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getAssessment('course-1', guid)).resolves.toBeNull();

    mocks.getCourse.mockResolvedValueOnce({ ok: true, data: [{ id: 'a-1', slug: 'project', type: 'Project' }] });
    await expect(getAssessment('course-1', 'project')).resolves.toMatchObject({ id: 'a-1' });
    mocks.getCourse.mockResolvedValueOnce({ ok: true, data: [] });
    await expect(getAssessment('course-1', 'missing')).resolves.toBeNull();
    mocks.getOne.mockRejectedValueOnce(new Error('offline'));
    await expect(getAssessment('course-1', guid)).resolves.toBeNull();
  });

  it('maps assessment definitions with explicit and fallback payloads', async () => {
    mocks.getDefinition.mockResolvedValueOnce({ ok: true, data: {
      assessmentId: 'assessment-1', definitionSchemaVersion: 3, definition: { blocks: { a: {} } },
    } });
    await expect(getAssessmentDefinition('assessment-1')).resolves.toEqual({
      assessmentId: 'assessment-1', definitionSchemaVersion: 3, definition: { blocks: { a: {} } },
    });
    mocks.getDefinition.mockResolvedValueOnce({ ok: true, data: {} });
    await expect(getAssessmentDefinition('assessment-2')).resolves.toEqual({
      assessmentId: 'assessment-2', definitionSchemaVersion: 1, definition: { order: [], blocks: {} },
    });
    mocks.getDefinition.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getAssessmentDefinition('assessment-3')).resolves.toBeNull();
    mocks.getDefinition.mockRejectedValueOnce(new Error('offline'));
    await expect(getAssessmentDefinition('assessment-4')).resolves.toBeNull();
  });

  it('loads assessment submissions and contains API failures', async () => {
    const submissions = [{ id: 'submission-1' }];
    mocks.getSubmissions.mockResolvedValueOnce({ ok: true, data: submissions });
    await expect(getAssessmentSubmissions('assessment-1')).resolves.toEqual(submissions);
    mocks.getSubmissions.mockResolvedValueOnce({ ok: true, data: null });
    await expect(getAssessmentSubmissions('assessment-1')).resolves.toEqual([]);
    mocks.getSubmissions.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getAssessmentSubmissions('assessment-1')).resolves.toEqual([]);
    mocks.getSubmissions.mockRejectedValueOnce(new Error('offline'));
    await expect(getAssessmentSubmissions('assessment-1')).resolves.toEqual([]);
  });

  it('maps certificate lists across template and issuance result combinations', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-15T12:00:00.000Z'));
    mocks.getTemplates.mockResolvedValueOnce({ ok: true, data: [
      { id: 'active', courseId: 'course-1', name: 'Active', description: 'A', isActive: true, isDefault: true, createdAt: '2026-01-01', updatedAt: '2026-01-02' },
      { id: null, courseId: null, name: null, description: null, isActive: false, isDefault: false, createdAt: null, updatedAt: null },
    ] });
    mocks.getIssued.mockResolvedValueOnce({ ok: true, data: [{ id: 'issued-1' }] });
    const result = await getCourseCertificates('course-1');
    expect(result).toEqual({
      total: 2, issuedCount: 1,
      templates: [
        { id: 'active', courseId: 'course-1', name: 'Active', description: 'A', status: 'active', isDefault: true, issuedCount: 1, createdAt: '2026-01-01', updatedAt: '2026-01-02' },
        { id: '', courseId: '', name: '', description: null, status: 'archived', isDefault: false, issuedCount: 1, createdAt: '2026-09-15T12:00:00.000Z', updatedAt: '2026-09-15T12:00:00.000Z' },
      ],
    });
    vi.useRealTimers();

    mocks.getTemplates.mockResolvedValueOnce({ ok: false, error: {} });
    mocks.getIssued.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCourseCertificates('course-1')).resolves.toEqual({ templates: [], total: 0, issuedCount: 0 });
  });

  it('loads certificate template details and fallbacks', async () => {
    mocks.getTemplate.mockResolvedValueOnce({ ok: true, data: {
      id: null, courseId: null, name: null, isActive: undefined, isDefault: undefined,
      templateHtml: null, templateStyles: null, createdAt: '2026-01-01', updatedAt: null,
    } });
    await expect(getCertificateTemplate('template-1')).resolves.toEqual({
      id: '', courseId: '', name: '', description: null, status: 'active', isDefault: false, issuedCount: 0,
      createdAt: '2026-01-01', updatedAt: '2026-01-01', previewUrl: '/api/certificates/templates/template-1',
      templateHtml: '', templateStyles: null,
    });
    mocks.getTemplate.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCertificateTemplate('missing')).resolves.toBeNull();
  });

  it('loads public coding definitions and contains failures', async () => {
    const definition = { kind: 'code', language: 'cpp', workspaceConfig: {}, testPlan: {}, maxScore: 100, passingScore: 60 };
    mocks.request.mockResolvedValueOnce({ ok: true, data: definition });
    await expect(getCodingDefinitionPublic('assessment-1')).resolves.toEqual(definition);
    expect(mocks.request).toHaveBeenCalledWith({ method: 'GET', path: '/v1.0/assessments/assessment-1/coding-definition/public' });
    mocks.request.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCodingDefinitionPublic('assessment-2')).resolves.toBeNull();
    mocks.request.mockRejectedValueOnce(new Error('offline'));
    await expect(getCodingDefinitionPublic('assessment-3')).resolves.toBeNull();
  });

  it('maps course group sets and nested defaults', async () => {
    mocks.getGroupSets.mockResolvedValueOnce({ ok: true, data: [{
      id: 'set-1', name: 'Teams', groups: [
        { id: 'group-1', name: 'Blue', capacity: 4, memberCount: 3 },
        { id: null, name: null, capacity: null, memberCount: null },
      ],
    }, { id: null, name: null, groups: null }] });
    await expect(getCourseGroupSets('course-1')).resolves.toEqual([
      { id: 'set-1', name: 'Teams', groups: [
        { id: 'group-1', name: 'Blue', capacity: 4, memberCount: 3 },
        { id: '', name: '', capacity: 0, memberCount: 0 },
      ] },
      { id: '', name: '', groups: [] },
    ]);
    mocks.getGroupSets.mockResolvedValueOnce({ ok: true, data: null });
    await expect(getCourseGroupSets('course-1')).resolves.toEqual([]);
    mocks.getGroupSets.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getCourseGroupSets('course-1')).resolves.toEqual([]);
    mocks.getGroupSets.mockRejectedValueOnce(new Error('offline'));
    await expect(getCourseGroupSets('course-1')).resolves.toEqual([]);
  });

  it('maps group details and member identity fallbacks', async () => {
    mocks.getGroupSetGroups.mockResolvedValueOnce({ ok: true, data: [
      { id: 'group-1', name: 'Blue', capacity: 4, memberCount: 2, members: [
        { userId: 'user-1', displayName: 'Ada' }, { userId: 'user-2', displayName: null }, { userId: null, displayName: null },
      ] },
      { id: null, name: null, capacity: null, memberCount: null, members: null },
    ] });
    await expect(getGroupSetGroups('course-1', 'set-1')).resolves.toEqual([
      { id: 'group-1', name: 'Blue', capacity: 4, memberCount: 2, members: [
        { userId: 'user-1', displayName: 'Ada' }, { userId: 'user-2', displayName: 'user-2' }, { userId: '', displayName: '' },
      ] },
      { id: '', name: '', capacity: 0, memberCount: 0, members: [] },
    ]);
    mocks.getGroupSetGroups.mockResolvedValueOnce({ ok: true, data: null });
    await expect(getGroupSetGroups('course-1', 'set-1')).resolves.toEqual([]);
    mocks.getGroupSetGroups.mockResolvedValueOnce({ ok: false, error: {} });
    await expect(getGroupSetGroups('course-1', 'set-1')).resolves.toEqual([]);
    mocks.getGroupSetGroups.mockRejectedValueOnce(new Error('offline'));
    await expect(getGroupSetGroups('course-1', 'set-1')).resolves.toEqual([]);
  });

  it('joins group set summaries with detailed groups', async () => {
    mocks.getGroupSets.mockResolvedValue({ ok: true, data: [{ id: 'set-1', name: 'Teams', groups: [] }] });
    mocks.getGroupSetGroups.mockResolvedValue({ ok: true, data: [{ id: 'group-1', name: 'Blue', members: [] }] });
    await expect(getCourseGroupSetViews('course-1')).resolves.toEqual([
      { id: 'set-1', name: 'Teams', groups: [{ id: 'group-1', name: 'Blue', capacity: 0, memberCount: 0, members: [] }] },
    ]);
  });

  it('maps rubrics, lock conflicts, absence, and failures', async () => {
    mocks.getRubric.mockResolvedValueOnce({ ok: true, data: {
      id: null, title: null, criteria: [
        { description: 'Quality', points: 5, order: 1 },
        { description: null, points: null, order: null },
      ],
    } });
    await expect(getAssessmentRubric('assessment-1')).resolves.toEqual({
      rubric: { id: '', title: '', criteria: [
        { description: 'Quality', points: 5, order: 1 }, { description: '', points: 0, order: 0 },
      ] }, locked: false,
    });
    mocks.getRubric.mockResolvedValueOnce({ ok: true, data: { id: 'rubric-2', title: 'Simple', criteria: null } });
    await expect(getAssessmentRubric('assessment-2')).resolves.toEqual({
      rubric: { id: 'rubric-2', title: 'Simple', criteria: [] }, locked: false,
    });
    mocks.getRubric.mockResolvedValueOnce({ ok: false, error: { status: 409 } });
    await expect(getAssessmentRubric('assessment-3')).resolves.toEqual({ rubric: null, locked: true });
    mocks.getRubric.mockResolvedValueOnce({ ok: false, error: { status: 404 } });
    await expect(getAssessmentRubric('assessment-4')).resolves.toEqual({ rubric: null, locked: false });
    mocks.getRubric.mockResolvedValueOnce({ ok: false, error: undefined });
    await expect(getAssessmentRubric('assessment-5')).resolves.toEqual({ rubric: null, locked: false });
    mocks.getRubric.mockRejectedValueOnce(new Error('offline'));
    await expect(getAssessmentRubric('assessment-6')).resolves.toEqual({ rubric: null, locked: false });
  });

  it('uses public and local API URL fallbacks', async () => {
    mocks.getCourse.mockResolvedValue({ ok: true, data: [] });
    vi.stubEnv('API_URL', '');
    await getCourseAssessments('course-1');
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({ baseUrl: 'https://public-api.gameguild.test' }));
    vi.stubEnv('NEXT_PUBLIC_API_URL', '');
    await getCourseAssessments('course-1');
    expect(mocks.createServerClient).toHaveBeenLastCalledWith(expect.objectContaining({ baseUrl: 'http://localhost:8080' }));
  });
});
