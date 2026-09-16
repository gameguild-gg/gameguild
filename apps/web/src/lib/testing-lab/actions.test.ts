import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getRequestAuthContext: vi.fn(),
  createServerClient: vi.fn(() => ({})),
  revalidatePath: vi.fn(),
  postTestingSubmitSimple: vi.fn(),
  getTestingRequestsForGetTestingRequestsById: vi.fn(),
  putTestingRequests: vi.fn(),
  deleteTestingRequests: vi.fn(),
  postTestingRequestsRestore: vi.fn(),
  postTestingSessions: vi.fn(),
  getTestingSessionsForGetTestingSessionsById: vi.fn(),
  putTestingSessions: vi.fn(),
  deleteTestingSessions: vi.fn(),
  postTestingSessionsRestore: vi.fn(),
  postTestingSessionsAttendance: vi.fn(),
  postTestingSessionsProjects: vi.fn(),
  deleteTestingSessionsProjects: vi.fn(),
  postTestingLocations: vi.fn(),
  putTestingLocations: vi.fn(),
  deleteTestingLocations: vi.fn(),
  postTestingLocationsRestore: vi.fn(),
  postTestingRequestsParticipants: vi.fn(),
  deleteTestingRequestsParticipants: vi.fn(),
  postTestingSessionsRegister: vi.fn(),
  deleteTestingSessionsRegister: vi.fn(),
  postTestingSessionsWaitlist: vi.fn(),
  deleteTestingSessionsWaitlist: vi.fn(),
  postTestingFeedback: vi.fn(),
  postTestingFeedbackQuality: vi.fn(),
  postTestingFeedbackReport: vi.fn(),
  patchApiTestingLabSettings: vi.fn(),
  postApiTestingLabSettingsReset: vi.fn(),
  postApiTestingLabPermissionsRoleTemplates: vi.fn(),
  putApiTestingLabPermissionsRoleTemplates: vi.fn(),
  deleteApiTestingLabPermissionsRoleTemplates: vi.fn(),
  postApiTestingLabPermissionsUsersRoles: vi.fn(),
  deleteApiTestingLabPermissionsUsersRoles: vi.fn(),
  getApiTestingLabPermissionsUsers: vi.fn(),
  postApiTestingLabPermissionsUsersResources: vi.fn(),
  deleteApiTestingLabPermissionsUsersResources: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getRequestAuthContext: mocks.getRequestAuthContext,
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    TestingLabTestingRequestsModule: vi.fn(function TestingLabTestingRequestsModule() {
      return {
        postTestingSubmitSimple: mocks.postTestingSubmitSimple,
        getTestingRequestsForGetTestingRequestsById: mocks.getTestingRequestsForGetTestingRequestsById,
        putTestingRequests: mocks.putTestingRequests,
        deleteTestingRequests: mocks.deleteTestingRequests,
        postTestingRequestsRestore: mocks.postTestingRequestsRestore,
      };
    }),
    TestingLabTestingSessionsModule: vi.fn(function TestingLabTestingSessionsModule() {
      return {
        postTestingSessions: mocks.postTestingSessions,
        getTestingSessionsForGetTestingSessionsById: mocks.getTestingSessionsForGetTestingSessionsById,
        putTestingSessions: mocks.putTestingSessions,
        deleteTestingSessions: mocks.deleteTestingSessions,
        postTestingSessionsRestore: mocks.postTestingSessionsRestore,
        postTestingSessionsAttendance: mocks.postTestingSessionsAttendance,
        postTestingSessionsProjects: mocks.postTestingSessionsProjects,
        deleteTestingSessionsProjects: mocks.deleteTestingSessionsProjects,
      };
    }),
    TestingLabTestingLocationsModule: vi.fn(function TestingLabTestingLocationsModule() {
      return {
        postTestingLocations: mocks.postTestingLocations,
        putTestingLocations: mocks.putTestingLocations,
        deleteTestingLocations: mocks.deleteTestingLocations,
        postTestingLocationsRestore: mocks.postTestingLocationsRestore,
      };
    }),
    TestingLabTestingParticipantsModule: vi.fn(function TestingLabTestingParticipantsModule() {
      return {
        postTestingSessionsRegister: mocks.postTestingSessionsRegister,
        deleteTestingSessionsRegister: mocks.deleteTestingSessionsRegister,
        postTestingSessionsWaitlist: mocks.postTestingSessionsWaitlist,
        deleteTestingSessionsWaitlist: mocks.deleteTestingSessionsWaitlist,
        postTestingRequestsParticipants: mocks.postTestingRequestsParticipants,
        deleteTestingRequestsParticipants: mocks.deleteTestingRequestsParticipants,
      };
    }),
    TestingLabTestingFeedbackModule: vi.fn(function TestingLabTestingFeedbackModule() {
      return {
        postTestingFeedback: mocks.postTestingFeedback,
        postTestingFeedbackQuality: mocks.postTestingFeedbackQuality,
        postTestingFeedbackReport: mocks.postTestingFeedbackReport,
      };
    }),
    TestingLabSettingsModule: vi.fn(function TestingLabSettingsModule() {
      return {
        patchApiTestingLabSettings: mocks.patchApiTestingLabSettings,
        postApiTestingLabSettingsReset: mocks.postApiTestingLabSettingsReset,
      };
    }),
    TestingLabPermissionModule: vi.fn(function TestingLabPermissionModule() {
      return {
        postApiTestingLabPermissionsRoleTemplates: mocks.postApiTestingLabPermissionsRoleTemplates,
        putApiTestingLabPermissionsRoleTemplates: mocks.putApiTestingLabPermissionsRoleTemplates,
        deleteApiTestingLabPermissionsRoleTemplates: mocks.deleteApiTestingLabPermissionsRoleTemplates,
        postApiTestingLabPermissionsUsersRoles: mocks.postApiTestingLabPermissionsUsersRoles,
        deleteApiTestingLabPermissionsUsersRoles: mocks.deleteApiTestingLabPermissionsUsersRoles,
        getApiTestingLabPermissionsUsers: mocks.getApiTestingLabPermissionsUsers,
        postApiTestingLabPermissionsUsersResources: mocks.postApiTestingLabPermissionsUsersResources,
        deleteApiTestingLabPermissionsUsersResources: mocks.deleteApiTestingLabPermissionsUsersResources,
      };
    }),
  },
}));

