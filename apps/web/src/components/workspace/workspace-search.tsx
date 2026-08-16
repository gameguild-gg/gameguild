'use client';

import { Search } from 'lucide-react';
import * as React from 'react';

import { useRouter } from '@/i18n/navigation';
import { CommandDialog, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
import { Button } from '@game-guild/ui/components/button';

import { workspaceNav } from './workspace-sidebar';

export const WORKSPACE_PALETTE_OPEN_EVENT = 'gameguild:open-workspace-palette';

/** Search entry for the workspace header — opens a destination palette (⌘K). */
export function WorkspaceSearch(): React.JSX.Element {
  const router = useRouter();
  const [open, setOpen] = React.useState(false);

  React.useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'k' && (event.metaKey || event.ctrlKey)) {
        event.preventDefault();
        setOpen((value) => !value);
      }
    };
    const onOpen = () => setOpen(true);
    window.addEventListener('keydown', onKey);
    window.addEventListener(WORKSPACE_PALETTE_OPEN_EVENT, onOpen);
    return () => {
      window.removeEventListener('keydown', onKey);
      window.removeEventListener(WORKSPACE_PALETTE_OPEN_EVENT, onOpen);
    };
  }, []);

  function run(url: string) {
    setOpen(false);
    router.push(url);
  }

  return (
    <>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        aria-label="Search workspace"
        onClick={() => setOpen(true)}
      >
        <Search className="size-5" />
      </Button>
      <CommandDialog open={open} onOpenChange={setOpen}>
        <CommandInput placeholder="Search workspace…" />
        <CommandList>
          <CommandEmpty>No results found.</CommandEmpty>
          <CommandGroup heading="Workspace">
            {workspaceNav.map((item) => (
              <CommandItem key={item.url} onSelect={() => run(item.url)}>
                <item.icon className="size-4" />
                {item.title}
              </CommandItem>
            ))}
          </CommandGroup>
        </CommandList>
      </CommandDialog>
    </>
  );
}
