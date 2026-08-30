import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ getWorkspaceTeams: vi.fn() }));

vi.mock('@/lib/workspaces', () => ({ getWorkspaceTeams: mocks.getWorkspaceTeams }));
vi.mock('@/lib/workspace-actions', () => ({ createProjectForm: vi.fn() }));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { readonly href: string; readonly children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

import NewProjectPage from './page';

describe('member Project creation', () => {
  it('explains personal and Team ownership before creation', async () => {
    mocks.getWorkspaceTeams.mockResolvedValue([
      { id: 'team-1', name: 'Pixel Forge', slug: 'pixel-forge' },
    ]);

    render(await NewProjectPage());

    expect(screen.getByRole('heading', { name: 'Create Project' })).toBeInTheDocument();
    expect(screen.getByRole('combobox', { name: 'Project ownership' })).toHaveDisplayValue(
      'Personal project',
    );
    expect(screen.getByRole('option', { name: 'Team project · Pixel Forge' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Cancel' })).toHaveAttribute(
      'href',
      '/workspace/projects',
    );
  });
});
