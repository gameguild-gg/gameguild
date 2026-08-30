import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  request: vi.fn(),
  revalidatePath: vi.fn(),
  redirect: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
  redirect: mocks.redirect,
}));

import {
  counterProjectAgreementForm,
  createProjectAgreementForm,
  createProjectAllocationForm,
  createProjectMilestoneForm,
  createProjectTaskForm,
  createTeamForm,
  createTeamInvitationForm,
  transitionProjectForm,
} from './workspace-actions';

describe('workspace form date serialization', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ tenantId: 'tenant-1' });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({ request: mocks.request });
    mocks.request.mockResolvedValue({ ok: true, data: {} });
  });

  it('sends the UTC date selected by the Team invitation picker', async () => {
    const formData = new FormData();
    formData.set('teamId', 'team-1');
    formData.set('email', 'member@example.test');
    formData.set('authority', 'Member');
    formData.set('expiresAt', '2026-08-21T12:00');
    formData.set('returnPath', '/teams/team-1/invitations');

    await createTeamInvitationForm(formData);

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/teams/team-1/invitations',
      body: {
        userId: null,
        email: 'member@example.test',
        authority: 'Member',
        expiresAt: '2026-08-21T12:00:00.000Z',
      },
      requiresAuth: true,
    });
  });

  it('derives a stable Team slug when JavaScript does not provide one', async () => {
    mocks.request.mockResolvedValue({
      ok: true,
      data: { id: 'team-1', slug: 'space-cadets' },
    });
    const formData = new FormData();
    formData.set('name', '  Space Cadets  ');
    formData.set('visibility', 'Private');

    await createTeamForm(formData);

    expect(mocks.request).toHaveBeenCalledWith(
      expect.objectContaining({
        body: expect.objectContaining({ slug: 'space-cadets' }),
      }),
    );
  });

  it('normalizes optional task and milestone dates before calling the API', async () => {
    const task = new FormData();
    task.set('projectId', 'project-1');
    task.set('columnId', 'column-1');
    task.set('title', 'Prepare QA build');
    task.set('priority', 'Medium');
    task.set('dueAt', '2026-08-22T09:30');
    task.set('returnPath', '/projects/project-1/work');

    const milestone = new FormData();
    milestone.set('projectId', 'project-1');
    milestone.set('name', 'QA complete');
    milestone.set('dueAt', '2026-08-23T18:45');
    milestone.set('returnPath', '/projects/project-1/work');

    await createProjectTaskForm(task);
    await createProjectMilestoneForm(milestone);

    expect(mocks.request).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({
        body: expect.objectContaining({
          priority: 'Normal',
          dueAt: '2026-08-22T09:30:00.000Z',
        }),
      }),
    );
    expect(mocks.request).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({
        body: expect.objectContaining({ dueAt: '2026-08-23T18:45:00.000Z' }),
      }),
    );
  });

  it('normalizes allocation and agreement date ranges before calling the API', async () => {
    const allocation = new FormData();
    allocation.set('projectId', 'project-1');
    allocation.set('projectTeamId', 'project-team-1');
    allocation.set('userId', 'user-1');
    allocation.set('function', 'Tester');
    allocation.set('startsAt', '2026-08-24T08:00');
    allocation.set('endsAt', '2026-08-25T17:00');
    allocation.set('returnPath', '/projects/project-1/people');

    const agreement = new FormData();
    agreement.set('projectId', 'project-1');
    agreement.set('proposingTeamId', 'team-1');
    agreement.set('receivingTeamId', 'team-2');
    agreement.set('scope', 'Browser QA');
    agreement.set('deliverables', 'Verified build');
    agreement.set('startsAt', '2026-08-26T10:00');
    agreement.set('endsAt', '2026-08-27T19:15');
    agreement.set('returnPath', '/projects/project-1/access');

    await createProjectAllocationForm(allocation);
    await createProjectAgreementForm(agreement);

    expect(mocks.request).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({
        body: expect.objectContaining({
          startsAt: '2026-08-24T08:00:00.000Z',
          endsAt: '2026-08-25T17:00:00.000Z',
        }),
      }),
    );
    expect(mocks.request).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({
        body: expect.objectContaining({
          startsAt: '2026-08-26T10:00:00.000Z',
          endsAt: '2026-08-27T19:15:00.000Z',
        }),
      }),
    );
  });

  it('normalizes counterproposal date ranges before calling the API', async () => {
    const formData = new FormData();
    formData.set('projectId', 'project-1');
    formData.set('agreementId', 'agreement-1');
    formData.set('scope', 'Revised browser QA');
    formData.set('deliverables', 'Verified revised build');
    formData.set('startsAt', '2026-08-28T11:00');
    formData.set('endsAt', '2026-08-29T20:30');
    formData.set('returnPath', '/projects/project-1/access');

    await counterProjectAgreementForm(formData);

    expect(mocks.request).toHaveBeenCalledWith(
      expect.objectContaining({
        body: expect.objectContaining({
          startsAt: '2026-08-28T11:00:00.000Z',
          endsAt: '2026-08-29T20:30:00.000Z',
        }),
      }),
    );
  });

  it('restores an archived Project through the lifecycle action', async () => {
    const formData = new FormData();
    formData.set('projectId', 'project-1');
    formData.set('projectAction', 'restore');
    formData.set('returnPath', '/projects/project-1/settings');

    await transitionProjectForm(formData);

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/projects/project-1:restore',
      requiresAuth: true,
    });
  });
});
