import { CourseProvider } from '@/components/course-context'
import { CourseSubNav } from '@/components/course-sub-nav'
import { PageHeader } from '@/components/page-header'
import { notFound } from 'next/navigation'
import * as React from 'react'
import { getCourseBySlug } from '../actions'

interface LayoutProps {
  children: React.ReactNode;
  params: Promise<{ slug: string }>;
}

export default async function Layout({ children, params }: LayoutProps) {
  const { slug } = await params;

  // Fetch course data server-side using GraphQL
  const course = await getCourseBySlug(slug);

  if (!course) {
    notFound();
  }

  return (
    <CourseProvider course={course}>
      <div className="flex flex-col min-h-svh">
        <PageHeader title={course.title} />
        <div className="flex flex-1">
          <CourseSubNav courseSlug={slug} />
          <main className="flex-1 p-4 md:p-6 bg-muted/40">{children}</main>
        </div>
      </div>
    </CourseProvider>
  )
}
