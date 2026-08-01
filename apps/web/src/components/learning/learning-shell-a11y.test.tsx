import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  push: vi.fn(),
  search: vi.fn(),
  signOut: vi.fn().mockResolvedValue(undefined),
}));

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => ({ isLoading: false, signOut: mocks.signOut }),
}));
vi.mock('next/navigation', () => ({
  usePathname: () => '/courses/game-ai',
  useRouter: () => ({ push: mocks.push }),
}));
vi.mock('@/components/ui/theme-toggle', () => ({
  ThemeToggle: () => <button type="button">Theme</button>,
}));
vi.mock('@/lib/learner/search-actions', () => ({
  searchLearnerWorkspace: mocks.search,
}));

beforeAll(() => {
  global.ResizeObserver = class ResizeObserver {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

  Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
    configurable: true,
    value: vi.fn(),
  });
const { LearningShell } = await import('./learning-shell');

describe('LearningShell accessibility and search', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.search.mockResolvedValue({ success: true, items: [] });
  });

  it('provides a focusable content target and announces unread notifications', () => {
    render(
      <LearningShell
        notifications={{ unreadCount: 3, items: [] }}
        user={{ id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' }}
      >
        <p>Workspace</p>
      </LearningShell>,
    );

    expect(screen.getByRole('link', { name: 'Skip to learning content' })).toHaveAttribute(
      'href',
      '#learning-content',
    );
    expect(document.querySelector('#learning-content')).toHaveAttribute('tabindex', '-1');
    expect(screen.getByText('3 unread notifications')).toHaveClass('sr-only');
  });

  it('uses an accessible modal navigation surface on narrow viewports', async () => {
    const user = userEvent.setup();
    render(
      <LearningShell user={{ id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' }}>
        <p>Workspace</p>
      </LearningShell>,
    );

    const trigger = screen.getByRole('button', { name: 'Toggle navigation' });
    expect(trigger).toHaveAttribute('aria-expanded', 'false');
    await user.click(trigger);

    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByRole('dialog', { name: 'GameGuild Learning' })).toBeInTheDocument();
    await user.keyboard('{Escape}');
    await waitFor(() => expect(trigger).toHaveAttribute('aria-expanded', 'false'));
  });

  it('searches only the learner workspace and navigates to a returned lesson', async () => {
    mocks.search.mockResolvedValue({
      success: true,
      items: [
        {
          id: 'lesson-1',
          kind: 'Lesson',
          title: 'Navigation meshes',
          description: 'Build a walkable graph',
          route: '/courses/game-ai/lessons/lesson-1',
        },
      ],
    });
    const user = userEvent.setup();
    render(
      <LearningShell user={{ id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' }}>
        <p>Workspace</p>
      </LearningShell>,
    );

    await user.click(screen.getByRole('button', { name: /Search learning/ }));
    await user.type(screen.getByPlaceholderText('Search your courses and lessons...'), 'navigation');

    await waitFor(() =>
      expect(mocks.search).toHaveBeenCalledWith('navigation'),
    );
    await user.click(await screen.findByText('Navigation meshes'));
    expect(mocks.push).toHaveBeenCalledWith('/courses/game-ai/lessons/lesson-1');
  });
});
