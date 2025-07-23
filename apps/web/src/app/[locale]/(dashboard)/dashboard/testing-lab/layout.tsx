import type { Metadata } from 'next';
import React, { PropsWithChildren } from 'react';
import { TestingLabSidebar } from '@/components/testing-lab/testing-lab-sidebar';

export const metadata: Metadata = {
  title: 'Testing Lab | Game Guild',
  description: 'Submit, test, and manage game projects for Capstone teams',
};

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-1 relative">
      {/* Testing Lab Secondary Sidebar */}
      <div className="w-64 border-r border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <TestingLabSidebar />
      </div>

      {/* Main Content */}
      <div className="flex-1 flex flex-col min-w-0">{children}</div>
    </div>
  );
}
