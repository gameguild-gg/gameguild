import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

const mockSignOut = vi.fn().mockResolvedValue(undefined);
vi.mock('@game-guild/client/react', () => ({ useAuth: () => ({ signOut: mockSignOut, isLoading: false }) }));
vi.mock('next/navigation', () => ({ usePathname: () => '/' }));
const { StudentShell } = await import('./student-shell');

describe('StudentShell', () => {
    it('exposes the complete learner navigation and account identity', () => {
        render(<StudentShell user={{ id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' }}><p>Dashboard content</p></StudentShell>);
        expect(screen.getByRole('link', { name: 'My learning' })).toHaveAttribute('href', '/');
        expect(screen.getByRole('link', { name: 'Catalog' })).toHaveAttribute('href', '/catalog');
        expect(screen.getByRole('link', { name: 'Calendar' })).toHaveAttribute('href', '/calendar');
        expect(screen.getByRole('link', { name: 'Grades' })).toHaveAttribute('href', '/grades');
        expect(screen.getByRole('link', { name: 'Certificates' })).toHaveAttribute('href', '/certificates');
        expect(screen.getByText('Ada Learner')).toBeInTheDocument();
        expect(screen.getByText('Dashboard content')).toBeInTheDocument();
    });

    it('opens and closes the mobile navigation with an explicit control', async () => {
        const user = userEvent.setup();
        render(<StudentShell user={{ id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' }}><p>Content</p></StudentShell>);

        const closeButton = screen.getByRole('button', { name: 'Close navigation' });
        const sidebar = closeButton.closest('aside');
        expect(sidebar).toHaveClass('hidden');
        await user.click(screen.getByRole('button', { name: 'Toggle navigation' }));
        expect(sidebar).toHaveClass('flex');
        await user.click(closeButton);
        expect(sidebar).toHaveClass('hidden');
    });

    it('signs the learner out from the account menu', async () => {
        const user = userEvent.setup();
        render(<StudentShell user={{ id: 'user-1', name: 'Ada Learner', email: 'ada@example.com' }}><p>Content</p></StudentShell>);
        await user.click(screen.getByRole('button', { name: 'Open account menu' }));
        await user.click(await screen.findByRole('menuitem', { name: 'Sign out' }));
        await waitFor(() => expect(mockSignOut).toHaveBeenCalledWith({ redirectTo: '/sign-in' }));
    });
});
