import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getWorkspaceProjects: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('@/lib/workspaces', () => ({
  getWorkspaceProjects: mocks.getWorkspaceProjects,
}));

import ProjectsPage from './page';

const projects = [
  {
    id: 'project-1',
    slug: 'neon-racer',
    title: 'Neon Racer',
    status: 'Draft',
    visibility: 'Private',
    shortDescription: 'A racing game',
    description: null,
  },
];

describe('member projects list page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getWorkspaceProjects.mockResolvedValue(projects);
  });
  afterEach(cleanup);

  it('lists projects with links to the new /projects/:slug routes', async () => {
    render(await ProjectsPage());

    expect(screen.getByRole('heading', { name: 'Projects' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Neon Racer/ })).toHaveAttribute(
      'href',
      '/projects/neon-racer',
    );
  });

  it('links project creation to /projects/new', async () => {
    render(await ProjectsPage());

    expect(screen.getByRole('link', { name: /Create Project/ })).toHaveAttribute(
      'href',
      '/projects/new',
    );
  });

  it('shows the empty state when no projects exist', async () => {
    mocks.getWorkspaceProjects.mockResolvedValue([]);

    render(await ProjectsPage());

    expect(screen.getByText('No Projects yet')).toBeInTheDocument();
  });
});