import {
  addTestingParticipant,
  assignTestingLabRole,
  bulkUpdateTestingRequests,
  createTestingLabLocation,
  createTestingLabRole,
  createTestingSession,
  deleteTestingLabLocation,
  deleteTestingLabRole,
  deleteTestingRequest,
  deleteTestingSession,
  leaveTestingSessionWaitlist,
  linkTestingSessionProject,
  rateTestingFeedback,
  removeTestingParticipant,
  reportTestingFeedback,
  resetTestingLabSettings,
  restoreTestingRequest,
  restoreTestingLabLocation,
  restoreTestingSession,
  registerForTestingSession,
  revokeTestingLabRole,
  joinTestingSessionWaitlist,
  inspectTestingLabUserAccess,
  grantTestingLabResourcePermission,
  revokeTestingLabResourcePermission,
  submitTestingFeedback,
  submitTestingBuild,
  unlinkTestingSessionProject,
  unregisterFromTestingSession,
  updateTestingAttendance,
  updateTestingLabLocation,
  updateTestingLabRole,
  updateTestingLabSettings,
  updateTestingRequest,
  updateTestingSession,
} from './actions';

function form(values: Record<string, string>) {
  const data = new FormData();
  Object.entries(values).forEach(([key, value]) => data.set(key, value));
  return data;
}

