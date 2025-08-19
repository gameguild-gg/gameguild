'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronRight, ChevronDown, BookOpen, FileText, ClipboardList, HelpCircle, Code, MessageSquare, Trophy, BarChart3 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { ProgramContent } from '@/lib/api/generated/types.gen';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useState } from 'react';
import { Button } from '@/components/ui/button';

interface CourseContentSidebarProps {
  courseSlug: string;
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
}

function ContentItem({ item, courseSlug, index, level, parentPath = '' }: ContentItemProps) {
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
        
        <Link href={href} className="flex items-center gap-2 flex-1 min-w-0">
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
            />
          ))}
        </div>
      )}
    </div>
  );
}

export function CourseContentSidebar({ courseSlug, content }: CourseContentSidebarProps) {
  return (
    <div className="w-80 border-r border-border bg-background flex flex-col">
      {/* Header */}
      <div className="p-4 border-b border-border">
        <div className="flex items-center gap-2 mb-2">
          <BookOpen className="h-5 w-5 text-primary" />
          <h2 className="font-semibold">Course Content</h2>
        </div>
      </div>
      
      {/* Navigation */}
      <ScrollArea className="flex-1">
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
              />
            ))
          )}
        </div>
      </ScrollArea>
      
      {/* Footer */}
      <div className="p-4 border-t border-border">
        <Link href={`/courses/${courseSlug}/content`}>
          <Button variant="outline" size="sm" className="w-full">
            Manage Content
          </Button>
        </Link>
      </div>
    </div>
  );
}