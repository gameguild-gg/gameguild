import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { CommandDialog, CommandInput, CommandItem, CommandList } from '../src/components/command';

describe('CommandDialog', () => {
  afterEach(cleanup);

  it('provides command context and accessible modal labels for search', async () => {
    const user = userEvent.setup();
    const select = vi.fn();
    render(
      <CommandDialog open title="Search courses" description="Find a course or lesson.">
        <CommandInput placeholder="Search lessons" />
        <CommandList>
          <CommandItem onSelect={select}>Navigation meshes</CommandItem>
          <CommandItem>Lighting</CommandItem>
        </CommandList>
      </CommandDialog>,
    );

    expect(screen.getByRole('dialog', { name: 'Search courses' })).toHaveAccessibleDescription('Find a course or lesson.');
    await user.type(screen.getByPlaceholderText('Search lessons'), 'navigation');
    await waitFor(() => expect(screen.queryByRole('option', { name: 'Lighting' })).not.toBeInTheDocument());
    await user.click(screen.getByRole('option', { name: 'Navigation meshes' }));
    expect(select).toHaveBeenCalledOnce();
  });
});