describe('Testing Lab server actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getRequestAuthContext.mockResolvedValue({
      token: 'token',
      tenantId: 'tenant-1',
      session: { user: { id: 'manager-1' }, tenantId: 'tenant-1' },
    });
  });

  it('uses one request authentication context for both token and tenant', async () => {
    mocks.postApiTestingLabPermissionsRoleTemplates.mockResolvedValue({ ok: true, data: { id: 'role-1' } });

    await createTestingLabRole(form({ name: 'Facilitator' }));

    const clientOptions = mocks.createServerClient.mock.calls[0]?.[0];
    await expect(clientOptions.auth.getAccessToken()).resolves.toBe('token');
    await expect(clientOptions.tenant.getTenantId()).resolves.toBe('tenant-1');
    expect(mocks.getRequestAuthContext).toHaveBeenCalledTimes(1);
  });

  it('submits a build through the generated requests client', async () => {
    mocks.postTestingSubmitSimple.mockResolvedValue({ ok: true, data: { id: 'request-1' } });

    const result = await submitTestingBuild(
      form({
        title: 'Vertical slice',
        projectId: 'project-1',
        versionNumber: '0.3.0',
        maxTesters: '12',
        instructionsType: 'Text',
      }),
    );

    expect(result).toEqual({ success: true, data: { id: 'request-1' }, message: 'Testing request created.' });
    expect(mocks.postTestingSubmitSimple).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Vertical slice',
        projectId: 'project-1',
        versionNumber: '0.3.0',
        maxTesters: 12,
        instructionsType: 'Text',
      }),
    );
    expect(mocks.postTestingSubmitSimple.mock.calls[0]?.[0]).not.toHaveProperty('teamIdentifier');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/workspace/testing-lab');
  });

  it('returns validation errors without calling the API', async () => {
    const result = await submitTestingBuild(form({ title: '', projectId: '', versionNumber: '' }));

    expect(result).toEqual({ success: false, error: 'Title, project, and version are required.' });
    expect(mocks.postTestingSubmitSimple).not.toHaveBeenCalled();
  });

  it('surfaces field-level API validation details instead of the generic validation title', async () => {
    mocks.postTestingSubmitSimple.mockResolvedValue({
      ok: false,
      error: {
        message: 'One or more validation errors occurred.',
        fieldErrors: {
          TeamIdentifier: ['The TeamIdentifier field is required.'],
          StartDate: ['Start date must be in the future.'],
        },
      },
    });

    const result = await submitTestingBuild(
      form({
        title: 'Vertical slice',
        projectId: 'project-1',
        versionNumber: '0.3.0',
        instructionsType: 'Text',
      }),
    );

    expect(result).toEqual({
      success: false,
      error: 'TeamIdentifier: The TeamIdentifier field is required. StartDate: Start date must be in the future.',
    });
  });

  it('deletes and restores requests through generated client operations', async () => {
    mocks.deleteTestingRequests.mockResolvedValue({ ok: true, data: undefined });
    mocks.postTestingRequestsRestore.mockResolvedValue({ ok: true, data: undefined });

    await expect(deleteTestingRequest(form({ requestId: 'request-1' }))).resolves.toEqual({
      success: true,
      data: null,
      message: 'Testing request archived.',
    });
    await expect(restoreTestingRequest(form({ requestId: 'request-1' }))).resolves.toEqual({
      success: true,
      data: null,
      message: 'Testing request restored.',
    });
  });

  it('creates sessions and locations through their generated modules', async () => {
    mocks.postTestingSessions.mockResolvedValue({ ok: true, data: { id: 'session-1' } });
    mocks.postTestingLocations.mockResolvedValue({ ok: true, data: { id: 'location-1' } });

    const session = await createTestingSession(
      form({
        testingRequestId: 'request-1',
        locationId: 'location-1',
        sessionName: 'Friday playtest',
        sessionDate: '2026-08-01',
        startTime: '2026-08-01T18:00:00.000Z',
        endTime: '2026-08-01T20:00:00.000Z',
        maxTesters: '16',
        maxProjects: '4',
        status: 'Scheduled',
      }),
    );
    const location = await createTestingLabLocation(
      form({
        name: 'Remote Lab',
        isVirtual: 'true',
        virtualUrl: 'https://meet.gameguild.gg/lab',
        contactEmail: 'facilitator@gameguild.gg',
        contactPhone: '+55 11 5555-0100',
        status: 'Active',
        maxTestersCapacity: '30',
        maxProjectsCapacity: '8',
      }),
    );

    expect(session.success).toBe(true);
    expect(mocks.postTestingSessions).toHaveBeenCalledWith(expect.objectContaining({ managerUserId: 'manager-1' }));
    expect(location.success).toBe(true);
    expect(mocks.postTestingLocations).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Remote Lab',
        isVirtual: true,
        virtualUrl: 'https://meet.gameguild.gg/lab',
        contactEmail: 'facilitator@gameguild.gg',
        contactPhone: '+55 11 5555-0100',
      }),
    );
  });

  it('registers members or joins the waitlist through the generated participant module', async () => {
    mocks.postTestingSessionsRegister.mockResolvedValue({ ok: true, data: { id: 'registration-1' } });
    mocks.postTestingSessionsWaitlist.mockResolvedValue({ ok: true, data: { id: 'waitlist-1' } });

    await expect(registerForTestingSession(form({ sessionId: 'session-1', registrationType: 'Tester' }))).resolves.toEqual({
      success: true,
      data: { id: 'registration-1' },
      message: 'Registered for testing session.',
    });
    await expect(joinTestingSessionWaitlist(form({ sessionId: 'session-1', registrationType: 'Tester' }))).resolves.toEqual({
      success: true,
      data: { id: 'waitlist-1' },
      message: 'Added to session waitlist.',
    });
  });

  it('updates settings and manages roles through generated modules', async () => {
    mocks.patchApiTestingLabSettings.mockResolvedValue({ ok: true, data: { labName: 'GameGuild Testing Lab' } });
    mocks.postApiTestingLabPermissionsRoleTemplates.mockResolvedValue({ ok: true, data: { id: 'role-1' } });
    mocks.postApiTestingLabPermissionsUsersRoles.mockResolvedValue({ ok: true, data: undefined });

    const settings = await updateTestingLabSettings(
      form({
        labName: 'GameGuild Testing Lab',
        timezone: 'America/Sao_Paulo',
        maxSimultaneousSessions: '6',
        defaultSessionDuration: '120',
        allowPublicSignups: 'on',
      }),
    );
    const role = await createTestingLabRole(
      form({
        name: 'Facilitator',
        description: 'Runs testing sessions',
        canViewSessions: 'on',
        canCreateSessions: 'on',
      }),
    );
    const assignment = await assignTestingLabRole(
      form({
        userId: 'user-1',
        roleName: 'Facilitator',
        tenantId: 'tenant-1',
      }),
    );

    expect(settings.success).toBe(true);
    expect(role.success).toBe(true);
    expect(mocks.postApiTestingLabPermissionsRoleTemplates).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Facilitator',
        permissions: expect.objectContaining({ canViewSessions: true, canCreateSessions: true }),
      }),
    );
    expect(assignment.success).toBe(true);
  });
  it('inspects effective access and grants or revokes resource permissions', async () => {
    mocks.getApiTestingLabPermissionsUsers.mockResolvedValue({
      ok: true,
      data: { userId: 'user-1', assignedRoles: ['Facilitator'], permissions: { canViewSessions: true } },
    });
    mocks.postApiTestingLabPermissionsUsersResources.mockResolvedValue({ ok: true, data: undefined });
    mocks.deleteApiTestingLabPermissionsUsersResources.mockResolvedValue({ ok: true, data: undefined });

    await expect(inspectTestingLabUserAccess(form({ userId: 'user-1', tenantId: 'tenant-1' }))).resolves.toEqual({
      success: true,
      data: { userId: 'user-1', assignedRoles: ['Facilitator'], permissions: { canViewSessions: true } },
      message: 'Effective Testing Lab access loaded.',
    });
    await expect(
      grantTestingLabResourcePermission(
        form({
          userId: 'user-1',
          resourceType: 'TestingSession',
          resourceId: 'session-1',
          action: 'edit',
          expiresAt: '2026-08-15T18:00',
        }),
      ),
    ).resolves.toEqual({ success: true, data: null, message: 'Resource permission granted.' });
    await expect(
      revokeTestingLabResourcePermission(form({ userId: 'user-1', resourceType: 'TestingSession', resourceId: 'session-1', action: 'edit' })),
    ).resolves.toEqual({ success: true, data: null, message: 'Resource permission revoked.' });

    expect(mocks.getApiTestingLabPermissionsUsers).toHaveBeenCalledWith('user-1', { tenantId: 'tenant-1' });
    expect(mocks.postApiTestingLabPermissionsUsersResources).toHaveBeenCalledWith(
      'user-1',
      'TestingSession',
      'session-1',
      expect.objectContaining({ action: 'edit', expiresAt: expect.stringMatching(/^2026-08-15T/) }),
    );
    expect(mocks.deleteApiTestingLabPermissionsUsersResources).toHaveBeenCalledWith('user-1', 'TestingSession', 'session-1', {
      action: 'edit',
      tenantId: undefined,
    });
  });

  it('updates requests and sessions from their current persisted state', async () => {
    mocks.getTestingRequestsForGetTestingRequestsById.mockResolvedValue({
      ok: true,
      data: {
        id: 'request-1',
        status: 'Open',
        startDate: '2026-09-20T10:00:00.000Z',
        endDate: '2026-09-20T12:00:00.000Z',
      },
    });
    mocks.putTestingRequests.mockResolvedValue({ ok: true, data: { id: 'request-1' } });
    mocks.getTestingSessionsForGetTestingSessionsById.mockResolvedValue({
      ok: true,
      data: {
        id: 'session-1',
        sessionName: 'Original session',
        sessionDate: '2026-09-20',
        startTime: '2026-09-20T10:00:00.000Z',
        endTime: '2026-09-20T12:00:00.000Z',
        locationId: 'location-1',
        maxTesters: 10,
        maxProjects: 3,
        status: 'Scheduled',
      },
    });
    mocks.putTestingSessions.mockResolvedValue({ ok: true, data: { id: 'session-1' } });

    await expect(
      updateTestingRequest(
        form({
          requestId: 'request-1',
          title: 'Updated build',
          description: 'A focused objective',
          maxTesters: '18',
          status: 'Active',
        }),
      ),
    ).resolves.toMatchObject({ success: true, message: 'Testing request updated.' });
    await expect(
      updateTestingSession(
        form({
          sessionId: 'session-1',
          sessionName: 'Updated session',
          locationId: 'location-2',
          maxTesters: '20',
          maxProjects: '5',
          status: 'Active',
        }),
      ),
    ).resolves.toMatchObject({ success: true, message: 'Testing session updated.' });

    expect(mocks.putTestingRequests).toHaveBeenCalledWith(
      'request-1',
      expect.objectContaining({
        title: 'Updated build',
        startDate: '2026-09-20T10:00:00.000Z',
        endDate: '2026-09-20T12:00:00.000Z',
        maxTesters: 18,
        status: 'Active',
      }),
    );
    expect(mocks.putTestingSessions).toHaveBeenCalledWith(
      'session-1',
      expect.objectContaining({
        sessionName: 'Updated session',
        locationId: 'location-2',
        maxTesters: 20,
        maxProjects: 5,
        status: 'Active',
      }),
    );
  });

  it('executes the remaining request, session, participant, feedback, location, settings, and role lifecycle actions', async () => {
    const successful = { ok: true, data: undefined };
    [
      mocks.deleteTestingSessions,
      mocks.postTestingSessionsRestore,
      mocks.postTestingSessionsAttendance,
      mocks.postTestingSessionsProjects,
      mocks.deleteTestingSessionsProjects,
      mocks.postTestingRequestsParticipants,
      mocks.deleteTestingRequestsParticipants,
      mocks.deleteTestingSessionsRegister,
      mocks.deleteTestingSessionsWaitlist,
      mocks.postTestingFeedback,
      mocks.postTestingFeedbackQuality,
      mocks.postTestingFeedbackReport,
      mocks.putTestingLocations,
      mocks.deleteTestingLocations,
      mocks.postTestingLocationsRestore,
      mocks.postApiTestingLabSettingsReset,
      mocks.putApiTestingLabPermissionsRoleTemplates,
      mocks.deleteApiTestingLabPermissionsRoleTemplates,
      mocks.deleteApiTestingLabPermissionsUsersRoles,
    ].forEach((mock) => mock.mockResolvedValue(successful));

    const results = await Promise.all([
      deleteTestingSession(form({ sessionId: 'session-1' })),
      restoreTestingSession(form({ sessionId: 'session-1' })),
      updateTestingAttendance(form({ sessionId: 'session-1', userId: 'user-1', attendanceStatus: 'Present' })),
      linkTestingSessionProject(form({ sessionId: 'session-1', projectId: 'project-1', projectVersionId: 'version-1', notes: 'Focus' })),
      unlinkTestingSessionProject(form({ sessionId: 'session-1', projectId: 'project-1' })),
      addTestingParticipant(form({ requestId: 'request-1', userId: 'user-1' })),
      removeTestingParticipant(form({ requestId: 'request-1', userId: 'user-1' })),
      unregisterFromTestingSession(form({ sessionId: 'session-1' })),
      leaveTestingSessionWaitlist(form({ sessionId: 'session-1' })),
      submitTestingFeedback(
        form({
          testingRequestId: 'request-1',
          sessionId: 'session-1',
          feedbackResponses: '{"fun":"high"}',
          overallRating: '5',
          wouldRecommend: 'on',
        }),
      ),
      rateTestingFeedback(form({ feedbackId: 'feedback-1', quality: 'High' })),
      reportTestingFeedback(form({ feedbackId: 'feedback-1', reason: 'Needs moderation' })),
      updateTestingLabLocation(
        form({
          locationId: 'location-1',
          name: 'Updated lab',
          isVirtual: 'true',
          maxTestersCapacity: '24',
          maxProjectsCapacity: '6',
          status: 'Maintenance',
        }),
      ),
      deleteTestingLabLocation(form({ locationId: 'location-1' })),
      restoreTestingLabLocation(form({ locationId: 'location-1' })),
      resetTestingLabSettings(),
      updateTestingLabRole(
        form({
          idOrName: 'role-1',
          name: 'Lead facilitator',
          canViewRequests: 'true',
          canApproveRequests: 'on',
        }),
      ),
      deleteTestingLabRole(form({ idOrName: 'role-1' })),
      revokeTestingLabRole(form({ userId: 'user-1', roleName: 'Facilitator', tenantId: 'tenant-1' })),
    ]);

    expect(results).toHaveLength(19);
    expect(results.every((result) => result.success)).toBe(true);
    expect(mocks.postTestingFeedback).toHaveBeenCalledWith(
      expect.objectContaining({ overallRating: 5, wouldRecommend: true }),
    );
    expect(mocks.putTestingLocations).toHaveBeenCalledWith(
      'location-1',
      expect.objectContaining({ isVirtual: true, maxTestersCapacity: 24, status: 'Maintenance' }),
    );
    expect(mocks.deleteApiTestingLabPermissionsUsersRoles).toHaveBeenCalledWith(
      'user-1',
      'Facilitator',
      { tenantId: 'tenant-1' },
    );
  });

  it('archives and restores request selections in bulk and stops on an API failure', async () => {
    const selection = new FormData();
    selection.append('requestIds', 'request-1');
    selection.append('requestIds', 'request-2');
    selection.set('operation', 'archive');
    mocks.deleteTestingRequests
      .mockResolvedValueOnce({ ok: true, data: undefined })
      .mockResolvedValueOnce({ ok: true, data: undefined });

    await expect(bulkUpdateTestingRequests(selection)).resolves.toEqual({
      success: true,
      data: { processed: 2 },
      message: '2 requests updated.',
    });

    selection.set('operation', 'restore');
    mocks.postTestingRequestsRestore
      .mockResolvedValueOnce({ ok: true, data: undefined })
      .mockResolvedValueOnce({ ok: false, error: { message: 'Restore denied' } });
    await expect(bulkUpdateTestingRequests(selection)).resolves.toEqual({
      success: false,
      error: 'Restore denied',
    });
  });

  it('returns deterministic validation failures before invoking generated clients', async () => {
    const empty = new FormData();
    const invalidCalls = [
      updateTestingRequest(empty),
      deleteTestingRequest(empty),
      restoreTestingRequest(empty),
      bulkUpdateTestingRequests(empty),
      createTestingSession(empty),
      updateTestingSession(empty),
      deleteTestingSession(empty),
      restoreTestingSession(empty),
      updateTestingAttendance(empty),
      linkTestingSessionProject(empty),
      unlinkTestingSessionProject(empty),
      addTestingParticipant(empty),
      removeTestingParticipant(empty),
      registerForTestingSession(empty),
      unregisterFromTestingSession(empty),
      joinTestingSessionWaitlist(empty),
      leaveTestingSessionWaitlist(empty),
      submitTestingFeedback(empty),
      rateTestingFeedback(empty),
      reportTestingFeedback(empty),
      createTestingLabLocation(empty),
      updateTestingLabLocation(empty),
      deleteTestingLabLocation(empty),
      restoreTestingLabLocation(empty),
      updateTestingLabSettings(empty),
      createTestingLabRole(empty),
      updateTestingLabRole(empty),
      deleteTestingLabRole(empty),
      assignTestingLabRole(empty),
      revokeTestingLabRole(empty),
      inspectTestingLabUserAccess(empty),
      grantTestingLabResourcePermission(empty),
      revokeTestingLabResourcePermission(empty),
    ];

    const results = await Promise.all(invalidCalls);
    expect(results).toHaveLength(33);
    expect(results.every((result) => !result.success && result.error.length > 0)).toBe(true);
  });

  it('normalizes generated-client failures and unexpected exceptions', async () => {
    mocks.postTestingSubmitSimple.mockResolvedValue({
      ok: false,
      error: { message: 'Submission rejected' },
    });
    await expect(
      submitTestingBuild(form({ title: 'Build', projectId: 'project-1', versionNumber: '1.0.0' })),
    ).resolves.toEqual({ success: false, error: 'Submission rejected' });

    mocks.postTestingSubmitSimple.mockRejectedValue(new Error('Network unavailable'));
    await expect(
      submitTestingBuild(form({ title: 'Build', projectId: 'project-1', versionNumber: '1.0.0' })),
    ).resolves.toEqual({ success: false, error: 'Network unavailable' });

    mocks.getApiTestingLabPermissionsUsers.mockRejectedValue('offline');
    await expect(inspectTestingLabUserAccess(form({ userId: 'user-1' }))).resolves.toEqual({
      success: false,
      error: 'Could not load Testing Lab access.',
    });
  });

  it('covers fallback values, lookup failures, and invalid optional inputs', async () => {
    mocks.postTestingSubmitSimple.mockResolvedValue({ ok: true, data: undefined });
    await expect(
      submitTestingBuild(
        form({
          title: 'Build',
          projectId: 'project-1',
          versionNumber: '1.0.0',
          maxTesters: 'not-a-number',
          startDate: 'not-a-date',
          endDate: 'not-a-date',
        }),
      ),
    ).resolves.toMatchObject({ success: true });
    expect(mocks.postTestingSubmitSimple).toHaveBeenLastCalledWith(
      expect.objectContaining({ maxTesters: null, startDate: null, endDate: null }),
    );

    mocks.getTestingRequestsForGetTestingRequestsById.mockResolvedValueOnce({
      ok: false,
      error: { message: 'Request missing' },
    });
    await expect(updateTestingRequest(form({ requestId: 'missing', title: 'Build' }))).resolves.toEqual({
      success: false,
      error: 'Request missing',
    });

    mocks.getTestingRequestsForGetTestingRequestsById.mockResolvedValueOnce({
      ok: true,
      data: { status: 'Open', startDate: null, endDate: null },
    });
    await expect(updateTestingRequest(form({ requestId: 'request-1', title: 'Build' }))).resolves.toEqual({
      success: false,
      error: 'Testing requests require both a start and end date.',
    });

    mocks.getTestingRequestsForGetTestingRequestsById.mockResolvedValueOnce({
      ok: true,
      data: { status: 'Open', startDate: null, endDate: null },
    });
    mocks.putTestingRequests.mockResolvedValueOnce({ ok: true, data: { id: 'request-1' } });
    await expect(
      updateTestingRequest(
        form({
          requestId: 'request-1',
          title: 'Build',
          startDate: '2026-10-01T10:00',
          endDate: '2026-10-01T12:00',
        }),
      ),
    ).resolves.toMatchObject({ success: true });

    const invalidBulk = new FormData();
    invalidBulk.append('requestIds', 'request-1');
    invalidBulk.append('requestIds', new Blob(['ignored']), 'ignored.txt');
    invalidBulk.set('operation', 'publish');
    await expect(bulkUpdateTestingRequests(invalidBulk)).resolves.toEqual({
      success: false,
      error: 'Choose a valid bulk action.',
    });

    mocks.getRequestAuthContext.mockResolvedValueOnce({
      token: 'token',
      tenantId: 'tenant-1',
      session: { user: { id: ' ' }, tenantId: 'tenant-1' },
    });
    await expect(
      createTestingSession(
        form({
          testingRequestId: 'request-1',
          locationId: 'location-1',
          sessionName: 'Session',
          sessionDate: '2026-10-01',
          startTime: '2026-10-01T10:00',
          endTime: '2026-10-01T12:00',
        }),
      ),
    ).resolves.toEqual({ success: false, error: 'A session manager is required.' });

    mocks.postTestingSessions.mockResolvedValueOnce({ ok: true, data: { id: 'session-1' } });
    await expect(
      createTestingSession(
        form({
          testingRequestId: 'request-1',
          locationId: 'location-1',
          managerUserId: 'manager-2',
          sessionName: 'Session',
          sessionDate: '2026-10-01',
          startTime: '2026-10-01T10:00',
          endTime: '2026-10-01T12:00',
          maxTesters: 'invalid',
          maxProjects: 'invalid',
        }),
      ),
    ).resolves.toMatchObject({ success: true });
    expect(mocks.postTestingSessions).toHaveBeenLastCalledWith(
      expect.objectContaining({ managerUserId: 'manager-2', maxTesters: 0, maxProjects: 0, status: 'Scheduled' }),
    );

    mocks.getTestingSessionsForGetTestingSessionsById.mockResolvedValueOnce({
      ok: false,
      error: { message: 'Session missing' },
    });
    await expect(updateTestingSession(form({ sessionId: 'missing' }))).resolves.toEqual({
      success: false,
      error: 'Session missing',
    });

    const persistedSession = {
      sessionName: 'Persisted',
      sessionDate: '2026-10-01',
      startTime: '2026-10-01T10:00:00.000Z',
      endTime: '2026-10-01T12:00:00.000Z',
      locationId: 'location-1',
      maxTesters: 10,
      maxProjects: 3,
      status: 'Scheduled',
    };
    mocks.getTestingSessionsForGetTestingSessionsById.mockResolvedValueOnce({ ok: true, data: persistedSession });
    mocks.putTestingSessions.mockResolvedValueOnce({ ok: true, data: { id: 'session-1' } });
    await expect(updateTestingSession(form({ sessionId: 'session-1' }))).resolves.toMatchObject({ success: true });
    expect(mocks.putTestingSessions).toHaveBeenLastCalledWith('session-1', expect.objectContaining(persistedSession));

    mocks.postTestingSessionsRegister.mockResolvedValueOnce({ ok: true, data: undefined });
    mocks.postTestingSessionsWaitlist.mockResolvedValueOnce({ ok: true, data: undefined });
    await expect(registerForTestingSession(form({ sessionId: 'session-1' }))).resolves.toMatchObject({ success: true });
    await expect(joinTestingSessionWaitlist(form({ sessionId: 'session-1' }))).resolves.toMatchObject({ success: true });

    mocks.postTestingLocations.mockResolvedValueOnce({ ok: true, data: undefined });
    await expect(createTestingLabLocation(form({ name: 'Default lab' }))).resolves.toMatchObject({ success: true });
    expect(mocks.postTestingLocations).toHaveBeenLastCalledWith(
      expect.objectContaining({ maxTestersCapacity: 0, maxProjectsCapacity: 0, status: 'Active' }),
    );

    mocks.putTestingLocations.mockResolvedValueOnce({ ok: true, data: undefined });
    await expect(updateTestingLabLocation(form({ locationId: 'location-1' }))).resolves.toMatchObject({ success: true });
    expect(mocks.putTestingLocations).toHaveBeenLastCalledWith(
      'location-1',
      expect.objectContaining({ status: 'Active' }),
    );

    mocks.deleteApiTestingLabPermissionsUsersRoles.mockResolvedValueOnce({ ok: true, data: undefined });
    await expect(revokeTestingLabRole(form({ userId: 'user-1', roleName: 'Facilitator' }))).resolves.toMatchObject({ success: true });
    expect(mocks.deleteApiTestingLabPermissionsUsersRoles).toHaveBeenLastCalledWith(
      'user-1',
      'Facilitator',
      { tenantId: undefined },
    );

    mocks.getApiTestingLabPermissionsUsers.mockResolvedValueOnce({
      ok: false,
      error: { message: 'Access denied' },
    });
    await expect(inspectTestingLabUserAccess(form({ userId: 'user-1' }))).resolves.toEqual({
      success: false,
      error: 'Access denied',
    });
    mocks.getApiTestingLabPermissionsUsers.mockRejectedValueOnce(new Error('Inspection unavailable'));
    await expect(inspectTestingLabUserAccess(form({ userId: 'user-1' }))).resolves.toEqual({
      success: false,
      error: 'Inspection unavailable',
    });

    mocks.postTestingSubmitSimple.mockRejectedValueOnce('offline');
    await expect(
      submitTestingBuild(form({ title: 'Build', projectId: 'project-1', versionNumber: '1.0.0' })),
    ).resolves.toEqual({ success: false, error: 'The Testing Lab operation failed.' });
  });
});
