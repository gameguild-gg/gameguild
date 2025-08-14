"use client";

import React from 'react';
import { useParams } from 'next/navigation';
import { PageHeader } from '@/components/projects/page-header';
import { ProjectSubNav } from '@/components/projects/project-sub-nav';

export default function Layout({ children }: { children: React.ReactNode }) {
  const params = useParams();
  const slug = String((params as any).slug ?? 'current');
  return (
    <div className="flex flex-col min-h-svh">
      <PageHeader title="Project" />
      <div className="flex flex-1">
        <ProjectSubNav projectId={slug} />
        <main className="flex-1 p-4 md:p-6 bg-muted/40">{children}</main>
      </div>
    </div>
  );
}
