import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getWorkspaceProject: vi.fn(),
  getWorkspaceProjectVersions: vi.fn(),
}));

vi.mock('@/lib/workspaces', () => ({
  getWorkspaceProject: mocks.getWorkspaceProject,
  getWorkspaceProjectVersions: mocks.getWorkspaceProjectVersions,
}));
vi.mock('@/components/workspace/project-workspace', () => ({
  ProjectWorkspaceView: () => <section aria-label="Project overview" />,
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { readonly href: string; readonly children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('next/navigation', () => ({ notFound: vi.fn() }));

import Page from './page';

describe('Project Testing Lab readiness', () => {
  const renderPage = async () =>
    render(
      await Page({
        params: Promise.resolve({ locale: 'en-US', slug: 'neon-racer' }),
      } as never),
    );

  it('makes the first version the next action when the Project has no version', async () => {
    mocks.getWorkspaceProject.mockResolvedValue({
      id: 'project-1',
      slug: 'neon-racer',
      title: 'Neon Racer',
    });
    mocks.getWorkspaceProjectVersions.mockResolvedValue([]);

    await renderPage();

    expect(screen.getByRole('region', { name: 'Testing readiness' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Create first version' })).toHaveAttribute(
      'href',
      '/workspace/projects/neon-racer/versions-builds',
    );
  });

  it('asks the editor to prepare a Draft version before applications', async () => {
    mocks.getWorkspaceProject.mockResolvedValue({
      id: 'project-1',
      slug: 'neon-racer',
      title: 'Neon Racer',
    });
    mocks.getWorkspaceProjectVersions.mockResolvedValue([
      { id: 'version-1', versionNumber: '0.1.0', status: 'Draft' },
    ]);

    await renderPage();

    expect(screen.getByRole('link', { name: 'Prepare version for testing' })).toHaveAttribute(
      'href',
      '/workspace/projects/neon-racer/versions-builds',
    );
  });

  it('links an eligible version directly to Testing Lab events', async () => {
    mocks.getWorkspaceProject.mockResolvedValue({
      id: 'project-1',
      slug: 'neon-racer',
      title: 'Neon Racer',
    });
    mocks.getWorkspaceProjectVersions.mockResolvedValue([
      { id: 'version-1', versionNumber: '1.0.0', status: 'ReadyForTesting' },
    ]);

    await renderPage();

    expect(screen.getByRole('link', { name: 'Find Testing Lab events' })).toHaveAttribute(
      'href',
      '/testing-lab/events?projectId=project-1',
    );
  });
});
