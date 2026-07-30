import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getTestingRequestDetail: vi.fn(),
  getMembers: vi.fn(),
}));

vi.mock('@/lib/testing-lab', () => ({
  getTestingRequestDetail: mocks.getTestingRequestDetail,
  normalizeTestingRequestStatus: (status: string) => status,
}));
vi.mock('@/lib/community/queries/members', () => ({
  getMembers: mocks.getMembers,
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ children }: { children: ReactNode }) => children,
}));
vi.mock('@/lib/testing-lab/actions', () => ({
  addTestingParticipant: vi.fn(),
  deleteTestingRequest: vi.fn(),
  removeTestingParticipant: vi.fn(),
  restoreTestingRequest: vi.fn(),
  updateTestingRequest: vi.fn(),
}));

import TestingProjectDetailPage from './page';

describe('Testing Project detail', () => {
  it('uses human participant labels and a dialog for participant management', async () => {
    mocks.getTestingRequestDetail.mockResolvedValue({
      request: {
        id: 'request-1',
        title: 'Asterion usability pass',
        description: 'Validate the onboarding loop.',
        status: 'InProgress',
        isDeleted: false,
      },
      participants: [
        {
          id: 'participant-1',
          userId: 'user-1',
          status: 'Registered',
          timeSpentMinutes: 20,
        },
      ],
      sessions: [],
      feedback: [],
      accessIssues: [],
    });
    mocks.getMembers.mockResolvedValue({
      members: [{ id: 'user-1', displayName: 'Ana Member', email: 'ana@example.com' }],
      total: 1,
    });

    const user = userEvent.setup();
    const view = render(
      await TestingProjectDetailPage({
        params: Promise.resolve({ requestId: 'request-1' }),
      }),
    );

    expect(screen.getByText('Ana Member')).toBeInTheDocument();
    expect(screen.queryByText('user-1')).not.toBeInTheDocument();
    expect(view.container.querySelector('select')).toBeNull();

    await user.click(screen.getByRole('button', { name: 'Add participant' }));
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Add a participant' })).toBeInTheDocument();
  });
});
