"use client";

import React from 'react';
import { SidebarTrigger } from '@/components/ui/sidebar';
import { Input } from '@/components/ui/input';

export function PageHeader({ title = 'Dashboard', actions }: { title?: string; actions?: React.ReactNode }) {
  return (
    <header className="sticky top-0 z-10 bg-background/80 backdrop-blur supports-[backdrop-filter]:bg-background/60 border-b">
      <div className="flex items-center gap-3 p-3 md:p-4">
        <SidebarTrigger />
        <h1 className="text-lg md:text-xl font-semibold">{title}</h1>
        <div className="ml-auto flex items-center gap-3">
          <div className="hidden md:block">
            <Input placeholder="Search projects..." className="w-[260px]" />
          </div>
          {actions}
        </div>
      </div>
    </header>
  );
}
