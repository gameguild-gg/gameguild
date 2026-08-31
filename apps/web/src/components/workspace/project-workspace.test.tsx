import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getWorkspaceProject: vi.fn(),
  getWorkspaceProjectOwnership: vi.fn(),
  getWorkspaceProjectVersions: vi.fn(),
  getWorkspaceProjectBoard: vi.fn(),
  getWorkspaceProjectTask: vi.fn(),
  getWorkspaceProjectMilestones: vi.fn(),
  getWorkspaceProjectLabels: vi.fn(),
  getWorkspaceProjectWorkHistory: vi.fn(),
  getWorkspaceProjectCollaborators: vi.fn(),
  getWorkspaceLibrary: vi.fn(),
  notFound: vi.fn(() => {
    throw new Error('not-found');
  }),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning', notFound: mocks.notFound }));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('@/lib/workspaces', () => ({
  getWorkspaceProject: mocks.getWorkspaceProject,
  getWorkspaceProjectOwnership: mocks.getWorkspaceProjectOwnership,
  getWorkspaceProjectVersions: mocks.getWorkspaceProjectVersions,
  getWorkspaceProjectBoard: mocks.getWorkspaceProjectBoard,
  getWorkspaceProjectTask: mocks.getWorkspaceProjectTask,
  getWorkspaceProjectMilestones: mocks.getWorkspaceProjectMilestones,
  getWorkspaceProjectLabels: mocks.getWorkspaceProjectLabels,
  getWorkspaceProjectWorkHistory: mocks.getWorkspaceProjectWorkHistory,
  getWorkspaceProjectCollaborators: mocks.getWorkspaceProjectCollaborators,
  getWorkspaceLibrary: mocks.getWorkspaceLibrary,
  getManagedTeams: vi.fn().mockResolvedValue([]),
  getWorkspaceTeams: vi.fn().mockResolvedValue([]),
}));
vi.mock('@/lib/workspace-actions', () => ({
  addProjectTeamForm: vi.fn(),
  addProjectTaskChecklistForm: vi.fn(),
  addProjectTaskCommentForm: vi.fn(),
  addProjectTaskDependencyForm: vi.fn(),
  addProjectCollaboratorForm: vi.fn(),
  assignProjectTaskLabelForm: vi.fn(),
  changeProjectAgreementForm: vi.fn(),
  counterProjectAgreementForm: vi.fn(),
  createProjectAgreementForm: vi.fn(),
  createProjectAllocationForm: vi.fn(),
  createProjectLabelForm: vi.fn(),
  createProjectMilestoneForm: vi.fn(),
  createProjectTaskForm: vi.fn(),
  createProjectVersionForm: vi.fn(),
  deleteProjectForm: vi.fn(),
  moveProjectTaskForm: vi.fn(),
  removeProjectAllocationForm: vi.fn(),
  removeProjectCollaboratorForm: vi.fn(),
  removeProjectTeamForm: vi.fn(),
  setProjectTaskChecklistForm: vi.fn(),
  transferProjectOwnerTeamForm: vi.fn(),
  transitionProjectForm: vi.fn(),
  transitionProjectVersionForm: vi.fn(),
  updateProjectForm: vi.fn(),
}));
vi.mock('@/components/workspaces/context-workspace-nav', () => ({
  ContextWorkspaceNav: ({
    base,
    active,
    items,
  }: {
    base: string;
    active: string;
    items: string[];
  }) => (
    <nav data-testid="workspace-nav" data-base={base} data-active={active}>
      {items.join(',')}
    </nav>
  ),
}));
vi.mock('@/components/workspaces/workspace-library-panel', () => ({
  WorkspaceLibraryPanel: () => <div data-testid="library-panel" />,
}));

import ProjectWorkspaceView from './project-workspace';

const project = {
  id: 'project-1',
  slug: 'neon-racer',
  title: 'Neon Racer',
  status: 'Draft',
  visibility: 'Private',
  shortDescription: 'A racing game',
  description: null,
};

function props(slug: string, section?: string) {
  return { slug, section };
}

describe('project workspace page (member surface)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getWorkspaceProject.mockResolvedValue(project);
    mocks.getWorkspaceProjectOwnership.mockResolvedValue({
      teams: [],
      allocations: [],
      agreements: [],
    });
    mocks.getWorkspaceProjectVersions.mockResolvedValue([
      { id: 'v1', versionNumber: '0.1.0', status: 'Draft', releaseNotes: 'First build' },
    ]);
    mocks.getWorkspaceProjectBoard.mockResolvedValue(null);
    mocks.getWorkspaceProjectCollaborators.mockResolvedValue([]);
  });
  afterEach(cleanup);

  it('uses /projects/:slug as the member base route', async () => {
    render(await ProjectWorkspaceView(props('neon-racer') as never));

    expect(screen.getByTestId('workspace-nav')).toHaveAttribute('data-base', '/workspace/projects/neon-racer');
    expect(screen.getByRole('heading', { name: 'Neon Racer' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Publish project' })).toBeInTheDocument();
  });

  it('links testing lab and launch pad from the distribution section', async () => {
    render(await ProjectWorkspaceView(props('neon-racer', 'distribution') as never));

    const links = screen
      .getAllByRole('link', { name: 'Open community events' })
      .map((link) => link.getAttribute('href'));
    expect(links).toEqual([
      '/testing-lab/events?projectId=project-1',
      '/launch-pad/events?projectId=project-1',
    ]);
    expect(screen.getAllByText('Version required')).toHaveLength(2);
  });

  it('renders versions list on the versions-builds section', async () => {
    render(await ProjectWorkspaceView(props('neon-racer', 'versions-builds') as never));

    expect(screen.getByText('0.1.0')).toBeInTheDocument();
    expect(screen.getByText('First build')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Mark ready for testing' })).toBeInTheDocument();
  });

  it('returns not-found when the project does not exist', async () => {
    mocks.getWorkspaceProject.mockResolvedValue(null);

    await expect(ProjectWorkspaceView(props('ghost') as never)).rejects.toThrow('not-found');
  });
});
