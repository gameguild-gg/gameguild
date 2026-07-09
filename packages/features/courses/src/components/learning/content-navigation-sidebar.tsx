'use client';

import { Badge } from '@game-guild/ui/components/badge';
import { Progress } from '@game-guild/ui/components/progress';
import { cn } from '@game-guild/ui/lib/utils';
import { BookOpen, CheckCircle2, Circle, FileQuestion, Lock, MessageSquare, PlayCircle, Upload } from 'lucide-react';

export interface ContentNavigationItem {
  id: string;
  title: string;
  type: string;
  status?: string;
  children?: ContentNavigationItem[];
  order?: number;
  progress?: number;
}

export interface CourseModule {
  id: string;
  title: string;
  description?: string;
  order: number;
  items: ContentNavigationItem[];
  contentItems?: ContentNavigationItem[];
  isLocked?: boolean;
  progress?: number;
}

export interface ContentNavigationSidebarProps {
  courseId?: string;
  modules?: CourseModule[];
  currentContentId?: string;
  onContentSelect?: (contentId: string) => void;
  items?: ContentNavigationItem[];
  currentItemId?: string;
  onItemClick?: (item: ContentNavigationItem) => void;
}

function getItemIcon(type: string, locked: boolean, completed: boolean) {
  if (locked) return <Lock className="h-4 w-4" aria-hidden="true" />;
  if (completed) return <CheckCircle2 className="h-4 w-4" aria-hidden="true" />;

  switch (type) {
    case 'activity':
      return <PlayCircle className="h-4 w-4" aria-hidden="true" />;
    case 'quiz':
      return <FileQuestion className="h-4 w-4" aria-hidden="true" />;
    case 'assignment':
      return <Upload className="h-4 w-4" aria-hidden="true" />;
    case 'peer-review':
      return <MessageSquare className="h-4 w-4" aria-hidden="true" />;
    case 'lesson':
      return <BookOpen className="h-4 w-4" aria-hidden="true" />;
    default:
      return <Circle className="h-4 w-4" aria-hidden="true" />;
  }
}

function getStatusLabel(status?: string, moduleLocked = false): string | null {
  if (moduleLocked || status === 'locked') return 'Locked';
  if (status === 'completed' || status === 'graded') return 'Completed';
  if (status === 'in-progress') return 'In progress';
  return null;
}

function normalizeModules(modules?: CourseModule[], items?: ContentNavigationItem[]): CourseModule[] {
  if (modules?.length) {
    return [...modules]
      .sort((left, right) => left.order - right.order)
      .map((module) => ({
        ...module,
        items: [...(module.items?.length ? module.items : module.contentItems ?? [])].sort((left, right) => (left.order ?? 0) - (right.order ?? 0)),
      }));
  }

  return [
    {
      id: 'course-content',
      title: 'Course Content',
      order: 0,
      progress: undefined,
      items: [...(items ?? [])].sort((left, right) => (left.order ?? 0) - (right.order ?? 0)),
    },
  ];
}

export function ContentNavigationSidebar({
  courseId,
  modules,
  currentContentId,
  onContentSelect,
  items,
  currentItemId,
  onItemClick,
}: ContentNavigationSidebarProps) {
  const normalizedModules = normalizeModules(modules, items);
  const effectiveCurrentId = currentContentId || currentItemId;

  function handleSelect(item: ContentNavigationItem, locked: boolean) {
    if (locked) return;
    onContentSelect?.(item.id);
    onItemClick?.(item);
  }

  return (
    <aside className="fixed inset-y-0 left-0 z-20 w-80 overflow-y-auto border-r border-gray-800 bg-gray-950/95 p-4 text-gray-100 shadow-xl" aria-label="Course content navigation">
      <div className="mb-5 space-y-1">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-gray-200">Course content</h2>
        {courseId && <p className="text-xs text-gray-500">Course {courseId}</p>}
      </div>

      <div className="space-y-5">
        {normalizedModules.map((module) => {
          const moduleItems = module.items ?? [];
          const completedItems = moduleItems.filter((item) => item.status === 'completed' || item.status === 'graded').length;
          const progress = typeof module.progress === 'number' ? module.progress : moduleItems.length > 0 ? Math.round((completedItems / moduleItems.length) * 100) : 0;

          return (
            <section key={module.id} aria-labelledby={`module-${module.id}`}>
              <div className="mb-2 space-y-2">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h3 id={`module-${module.id}`} className="truncate text-sm font-semibold text-gray-100">
                      {module.title}
                    </h3>
                    {module.description && <p className="line-clamp-2 text-xs text-gray-500">{module.description}</p>}
                  </div>
                  <div className="flex shrink-0 items-center gap-2">
                    {module.isLocked && <Badge variant="secondary">Locked</Badge>}
                    <span className="text-xs tabular-nums text-gray-400">{progress}%</span>
                  </div>
                </div>
                <Progress value={progress} className="h-1.5" />
              </div>

              <div className="space-y-1">
                {moduleItems.map((item) => {
                  const locked = Boolean(module.isLocked || item.status === 'locked');
                  const completed = item.status === 'completed' || item.status === 'graded';
                  const selected = effectiveCurrentId === item.id;
                  const statusLabel = getStatusLabel(item.status, module.isLocked);

                  return (
                    <button
                      key={item.id}
                      type="button"
                      disabled={locked}
                      aria-current={selected ? 'step' : undefined}
                      onClick={() => handleSelect(item, locked)}
                      className={cn(
                        'flex w-full items-start gap-3 rounded-md border px-3 py-2 text-left text-sm transition',
                        selected
                          ? 'border-blue-500/60 bg-blue-500/15 text-white'
                          : 'border-transparent text-gray-300 hover:border-gray-700 hover:bg-gray-900',
                        locked && 'cursor-not-allowed opacity-55 hover:border-transparent hover:bg-transparent',
                      )}
                    >
                      <span className={cn('mt-0.5 text-gray-500', completed && 'text-green-400', selected && 'text-blue-300', locked && 'text-gray-600')}>
                        {getItemIcon(item.type, locked, completed)}
                      </span>
                      <span className="min-w-0 flex-1">
                        <span className="block truncate font-medium">{item.title}</span>
                        <span className="mt-1 flex flex-wrap items-center gap-2 text-xs text-gray-500">
                          <span className="capitalize">{item.type}</span>
                          {typeof item.progress === 'number' && <span>{item.progress}%</span>}
                          {statusLabel && <Badge variant={completed ? 'default' : 'secondary'}>{statusLabel}</Badge>}
                        </span>
                      </span>
                    </button>
                  );
                })}
              </div>
            </section>
          );
        })}
      </div>
    </aside>
  );
}

export default ContentNavigationSidebar;
