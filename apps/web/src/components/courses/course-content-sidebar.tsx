'use client';

import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { ProgramContent } from '@/lib/api/generated/types.gen';
import { cn } from '@/lib/utils';
import { BarChart3, BookOpen, ChevronDown, ChevronRight, ClipboardList, Code, FileText, HelpCircle, MessageSquare, Trophy } from 'lucide-react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState } from 'react';
import { useSidebar } from './sidebar-context';
import { ThemeToggle } from '@/components/theme/theme-toggle';

interface CourseContentSidebarProps {
  courseSlug: string;
  courseTitle?: string;
  content: ProgramContent[];
}



function getContentIcon(type: number) {
  switch (type) {
    case 0: return FileText; // Page
    case 1: return ClipboardList; // Assignment
    case 2: return HelpCircle; // Questionnaire
    case 3: return MessageSquare; // Discussion
    case 4: return Code; // Code
    case 5: return Trophy; // Challenge
    case 6: return BarChart3; // Reflection
    case 7: return HelpCircle; // Survey
    default: return FileText;
  }
}

interface ContentItemProps {
  item: ProgramContent;
  courseSlug: string;
  index: number;
  level: number;
  parentPath?: string;
  isMobile: boolean;
  closeSidebar: () => void;
}

function ContentItem({ item, courseSlug, index, level, parentPath = '', isMobile, closeSidebar }: ContentItemProps) {
  const pathname = usePathname();
  const [isExpanded, setIsExpanded] = useState(false);

  const contentSlug = item.slug || item.id || 'untitled';
  const currentPath = parentPath ? `${parentPath}/${contentSlug}` : contentSlug;
  const href = `/courses/${courseSlug}/content/${currentPath}`;
  const isActive = pathname === href || pathname.startsWith(`${href}/`);
  const Icon = getContentIcon(item.type || 0);

  const hasChildren = item.children && item.children.length > 0;
  const paddingLeft = level * 16;

  return (
    <div>
      <div className={cn(
        "flex items-center gap-2 p-2 rounded-lg transition-colors cursor-pointer",
        isActive
          ? "bg-primary text-primary-foreground"
          : "hover:bg-muted"
      )} style={{ paddingLeft: `${paddingLeft + 12}px` }}>
        {hasChildren && (
          <button
            onClick={(e) => {
              e.preventDefault();
              setIsExpanded(!isExpanded);
            }}
            className="p-1 hover:bg-black/10 rounded"
          >
            {isExpanded ? (
              <ChevronDown className="h-3 w-3" />
            ) : (
              <ChevronRight className="h-3 w-3" />
            )}
          </button>
        )}

        <Link
          href={href}
          className="flex items-center gap-2 flex-1 min-w-0"
          onClick={() => {
            // Close sidebar on mobile when clicking a content item
            if (isMobile) {
              closeSidebar();
            }
          }}
        >
          <div className="flex items-center justify-center w-5 h-5 rounded-full bg-primary/10 text-primary font-medium text-xs">
            {index + 1}
          </div>
          <Icon className="h-3 w-3" />
          <div className="flex-1 min-w-0">
            <div className="font-medium text-sm break-words">{item.title || 'Untitled'}</div>
            {item.estimatedMinutes && (
              <div className="text-xs text-muted-foreground">
                {item.estimatedMinutes}m
              </div>
            )}
          </div>
        </Link>
      </div>

      {hasChildren && isExpanded && (
        <div className="mt-1">
          {item.children!.map((child, childIndex) => (
            <ContentItem
              key={child.id}
              item={child}
              courseSlug={courseSlug}
              index={childIndex}
              level={level + 1}
              parentPath={currentPath}
              isMobile={isMobile}
              closeSidebar={closeSidebar}
            />
          ))}
        </div>
      )}
    </div>
  );
}

export function CourseContentSidebar({ courseSlug, courseTitle, content }: CourseContentSidebarProps) {
  const { isSidebarOpen, closeSidebar, isMobile } = useSidebar();

  return (
    <>
      {/* Overlay - only on small screens when sidebar is open */}
      {isSidebarOpen && (
        <div
          className="fixed top-0 left-0 right-0 bottom-0 bg-black/50 z-40 lg:hidden"
          onClick={closeSidebar}
        />
      )}

      {/* Sidebar */}
      <div className={cn(
        "w-80 bg-background flex flex-col transition-all duration-300 ease-in-out",
        // Large screens: fixed positioning, always visible when open
        "lg:fixed lg:top-0 lg:left-0 lg:h-screen lg:border-r lg:border-border lg:z-40",
        // Large screens: hide/show with transform
        isSidebarOpen ? "lg:translate-x-0" : "lg:-translate-x-full",
        // Small screens: fixed positioning with overlay behavior
        "fixed top-0 left-0 h-screen z-50 border-r border-border",
        // Small screens: hide/show with transform
        isSidebarOpen ? "translate-x-0" : "-translate-x-full"
      )}>
        {/* Header */}
        <div className="p-4 border-b border-border">
          <div className="flex items-center justify-between mb-3">
            <h2 className="font-semibold text-lg truncate">
              {courseTitle || 'Course Content'}
            </h2>
            <div className="flex items-center gap-2">
              <ThemeToggle />
              <Button
                 variant="ghost"
                 size="sm"
                 asChild
                 className="text-muted-foreground hover:text-foreground"
               >
                 <Link href="/courses/catalog">
                   ← Courses
                 </Link>
               </Button>
            </div>
          </div>
        </div>


        {/* Navigation */}
        <ScrollArea className="flex-1 min-h-0">
          <div className="p-2 space-y-1">
            {content.length === 0 ? (
              <div className="text-sm text-muted-foreground p-3 text-center">
                No content available
              </div>
            ) : (
              content.map((item, index) => (
                <ContentItem
                  key={item.id}
                  item={item}
                  courseSlug={courseSlug}
                  index={index}
                  level={0}
                  isMobile={isMobile}
                  closeSidebar={closeSidebar}
                />
              ))
            )}
          </div>
        </ScrollArea>


      </div>
    </>
  );
}