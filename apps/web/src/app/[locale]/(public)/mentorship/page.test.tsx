import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  listMentors: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => <a href={href}>{children}</a>,
}));
vi.mock('@/lib/mentorship/actions', () => ({
  listMentors: mocks.listMentors,
}));

import MentorshipDirectoryPage from './page';

const mentor = {
  id: '0193bbbb-0000-7000-8000-000000000001',
  userId: '0193bbbb-0000-7000-8000-000000000010',
  displayName: 'Grace Hopper',
  headline: 'Gameplay engineer and mentor',
  bio: 'Shipped a dozen titles; loves coaching first-time builders.',
  role: 0,
  pricingModel: 0,
  freeContributionReason: 'Giving back to the community',
  status: 1,
  bookingMode: 0,
  offersIntroductoryCall: true,
  introductoryCallMinutes: 15,
  timezoneId: 'UTC',
  skills: [
    { id: '0193bbbb-0000-7000-8000-000000000011', skill: 'gameplay programming' },
    { id: '0193bbbb-0000-7000-8000-000000000012', skill: 'scoping' },
  ],
  publishedAt: '2026-10-20T00:00:00.000Z',
  createdAt: '2026-10-01T00:00:00.000Z',
  updatedAt: '2026-10-20T00:00:00.000Z',
};

describe('/mentorship directory page (issue #102)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(cleanup);

  it('lists published mentors with skills and the free intro badge', async () => {
    mocks.listMentors.mockResolvedValue([mentor]);

    render(
      await MentorshipDirectoryPage({
        searchParams: Promise.resolve({}),
      }),
    );

    expect(screen.getByRole('heading', { name: /1 mentor available/i })).toBeInTheDocument();
    expect(screen.getByText('Grace Hopper')).toBeInTheDocument();
    expect(screen.getByText('gameplay programming')).toBeInTheDocument();
    expect(screen.getByText(/Free 15-min intro/i)).toBeInTheDocument();
    expect(mocks.listMentors).toHaveBeenCalledWith({});
  });

  it('filters mentors by skill from the query string', async () => {
    mocks.listMentors.mockResolvedValue([]);

    render(
      await MentorshipDirectoryPage({
        searchParams: Promise.resolve({ skill: 'shaders' }),
      }),
    );

    expect(screen.getByRole('heading', { name: /No mentors match yet/i })).toBeInTheDocument();
    expect(mocks.listMentors).toHaveBeenCalledWith({ skill: 'shaders', search: undefined });
  });
});
