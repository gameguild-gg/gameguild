"use client"

import * as React from 'react'
import { notFound, useParams } from 'next/navigation'
import { PageHeader } from '@/components/page-header'
import { CourseSubNav } from '@/components/course-sub-nav'
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service'
import { transformProgramToCourse } from '@/lib/content-management/programs/programs.utils'
import type { Course } from '@/lib/types'
import { CourseProvider } from '@/components/course-context'

export default function Layout({ children }: { children: React.ReactNode }) {
  const params = useParams()
  const slug = String((params as any).slug)
  const [course, setCourse] = React.useState<Course | undefined>()
  const [loading, setLoading] = React.useState(true)

  React.useEffect(() => {
    const loadCourse = async () => {
      try {
        setLoading(true)
        const result = await getProgramBySlugService(slug)
        if (result.success && result.data) {
          const transformedCourse = transformProgramToCourse(result.data)
          setCourse(transformedCourse)
        } else {
          notFound()
        }
      } catch (error) {
        console.error('Error loading course:', error)
        notFound()
      } finally {
        setLoading(false)
      }
    }

    loadCourse()
  }, [slug])

  if (loading) {
    return (
      <div className="flex flex-col min-h-svh">
        <div className="border-b border-border/50 bg-card/30 backdrop-blur">
          <div className="p-6">
            <div className="h-6 bg-muted animate-pulse rounded w-64 mb-2"></div>
            <div className="h-4 bg-muted animate-pulse rounded w-96"></div>
          </div>
        </div>
        <div className="flex-1 p-6">
          <div className="animate-pulse">
            <div className="h-4 bg-muted rounded w-full mb-4"></div>
            <div className="h-4 bg-muted rounded w-3/4 mb-4"></div>
            <div className="h-4 bg-muted rounded w-1/2"></div>
          </div>
        </div>
      </div>
    )
  }

  if (!course) {
    notFound()
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
