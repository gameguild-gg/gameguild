import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight, BookOpen } from 'lucide-react';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { getTopLevelProgramContent, getProgramContentChildren } from '@/lib/content-management/programs/programs.actions';
import type { ProgramContentDto } from '@/lib/api/generated/types.gen';
import { notFound } from 'next/navigation';
import Link from 'next/link';
import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';
import { getProgramBySlug, getProgramContentBySlug } from '@/data/courses/mock-data';

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
      },
      url: '/api/programs/{programId}/content/{parentId}/children'
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
  let program = await getProgramBySlugService(slug);
  let programData = null;
  
  if (program.success && program.data) {
    programData = program.data;
  } else {
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

  // Get children if any
  const childrenResult = content.id ? await getProgramContentChildren({
    path: { 
      programId: programData.id,
      parentId: content.id 
    },
    url: '/api/programs/{programId}/content/{parentId}/children'
  }) : { data: [] };
  
  const children: ProgramContentDto[] = (childrenResult?.data as any) ?? [];
  
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
        },
        url: '/api/programs/{programId}/content/{parentId}/children'
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
    <div className="flex-1 flex flex-col min-h-0 p-6">
      <div className="max-w-4xl mx-auto space-y-6">
        {/* Breadcrumb Navigation */}
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <span className="text-foreground font-medium">{programData.title}</span>
          {breadcrumb.map((item, index) => {
            const isLast = index === breadcrumb.length - 1;
            const itemPath = contentPath.slice(0, index + 1).join('/');
            
            return (
              <div key={item.id} className="flex items-center gap-2">
                <ChevronRight className="h-3 w-3" />
                {isLast ? (
                  <span className="text-foreground font-medium">{item.title}</span>
                ) : (
                  <Link 
                    href={`${basePath}/${itemPath}`} 
                    className="hover:text-foreground transition-colors"
                  >
                    {item.title}
                  </Link>
                )}
              </div>
            );
          })}
        </div>

        {/* Content */}
        <Card>
          <CardContent className="space-y-6">
            {/* Content Body */}
            {content.body && (
              <div className="prose max-w-none">
                {typeof content.body === 'string' && (
                  <MarkdownRenderer content={content.body} />
                )}
                {typeof content.body !== 'string' && content.body && (
                  <pre className="whitespace-pre-wrap">
                    {String(content.body) as React.ReactNode}
                  </pre>
                )}
              </div>
            )}
            
            {/* Content Metadata */}
            <div className="flex items-center gap-4 text-sm text-muted-foreground border-t pt-4">
              {content.estimatedMinutes && (
                <span>Estimated time: {content.estimatedMinutes} minutes</span>
              )}
              {content.maxPoints && (
                <span>Points: {content.maxPoints}</span>
              )}
              {content.isRequired && (
                <span className="text-orange-600">Required</span>
              )}
            </div>
            
            {/* Children Content */}
            {children.length > 0 && (
              <div className="space-y-4">
                <h3 className="text-lg font-semibold">Sub-content</h3>
                <div className="space-y-2">
                  {children.map((child, index) => {
                    const childSlug = child.id;
                    const childPath = `${currentPath}/${childSlug}`;
                    
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
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}