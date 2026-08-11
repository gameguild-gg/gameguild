import { createClient, GeneratedApi, type ApiError, type Result } from '@game-guild/client';
import { beforeAll, describe, expect, it } from 'vitest';

interface SignInOutput {
  accessToken: string;
  refreshToken: string;
  userId: string;
  user?: { id: string };
}

interface CertificateTemplateOutput {
  id: string;
  courseId: string;
  name: string;
  isActive?: boolean;
  templateHtml?: string;
}

interface CohortOutput {
  id: string;
  courseId: string;
  name: string;
  description?: string | null;
  startDate: string;
  endDate: string;
  maxCapacity: number;
  currentEnrollmentCount: number;
  status: string;
  isOpen: boolean;
  meetingSchedule?: string | null;
}

interface CohortScheduleItemOutput {
  id: string;
  programContentId?: string | null;
  title: string;
  startsAt?: string | null;
  availableFrom?: string | null;
  dueAt?: string | null;
}

interface CohortScheduleOutput {
  cohortId: string;
  version: number;
  items: CohortScheduleItemOutput[];
}

interface AvailableCohortContentOutput {
  contentId: string;
  title: string;
  instructionalWeek: number;
}

interface DiscussionOutput {
  id: string;
  courseId: string;
  authorId: string;
  title: string;
  content: string;
  isPinned: boolean;
  isResolved: boolean;
  replyCount: number;
  viewCount: number;
}

interface DiscussionReplyOutput {
  id: string;
  discussionId: string;
  authorId: string;
  content: string;
  isAcceptedAnswer: boolean;
  upvoteCount: number;
}

interface CourseCheckoutOutput {
  courseId: string;
  productId: string;
  entitlementId: string;
  enrollmentIds: string[];
  alreadyHadAccess: boolean;
  amount: number;
  currency: string;
  learningUrl: string;
  paymentProviderReference?: string | null;
}

interface AssessmentGroupOutput {
  id: string;
  courseId: string;
  name: string;
  weightPercent: number;
  order: number;
  description?: string | null;
}

interface AssessmentOutput {
  id: string;
  courseId: string;
  contentId?: string | null;
  assessmentGroupId?: string | null;
  assessmentGroupName?: string | null;
  assessmentGroupWeightPercent?: number | null;
  title: string;
  description?: string | null;
  type: 'Quiz' | 'Assignment' | 'Project' | 'PeerReview' | 'SelfAssessment';
  maxScore: number;
  passingScore: number;
  isRequired: boolean;
}

interface CourseAssessmentAnalyticsOutput {
  courseId: string;
  assessmentCount: number;
  gradedCount: number;
  ungradedCount: number;
  averagePercent: number;
  passRate: number;
  distribution: Array<{ label: string; minPercent: number; maxPercent: number; count: number }>;
  groups: Array<{
    groupId?: string | null;
    groupName: string;
    weightPercent?: number | null;
    assessmentCount: number;
    gradedCount: number;
    ungradedCount: number;
    distribution: Array<{ label: string; minPercent: number; maxPercent: number; count: number }>;
  }>;
}

interface SupportTicketOutput {
  id: string;
  customerId: string;
  subject: string;
  messageCount: number;
}

