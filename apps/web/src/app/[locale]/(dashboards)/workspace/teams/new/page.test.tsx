import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/workspace-actions', () => ({ createTeamForm: vi.fn() }));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { readonly href: string; readonly children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

import NewTeamPage from './page';

describe('member Team creation', () => {
  it('derives the slug from the Team name while keeping it editable', async () => {
    const user = userEvent.setup();
    render(<NewTeamPage />);

    await user.type(screen.getByRole('textbox', { name: /Name/ }), 'Space Cadets');

    expect(screen.getByRole('textbox', { name: /Slug/ })).toHaveValue('space-cadets');
    expect(screen.getByRole('link', { name: 'Cancel' })).toHaveAttribute(
      'href',
      '/workspace/teams',
    );
  });
});
