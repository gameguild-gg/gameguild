import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Link from 'next/link';
import { describe, expect, it } from 'vitest';

import { Button } from '@game-guild/ui/components/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import { HoverCard, HoverCardContent, HoverCardTrigger } from '@game-guild/ui/components/hover-card';

describe('Base UI compatibility wrappers', () => {
  it('opens a dropdown whose trigger uses the legacy asChild API', async () => {
    const user = userEvent.setup();

    render(
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button>Actions</Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent>
          <DropdownMenuGroup>
            <DropdownMenuItem>View profile</DropdownMenuItem>
          </DropdownMenuGroup>
        </DropdownMenuContent>
      </DropdownMenu>,
    );

    await user.click(screen.getByRole('button', { name: 'Actions' }));

    expect(await screen.findByRole('menuitem', { name: 'View profile' })).toBeInTheDocument();
  });

  it('opens a hover card whose trigger uses the legacy asChild API', async () => {
    const user = userEvent.setup();

    render(
      <HoverCard openDelay={0}>
        <HoverCardTrigger asChild>
          <Link href="/events/one">Campus playtest</Link>
        </HoverCardTrigger>
        <HoverCardContent>Operational details</HoverCardContent>
      </HoverCard>,
    );

    await user.hover(screen.getByRole('link', { name: 'Campus playtest' }));

    expect(await screen.findByText('Operational details')).toBeInTheDocument();
  });
});
