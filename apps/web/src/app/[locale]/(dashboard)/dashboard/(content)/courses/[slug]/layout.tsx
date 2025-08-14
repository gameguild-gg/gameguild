"use client"

import React, { PropsWithChildren } from 'react';
import { useParams } from 'next/navigation';
import { PageHeader } from '@/components/projects/page-header';
import { CourseSubNav } from '@/components/courses/course-sub-nav';

export default function Layout({ children }: PropsWithChildren): React.JSX.Element {
  const params = useParams();
  const slug = String((params as any).slug ?? 'current');
  
  return (
    <div className="flex flex-col min-h-svh">
      <PageHeader title="Course" />
      <div className="flex flex-1">
        <CourseSubNav courseSlug={slug} />
        <main className="flex-1 p-4 md:p-6 bg-muted/40">{children}</main>
      </div>
    </div>
  );
}
