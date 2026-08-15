import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getWorkspaceProjects: vi.fn(),
  getWorkspaceProjectBoard: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('@/lib/workspaces', () => ({
  getWorkspaceProjects: mocks.getWorkspaceProjects,
  getWorkspaceProjectBoard: mocks.getWorkspaceProjectBoard,
}));

import WorkPage from './page';

describe('member work page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getWorkspaceProjects.mockResolvedValue([
      { id: 'project-1', slug: 'neon-racer', title: 'Neon Racer' },
    ]);
    mocks.getWorkspaceProjectBoard.mockResolvedValue({
      columns: [
        {
          id: 'col-1',
          name: 'To do',
          tasks: [
            { id: 'task-1', title: 'Design HUD', status: 'InProgress', priority: 'High' },
            { id: 'task-2', title: 'Ship demo', status: 'Done', priority: 'Low' },
          ],
        },
      ],
    });
  });
  afterEach(cleanup);

  it('lists open tasks linking into /projects/:slug/work/:taskId', async () => {
    render(await WorkPage());

    expect(screen.getByText('Design HUD')).toBeInTheDocument();
    expect(screen.queryByText('Ship demo')).not.toBeInTheDocument();

    const link = screen.getByRole('link', { name: /Design HUD/ });
    expect(link).toHaveAttribute('href', '/projects/neon-racer/work/task-1');
  });

  it('shows the empty state when no board exists', async () => {
    mocks.getWorkspaceProjectBoard.mockResolvedValue(null);

    render(await WorkPage());

    expect(screen.getByText('No open tasks in your accessible Projects.')).toBeInTheDocument();
  });
});
