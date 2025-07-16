'use client';

import * as React from 'react';
import { useEffect, useState } from 'react';
import { Bug, Lightbulb, ListChecks, MessageCircle, MessageSquare } from 'lucide-react';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

interface FloatingFeedbackButtonProps {
  className?: string;
}

export function FeedbackFloatingButton({ className }: FloatingFeedbackButtonProps) {
  const [version, setVersion] = useState('v0.0.1');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    import('@/lib/core/actions').then(({ getVersion }) => {
      getVersion()
        .then((data) => {
          if (data.version) {
            setVersion(data.version);
          } else {
            throw new Error('Version not found in response');
          }
        })
        .catch((error) => {
          console.error('Error fetching version:', error);
          setError('Failed to fetch version');
        });
    });
  }, []);

  const getActionUrl = (action: 'bug_report' | 'feature_request' | 'discussion') => {
    switch (action) {
      case 'bug_report':
        return `https://github.com/gameguild-gg/website/issues/new?assignees=&labels=bug&projects=&template=bug_report.yml&title=${encodeURIComponent('[Bug Report] ')}`;
      case 'feature_request':
        return `https://github.com/gameguild-gg/website/issues/new?assignees=&labels=enhancement&projects=&template=feature_request.yml&title=${encodeURIComponent('[Feature Request] ')}`;
      case 'discussion':
        return 'https://discord.gg/9CdJeQ2XKB';
    }
  };

  const openIssuesUrl = '/issues';

  return (
    <div className={cn('fixed bottom-4 right-4 z-50', className)}>
      <TooltipProvider>
        <DropdownMenu>
          <Tooltip>
            <TooltipTrigger asChild>
              <DropdownMenuTrigger asChild>
                <Button size="lg" className="rounded-full shadow-lg flex items-center gap-2">
                  <MessageCircle className="h-5 w-5" />
                  <span className="flex flex-col items-start">
                    <span>Give Feedback</span>
                    <span className="text-xs opacity-70">{error ? 'Version unavailable' : version}</span>
                  </span>
                  <span className="sr-only">Open feedback menu</span>
                </Button>
              </DropdownMenuTrigger>
            </TooltipTrigger>
            <TooltipContent side="left">
              <p>Give Feedback</p>
            </TooltipContent>
          </Tooltip>
          <DropdownMenuContent align="end" alignOffset={-8} className="w-[200px]">
            <DropdownMenuItem onClick={() => window.open(getActionUrl('bug_report'), '_blank')}>
              <Bug className="mr-2 h-4 w-4" />
              Report Bug
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => window.open(getActionUrl('feature_request'), '_blank')}>
              <Lightbulb className="mr-2 h-4 w-4" />
              Request Feature
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => window.open(openIssuesUrl)}>
              <ListChecks className="mr-2 h-4 w-4" />
              View Open Issues
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => window.open(getActionUrl('discussion'), '_blank')}>
              <MessageSquare className="mr-2 h-4 w-4" />
              Discord Discussion
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </TooltipProvider>
    </div>
  );
}
