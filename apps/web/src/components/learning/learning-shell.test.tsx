import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

const signOut = vi.fn().mockResolvedValue(undefined);

vi.mock('@game-guild/client/react', () => ({
  useAuth: () => ({ isLoading: false, signOut }),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/courses/game-ai',
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock('@/components/ui/theme-toggle', () => ({
  ThemeToggle: () => <button type="button">Theme</button>,
}));

const { LearningShell } = await import('./learning-shell');

describe('LearningShell', () => {
  it('exposes the learner navigation with clean learning-host URLs', () => {
    render(
      <LearningShell user={{ id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' }}>
        <p>Learning workspace</p>
      </LearningShell>,
    );

    expect(screen.getByRole('link', { name: 'Home' })).toHaveAttribute('href', '/');
    expect(screen.getByRole('link', { name: 'My courses' })).toHaveAttribute('href', '/courses');
    expect(screen.getByRole('link', { name: 'Calendar' })).toHaveAttribute('href', '/calendar');
    expect(screen.getByRole('link', { name: 'Grades' })).toHaveAttribute('href', '/grades');
    expect(screen.getByRole('link', { name: 'Certificates' })).toHaveAttribute('href', '/certificates');
    expect(screen.getByRole('link', { name: 'Browse courses' })).toHaveAttribute(
      'href',
      'https://gameguild.gg/courses',
    );
    expect(screen.getByText('Learning workspace')).toBeInTheDocument();
  });

  it('keeps account identity and sign-out available', async () => {
    const user = userEvent.setup();
    render(
      <LearningShell user={{ id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' }}>
        <p>Content</p>
      </LearningShell>,
    );

    await user.click(screen.getByRole('button', { name: 'Open account menu' }));
    expect(screen.getByText('ada@example.com')).toBeInTheDocument();
    await user.click(screen.getByRole('menuitem', { name: 'Sign out' }));

    await waitFor(() =>
      expect(signOut).toHaveBeenCalledWith({
        redirectTo: 'https://gameguild.gg/sign-in',
      }),
    );
  });
});
