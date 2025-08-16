"use client"

import { CourseDetails } from './course-details.client'
import { useCourse } from '@/components/course-context'
import type { Program } from '@/lib/api/generated/types.gen'
import { AccessLevel, ContentStatus } from '@/lib/api/generated/types.gen'

export default function CourseDetailsPage() {
  const course = useCourse()
  
  // Transform course to match expected Program type
  const programData: Program = {
    id: course.id,
    title: course.title,
    description: course.description,
    slug: course.slug,
    status: course.status === 'published' ? ContentStatus.PUBLISHED : 
           course.status === 'draft' ? ContentStatus.DRAFT : ContentStatus.ARCHIVED,
    difficulty: course.level as any, // Type mapping between CourseLevel and ProgramDifficulty
    createdAt: new Date(course.createdAt).toISOString(),
    updatedAt: new Date(course.updatedAt).toISOString(),
    visibility: AccessLevel.PUBLIC, // Default visibility
    thumbnail: null
  }

  return <CourseDetails course={programData} />
}