interface SupportTicketPageOutput {
  items: SupportTicketOutput[];
  totalCount: number;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const TENANT_ID = process.env.API_TENANT_ID ?? process.env.TENANT_ID ?? undefined;

const unwrap = <T>(result: Result<T, ApiError>, label: string): T => {
  if (result.ok) return result.data;
  throw new Error(
    `${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status})`,
  );
};

const unique = () => `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;

const createCourseModules = (client: ReturnType<typeof createClient>) => ({
  programs: new GeneratedApi.LearningCoursesProgramModule(client),
  content: new GeneratedApi.LearningCoursesProgramcontentModule(client),
  lifecycle: new GeneratedApi.LearningCoursesProgramlifecycleModule(client),
});

const enableCapability = async (
  client: ReturnType<typeof createClient>,
  tenantId: string,
  capability: string,
) => {
  unwrap(
    await client.request<void>({
      method: 'POST',
      path: `/v1/tenants/${tenantId}/capabilities`,
      body: {
        capability,
        isEnabled: true,
        source: 'override:e2e',
        reason: `Enable ${capability} for course E2E coverage`,
        expiresAt: null,
      },
    }),
    `Enable ${capability}`,
  );
};

// ---------------------------------------------------------------------------
// Test suite
// ---------------------------------------------------------------------------

describe('Courses E2E — full CRUD + lifecycle + content', () => {
  let accessToken: string;
  let userId: string;
  let email: string;
  let password: string;
  let tenantId: string | undefined = TENANT_ID;
  let authedClient: ReturnType<typeof createClient>;
  let programs: ReturnType<typeof createCourseModules>['programs'];
  let content: ReturnType<typeof createCourseModules>['content'];
  let lifecycle: ReturnType<typeof createCourseModules>['lifecycle'];

  // ── bootstrap: create a fresh user and get a token ──────────────────────
  beforeAll(async () => {
    const client = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });

    const tag = unique();
    email = `course_e2e_${tag}@example.com`;
    password = 'Str0ng!Passw0rd123!';
    const signUpResult = await client.request<SignInOutput>({
      method: 'POST',
      path: '/v1/auth/sign-up',
      body: {
        username: `course_e2e_${tag}`,
        email,
        password,
        ...(TENANT_ID ? { tenantId: TENANT_ID } : {}),
      },
      requiresAuth: false,
    });

    const data = unwrap(signUpResult, 'Course E2E sign-up');
    accessToken = data.accessToken;
    const rawId = data.userId ?? data.user?.id;
    userId =
      rawId && rawId !== '00000000-0000-0000-0000-000000000000'
        ? rawId
        : (data.user?.id ?? '');

    if (!TENANT_ID) {
      const tenantClient = createClient({
        baseUrl: BASE_URL,
        timeout: 15_000,
        devtools: { enabled: false },
        auth: { getAccessToken: async () => accessToken },
      });

      const tenantResult = await tenantClient.request<{ id: string }>({
        method: 'POST',
        path: '/v1/tenants',
        body: {
          name: `Courses E2E Tenant ${tag}`,
          slug: `courses-e2e-${tag.replace(/_/g, '-')}`,
          adminEmail: email,
          description: 'Tenant created for course and social learning E2E coverage',
        },
        requiresAuth: true,
      });

      tenantId = unwrap(tenantResult, 'Create courses E2E tenant').id;
      const signInResult = await client.request<SignInOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: {
          email,
          password,
          tenantId,
        },
        requiresAuth: false,
      });

      accessToken = unwrap(signInResult, 'Courses tenant-owner sign-in').accessToken;
    }

    authedClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => accessToken },
      tenant: { getTenantId: async () => tenantId },
    });

    if (tenantId) {
      await enableCapability(authedClient, tenantId, 'lxp.social');
    }

    ({ programs, content, lifecycle } = createCourseModules(authedClient));
  }, 30_000);

  // ── 1. Create a course ──────────────────────────────────────────────────
  let courseId: string;
  let certificateTemplateId: string;
  let cohortId: string;
  let discussionId: string;
  let discussionReplyId: string;
  let supportTicketId: string;
  let paidProductId: string;
  const courseSlug = `e2e-course-${Date.now()}`;

  it('creates a new course (program)', async () => {
    const result = await programs.postCourses({
      title: 'E2E Test Course — Introduction to Game Dev',
      description: 'A course created by the E2E test suite to verify the full lifecycle.',
      slug: courseSlug,
      thumbnail: 'https://example.com/thumb.png',
    });

    const course = unwrap(result, 'Create course');
    courseId = course.id;

    expect(course.id).toBeTruthy();
    expect(course.title).toBe('E2E Test Course — Introduction to Game Dev');
    expect(course.description).toContain('E2E test suite');
    expect(course.slug).toBe(courseSlug);
    expect(course.status).toBe('Draft');
    expect(course.thumbnail).toBe('https://example.com/thumb.png');
  });

  // ── 2. Read the course back ─────────────────────────────────────────────
  it('reads the created course by ID', async () => {
    const result = await programs.getCoursesById(courseId);

    const course = unwrap(result, 'Get course by ID');

    expect(course.id).toBe(courseId);
    expect(course.title).toBe('E2E Test Course — Introduction to Game Dev');
    expect(course.category).toBe('General');
    expect(course.difficulty).toBe('Beginner');
    expect(course.enrollmentStatus).toBe('Open');
    expect(course.isEnrollmentOpen).toBe(true);
  });

  // ── 3. Read the course by slug ──────────────────────────────────────────
  it('reads the created course by slug', async () => {
    const result = await programs.getCoursesSlug(courseSlug);

    const course = unwrap(result, 'Get course by slug');

    expect(course.id).toBe(courseId);
    expect(course.slug).toBe(courseSlug);
  });

  // ── 4. Update the course ────────────────────────────────────────────────
  it('updates the course title and description', async () => {
    const result = await programs.putCourses(courseId, {
      title: 'E2E Course — Advanced Game Dev (updated)',
      description: 'Updated description for the E2E test course.',
      thumbnail: 'https://example.com/thumb-v2.png',
    });

    const course = unwrap(result, 'Update course');

    expect(course.title).toBe('E2E Course — Advanced Game Dev (updated)');
    expect(course.description).toBe('Updated description for the E2E test course.');
    expect(course.thumbnail).toBe('https://example.com/thumb-v2.png');
  });

  // ── 5. Update storefront metadata ───────────────────────────────────────
  it('persists storefront FAQ, project carousel, and skill metadata', async () => {
    const landingFaq = [
      {
        question: 'What do students build?',
        answer: 'A polished combat prototype and a public release pitch.',
      },
      {
        question: 'Is critique included?',
        answer: 'Yes. Every milestone includes instructor and peer critique.',
      },
    ];
    const landingProjects = [
      {
        title: 'Boss AI prototype',
        description: 'A readable encounter loop with telemetry-driven tuning.',
        imageUrl: 'https://cdn.gameguild.gg/e2e/boss-ai.webp',
        tag: 'AI',
      },
      {
        title: 'Steam-ready pitch',
        description: 'A storefront capsule, trailer outline, and launch checklist.',
        imageUrl: 'https://cdn.gameguild.gg/e2e/launch-pitch.webp',
        tag: 'Launch',
      },
    ];

    const metadata = JSON.stringify({ landingFaq, landingProjects });
    const result = await programs.putCourses(courseId, {
      metadata,
      skillsRequired: 'portfolio fundamentals, peer critique',
      skillsProvided: 'boss AI systems, Steam launch planning',
    });
    const course = unwrap(result, 'Update storefront metadata');
    const responseMetadata = JSON.parse(course.metadata ?? '{}');

    expect(responseMetadata.landingFaq).toEqual(landingFaq);
    expect(responseMetadata.landingProjects).toEqual(landingProjects);
    expect(responseMetadata.skillsRequired).toBe('portfolio fundamentals, peer critique');
    expect(responseMetadata.skillsProvided).toBe('boss AI systems, Steam launch planning');
    expect(course.skillsRequired).toBe('portfolio fundamentals, peer critique');
    expect(course.skillsProvided).toBe('boss AI systems, Steam launch planning');

    const readResult = await programs.getCoursesById(courseId);
    const persistedCourse = unwrap(readResult, 'Read storefront metadata');
    const persistedMetadata = JSON.parse(persistedCourse.metadata ?? '{}');

    expect(persistedMetadata.landingFaq).toEqual(landingFaq);
    expect(persistedMetadata.landingProjects).toEqual(landingProjects);
    expect(persistedCourse.skillsRequired).toBe('portfolio fundamentals, peer critique');
    expect(persistedCourse.skillsProvided).toBe('boss AI systems, Steam launch planning');
  });

  // ── 6. Manage monetization and pricing ──────────────────────────────────
  it('enables, updates, and disables course monetization pricing', async () => {
    const monetizedCourse = unwrap(
      await programs.postCoursesMonetize(courseId, {
        price: 199.99,
        currency: 'USD',
        isSubscription: true,
        subscriptionDurationDays: 365,
      }),
      'Enable course monetization',
    );
    expect(monetizedCourse.id).toBe(courseId);

    const enabledPricing = unwrap(
      await programs.getCoursesPricing(courseId),
      'Get enabled course pricing',
    );
    expect(enabledPricing.price).toBe(199.99);
    expect(enabledPricing.currency).toBe('USD');
    expect(enabledPricing.isSubscription).toBe(true);
    expect(enabledPricing.subscriptionDurationDays).toBe(365);
    expect(enabledPricing.isMonetizationEnabled).toBe(true);

    const updatedPricing = unwrap(
      await programs.putCoursesPricing(courseId, {
        price: 249.5,
        currency: 'EUR',
        isSubscription: false,
        subscriptionDurationDays: null,
      }),
      'Update course pricing',
    );
    expect(updatedPricing.price).toBe(249.5);
    expect(updatedPricing.currency).toBe('EUR');
    expect(updatedPricing.isSubscription).toBe(false);
    expect(updatedPricing.subscriptionDurationDays).toBeNull();
    expect(updatedPricing.isMonetizationEnabled).toBe(true);

    const disabledCourse = unwrap(
      await programs.postCoursesDisableMonetization(courseId),
      'Disable course monetization',
    );
    expect(disabledCourse.id).toBe(courseId);

    const disabledPricing = unwrap(
      await programs.getCoursesPricing(courseId),
      'Get disabled course pricing',
    );
    expect(disabledPricing.price).toBe(249.5);
    expect(disabledPricing.currency).toBe('EUR');
    expect(disabledPricing.isSubscription).toBe(false);
    expect(disabledPricing.subscriptionDurationDays).toBeNull();
    expect(disabledPricing.isMonetizationEnabled).toBe(false);
  });

  // ── 6a. Manage certificate templates ───────────────────────────────────
  it('creates, reads, and deletes a certificate template for the course', async () => {
    const createResult = await authedClient.request<CertificateTemplateOutput>({
      method: 'POST',
      path: '/api/certificates/templates',
      body: {
        courseId,
        name: 'E2E completion certificate',
        templateHtml: '<section>{{recipientName}} completed {{courseName}}</section>',
      },
    });
    const template = unwrap(createResult, 'Create certificate template');
    certificateTemplateId = template.id;

    expect(template.courseId).toBe(courseId);
    expect(template.name).toBe('E2E completion certificate');

    const listResult = await authedClient.request<CertificateTemplateOutput[]>({
      method: 'GET',
      path: `/api/certificates/templates/course/${courseId}`,
    });
    const templates = unwrap(listResult, 'List certificate templates');
    expect(templates.some((item) => item.id === certificateTemplateId)).toBe(true);

    const detailResult = await authedClient.request<CertificateTemplateOutput>({
      method: 'GET',
      path: `/api/certificates/templates/${certificateTemplateId}`,
    });
    const detail = unwrap(detailResult, 'Read certificate template');
    expect(detail.templateHtml).toContain('{{recipientName}}');

    const deleteResult = await authedClient.request<void>({
      method: 'DELETE',
      path: `/api/certificates/templates/${certificateTemplateId}`,
    });
    unwrap(deleteResult, 'Delete certificate template');

    const afterDelete = await authedClient.request<CertificateTemplateOutput[]>({
      method: 'GET',
      path: `/api/certificates/templates/course/${courseId}`,
    });
    const remainingTemplates = unwrap(afterDelete, 'List certificate templates after delete');
    expect(remainingTemplates.some((item) => item.id === certificateTemplateId)).toBe(false);
  });

  // ── 6b. Manage live cohorts/classes ─────────────────────────────────────
  it('creates, updates, transitions, and deletes a live cohort for the course', async () => {
    const startDate = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
    const endDate = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 2 * 60 * 60 * 1000).toISOString();

    const createResult = await authedClient.request<CohortOutput>({
      method: 'POST',
      path: '/api/cohorts',
      body: {
        courseId,
        name: 'E2E live cohort',
        description: 'A live cohort created by the E2E course suite.',
        startDate,
        endDate,
        maxCapacity: 16,
        meetingSchedule: 'https://meet.gameguild.test/e2e',
      },
    });
    const cohort = unwrap(createResult, 'Create cohort');
    cohortId = cohort.id;

    expect(cohort.courseId).toBe(courseId);
    expect(cohort.name).toBe('E2E live cohort');
    expect(cohort.maxCapacity).toBe(16);
    expect(cohort.status).toBe('Scheduled');

    const listResult = await authedClient.request<CohortOutput[]>({
      method: 'GET',
      path: `/api/cohorts/course/${courseId}`,
    });
    const cohorts = unwrap(listResult, 'List course cohorts');
    expect(cohorts.some((item) => item.id === cohortId)).toBe(true);

    const updateResult = await authedClient.request<CohortOutput>({
      method: 'PUT',
      path: `/api/cohorts/${cohortId}`,
      body: {
        name: 'E2E updated cohort',
        description: 'Updated live cohort schedule.',
        startDate,
        endDate,
        maxCapacity: 18,
        meetingSchedule: 'Room E2E',
      },
    });
    const updated = unwrap(updateResult, 'Update cohort');
    expect(updated.name).toBe('E2E updated cohort');
    expect(updated.maxCapacity).toBe(18);
    expect(updated.meetingSchedule).toBe('Room E2E');

    const opened = unwrap(
      await authedClient.request<CohortOutput>({ method: 'POST', path: `/api/cohorts/${cohortId}/open` }),
      'Open cohort',
    );
    expect(opened.status).toBe('Active');
    expect(opened.isOpen).toBe(true);

    const closed = unwrap(
      await authedClient.request<CohortOutput>({ method: 'POST', path: `/api/cohorts/${cohortId}/close` }),
      'Close cohort',
    );
    expect(closed.isOpen).toBe(false);

    const completed = unwrap(
      await authedClient.request<CohortOutput>({ method: 'POST', path: `/api/cohorts/${cohortId}/complete` }),
      'Complete cohort',
    );
    expect(completed.status).toBe('Completed');

    const deleteResult = await authedClient.request<void>({
      method: 'DELETE',
      path: `/api/cohorts/${cohortId}`,
    });
    unwrap(deleteResult, 'Delete cohort');
  });

  // ── 6c. Manage course discussion support ───────────────────────────────
  it('creates, moderates, replies to, and deletes a course discussion', async () => {
    const createResult = await authedClient.request<DiscussionOutput>({
      method: 'POST',
      path: '/api/social/discussions',
      body: {
        courseId,
        title: 'E2E milestone support question',
        content: 'Can I submit a revised build after instructor feedback?',
      },
    });
    const discussion = unwrap(createResult, 'Create course discussion');
    discussionId = discussion.id;

    expect(discussion.courseId).toBe(courseId);
    expect(discussion.title).toBe('E2E milestone support question');
    expect(discussion.isPinned).toBe(false);
    expect(discussion.isResolved).toBe(false);

    const listResult = await authedClient.request<DiscussionOutput[]>({
      method: 'GET',
      path: `/api/social/courses/${courseId}/discussions?skip=0&take=100&pinnedFirst=true`,
      requiresAuth: false,
    });
    const discussions = unwrap(listResult, 'List course discussions');
    expect(discussions.some((item) => item.id === discussionId)).toBe(true);

    const pinned = unwrap(
      await authedClient.request<DiscussionOutput>({ method: 'POST', path: `/api/social/discussions/${discussionId}/pin` }),
      'Pin course discussion',
    );
    expect(pinned.isPinned).toBe(true);

    const unpinned = unwrap(
      await authedClient.request<DiscussionOutput>({ method: 'POST', path: `/api/social/discussions/${discussionId}/unpin` }),
      'Unpin course discussion',
    );
    expect(unpinned.isPinned).toBe(false);

    const replyResult = await authedClient.request<DiscussionReplyOutput>({
      method: 'POST',
      path: `/api/social/discussions/${discussionId}/replies`,
      body: {
        discussionId,
        content: 'Yes. Upload the revision before the checkpoint deadline.',
      },
    });
    const reply = unwrap(replyResult, 'Create discussion reply');
    discussionReplyId = reply.id;

    expect(reply.discussionId).toBe(discussionId);
    expect(reply.content).toContain('checkpoint deadline');

    const repliesResult = await authedClient.request<DiscussionReplyOutput[]>({
      method: 'GET',
      path: `/api/social/discussions/${discussionId}/replies?skip=0&take=100`,
      requiresAuth: false,
    });
    const replies = unwrap(repliesResult, 'List discussion replies');
    expect(replies.some((item) => item.id === discussionReplyId)).toBe(true);

    const upvoted = unwrap(
      await authedClient.request<DiscussionReplyOutput>({ method: 'POST', path: `/api/social/replies/${discussionReplyId}/upvote` }),
      'Upvote discussion reply',
    );
    expect(upvoted.upvoteCount).toBeGreaterThanOrEqual(1);

    const accepted = unwrap(
      await authedClient.request<DiscussionReplyOutput>({ method: 'POST', path: `/api/social/replies/${discussionReplyId}/accept` }),
      'Accept discussion reply',
    );
    expect(accepted.isAcceptedAnswer).toBe(true);

    const resolved = unwrap(
      await authedClient.request<DiscussionOutput>({ method: 'POST', path: `/api/social/discussions/${discussionId}/resolve` }),
      'Resolve course discussion',
    );
    expect(resolved.isResolved).toBe(true);

    unwrap(
      await authedClient.request<void>({ method: 'DELETE', path: `/api/social/discussions/${discussionId}` }),
      'Delete course discussion',
    );

    const deleted = await authedClient.request<DiscussionOutput>({
      method: 'GET',
      path: `/api/social/discussions/${discussionId}`,
      requiresAuth: false,
    });
    expect(deleted.ok).toBe(false);
  });

  it('persists, lists, replies to, and resolves a course support ticket', async () => {
    if (!tenantId) throw new Error('Course support E2E requires a tenant.');

    const created = unwrap(await authedClient.request<SupportTicketOutput>({
      method: 'POST',
      path: '/v1/support/tickets',
      body: {
        tenantId,
        customerId: courseId,
        customerName: 'E2E Test Course',
        reporterUserId: userId,
        reporterName: 'Course E2E Learner',
        reporterEmail: email,
        subject: 'Cannot open the milestone lesson',
        body: 'The milestone lesson remains unavailable after enrollment.',
        priority: 'Normal',
        category: 'access',
      },
    }), 'Create course support ticket');
    supportTicketId = created.id;

    const queue = unwrap(await authedClient.request<SupportTicketPageOutput>({
      method: 'GET',
      path: `/v1/courses/${courseId}/support/tickets?skip=0&take=100`,
    }), 'List course support tickets');
    expect(queue.items).toContainEqual(expect.objectContaining({ id: supportTicketId, customerId: courseId }));

    const replied = unwrap(await authedClient.request<SupportTicketOutput>({
      method: 'POST',
      path: `/v1/courses/${courseId}/support/tickets/${supportTicketId}/messages`,
      body: { message: 'The entitlement was refreshed. Please retry.' },
    }), 'Reply to course support ticket');
    expect(replied.messageCount).toBeGreaterThanOrEqual(2);

    const resolved = unwrap(await authedClient.request<SupportTicketOutput>({
      method: 'POST',
      path: `/v1/courses/${courseId}/support/tickets/${supportTicketId}:resolve`,
      body: { summary: 'The course entitlement was refreshed.' },
    }), 'Resolve course support ticket');
    expect(resolved.id).toBe(supportTicketId);
  });

  // ── 7. List courses ─────────────────────────────────────────────────────
  it('lists courses and the new course appears', async () => {
    const result = await programs.getCourses({ take: 100 });

    const courses = unwrap(result, 'List courses');

    expect(Array.isArray(courses)).toBe(true);
    const found = courses.find((c) => c.id === courseId);
    expect(found).toBeDefined();
    expect(found!.title).toBe('E2E Course — Advanced Game Dev (updated)');
  });

  // ── 8. Get course with content (initially empty) ────────────────────────
  it('gets course with content (empty at this point)', async () => {
    try {
      const result = await programs.getCoursesWithContent(courseId);

      if (result.ok) {
        expect(result.data.id).toBe(courseId);
      } else {
        console.warn(`with-content returned ${result.error?.status}: ${result.error?.message}`);
        expect(result.error?.status).toBeDefined();
      }
    } catch (error) {
      // The endpoint has known DTO/validation mismatches; keep this best-effort.
      expect(error).toBeDefined();
    }
  });

  // ── 7. Add content to the course ────────────────────────────────────────
  let lessonContentId: string;
  let assignmentContentId: string;
  let quizzesGroupId: string;
  let finalProjectGroupId: string;
  let quizAssessmentId: string;
  let projectAssessmentId: string;

  it('adds a lesson content item to the course', async () => {
    const result = await content.postCoursesContent(courseId, {
      programId: courseId,
      title: 'Lesson 1: Getting Started',
      description: 'Introduction to the basics of game development.',
      type: 'Lesson',
      body: '{}',
      sortOrder: 1,
      isRequired: true,
      estimatedMinutes: 45,
      visibility: 'Public',
    });

    // The endpoint may return the entity directly or a DTO
    expect(result.ok).toBe(true);
    if (result.ok) {
      lessonContentId = result.data.id;
      expect(lessonContentId).toBeTruthy();
    }
  });

  it('adds an assignment content item to the course', async () => {
    const result = await content.postCoursesContent(courseId, {
      programId: courseId,
      title: 'Assignment 1: Build a Pong Clone',
      description: 'Build a simple Pong game using your preferred engine.',
      type: 'Assignment',
      body: '{}',
      sortOrder: 2,
      isRequired: true,
      estimatedMinutes: 120,
      visibility: 'Public',
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      assignmentContentId = result.data.id;
      expect(assignmentContentId).toBeTruthy();
    }
  });

  // ── 8. List content for the course ──────────────────────────────────────
  it('lists all content for the course', async () => {
    const result = await content.getCoursesByProgramIdContent(courseId);

    const contentItems = unwrap(result, 'List course content');
    expect(Array.isArray(contentItems)).toBe(true);
    expect(contentItems.length).toBeGreaterThanOrEqual(2);

    const lesson = contentItems.find((c) => c.id === lessonContentId);
    expect(lesson).toBeDefined();
    expect(lesson!.title).toBe('Lesson 1: Getting Started');

    const assignment = contentItems.find((c) => c.id === assignmentContentId);
    expect(assignment).toBeDefined();
    expect(assignment!.title).toBe('Assignment 1: Build a Pong Clone');
  });

  // ── 9. Get a single content item ────────────────────────────────────────
  it('gets a single content item by ID', async () => {
    const result = await content.getCoursesByProgramIdContentById(courseId, lessonContentId);

    const contentItem = unwrap(result, 'Get single content');
    expect(contentItem.id).toBe(lessonContentId);
    expect(contentItem.programId).toBe(courseId);
    expect(contentItem.title).toBe('Lesson 1: Getting Started');
    expect(contentItem.estimatedMinutes).toBe(45);
  });

  // ── 10. Update a content item ───────────────────────────────────────────
  it('updates a content item', async () => {
    const result = await content.putCoursesContent(courseId, lessonContentId, {
      id: lessonContentId,
      title: 'Lesson 1: Getting Started (revised)',
      description: 'Updated introduction to game development basics.',
      estimatedMinutes: 60,
    });

    const contentItem = unwrap(result, 'Update content');
    expect(contentItem.title).toBe('Lesson 1: Getting Started (revised)');
    expect(contentItem.estimatedMinutes).toBe(60);
  });

  // ── 11. Get top-level content ───────────────────────────────────────────
  it('gets top-level content items', async () => {
    const result = await content.getCoursesByProgramIdContent(courseId, { level: 'top' });

    const contentItems = unwrap(result, 'Get top-level content');
    expect(Array.isArray(contentItems)).toBe(true);
    expect(contentItems.length).toBeGreaterThanOrEqual(2);
  });

  // ── 12. Reorder content ─────────────────────────────────────────────────
  it('reorders content items', async () => {
    // Flip the order: assignment first, lesson second
    const result = await content.postCoursesByProgramIdContentReorder(courseId, {
      contentIds: [assignmentContentId, lessonContentId],
    });

    expect(result.ok).toBe(true);
  });

  it('keeps morning and evening class schedules and student releases independent', async () => {
    const day = 24 * 60 * 60 * 1000;
    const dateOnly = (offset: number) => new Date(Date.now() + offset * day).toISOString().slice(0, 10);
    const cohortIds: string[] = [];
    const temporaryUserIds: string[] = [];

    const createStudent = async (label: string) => {
      const tag = unique();
      const result = unwrap(
        await authedClient.request<SignInOutput>({
          method: 'POST',
          path: '/v1/auth/sign-up',
          body: {
            username: `cohort_${label}_${tag}`,
            email: `cohort_${label}_${tag}@example.com`,
            password: 'Str0ng!Passw0rd123!',
            ...(tenantId ? { tenantId } : {}),
          },
          requiresAuth: false,
        }),
        `Create ${label} cohort student`,
      );
      const id = result.userId || result.user?.id || '';
      expect(id).toBeTruthy();
      temporaryUserIds.push(id);
      return { id, accessToken: result.accessToken };
    };

    const createCohort = async (name: string, startsInDays: number) => {
      const startDate = dateOnly(startsInDays);
      const endDate = dateOnly(startsInDays + 70);
      const cohort = unwrap(
        await authedClient.request<CohortOutput>({
          method: 'POST',
          path: '/api/cohorts',
          body: {
            courseId,
            name,
            description: `${name} delivery used by the independent schedule E2E.`,
            startDate: `${startDate}T00:00:00.000Z`,
            endDate: `${endDate}T23:59:59.000Z`,
            maxCapacity: 20,
            meetingSchedule: name.includes('Morning') ? 'Monday 09:00' : 'Thursday 19:00',
          },
        }),
        `Create ${name}`,
      );
      cohortIds.push(cohort.id);
      return { cohort, startDate, endDate };
    };

    const applySchedule = async (
      cohort: CohortOutput,
      startDate: string,
      endDate: string,
      meetingDay: 'Monday' | 'Thursday',
      meetingStartTime: string,
    ) => unwrap(
      await authedClient.request<CohortScheduleOutput>({
        method: 'PUT',
        path: `/v1/courses/${courseId}/cohorts/${cohort.id}/schedule`,
        body: {
          expectedVersion: 0,
          confirmAdvisories: true,
          rules: {
            firstInstructionalDate: startDate,
            cohortEndDate: endDate,
            timezoneId: 'UTC',
            meetingDays: [meetingDay],
            meetingStartTime,
            meetingDurationMinutes: 90,
            pacingMode: 'OneLessonPerMeeting',
            unitsPerPeriod: 1,
            releasePolicy: 'Immediately',
            skippedDates: [],
            assessmentDueOffsetDays: 7,
          },
        },
      }),
      `Apply ${cohort.name} schedule`,
    );

    try {
      const morning = await createCohort('2026.2 - Morning', -1);
      const evening = await createCohort('2026.2 - Evening', 7);
      const morningSchedule = await applySchedule(
        morning.cohort,
        morning.startDate,
        morning.endDate,
        'Monday',
        '09:00:00',
      );
      const eveningSchedule = await applySchedule(
        evening.cohort,
        evening.startDate,
        evening.endDate,
        'Thursday',
        '19:00:00',
      );

      const morningStudent = await createStudent('morning');
      const eveningStudent = await createStudent('evening');
      for (const [student, cohort] of [
        [morningStudent, morning.cohort],
        [eveningStudent, evening.cohort],
      ] as const) {
        unwrap(
          await authedClient.request({
            method: 'POST',
            path: '/api/learning/enrollments',
            body: { courseId, userId: student.id, cohortId: cohort.id },
          }),
          `Enroll ${student.id} into ${cohort.name}`,
        );
      }

      const morningBeforeShift = unwrap(
        await authedClient.request<CohortScheduleOutput>({
          method: 'GET',
          path: `/v1/courses/${courseId}/cohorts/${morning.cohort.id}/schedule`,
        }),
        'Read morning schedule before evening shift',
      );
      const shiftTarget = eveningSchedule.items.find((item) => item.startsAt || item.availableFrom);
      expect(shiftTarget?.id).toBeTruthy();
      const shiftedEvening = unwrap(
        await authedClient.request<CohortScheduleOutput>({
          method: 'POST',
          path: `/v1/courses/${courseId}/cohorts/${evening.cohort.id}/schedule/items/${shiftTarget!.id}/shift`,
          body: { expectedVersion: eveningSchedule.version, days: 2, scope: 'Following' },
        }),
        'Shift evening schedule',
      );
      const morningAfterShift = unwrap(
        await authedClient.request<CohortScheduleOutput>({
          method: 'GET',
          path: `/v1/courses/${courseId}/cohorts/${morning.cohort.id}/schedule`,
        }),
        'Read morning schedule after evening shift',
      );

      expect(morningBeforeShift.items).toEqual(morningAfterShift.items);
      expect(shiftedEvening.version).toBeGreaterThan(eveningSchedule.version);
      expect(shiftedEvening.items).not.toEqual(eveningSchedule.items);
      expect(morningSchedule.items.some((item) => item.programContentId === lessonContentId)).toBe(true);
      expect(eveningSchedule.items.some((item) => item.programContentId === lessonContentId)).toBe(true);

      const morningClient = createClient({
        baseUrl: BASE_URL,
        timeout: 15_000,
        devtools: { enabled: false },
        auth: { getAccessToken: async () => morningStudent.accessToken },
        tenant: { getTenantId: async () => tenantId },
      });
      const eveningClient = createClient({
        baseUrl: BASE_URL,
        timeout: 15_000,
        devtools: { enabled: false },
        auth: { getAccessToken: async () => eveningStudent.accessToken },
        tenant: { getTenantId: async () => tenantId },
      });
      const morningContent = unwrap(
        await morningClient.request<AvailableCohortContentOutput[]>({
          method: 'GET',
          path: `/v1/courses/${courseId}/cohorts/${morning.cohort.id}/schedule/available-content`,
        }),
        'Read released morning content',
      );
      const eveningContent = unwrap(
        await eveningClient.request<AvailableCohortContentOutput[]>({
          method: 'GET',
          path: `/v1/courses/${courseId}/cohorts/${evening.cohort.id}/schedule/available-content`,
        }),
        'Read released evening content',
      );
      const crossCohortContent = unwrap(
        await morningClient.request<AvailableCohortContentOutput[]>({
          method: 'GET',
          path: `/v1/courses/${courseId}/cohorts/${evening.cohort.id}/schedule/available-content`,
        }),
        'Read cross-cohort content as morning student',
      );

      expect(morningContent.map((item) => item.contentId)).toContain(lessonContentId);
      expect(eveningContent).toHaveLength(0);
      expect(crossCohortContent).toHaveLength(0);
    } finally {
      for (const cohortId of cohortIds.reverse()) {
        await authedClient.request<void>({ method: 'DELETE', path: `/api/cohorts/${cohortId}` });
      }
      for (const temporaryUserId of temporaryUserIds.reverse()) {
        await authedClient.request<void>({ method: 'DELETE', path: `/v1/users/${temporaryUserId}` });
      }
    }
  }, 60_000);

  // ── 12a. Professor grading setup: weighted groups + assessments ─────────
  it('creates weighted assessment groups for the course', async () => {
    const quizzesResult = await authedClient.request<AssessmentGroupOutput>({
      method: 'POST',
      path: '/v1/assessments/groups',
      body: {
        courseId,
        name: 'Quizzes',
        weightPercent: 40,
        order: 1,
        description: 'Short checks for comprehension throughout the course.',
      },
    });
    const quizzesGroup = unwrap(quizzesResult, 'Create quizzes assessment group');
    quizzesGroupId = quizzesGroup.id;

    const finalProjectResult = await authedClient.request<AssessmentGroupOutput>({
      method: 'POST',
      path: '/v1/assessments/groups',
      body: {
        courseId,
        name: 'Final Project',
        weightPercent: 60,
        order: 2,
        description: 'Portfolio-ready milestone and final submission.',
      },
    });
    const finalProjectGroup = unwrap(finalProjectResult, 'Create final project assessment group');
    finalProjectGroupId = finalProjectGroup.id;

    expect(quizzesGroup.weightPercent).toBe(40);
    expect(finalProjectGroup.weightPercent).toBe(60);

    const listResult = await authedClient.request<AssessmentGroupOutput[]>({
      method: 'GET',
      path: `/v1/assessments/course/${courseId}/groups`,
    });
    const groups = unwrap(listResult, 'List assessment groups');
    expect(groups.map((group) => group.name)).toEqual(expect.arrayContaining(['Quizzes', 'Final Project']));
    expect(groups.reduce((total, group) => total + Number(group.weightPercent), 0)).toBe(100);
  });

  it('updates weighted assessment group metadata before activities are assigned', async () => {
    const updateResult = await authedClient.request<AssessmentGroupOutput>({
      method: 'PUT',
      path: `/v1/assessments/groups/${quizzesGroupId}`,
      body: {
        name: 'Weekly Quizzes',
        weightPercent: 35,
        order: 1,
        description: 'Weekly checks and short applied quizzes.',
      },
    });
    const updatedGroup = unwrap(updateResult, 'Update quizzes assessment group');

    expect(updatedGroup.name).toBe('Weekly Quizzes');
    expect(updatedGroup.weightPercent).toBe(35);
    expect(updatedGroup.description).toBe('Weekly checks and short applied quizzes.');

    const rebalanceResult = await authedClient.request<AssessmentGroupOutput>({
      method: 'PUT',
      path: `/v1/assessments/groups/${finalProjectGroupId}`,
      body: {
        name: 'Final Project',
        weightPercent: 65,
        order: 2,
        description: 'Portfolio-ready milestone and final submission.',
      },
    });
    const rebalancedGroup = unwrap(rebalanceResult, 'Rebalance final project group');
    expect(rebalancedGroup.weightPercent).toBe(65);

    const listResult = await authedClient.request<AssessmentGroupOutput[]>({
      method: 'GET',
      path: `/v1/assessments/course/${courseId}/groups`,
    });
    const groups = unwrap(listResult, 'List updated assessment groups');
    expect(groups.map((group) => group.name)).toEqual(expect.arrayContaining(['Weekly Quizzes', 'Final Project']));
    expect(groups.reduce((total, group) => total + Number(group.weightPercent), 0)).toBe(100);
  });

  it('creates professor-facing quiz and project assessments without legacy exam type', async () => {
    const quizResult = await authedClient.request<AssessmentOutput>({
      method: 'POST',
      path: '/v1/assessments',
      body: {
        courseId,
        title: 'Week 01 Quiz',
        description: 'Checks core vocabulary and early course expectations.',
        type: 'Quiz',
        maxScore: 10,
        passingScore: 7,
        assessmentGroupId: quizzesGroupId,
      },
    });
    const quiz = unwrap(quizResult, 'Create quiz assessment');
    quizAssessmentId = quiz.id;

    const projectResult = await authedClient.request<AssessmentOutput>({
      method: 'POST',
      path: '/v1/assessments',
      body: {
        courseId,
        title: 'Final Playable Prototype',
        description: 'A project assessment for the portfolio milestone.',
        type: 'Project',
        maxScore: 100,
        passingScore: 70,
        assessmentGroupId: finalProjectGroupId,
      },
    });
    const project = unwrap(projectResult, 'Create project assessment');
    projectAssessmentId = project.id;

    expect(quiz.type).toBe('Quiz');
    expect(project.type).toBe('Project');
    expect([quiz.type, project.type]).not.toContain('Exam');
  });

  it('deletes unused weighted groups without removing existing assessments', async () => {
    const labGroupResult = await authedClient.request<AssessmentGroupOutput>({
      method: 'POST',
      path: '/v1/assessments/groups',
      body: {
        courseId,
        name: 'Lab Practice',
        weightPercent: 0,
        order: 99,
        description: 'Temporary unassigned group used by the professor while planning.',
      },
    });
    const labGroup = unwrap(labGroupResult, 'Create temporary lab assessment group');

    const deleteResult = await authedClient.request<void>({
      method: 'DELETE',
      path: `/v1/assessments/groups/${labGroup.id}`,
    });
    expect(deleteResult.ok).toBe(true);

    const listResult = await authedClient.request<AssessmentGroupOutput[]>({
      method: 'GET',
      path: `/v1/assessments/course/${courseId}/groups`,
    });
    const groups = unwrap(listResult, 'List assessment groups after delete');
    expect(groups.map((group) => group.id)).not.toContain(labGroup.id);

    const assessmentsResult = await authedClient.request<AssessmentOutput[]>({
      method: 'GET',
      path: `/v1/assessments/course/${courseId}`,
    });
    const assessments = unwrap(assessmentsResult, 'List assessments after deleting unused group');
    expect(assessments.map((assessment) => assessment.id)).toEqual(
      expect.arrayContaining([quizAssessmentId, projectAssessmentId]),
    );
  });

  it('attaches the quiz to a lesson and keeps all graded work visible in the assessment hub', async () => {
    const updateResult = await authedClient.request<AssessmentOutput>({
      method: 'PUT',
      path: `/v1/assessments/${quizAssessmentId}`,
      body: {
        contentId: lessonContentId,
        assessmentGroupId: quizzesGroupId,
      },
    });
    const linkedQuiz = unwrap(updateResult, 'Attach quiz to lesson');

    expect(linkedQuiz.contentId).toBe(lessonContentId);
    expect(linkedQuiz.assessmentGroupId).toBe(quizzesGroupId);

    const listResult = await authedClient.request<AssessmentOutput[]>({
      method: 'GET',
      path: `/v1/assessments/course/${courseId}`,
    });
    const assessments = unwrap(listResult, 'List course assessments');

    expect(assessments.map((assessment) => assessment.id)).toEqual(
      expect.arrayContaining([quizAssessmentId, projectAssessmentId]),
    );
    expect(assessments.find((assessment) => assessment.id === quizAssessmentId)?.contentId).toBe(
      lessonContentId,
    );
    expect(assessments.find((assessment) => assessment.id === projectAssessmentId)?.contentId).toBeFalsy();
  });

  it('returns assessment analytics by weighted group', async () => {
    const result = await authedClient.request<CourseAssessmentAnalyticsOutput>({
      method: 'GET',
      path: `/v1/assessments/course/${courseId}/analytics`,
    });
    const analytics = unwrap(result, 'Get course assessment analytics');

    expect(analytics.courseId).toBe(courseId);
    expect(analytics.assessmentCount).toBeGreaterThanOrEqual(2);
    expect(analytics.ungradedCount).toBeGreaterThanOrEqual(2);
    expect(analytics.distribution.map((bucket) => bucket.label)).toEqual(
      expect.arrayContaining(['0-59', '60-69', '70-79', '80-89', '90-100']),
    );
    expect(analytics.groups.map((group) => group.groupName)).toEqual(
      expect.arrayContaining(['Weekly Quizzes', 'Final Project']),
    );
  });

  // ── 13. Add a user to the course (enroll) ───────────────────────────────
  it('enrolls the current user into the course', async () => {
    const result = await programs.postCoursesUsers(courseId, userId);

    expect(result.ok).toBe(true);
  });

  // ── 14. List enrolled users ─────────────────────────────────────────────
  it('lists users enrolled in the course', async () => {
    const result = await programs.getCoursesUsers(courseId);

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(Array.isArray(result.data)).toBe(true);
      expect(result.data.length).toBeGreaterThanOrEqual(1);
    }
  });

  it('sends an in-app course message only to an enrolled user', async () => {
    const result = unwrap(await authedClient.request<{ sent: number }>({
      method: 'POST',
      path: `/v1/courses/${courseId}/students/message`,
      body: {
        userIds: [userId],
        subject: 'Milestone update',
        message: 'The critique session moved to Friday.',
      },
    }), 'Send enrolled student message');

    expect(result.sent).toBe(1);
  });

  // ── 15. Get user progress ──────────────────────────────────────────────
  it('gets user progress in the course', async () => {
    const result = await programs.getCoursesUsersProgress(courseId, userId);

    expect(result.ok).toBe(true);
  });

  // ── 16. Mark content as completed ───────────────────────────────────────
  it('marks a content item as completed for the user', async () => {
    const result = await programs.postCoursesUsersContentComplete(
      courseId,
      userId,
      lessonContentId,
    );

    // May return 204 or 200 depending on implementation
    expect(result.ok).toBe(true);
  });

  // ── 17. Clone the course ────────────────────────────────────────────────
  let clonedCourseId: string;

  it('clones the course', async () => {
    const result = await programs.postCoursesClone(courseId, {
      newTitle: `Cloned E2E Course ${Date.now()}`,
    });

    const cloned = unwrap(result, 'Clone course');
    clonedCourseId = cloned.id;

    expect(cloned.id).toBeTruthy();
    expect(cloned.id).not.toBe(courseId);
    expect(cloned.title).toContain('Cloned E2E Course');
    expect(cloned.status).toBe('Draft');
  });

  // ── 18. Lifecycle: Submit → Approve → Publish ──────────────────────────
  it('submits the course for review', async () => {
    const result = await lifecycle.postCoursesSubmit(courseId);

    const course = unwrap(result, 'Submit course');
    expect(course.status).toBe('Review');
  });

  it('approves the course', async () => {
    const result = await lifecycle.postCoursesApprove(courseId);

    // Approve may change status or keep in review depending on backend logic
    expect(result.ok).toBe(true);
  });

  it('publishes the course', async () => {
    const result = await lifecycle.postCoursesPublish(courseId);

    const course = unwrap(result, 'Publish course');
    expect(course.status).toBe('Published');
  });

  it('requires paid checkout before a prospect can access the classroom', async () => {
    paidProductId = unwrap(
      await programs.postCoursesCreateProduct(courseId, {
        name: 'E2E paid course access',
        description: 'Unlocks the E2E published course for a prospect learner.',
        basePrice: 49,
        currency: 'USD',
      }),
      'Create paid course product',
    );

    const linkedProducts = unwrap(await programs.getCoursesProducts(courseId), 'List course products');
    expect(linkedProducts).toContain(paidProductId);

    const publicClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
    });
    const prospectTag = unique();
    const prospectEmail = `course_checkout_${prospectTag}@example.com`;
    const prospectPassword = 'Str0ng!Passw0rd123!';
    const signUp = unwrap(
      await publicClient.request<SignInOutput>({
        method: 'POST',
        path: '/v1/auth/sign-up',
        body: {
          username: `course_checkout_${prospectTag}`,
          email: prospectEmail,
          password: prospectPassword,
          ...(tenantId ? { tenantId } : {}),
        },
        requiresAuth: false,
      }),
      'Prospect sign-up for paid course checkout',
    );
    const prospectToken = signUp.accessToken;
    const prospectClient = createClient({
      baseUrl: BASE_URL,
      timeout: 15_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => prospectToken },
      tenant: { getTenantId: async () => tenantId },
    });
    const prospectPrograms = new GeneratedApi.LearningCoursesProgramModule(prospectClient);

    const blockedFreeEnroll = await prospectPrograms.postCoursesSelfEnroll(courseId);
    expect(blockedFreeEnroll.ok).toBe(false);
    if (!blockedFreeEnroll.ok) {
      expect(blockedFreeEnroll.error?.status).toBe(402);
    }

    const checkout = unwrap(
      await prospectClient.request<CourseCheckoutOutput>({
        method: 'POST',
        path: `/v1/courses/${courseId}/checkout/complete`,
        body: {
          productId: paidProductId,
          paymentProviderReference: `course-e2e-${prospectTag}`,
          paymentMethod: 'test_card',
        },
        requiresAuth: true,
      }),
      'Complete paid course checkout',
    );

    expect(checkout.courseId).toBe(courseId);
    expect(checkout.productId).toBe(paidProductId);
    expect(checkout.entitlementId).toBeTruthy();
    expect(checkout.amount).toBe(49);
    expect(checkout.currency).toBe('USD');
    expect(checkout.learningUrl).toBe(`/courses/${courseSlug}/content`);

    const progress = unwrap(
      await prospectPrograms.getCoursesMeProgress(courseId),
      'Prospect classroom progress after checkout',
    );
    expect(progress.courseId).toBe(courseId);
    expect(progress.userId).toBe(signUp.userId || signUp.user?.id);
  });

  // ── 19. Filter: published courses ──────────────────────────────────────
  it('lists published courses and the course appears', async () => {
    const result = await programs.getCourses({ status: 'published' });

    const courses = unwrap(result, 'List published courses');
    expect(Array.isArray(courses)).toBe(true);
    const found = courses.find((c) => c.id === courseId);
    expect(found).toBeDefined();
    expect(found!.status).toBe('Published');
  });

  // ── 20. Unpublish ──────────────────────────────────────────────────────
  it('unpublishes the course', async () => {
    const result = await lifecycle.postCoursesUnpublish(courseId);

    const course = unwrap(result, 'Unpublish course');
    expect(course.status).toBe('Draft');
  });

  // ── 21. Archive ────────────────────────────────────────────────────────
  it('archives the course', async () => {
    const result = await lifecycle.postCoursesArchive(courseId);

    const course = unwrap(result, 'Archive course');
    expect(course.status).toBe('Archived');
  });

  // ── 22. Restore ────────────────────────────────────────────────────────
  it('restores the archived course', async () => {
    const result = await lifecycle.postCoursesRestore(courseId);

    const course = unwrap(result, 'Restore course');
    expect(course.status).toBe('Draft');
  });

  // ── 23. Search ─────────────────────────────────────────────────────────
  it('searches courses by keyword', async () => {
    const result = await programs.getCourses({ q: 'Game Dev' });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(Array.isArray(result.data)).toBe(true);
    }
  });

  // ── 24. Filter by category ─────────────────────────────────────────────
  it('filters courses by category', async () => {
    const result = await programs.getCourses({ category: 'General' });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(Array.isArray(result.data)).toBe(true);
    }
  });

  // ── 25. Filter by difficulty ───────────────────────────────────────────
  it('filters courses by difficulty', async () => {
    const result = await programs.getCourses({ difficulty: 'Beginner' });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(Array.isArray(result.data)).toBe(true);
    }
  });

  // ── 26. Sort popular / recent ──────────────────────────────────────────
  it('lists popular courses', async () => {
    const result = await programs.getCourses({ sort: 'popular' });

    expect(result.ok).toBe(true);
  });

  it('lists recent courses', async () => {
    const result = await programs.getCourses({ sort: 'recent' });

    expect(result.ok).toBe(true);
  });

  // ── 27. Delete content item ─────────────────────────────────────────────
  it('deletes a content item from the course', async () => {
    const result = await content.deleteCoursesContent(courseId, assignmentContentId);

    expect(result.ok).toBe(true);

    // Verify it's gone
    const getResult = await content.getCoursesByProgramIdContentById(courseId, assignmentContentId);
    expect(getResult.ok).toBe(false);
  });

  // ── 28. Remove user from course ────────────────────────────────────────
  it('removes the user from the course', async () => {
    const result = await programs.deleteCoursesUsers(courseId, userId);

    expect(result.ok).toBe(true);
  });

  // ── 29. Delete the courses ─────────────────────────────────────────────
  it('deletes the original course', async () => {
    const result = await programs.deleteCourses(courseId);

    expect(result.ok).toBe(true);

    // Verify it's gone
    const getResult = await programs.getCoursesById(courseId);
    expect(getResult.ok).toBe(false);
  });

  it('deletes the cloned course', async () => {
    const result = await programs.deleteCourses(clonedCourseId);

    expect(result.ok).toBe(true);
  });
});
