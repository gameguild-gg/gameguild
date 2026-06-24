import { render, screen, within } from '@testing-library/react';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';

const { authMock } = vi.hoisted(() => ({
  authMock: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: authMock,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
  usePathname: () => '/',
}));

vi.mock('@/i18n', () => ({
  Link: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import { PublicWebsiteHeader } from '@/components/site/public-website-shell';
import CommunityPage from './[locale]/(community)/community/page';
import JobsPage from './[locale]/(contents)/(jobs)/jobs/page';
import LaunchPadPage from './[locale]/(contents)/(launch-pad)/launch-pad/page';
import ProjectsPage from './[locale]/(contents)/(projects)/projects/page';
import ProjectDetailPage from './[locale]/(contents)/(projects)/projects/[project]/page';
import TestingLabPage from './[locale]/(contents)/(testing-lab)/testing-lab/page';
import HomePage from './[locale]/(site)/page';

describe('public community website UX', () => {
  it('exposes the learning-to-community information architecture in the header', async () => {
    authMock.mockResolvedValueOnce(null);

    render(await PublicWebsiteHeader());

    const nav = screen.getByRole('navigation', { name: /main navigation/i });
    expect(within(nav).getAllByRole('link').map((link) => link.textContent)).toEqual([
      'Courses',
      'Programs',
      'Testing Lab',
      'Launch Pad',
      'Projects',
      'Community',
      'Jobs',
      'About',
    ]);
  });

  it('shows the authenticated member profile instead of sign-in calls to action', async () => {
    authMock.mockResolvedValueOnce({
      user: {
        id: 'user-1',
        name: 'Ada Lovelace',
        email: 'ada@gameguild.gg',
        image: null,
      },
      expires: '2026-12-31T00:00:00.000Z',
    });

    render(await PublicWebsiteHeader());

    expect(screen.queryByRole('link', { name: /^sign in$/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /join community/i })).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: /ada lovelace profile/i })).toHaveAttribute('href', '/dashboard');
    expect(screen.getByText('AL')).toBeInTheDocument();
  });

  it('turns the home page into a community gateway', async () => {
    render(await HomePage({ params: Promise.resolve({ locale: 'en-US' }) } as PageProps<'/[locale]'>));

    expect(screen.getByRole('heading', { name: /learn, build & connect/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /featured community projects/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /active members/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /upcoming playtests/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /community activity/i })).toBeInTheDocument();
  });

  it('renders a public project showcase and detail path', async () => {
    render(await ProjectsPage());

    expect(screen.getByRole('heading', { name: /project showcase/i })).toBeInTheDocument();
    expect(screen.getByAltText(/skybound courier project preview/i)).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /view project/i }).length).toBeGreaterThan(0);
    expect(screen.getByRole('link', { name: /submit to testing lab/i })).toBeInTheDocument();
  });

  it('renders project detail with creator, media, playtest status, and community CTAs', async () => {
    render(await ProjectDetailPage({ params: Promise.resolve({ project: 'skybound-courier' }) }));

    expect(screen.getByRole('heading', { name: /skybound courier/i })).toBeInTheDocument();
    expect(screen.getByAltText(/skybound courier project preview/i)).toBeInTheDocument();
    expect(screen.getAllByText(/creator/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/playtest/i).length).toBeGreaterThan(0);
    expect(screen.getByRole('link', { name: /join this playtest/i })).toBeInTheDocument();
  });

  it('renders the community hub and public testing lab entry', async () => {
    render(await CommunityPage());

    expect(screen.getByRole('heading', { name: /community hub/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /member spotlights/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /recent activity/i })).toBeInTheDocument();

    render(await TestingLabPage());
    expect(screen.getByRole('heading', { name: /testing lab/i })).toBeInTheDocument();
    expect(screen.getAllByText(/submit a build/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/define test goals/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/feedback report/i).length).toBeGreaterThan(0);
  });

  it('renders the public launch pad entry for release-ready projects', async () => {
    render(await LaunchPadPage());

    expect(screen.getByRole('heading', { name: /launch pad/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /from project to public release/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /readiness signals/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /open launch pad/i })).toBeInTheDocument();
  });

  it('replaces the jobs placeholder with a community opportunities page', async () => {
    render(await JobsPage());

    expect(screen.getByRole('heading', { name: /community opportunities/i })).toBeInTheDocument();
    expect(screen.getAllByText(/mentor/i).length).toBeGreaterThan(0);
    expect(screen.queryByText(/show modal immediately/i)).not.toBeInTheDocument();
  });
});
