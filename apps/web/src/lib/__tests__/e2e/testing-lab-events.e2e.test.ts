import { createClient, type ApiError, type Result } from '@game-guild/client';
import { describe, expect, it } from 'vitest';

interface AuthOutput {
  accessToken: string;
  refreshToken: string;
  userId: string;
  tenantId?: string;
  user?: { id?: string };
}

interface Identified {
  id: string;
}

interface EventProjection extends Identified {
  name?: string;
  status?: string;
  slots?: PublicSlotProjection[];
}

interface PublicSlotProjection extends Identified {
  meetingUrl?: string;
  approvedProjectCount?: number;
  registeredTesterCount?: number;
  availableTesterCount?: number | null;
  availableProjectCount?: number | null;
}

interface ApplicationProjection extends Identified {
  status?: string;
  assignedSlotId?: string | null;
  decisionRationale?: string | null;
}

interface RegistrationProjection extends Identified {
  slotId?: string;
  status?: string;
  waitlistPosition?: number | null;
  pendingFeedbackCount?: number;
}

interface FeedbackObligationProjection extends Identified {
  status?: string;
  applicationId?: string;
}

interface RoleTemplateProjection extends Identified {
  name?: string;
}

const BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:8080';
const SYSTEM_ADMIN_EMAIL = process.env.E2E_SYSTEM_ADMIN_EMAIL ?? 'admin@game-guild.com';
const SYSTEM_ADMIN_PASSWORD = process.env.E2E_SYSTEM_ADMIN_PASSWORD ?? 'Admin123!';

