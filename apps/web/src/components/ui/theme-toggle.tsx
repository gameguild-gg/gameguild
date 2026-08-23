'use client';

import * as React from 'react';
import { Moon, Sun } from 'lucide-react';
import { useTheme } from 'next-themes';

import { updateThemePreferenceAction } from '@/lib/user-settings/actions';
import type { ThemePreference } from '@/lib/user-settings/preferences-mappers';
import { Button } from '@game-guild/ui/components/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';

export function ThemeToggle() {
  const { setTheme } = useTheme();
  const [, startTransition] = React.useTransition();

  function updateTheme(theme: ThemePreference): void {
    setTheme(theme);
    startTransition(async () => {
      await updateThemePreferenceAction(theme);
    });
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-9 w-9">
          <Sun className="h-4 w-4 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
          <Moon className="absolute h-4 w-4 rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
          <span className="sr-only">Toggle theme</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => updateTheme('light')}>
          Light
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => updateTheme('dark')}>
          Dark
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => updateTheme('system')}>
          System
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
