'use client';

import React from 'react';
import { Button } from '@/components/ui/button';
import { GitFork } from 'lucide-react';

export const GitHubForkButton = (): React.JSX.Element => {
  const handleForkClick = () => {
    window.open('https://github.com/gameguild-gg/gameguild/', '_blank', 'noopener,noreferrer');
  };

  return (
    <Button
      variant="outline"
      size="icon"
      onClick={handleForkClick}
      className="text-black dark:text-slate-200 hover:text-purple-600 dark:hover:text-purple-300 transition-colors duration-300 bg-white/90 hover:bg-white dark:bg-slate-800/90 dark:hover:bg-slate-800 backdrop-blur-sm"
      aria-label="Fork us on GitHub"
    >
      <GitFork className="h-[1.2rem] w-[1.2rem]" />
      <span className="sr-only">Fork us on GitHub</span>
    </Button>
  );
};