function unique() {
  return `${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

function unwrap<T>(result: Result<T, ApiError>, label: string): T {
  if (result.ok) return result.data;
  throw new Error(
    `${label} failed: ${result.error?.message ?? 'Unknown'} (${result.error?.status}) ${JSON.stringify(result.error)}`,
  );
}

function actorClient(accessToken: string, tenantId: string) {
  return createClient({
    baseUrl: BASE_URL,
    timeout: 20_000,
    devtools: { enabled: false },
    auth: { getAccessToken: async () => accessToken },
    tenant: { getTenantId: async () => tenantId },
  });
}

describe('Testing Lab event workflow E2E', () => {
  it('completes manager, reviewer, applicant, tester, settings, role, and report journeys', async () => {
    const tag = unique();
    const password = 'Str0ng!Passw0rd123!';
    const anonymous = createClient({
      baseUrl: BASE_URL,
      timeout: 20_000,
      devtools: { enabled: false },
    });

    const managerEmail = `testing_event_manager_${tag}@example.com`;
    const managerSignUp = unwrap(
      await anonymous.request<AuthOutput>({
        method: 'POST',
        path: '/v1/auth/sign-up',
        body: {
          username: `testing_event_manager_${tag}`,
          email: managerEmail,
          password,
        },
        requiresAuth: false,
      }),
      'Manager sign-up',
    );

    const tenantBootstrap = actorClient(managerSignUp.accessToken, managerSignUp.tenantId ?? '');
    const tenant = unwrap(
      await tenantBootstrap.request<Identified>({
        method: 'POST',
        path: '/v1/tenants',
        body: {
          name: `Testing Event E2E ${tag}`,
          slug: `testing-event-e2e-${tag.replace(/_/g, '-')}`,
          adminEmail: managerEmail,
          description: 'Isolated tenant for the complete Testing Lab event journey.',
        },
        requiresAuth: true,
      }),
      'Create event E2E tenant',
    );

    const managerAuth = unwrap(
      await anonymous.request<AuthOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: { email: managerEmail, password, tenantId: tenant.id },
        requiresAuth: false,
      }),
      'Manager tenant sign-in',
    );
    const managerId = managerAuth.userId || managerAuth.user?.id;
    if (!managerId) throw new Error('Manager sign-in did not expose a user id.');
    const manager = actorClient(managerAuth.accessToken, tenant.id);

    const testerEmail = `testing_event_tester_${tag}@example.com`;
    const testerSignUp = unwrap(
      await anonymous.request<AuthOutput>({
        method: 'POST',
        path: '/v1/auth/sign-up',
        body: {
          username: `testing_event_tester_${tag}`,
          email: testerEmail,
          password,
        },
        requiresAuth: false,
      }),
      'Tester sign-up',
    );
    const testerSignUpId = testerSignUp.userId || testerSignUp.user?.id;
    if (!testerSignUpId) throw new Error('Tester sign-up did not expose a user id.');
    unwrap(
      await manager.request<unknown>({
        method: 'POST',
        path: `/v1/users/${testerSignUpId}/memberships`,
        body: {
          tenantId: tenant.id,
          role: 'Member',
          invitedByEmail: managerEmail,
        },
        requiresAuth: true,
      }),
      'Add tester tenant membership',
    );
    const testerAuth = unwrap(
      await anonymous.request<AuthOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: { email: testerEmail, password, tenantId: tenant.id },
        requiresAuth: false,
      }),
      'Tester tenant sign-in',
    );
    const testerId = testerAuth.userId || testerAuth.user?.id;
    if (!testerId) throw new Error('Tester sign-in did not expose a user id.');
    const tester = actorClient(testerAuth.accessToken, tenant.id);

    const reviewerEmail = `testing_event_reviewer_${tag}@example.com`;
    const reviewerSignUp = unwrap(
      await anonymous.request<AuthOutput>({
        method: 'POST',
        path: '/v1/auth/sign-up',
        body: {
          username: `testing_event_reviewer_${tag}`,
          email: reviewerEmail,
          password,
        },
        requiresAuth: false,
      }),
      'Reviewer sign-up',
    );
    const reviewerSignUpId = reviewerSignUp.userId || reviewerSignUp.user?.id;
    if (!reviewerSignUpId) throw new Error('Reviewer sign-up did not expose a user id.');
    unwrap(
      await manager.request<unknown>({
        method: 'POST',
        path: `/v1/users/${reviewerSignUpId}/memberships`,
        body: {
          tenantId: tenant.id,
          role: 'Member',
          invitedByEmail: managerEmail,
        },
        requiresAuth: true,
      }),
      'Add reviewer tenant membership',
    );
    const reviewerAuth = unwrap(
      await anonymous.request<AuthOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: { email: reviewerEmail, password, tenantId: tenant.id },
        requiresAuth: false,
      }),
      'Reviewer tenant sign-in',
    );
    const reviewerId = reviewerAuth.userId || reviewerAuth.user?.id;
    if (!reviewerId) throw new Error('Reviewer sign-in did not expose a user id.');
    const reviewer = actorClient(reviewerAuth.accessToken, tenant.id);

    const project = unwrap(
      await manager.request<Identified>({
        method: 'POST',
        path: '/v1/projects',
        body: {
          title: `Asterion E2E ${tag}`,
          description: 'Playable community project submitted to the event workflow.',
          shortDescription: 'Testing Lab event E2E project',
          imageUrl: 'https://example.com/asterion.jpg',
          websiteUrl: 'https://example.com/asterion',
          downloadUrl: 'https://example.com/asterion.zip',
          type: 0,
          visibility: 4,
          status: 2,
          tags: ['testing-lab', 'events', 'e2e'],
        },
        requiresAuth: true,
      }),
      'Create event project',
    );

    const now = Date.now();
    const event = unwrap(
      await manager.request<EventProjection>({
        method: 'POST',
        path: '/v1/testing/events',
        body: {
          name: `Campus showcase ${tag}`,
          description: 'Committee-reviewed project testing with mandatory feedback.',
          mode: 'InPerson',
          approvalMode: 'Committee',
          applicationsOpenAt: new Date(now - 60_000).toISOString(),
          applicationsCloseAt: new Date(now + 60 * 60_000).toISOString(),
          startsAt: new Date(now + 2 * 60 * 60_000).toISOString(),
          endsAt: new Date(now + 5 * 60 * 60_000).toISOString(),
          requiresFeedback: true,
        },
        requiresAuth: true,
      }),
      'Create Testing Lab event',
    );

    const slot = unwrap(
      await manager.request<PublicSlotProjection>({
        method: 'POST',
        path: `/v1/testing/events/${event.id}/slots`,
        body: {
          mode: 'InPerson',
          startsAt: new Date(now + 2 * 60 * 60_000).toISOString(),
          endsAt: new Date(now + 4 * 60 * 60_000).toISOString(),
          maxTesters: 1,
          maxProjects: 2,
          campusName: 'Downtown campus',
          roomName: 'Lab 4',
          meetingUrl: null,
          locationId: null,
        },
        requiresAuth: true,
      }),
      'Create event slot',
    );

    unwrap(
      await manager.request<Identified>({
        method: 'POST',
        path: `/v1/testing/events/${event.id}/committee`,
        body: { userId: reviewerId, isChair: true },
        requiresAuth: true,
      }),
      'Add committee reviewer',
    );
    unwrap(
      await manager.request<EventProjection>({
        method: 'POST',
        path: `/v1/testing/events/${event.id}:open-applications`,
        requiresAuth: true,
      }),
      'Open project applications',
    );

    const initialPublicEvent = unwrap(
      await anonymous.request<EventProjection>({
        method: 'GET',
        path: `/v1/testing/events/public/${event.id}`,
        requiresAuth: false,
      }),
      'Read initial public event',
    );
    const initialPublicSlot = initialPublicEvent.slots?.find((candidate) => candidate.id === slot.id);
    expect(initialPublicSlot?.approvedProjectCount).toBe(0);
    expect(initialPublicSlot).not.toHaveProperty('meetingUrl');

    const application = unwrap(
      await manager.request<ApplicationProjection>({
        method: 'POST',
        path: `/v1/testing/events/${event.id}/applications`,
        body: {
          projectId: project.id,
          projectVersionId: null,
          preferredAvailability: 'The first campus slot works.',
        },
        requiresAuth: true,
      }),
      'Submit project candidacy',
    );
    const pendingEvent = unwrap(
      await anonymous.request<EventProjection>({
        method: 'GET',
        path: `/v1/testing/events/public/${event.id}`,
        requiresAuth: false,
      }),
      'Read event after candidacy',
    );
    expect(pendingEvent.slots?.find((candidate) => candidate.id === slot.id)?.approvedProjectCount).toBe(0);

    unwrap(
      await manager.request<ApplicationProjection>({
        method: 'POST',
        path: `/v1/testing/events/applications/${application.id}:review`,
        requiresAuth: true,
      }),
      'Begin application review',
    );
    unwrap(
      await reviewer.request<Identified>({
        method: 'POST',
        path: `/v1/testing/events/applications/${application.id}/votes`,
        body: { decision: 'Approve', comments: 'Playable and suitable for this audience.' },
        requiresAuth: true,
      }),
      'Committee approval vote',
    );
    const approved = unwrap(
      await manager.request<ApplicationProjection>({
        method: 'POST',
        path: `/v1/testing/events/applications/${application.id}:approve`,
        body: { slotId: slot.id, rationale: 'Committee majority approved the project.' },
        requiresAuth: true,
      }),
      'Approve project and reserve capacity',
    );
    expect(approved.status).toBe('Approved');
    expect(approved.assignedSlotId).toBe(slot.id);

    const approvedEvent = unwrap(
      await anonymous.request<EventProjection>({
        method: 'GET',
        path: `/v1/testing/events/public/${event.id}`,
        requiresAuth: false,
      }),
      'Read event after approval',
    );
    expect(approvedEvent.slots?.find((candidate) => candidate.id === slot.id)?.approvedProjectCount).toBe(1);

    const rejectedProject = unwrap(
      await manager.request<Identified>({
        method: 'POST',
        path: '/v1/projects',
        body: {
          title: `Unready build ${tag}`,
          description: 'Second project used to prove rejection feedback.',
          shortDescription: 'Rejection E2E project',
          type: 0,
          visibility: 4,
          status: 2,
          tags: ['testing-lab', 'rejection'],
        },
        requiresAuth: true,
      }),
      'Create rejection project',
    );
    const rejectedApplication = unwrap(
      await manager.request<ApplicationProjection>({
        method: 'POST',
        path: `/v1/testing/events/${event.id}/applications`,
        body: { projectId: rejectedProject.id, preferredAvailability: 'Any slot.' },
        requiresAuth: true,
      }),
      'Submit rejection candidacy',
    );
    unwrap(
      await manager.request<ApplicationProjection>({
        method: 'POST',
        path: `/v1/testing/events/applications/${rejectedApplication.id}:review`,
        requiresAuth: true,
      }),
      'Begin rejection review',
    );
    unwrap(
      await tester.request<Identified>({
        method: 'POST',
        path: `/v1/testing/events/applications/${rejectedApplication.id}/votes`,
        body: { decision: 'Reject', comments: 'The build cannot complete its onboarding.' },
        requiresAuth: true,
      }),
      'Committee rejection vote',
    );
    const rejected = unwrap(
      await manager.request<ApplicationProjection>({
        method: 'POST',
        path: `/v1/testing/events/applications/${rejectedApplication.id}:reject`,
        body: { slotId: null, rationale: 'The current build is not yet playable end to end.' },
        requiresAuth: true,
      }),
      'Reject project with rationale',
    );
    expect(rejected.status).toBe('Rejected');
    expect(rejected.decisionRationale).toBe('The current build is not yet playable end to end.');

    unwrap(
      await manager.request<EventProjection>({
        method: 'POST',
        path: `/v1/testing/events/${event.id}:close-applications`,
        requiresAuth: true,
      }),
      'Close project applications',
    );
    unwrap(
      await manager.request<EventProjection>({
        method: 'POST',
        path: `/v1/testing/events/${event.id}:schedule`,
        requiresAuth: true,
      }),
      'Publish event schedule',
    );

    const managerRegistration = unwrap(
      await manager.request<RegistrationProjection>({
        method: 'POST',
        path: `/v1/testing/events/slots/${slot.id}/registrations`,
        body: { notes: 'Initial tester occupying the only seat.' },
        requiresAuth: true,
      }),
      'Register first tester',
    );
    expect(managerRegistration.status).toBe('Registered');

    const waitlistedRegistration = unwrap(
      await tester.request<RegistrationProjection>({
        method: 'POST',
        path: `/v1/testing/events/slots/${slot.id}/registrations`,
        body: { notes: 'Join if a seat becomes available.' },
        requiresAuth: true,
      }),
      'Join tester waitlist',
    );
    expect(waitlistedRegistration.status).toBe('Waitlisted');
    expect(waitlistedRegistration.waitlistPosition).toBe(1);

    unwrap(
      await manager.request<RegistrationProjection>({
        method: 'DELETE',
        path: `/v1/testing/events/registrations/${managerRegistration.id}`,
        requiresAuth: true,
      }),
      'Release tester capacity',
    );
    const testerRegistrations = unwrap(
      await tester.request<RegistrationProjection[]>({
        method: 'GET',
        path: `/v1/testing/events/registrations/me?eventId=${event.id}`,
        requiresAuth: true,
      }),
      'Read promoted tester registration',
    );
    const promoted = testerRegistrations.find((candidate) => candidate.id === waitlistedRegistration.id);
    expect(promoted?.status).toBe('Registered');
    expect(promoted?.waitlistPosition).toBeNull();

    unwrap(
      await manager.request<RegistrationProjection>({
        method: 'POST',
        path: `/v1/testing/events/registrations/${waitlistedRegistration.id}:check-in`,
        requiresAuth: true,
      }),
      'Check tester in',
    );
    const obligation = unwrap(
      await manager.request<FeedbackObligationProjection>({
        method: 'POST',
        path: `/v1/testing/events/registrations/${waitlistedRegistration.id}/tested-projects`,
        body: { applicationId: application.id },
        requiresAuth: true,
      }),
      'Assign approved project to tester',
    );
    unwrap(
      await manager.request<RegistrationProjection>({
        method: 'POST',
        path: `/v1/testing/events/registrations/${waitlistedRegistration.id}:check-out`,
        requiresAuth: true,
      }),
      'Check tester out',
    );

    const obligations = unwrap(
      await tester.request<FeedbackObligationProjection[]>({
        method: 'GET',
        path: `/v1/testing/events/feedback-obligations/me?eventId=${event.id}`,
        requiresAuth: true,
      }),
      'Read tester feedback obligations',
    );
    expect(obligations.find((candidate) => candidate.id === obligation.id)?.status).toBe('Pending');
    unwrap(
      await tester.request<Identified>({
        method: 'POST',
        path: `/v1/testing/events/feedback-obligations/${obligation.id}/feedback`,
        body: {
          feedbackData: 'The core loop is clear. The first checkpoint needs stronger visual feedback.',
          overallRating: 8,
          wouldRecommend: true,
          additionalNotes: 'Retest the onboarding after the next build.',
        },
        requiresAuth: true,
      }),
      'Submit required event feedback',
    );
    const completed = unwrap(
      await tester.request<RegistrationProjection>({
        method: 'POST',
        path: `/v1/testing/events/registrations/${waitlistedRegistration.id}:complete`,
        requiresAuth: true,
      }),
      'Complete tester participation',
    );
    expect(completed.status).toBe('Completed');
    expect(completed.pendingFeedbackCount).toBe(0);

    const settings = unwrap(
      await manager.request<{ labName?: string }>({
        method: 'PATCH',
        path: '/api/testing-lab/settings',
        body: {
          labName: `Testing Lab ${tag}`,
          allowPublicSignups: true,
          requireApproval: true,
          enableNotifications: true,
        },
        requiresAuth: true,
      }),
      'Update Testing Lab settings',
    );
    expect(settings.labName).toBe(`Testing Lab ${tag}`);

    const roleName = `Event Reviewer ${tag}`;
    const rolePayload = {
      name: roleName,
      description: 'Temporary E2E role for Testing Lab event review.',
      permissions: {
        canViewSessions: true,
        canViewRequests: true,
        canApproveRequests: true,
        canViewParticipants: true,
      },
    };
    const managerRoleAttempt = await manager.request<RoleTemplateProjection>({
      method: 'POST',
      path: '/api/testing-lab/permissions/role-templates',
      body: rolePayload,
      requiresAuth: true,
    });
    expect(managerRoleAttempt.ok).toBe(false);
    if (!managerRoleAttempt.ok) expect(managerRoleAttempt.error?.status).toBe(403);

    const systemAdminAuth = unwrap(
      await anonymous.request<AuthOutput>({
        method: 'POST',
        path: '/v1/auth/sign-in',
        body: { email: SYSTEM_ADMIN_EMAIL, password: SYSTEM_ADMIN_PASSWORD },
        requiresAuth: false,
      }),
      'System administrator sign-in',
    );
    const systemAdmin = createClient({
      baseUrl: BASE_URL,
      timeout: 20_000,
      devtools: { enabled: false },
      auth: { getAccessToken: async () => systemAdminAuth.accessToken },
    });
    const role = unwrap(
      await systemAdmin.request<RoleTemplateProjection>({
        method: 'POST',
        path: '/api/testing-lab/permissions/role-templates',
        body: rolePayload,
        requiresAuth: true,
      }),
      'Create Testing Lab role template',
    );
    expect(role.name).toBe(roleName);
    unwrap(
      await systemAdmin.request<unknown>({
        method: 'POST',
        path: `/api/testing-lab/permissions/users/${testerId}/roles`,
        body: { tenantId: tenant.id, roleName, expiresAt: null },
        requiresAuth: true,
      }),
      'Assign Testing Lab role',
    );
    const userPermissions = unwrap(
      await systemAdmin.request<{ assignedRoles?: string[] }>({
        method: 'GET',
        path: `/api/testing-lab/permissions/users/${testerId}?tenantId=${tenant.id}`,
        requiresAuth: true,
      }),
      'Read Testing Lab user permissions',
    );
    expect(userPermissions.assignedRoles).toContain(roleName);
    unwrap(
      await systemAdmin.request<unknown>({
        method: 'DELETE',
        path: `/api/testing-lab/permissions/users/${testerId}/roles/${encodeURIComponent(roleName)}?tenantId=${tenant.id}`,
        requiresAuth: true,
      }),
      'Revoke Testing Lab role',
    );
    unwrap(
      await systemAdmin.request<unknown>({
        method: 'DELETE',
        path: `/api/testing-lab/permissions/role-templates/${role.id}`,
        requiresAuth: true,
      }),
      'Delete Testing Lab role template',
    );

    const attendanceReport = unwrap(
      await manager.request<unknown>({
        method: 'GET',
        path: '/v1/testing/attendance/sessions',
        requiresAuth: true,
      }),
      'Read Testing Lab attendance report',
    );
    expect(attendanceReport).toBeTruthy();

    const myApplications = unwrap(
      await manager.request<ApplicationProjection[]>({
        method: 'GET',
        path: `/v1/testing/events/applications/me?eventId=${event.id}`,
        requiresAuth: true,
      }),
      'Read applicant event state',
    );
    expect(myApplications.map((candidate) => candidate.id)).toEqual(
      expect.arrayContaining([application.id, rejectedApplication.id]),
    );
  }, 120_000);
});


