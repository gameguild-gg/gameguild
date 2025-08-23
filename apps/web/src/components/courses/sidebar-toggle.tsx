'use client';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { PanelLeftIcon, X } from 'lucide-react';
import { useSidebar } from './sidebar-context';

interface SidebarToggleProps {
  className?: string;
}

export function SidebarToggle({ className }: SidebarToggleProps) {
  const { isSidebarOpen, toggleSidebar, mounted } = useSidebar();

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={toggleSidebar}
      className={cn(
        "fixed top-4 z-50 size-8 transition-all duration-300 ease-in-out",
        "bg-background/80 backdrop-blur-sm border border-border shadow-sm",
        "hover:bg-accent hover:text-accent-foreground",
        // Position based on sidebar state - only after hydration
        mounted && isSidebarOpen ? "left-[21rem] lg:left-[21rem]" : "left-4",
        className
      )}
    >
      {mounted && isSidebarOpen ? (
        <X className="h-4 w-4" />
      ) : (
        <PanelLeftIcon className="h-4 w-4" />
      )}
      <span className="sr-only">{mounted && isSidebarOpen ? 'Close Sidebar' : 'Open Sidebar'}</span>
    </Button>
  );
}
