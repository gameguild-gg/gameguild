import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import Link from 'next/link';
import { describe, it } from 'vitest';

import { Button } from '@game-guild/ui/components/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import { HoverCard, HoverCardContent, HoverCardTrigger } from '@game-guild/ui/components/hover-card';

describe('Base UI compatibility wrappers', () => {
  it('opens a dropdown whose trigger uses the render prop API', async () => {
    const user = userEvent.setup();

    render(
      <DropdownMenu>
        <DropdownMenuTrigger render={<Button>Actions</Button>} />
        <DropdownMenuContent>
          <DropdownMenuLabel>Project scope</DropdownMenuLabel>
          <DropdownMenuGroup>
            <DropdownMenuItem>View profile</DropdownMenuItem>
          </DropdownMenuGroup>
          <DropdownMenuItem>All projects</DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>,
    );

    await user.click(screen.getByRole('button', { name: 'Actions' }));

    const profileItem = await screen.findByRole('menuitem', { name: 'View profile' });
    if (profileItem.textContent !== 'View profile') {
      throw new Error('The dropdown menu item did not render.');
    }

    const projectScopeLabel = screen.getByText('Project scope');
    if (projectScopeLabel.textContent !== 'Project scope') {
      throw new Error('The standalone dropdown label did not render.');
    }

    const allProjectsItem = screen.getByRole('menuitem', { name: 'All projects' });
    if (allProjectsItem.textContent !== 'All projects') {
      throw new Error('The menu item did not render.');
    }
  });

  it('opens a hover card whose trigger uses the render prop API', async () => {
    const user = userEvent.setup();

    render(
      <HoverCard openDelay={0}>
        <HoverCardTrigger render={<Link href="/events/one">Campus playtest</Link>} />
        <HoverCardContent>Operational details</HoverCardContent>
      </HoverCard>,
    );

    await user.hover(screen.getByRole('link', { name: 'Campus playtest' }));

    const hoverCardContent = await screen.findByText('Operational details');
    if (hoverCardContent.textContent !== 'Operational details') {
      throw new Error('The hover card content did not render.');
    }
  });
});
