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
  it('explains project ownership and supports opening creation from a team', async () => {
    mocks.getWorkspaceTeams.mockResolvedValue([
      { id: 'team-1', name: 'Pixel Forge', slug: 'pixel-forge' },
    ]);

    render(await NewProjectPage({ searchParams: Promise.resolve({ teamId: 'team-1' }) }));

    expect(screen.getByRole('heading', { name: 'Create a project' })).toBeInTheDocument();
    expect(screen.getByRole('combobox', { name: 'Who owns this project?' })).toHaveDisplayValue(
      'Pixel Forge',
    );
    expect(screen.getByRole('combobox', { name: 'Project type' })).toHaveDisplayValue('Game');
    expect(screen.getByRole('combobox', { name: 'Who can discover it?' })).toHaveDisplayValue('Private');
    expect(screen.getByRole('option', { name: 'Pixel Forge' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Cancel' })).toHaveAttribute(
      'href',
      '/workspace/projects',
    );
  });
});
