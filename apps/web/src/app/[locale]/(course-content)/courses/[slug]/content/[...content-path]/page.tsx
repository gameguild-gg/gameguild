import { getProgramBySlug, getProgramContentBySlug } from '@/data/courses/mock-data';
import type { ProgramContentDto } from '@/lib/api/generated/types.gen';
import { getProgramContentChildren } from '@/lib/content-management/programs/programs.actions';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import { ChevronRight } from 'lucide-react';
import { CourseContentPageClient } from './course-content-page-client';

interface PageProps {
  params: Promise<{ slug: string; 'content-path': string[]; locale: string }>;
}



// Helper function to find content by path recursively
async function findContentByPath(
  programId: string,
  contentPath: string[],
  allContent: ProgramContentDto[]
): Promise<{ content: ProgramContentDto | null; breadcrumb: ProgramContentDto[] }> {
  if (contentPath.length === 0) {
    return { content: null, breadcrumb: [] };
  }

  const [currentSlug, ...remainingPath] = contentPath;

  // Find the content item by id
  const content = allContent.find(item => {
    return item.id === currentSlug;
  });

  if (!content || !content.id) {
    return { content: null, breadcrumb: [] };
  }

  // If this is the final item in the path, return it
  if (remainingPath.length === 0) {
    return { content, breadcrumb: [content] };
  }

  // Otherwise, get children and continue recursively
  let children: ProgramContentDto[] = [];
  try {
    const childrenResult = await getProgramContentChildren({
      path: {
        programId,
        parentId: content.id!
      }
    });
    children = (childrenResult?.data as any) ?? [];
  } catch (error) {
    // If API fails, return empty children array
    console.log('🚨 Failed to get children for content:', content.id, error);
    children = [];
  }

  const childResult = await findContentByPath(programId, remainingPath, children);

  return {
    content: childResult.content,
    breadcrumb: [content, ...childResult.breadcrumb]
  };
}

export default async function ContentPage({ params }: PageProps) {
  const { slug, 'content-path': contentPath } = await params;

  // If no content path is provided, redirect to the main content page
  if (!contentPath || contentPath.length === 0) {
    notFound();
  }

  // Try API service first, fallback to mock data
  let program = null;
  let programData = null;

  try {
    program = await getProgramBySlugService(slug);
    if (program.success && program.data) {
      programData = program.data;
    }
  } catch (error) {
    console.log('Authentication issue with getProgramBySlugService, falling back to mock data:', error);
  }

  if (!programData) {
    // Fallback to mock data
    const mockProgram = getProgramBySlug(slug);
    if (mockProgram) {
      programData = mockProgram;
    }
  }

  if (!programData || !programData.id) {
    notFound();
  }

  // For now, skip API calls and use mock data directly to avoid authentication issues
  let allContent: ProgramContentDto[] = [];

  // Use mock data directly
  if (programData.programContents) {
    allContent = programData.programContents;
  }

  console.log('📋 Using mock data, content count:', allContent.length);

  // Find the content item by path using slug-based lookup
  let content = null;
  let breadcrumb: any[] = [];

  // Try API first, then fallback to mock data
  if (allContent.length > 0) {
    try {
      // Use the existing API-based findContentByPath for API data
      const result = await findContentByPath(programData.id, contentPath, allContent);
      content = result.content;
      breadcrumb = result.breadcrumb;
    } catch (error) {
      console.log('🚨 API content lookup failed:', error);
      // Will fall through to mock data fallback below
    }
  }

  // If API didn't find content or returned empty, try mock data with slug-based lookup
  if (!content) {
    // Mock data fallback
    console.log('🔍 Trying mock data fallback for:', { slug, contentPath });
    const mockContent = getProgramContentBySlug(slug, contentPath);
    console.log('🔍 Mock data result:', mockContent ? { slug: mockContent.slug, title: mockContent.title } : 'Not found');
    if (mockContent) {
      content = mockContent;
      // For mock data, create a simple breadcrumb with just the content
      breadcrumb = [mockContent];
    } else {
      breadcrumb = [];
    }
  }

  if (!content) {
    notFound();
  }

  // Get children if any - handle authentication gracefully
  let children: ProgramContentDto[] = [];
  if (content.id) {
    try {
      const childrenResult = await getProgramContentChildren({
        path: {
          programId: programData.id,
          parentId: content.id
        }
      });
      children = (childrenResult?.data as any) ?? [];
    } catch (error) {
      console.log('🚨 Failed to get children from API (likely auth issue), using empty array:', error);
      // For unauthenticated users, we'll just show no children
      children = [];
    }
  }

  // Find siblings for navigation (from the parent level)
  let siblings: ProgramContentDto[] = [];
  let currentIndex = -1;

  if (breadcrumb.length === 1) {
    // Top-level content
    siblings = allContent;
    currentIndex = siblings.findIndex(item => item.id === content.id);
  } else if (breadcrumb.length > 1) {
    // Child content - get siblings from parent
    const parent = breadcrumb[breadcrumb.length - 2];
    if (parent?.id) {
      const siblingsResult = await getProgramContentChildren({
        path: {
          programId: programData.id,
          parentId: parent.id
        }
      });
      siblings = (siblingsResult?.data as any) ?? [];
      currentIndex = siblings.findIndex(item => item.id === content.id);
    }
  }

  const previousContent = currentIndex > 0 ? siblings[currentIndex - 1] : null;
  const nextContent = currentIndex < siblings.length - 1 ? siblings[currentIndex + 1] : null;

  // Generate navigation paths
  const basePath = `/courses/${slug}/content`;
  const currentPath = contentPath.join('/');
  const parentPath = contentPath.length > 1 ? contentPath.slice(0, -1).join('/') : '';

  const previousPath = previousContent ?
    (parentPath ? `${parentPath}/${previousContent.id}` : previousContent.id!) : null;
  const nextPath = nextContent ?
    (parentPath ? `${parentPath}/${nextContent.id}` : nextContent.id!) : null;

  return (
    <CourseContentPageClient
      programData={programData}
      content={content}
      contentPath={contentPath}
      basePath={basePath}
    >
      {children.length > 0 && (
        <>
          <h3 className="text-lg font-semibold">Sub-content</h3>
          <div className="space-y-2">
            {children.map((child, index) => {
              const childSlug = child.id;
              const childPath = `${contentPath.join('/')}/${childSlug}`;

              return (
                <Link
                  key={child.id}
                  href={`${basePath}/${childPath}`}
                  className="block"
                >
                  <div className="flex items-center gap-3 p-3 border rounded-lg hover:bg-muted/50 transition-colors">
                    <div className="flex items-center justify-center w-6 h-6 rounded-full bg-primary/10 text-primary font-medium text-xs">
                      {index + 1}
                    </div>
                    <div className="flex-1">
                      <h4 className="font-medium">{child.title || 'Untitled'}</h4>
                      {child.description && (
                        <p className="text-sm text-muted-foreground">{child.description}</p>
                      )}
                    </div>
                    <ChevronRight className="h-4 w-4 text-muted-foreground" />
                  </div>
                </Link>
              );
            })}
          </div>
        </>
      )}
    </CourseContentPageClient>
  );
}