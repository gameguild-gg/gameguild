import { CourseLandingPage } from '@/components/courses/course/course-landing-page';
import type { Program, ProgramContent } from '@/lib/api/generated';
import { ProgramContentType } from '@/lib/api/generated';
import { getCourse, getCourseContent } from '@/lib/learning';
import type { ContentItem, CourseDetails } from '@/lib/learning/types';
import { notFound } from 'next/navigation';

interface CoursePreviewPageProps {
  readonly params: Promise<{ course: string }>;
}

function toTitleCase(value: string | null | undefined): string | null {
  if (!value) return null;
  return value.charAt(0).toUpperCase() + value.slice(1);
}

function mapContentType(type: ContentItem['type']): ProgramContentType {
  switch (type) {
    case 'Page':
      return ProgramContentType.Page;
    case 'Assignment':
      return ProgramContentType.Assignment;
    case 'Questionnaire':
      return ProgramContentType.Questionnaire;
    case 'Discussion':
      return ProgramContentType.Discussion;
    case 'Code':
      return ProgramContentType.Code;
    case 'Challenge':
      return ProgramContentType.Challenge;
    case 'Reflection':
      return ProgramContentType.Reflection;
    case 'Survey':
      return ProgramContentType.Survey;
    case 'Lesson':
    default:
      return ProgramContentType.Lesson;
  }
}

function mapContentItem(item: ContentItem): ProgramContent {
  return {
    id: item.id,
    title: item.title,
    description: item.description,
    parentId: item.parentId,
    type: mapContentType(item.type),
    estimatedMinutes: item.duration,
    isRequired: true,
  };
}

function mapCourseToProgram(course: CourseDetails, programContents: ProgramContent[]): Program {
  return {
    id: course.id,
    title: course.title,
    slug: course.slug,
    description: course.description,
    metadata: course.metadata,
    category: course.category,
    difficulty: course.difficulty,
    estimatedHours: course.estimatedHours,
    currentEnrollments: course.currentEnrollments,
    averageRating: course.averageRating,
    totalRatings: course.totalRatings,
    isEnrollmentOpen: course.isEnrollmentOpen,
    thumbnail: course.thumbnail,
    videoShowcaseUrl: course.videoShowcaseUrl,
    visibility: toTitleCase(course.visibility),
    status: toTitleCase(course.status),
    maxEnrollments: course.maxEnrollments,
    enrollmentDeadline: course.enrollmentDeadline,
    skillsRequired: course.skillsRequired,
    skillsProvided: course.skillsProvided,
    programContents,
  };
}

export default async function CoursePreviewPage({ params }: CoursePreviewPageProps): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const [course, content] = await Promise.all([getCourse(courseId), getCourseContent(courseId)]);

  if (!course) {
    notFound();
  }

  const programContents = content.items.map(mapContentItem);
  const previewCourse = mapCourseToProgram(course, programContents);

  return <CourseLandingPage course={previewCourse} viewerAccess={{ state: 'has-access' }} />;
}
