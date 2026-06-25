'use client';

import { ThemeToggle } from '@/components/ui/theme-toggle';
import { Button } from '@/components/ui/button';
import { ProgramContent } from '@/lib/api/generated/types.gen';
import { cn } from '@/lib/utils';
import { BarChart3, ClipboardList, Code, FileText, Flag, Folder, FolderOpen, HelpCircle, MessageSquare } from 'lucide-react';
import type { Route } from 'next';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useEffect, type ReactElement } from 'react';
import { useSidebar } from './sidebar-context';

function getContentSlug(content: ProgramContent): string {
  const slug = (content.slug ?? '').trim();
  if (slug.length > 0) return slug;
  const id = String(content.id ?? '').trim();
  if (id.length > 0) return id;
  return 'untitled';
}

interface CourseContentSidebarProps {
  courseSlug: string;
  courseTitle?: string;
  content: ProgramContent[];
}

function calculateTotalMinutes(item: ProgramContent): number {
  const own = item.estimatedMinutes ?? 0;
  const children = item.children ?? [];
  if (children.length === 0) return own;
  return own + children.reduce((sum, child) => sum + calculateTotalMinutes(child), 0);
}

function formatDuration(totalMinutes: number): string {
  if (!totalMinutes || totalMinutes <= 0) return '';
  if (totalMinutes < 60) return `${totalMinutes}m`;
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return minutes === 0 ? `${hours}h` : `${hours}h ${minutes}m`;
}

function getContentIcon(type: number | null | undefined): typeof FileText {
  switch (type) {
    case 0: return FileText; // Lesson
    case 2: return ClipboardList; // Assignment
    case 3: return HelpCircle; // Quiz
    case 4: return MessageSquare; // Discussion
    case 5: return Code; // Code
    case 7: return BarChart3; // Reflection
    case 8: return HelpCircle; // Survey
    case 9: return Flag; // Project
    case null:
    case undefined:
    default: return FileText;
  }
}

/**
 * Collect the paths (built from slugs) of all ancestor items that contain
 * the currently active pathname. This lets us auto-expand the tree to reveal
 * whichever page the user is on.
 */
function collectAncestorPaths(
  items: ProgramContent[],
  courseSlug: string,
  pathname: string,
  parentPath = '',
): string[] {
  for (const item of items) {
    const contentSlug = getContentSlug(item);
    const currentPath = parentPath ? `${parentPath}/${contentSlug}` : contentSlug;
    const href = `/p/${courseSlug}/${currentPath}`;

    if (pathname === href || pathname.startsWith(`${href}/`)) {
      const children = item.children ?? [];
      if (children.length > 0) {
        const deeper = collectAncestorPaths(children, courseSlug, pathname, currentPath);
        return [currentPath, ...deeper];
      }
      return [currentPath];
    }
  }
  return [];
}

interface ContentItemProps {
  item: ProgramContent;
  courseSlug: string;
  level: number;
  parentPath?: string;
  isMobile: boolean;
  closeSidebar: () => void;
}

function ContentItem({ item, courseSlug, level, parentPath = '', isMobile, closeSidebar }: ContentItemProps): ReactElement {
  const pathname = usePathname();
  const { expandedIds, toggleExpanded } = useSidebar();

  const totalMinutes = calculateTotalMinutes(item);

  const contentSlug = getContentSlug(item);
  const currentPath = parentPath ? `${parentPath}/${contentSlug}` : contentSlug;
  const href = `/p/${courseSlug}/${currentPath}` as Route;
  const isActive = pathname === href;
  const Icon = getContentIcon(item.type ?? 0);

  const children = item.children ?? [];
  const hasChildren = children.length > 0;
  const isExpanded = expandedIds.has(currentPath);
  const hasActiveChild = hasChildren && children.some((child) => {
    const childSlug = getContentSlug(child);
    const childPath = `${currentPath}/${childSlug}`;
    const childHref = `/p/${courseSlug}/${childPath}`;
    return pathname === childHref || pathname.startsWith(`${childHref}/`);
  });
  const paddingLeft = level * 16;

  const handleItemClick = (): void => {
    if (hasChildren) {
      toggleExpanded(currentPath);
    }

    if (isMobile) {
      closeSidebar();
    }
  };

  return (
    <div>
      <div
        className={cn(
          "flex items-center gap-2 p-2 rounded-lg transition-colors cursor-pointer",
          level === 0
            ? (isActive && !hasActiveChild
              ? "bg-primary text-primary-foreground font-semibold"
              : "hover:bg-muted")
            : (isActive
              ? "bg-foreground text-background font-semibold dark:bg-white/90 dark:text-black"
              : "hover:bg-muted/50")
        )}
        style={{ paddingLeft: `${paddingLeft + 12}px` }}
      >
        <Link
          href={href}
          className="flex items-center gap-2 flex-1 min-w-0"
          onClick={handleItemClick}
        >
          {hasChildren ? (
            isExpanded ? (
              <FolderOpen className="h-3 w-3" />
            ) : (
              <Folder className="h-3 w-3" />
            )
          ) : (
            <Icon className="h-3 w-3" />
          )}
          <div className="flex-1 min-w-0">
            <div className="font-medium text-sm wrap-break-word">{item.title || 'Untitled'}</div>
            {totalMinutes > 0 && (
              <div className={cn("text-xs", isActive ? "text-background/70 dark:text-black/60" : "text-muted-foreground")}>
                {formatDuration(totalMinutes)}
              </div>
            )}
          </div>
        </Link>
      </div>

      {hasChildren ? (
        <div
          className={cn(
            "overflow-hidden transition-all duration-300 ease-in-out",
            isExpanded ? "max-h-[1000px] opacity-100" : "max-h-0 opacity-0"
          )}
        >
          <div className="mt-1">
            {children.map((child) => (
              <ContentItem
                key={child.id}
                item={child}
                courseSlug={courseSlug}
                level={level + 1}
                parentPath={currentPath}
                isMobile={isMobile}
                closeSidebar={closeSidebar}
              />
            ))}
          </div>
        </div>
      ) : null}
    </div>
  );
}

export function CourseContentSidebar({ courseSlug, courseTitle, content }: CourseContentSidebarProps): ReactElement {
  const { isSidebarOpen, closeSidebar, isMobile, mounted, expandIds, scrollRef, restoreScroll } = useSidebar();
  const pathname = usePathname();
  const rawTitle = courseTitle ?? '';
  const headerTitle = rawTitle.trim().length > 0 ? rawTitle : 'Course Content';

  // Auto-expand ancestors of the currently active page
  useEffect(() => {
    const ancestors = collectAncestorPaths(content, courseSlug, pathname);
    if (ancestors.length > 0) {
      expandIds(ancestors);
    }
  }, [pathname, content, courseSlug, expandIds]);

  // Restore saved scroll position after first render
  useEffect(() => {
    restoreScroll();
  }, [restoreScroll]);

  return (
    <>
      {mounted && isMobile && isSidebarOpen && (
        <div
          className="fixed top-0 left-0 right-0 bottom-0 bg-black/50 z-40 lg:hidden"
          onClick={closeSidebar}
        />
      )}

      <div
        className={cn(
          "w-80 bg-background flex flex-col transition-all duration-300 ease-in-out",
          "lg:fixed lg:top-0 lg:left-0 lg:h-screen lg:border-r lg:border-border lg:z-40",
          // Desktop: Start visible, hide if closed after mount
          "lg:translate-x-0",
          mounted && !isSidebarOpen && "lg:-translate-x-full",
          // Mobile: Start hidden, show if open after mount
          "fixed top-0 left-0 h-screen z-50 border-r border-border",
          "-translate-x-full",
          mounted && isSidebarOpen && "translate-x-0"
        )}
      >
        <div className="p-4 border-b border-border">
          <div className="flex items-center justify-between mb-3">
            <h2 className="font-semibold text-lg truncate">
              {headerTitle}
            </h2>
            <div className="flex items-center gap-2">
              <ThemeToggle />
              <Button
                variant="ghost"
                size="sm"
                asChild
                className="text-muted-foreground hover:text-foreground"
              >
                <Link href="/programs">
                  ← Courses
                </Link>
              </Button>
            </div>
          </div>
        </div>

        <div ref={scrollRef} className="flex-1 min-h-0 overflow-y-auto">
          <div className="p-2 space-y-1">
            {content.length === 0 ? (
              <div className="text-sm text-muted-foreground p-3 text-center">
                No content available
              </div>
            ) : (
              content.map((item) => (
                <ContentItem
                  key={item.id}
                  item={item}
                  courseSlug={courseSlug}
                  level={0}
                  isMobile={mounted ? isMobile : false}
                  closeSidebar={closeSidebar}
                />
              ))
            )}
          </div>
        </div>
      </div>
    </>
  );
}
