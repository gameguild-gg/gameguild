'use client';

import { addContent, deleteContent, reorderContent, updateAssessment, updateContent } from '@/lib/learning/actions';
import type { Assessment } from '@/lib/learning/queries/assessments';
import type { ContentItem, LearningCoursesProgramContentType } from '@/lib/learning/types';
import type { DragEndEvent } from '@dnd-kit/core';
import { closestCenter, DndContext, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import { arrayMove, SortableContext, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@game-guild/ui/components/collapsible';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Tooltip, TooltipContent, TooltipTrigger } from '@game-guild/ui/components/tooltip';
import { ArrowDown, ArrowUp, BookOpen, ChevronDown, ChevronRight, ClipboardList, Clock, Copy, Edit, FileText, GripVertical, HelpCircle, LinkIcon, Loader2, MessageSquare, Plus, Trash2, Unlink } from 'lucide-react';
import { usePathname, useRouter } from 'next/navigation';
import React, { useState, useTransition } from 'react';

interface ContentTreeProps {
  courseId: string;
  modules: ContentItem[];
  allItems: ContentItem[];
  assessments: Assessment[];
  virtualModuleIds?: string[];
}

const typeConfig: Record<LearningCoursesProgramContentType, { icon: React.ElementType; label: string }> = {
  Lesson: { icon: FileText, label: 'Lesson' },
  Page: { icon: FileText, label: 'Page' },
  Assignment: { icon: FileText, label: 'Assignment' },
  Questionnaire: { icon: HelpCircle, label: 'Quiz' },
  Discussion: { icon: MessageSquare, label: 'Discussion' },
  Code: { icon: FileText, label: 'Code' },
  Challenge: { icon: HelpCircle, label: 'Challenge' },
  Reflection: { icon: BookOpen, label: 'Reflection' },
  Survey: { icon: HelpCircle, label: 'Survey' },
};

const statusVariant: Record<string, 'default' | 'secondary' | 'outline'> = {
  published: 'default',
  draft: 'secondary',
  archived: 'outline',
};

// Lesson types available when adding a new lesson (backend ProgramContentType values)
const lessonTypes: Array<{ value: LearningCoursesProgramContentType; label: string }> = [
  { value: 'Lesson', label: 'Lesson' },
  { value: 'Page', label: 'Page' },
  { value: 'Assignment', label: 'Assignment' },
  { value: 'Questionnaire', label: 'Quiz' },
  { value: 'Discussion', label: 'Discussion' },
  { value: 'Code', label: 'Code' },
  { value: 'Challenge', label: 'Challenge' },
];

function SortableItem({ id, children }: {
  id: string;
  children: (props: {
    ref: (el: HTMLElement | null) => void;
    style: React.CSSProperties;
    listeners: Record<string, Function> | undefined;
    isDragging: boolean;
  }) => React.ReactNode;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id });
  const style: React.CSSProperties = {
    transform: CSS.Transform.toString(transform),
    transition,
  };
  return <>{children({ ref: setNodeRef, style, listeners, isDragging })}</>;
}

function ContentActionButton({
  label,
  icon: Icon,
  onClick,
  disabled,
  destructive = false,
  className = 'size-8',
}: {
  label: string;
  icon: React.ElementType;
  onClick: () => void;
  disabled?: boolean;
  destructive?: boolean;
  className?: string;
}) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className={`${className} ${destructive ? 'text-destructive hover:text-destructive' : ''}`}
          onClick={(event) => {
            event.stopPropagation();
            onClick();
          }}
          disabled={disabled}
          aria-label={label}
        >
          <Icon className="size-4" />
        </Button>
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}

export function ContentTree({ courseId, modules, allItems, assessments, virtualModuleIds = [] }: ContentTreeProps) {
  const router = useRouter();
  const pathname = usePathname();
  const [isPending, startTransition] = useTransition();
  const [openModules, setOpenModules] = useState<Set<string>>(() => new Set(modules.map((m) => m.id)));
  const virtualModuleIdSet = React.useMemo(() => new Set(virtualModuleIds), [virtualModuleIds]);

  // Derive the base path for navigation (e.g. /en-US/learning/courses/{id}/content)
  const contentBasePath = pathname.endsWith('/content') ? pathname : pathname.replace(/\/content\/.*$/, '/content');

  const navigateToContentItem = (contentId: string) => {
    router.push(`${contentBasePath}/${contentId}` as Parameters<typeof router.push>[0]);
  };

  const isVirtualModule = (moduleId: string) => virtualModuleIdSet.has(moduleId);
  const normalizeParentId = (parentId: string | null | undefined): string | undefined => {
    if (!parentId || virtualModuleIdSet.has(parentId)) {
      return undefined;
    }

    return parentId;
  };

  // Add Module dialog state
  const [showAddModule, setShowAddModule] = useState(false);
  const [moduleTitle, setModuleTitle] = useState('');
  const [moduleDescription, setModuleDescription] = useState('');

  // Add Lesson dialog state
  const [showAddLesson, setShowAddLesson] = useState(false);
  const [lessonParentId, setLessonParentId] = useState('');
  const [lessonTitle, setLessonTitle] = useState('');
  const [lessonType, setLessonType] = useState<LearningCoursesProgramContentType>('Lesson');

  // Delete confirmation state
  const [deleteTarget, setDeleteTarget] = useState<{ id: string; title: string; isModule: boolean } | null>(null);

  // Edit module dialog state
  const [editTarget, setEditTarget] = useState<ContentItem | null>(null);
  const [editTitle, setEditTitle] = useState('');
  const [editDescription, setEditDescription] = useState('');

  const [error, setError] = useState('');

  // Submodule dialog state
  const [submoduleParentId, setSubmoduleParentId] = useState<string | null>(null);

  // Assessment picker dialog state
  const [assessmentPickerTarget, setAssessmentPickerTarget] = useState<string | null>(null); // content item id

  // Build a map of contentId -> Assessment for quick lookup
  const assessmentsByContentId = React.useMemo(() => {
    const map = new Map<string, Assessment>();
    for (const a of assessments) {
      if (a.contentId) map.set(a.contentId, a);
    }
    return map;
  }, [assessments]);

  // DnD sensors
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 8 } }));

  function handleModuleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const oldIndex = modules.findIndex((m) => m.id === active.id);
    const newIndex = modules.findIndex((m) => m.id === over.id);
    if (oldIndex < 0 || newIndex < 0) return;
    const newIds = arrayMove(modules.map((m) => m.id), oldIndex, newIndex);
    setError('');
    startTransition(async () => {
      const result = await reorderContent(courseId, newIds);
      if (result.success) {
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function makeLessonDragEnd(parentId: string, children: ContentItem[]) {
    return (event: DragEndEvent) => {
      const { active, over } = event;
      if (!over || active.id === over.id) return;
      const oldIndex = children.findIndex((c) => c.id === active.id);
      const newIndex = children.findIndex((c) => c.id === over.id);
      if (oldIndex < 0 || newIndex < 0) return;
      const newIds = arrayMove(children.map((c) => c.id), oldIndex, newIndex);
      setError('');
      startTransition(async () => {
        const result = await reorderContent(courseId, newIds);
        if (result.success) {
          router.refresh();
        } else {
          setError(result.error);
        }
      });
    };
  }

  function openAddSubmoduleDialog(parentId: string) {
    setSubmoduleParentId(parentId);
    setModuleTitle('');
    setModuleDescription('');
    setError('');
  }

  function handleAddSubmodule() {
    if (!moduleTitle.trim() || !submoduleParentId) return;
    setError('');
    const parentChildren = allItems.filter((i) => i.parentId === submoduleParentId);
    startTransition(async () => {
      const result = await addContent({
        courseId,
        parentId: submoduleParentId,
        title: moduleTitle.trim(),
        description: moduleDescription.trim(),
        type: 'Lesson',
        sortOrder: parentChildren.length,
      });
      if (result.success) {
        setSubmoduleParentId(null);
        setModuleTitle('');
        setModuleDescription('');
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleAttachAssessment(assessmentId: string) {
    if (!assessmentPickerTarget) return;
    setError('');
    startTransition(async () => {
      const result = await updateAssessment({
        courseId,
        assessmentId,
        contentId: assessmentPickerTarget,
      });
      if (result.success) {
        setAssessmentPickerTarget(null);
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleDetachAssessment(assessmentId: string) {
    setError('');
    startTransition(async () => {
      const result = await updateAssessment({
        courseId,
        assessmentId,
        clearContentId: true,
      });
      if (result.success) {
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function toggleModule(id: string) {
    setOpenModules((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function handleAddModule() {
    if (!moduleTitle.trim()) return;
    setError('');
    startTransition(async () => {
      const result = await addContent({
        courseId,
        title: moduleTitle.trim(),
        description: moduleDescription.trim(),
        type: 'Lesson', // backend type for a structural grouping
        sortOrder: modules.length,
      });
      if (result.success) {
        setShowAddModule(false);
        setModuleTitle('');
        setModuleDescription('');
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleAddLesson() {
    if (!lessonTitle.trim()) return;
    setError('');
    const parentChildren = allItems.filter((i) => i.parentId === lessonParentId);
    startTransition(async () => {
      const result = await addContent({
        courseId,
        parentId: normalizeParentId(lessonParentId),
        title: lessonTitle.trim(),
        type: lessonType,
        sortOrder: parentChildren.length,
      });
      if (result.success) {
        setShowAddLesson(false);
        setLessonTitle('');
        setLessonType('Lesson');
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleDelete() {
    if (!deleteTarget) return;
    setError('');
    startTransition(async () => {
      const result = await deleteContent(courseId, deleteTarget.id);
      if (result.success) {
        setDeleteTarget(null);
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function openAddLessonDialog(parentId: string) {
    setLessonParentId(parentId);
    setLessonTitle('');
    setLessonType('Lesson' as LearningCoursesProgramContentType);
    setError('');
    setShowAddLesson(true);
  }

  function openEditModuleDialog(item: ContentItem) {
    setEditTarget(item);
    setEditTitle(item.title);
    setEditDescription(item.description ?? '');
    setError('');
  }

  function handleEditModule() {
    if (!editTarget || !editTitle.trim()) return;
    setError('');
    startTransition(async () => {
      const result = await updateContent({
        courseId,
        contentId: editTarget.id,
        title: editTitle.trim(),
        description: editDescription.trim(),
      });
      if (result.success) {
        setEditTarget(null);
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleDuplicate(item: ContentItem) {
    setError('');
    startTransition(async () => {
      const result = await addContent({
        courseId,
        parentId: normalizeParentId(item.parentId),
        title: `${item.title} (copy)`,
        description: item.description ?? undefined,
        type: item.type,
      });
      if (result.success) {
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleMoveModule(moduleId: string, direction: 'up' | 'down') {
    const ids = modules.map((m) => m.id);
    const idx = ids.indexOf(moduleId);
    if (idx < 0) return;
    const swapIdx = direction === 'up' ? idx - 1 : idx + 1;
    if (swapIdx < 0 || swapIdx >= ids.length) return;
    [ids[idx], ids[swapIdx]] = [ids[swapIdx], ids[idx]];
    setError('');
    startTransition(async () => {
      const result = await reorderContent(courseId, ids);
      if (result.success) {
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  function handleMoveLesson(parentId: string, itemId: string, direction: 'up' | 'down') {
    const siblings = allItems
      .filter((i) => i.parentId === parentId)
      .sort((a, b) => a.order - b.order);
    const ids = siblings.map((s) => s.id);
    const idx = ids.indexOf(itemId);
    if (idx < 0) return;
    const swapIdx = direction === 'up' ? idx - 1 : idx + 1;
    if (swapIdx < 0 || swapIdx >= ids.length) return;
    [ids[idx], ids[swapIdx]] = [ids[swapIdx], ids[idx]];
    setError('');
    startTransition(async () => {
      const result = await reorderContent(courseId, ids);
      if (result.success) {
        router.refresh();
      } else {
        setError(result.error);
      }
    });
  }

  return (
    <>
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleModuleDragEnd}>
        <SortableContext items={modules.map((m) => m.id)} strategy={verticalListSortingStrategy}>
          <div className="space-y-4">
            {modules.map((module, index) => {
              const children = allItems.filter((i) => i.parentId === module.id).sort((a, b) => a.order - b.order);
              const isOpen = openModules.has(module.id);
              const moduleIsVirtual = isVirtualModule(module.id);

              return (
                <SortableItem key={module.id} id={module.id}>
                  {({ ref, style, listeners, isDragging }) => (
                    <div ref={ref} style={style}>
                      <Collapsible open={isOpen} onOpenChange={() => toggleModule(module.id)}>
                        <Card className={isDragging ? 'opacity-50' : ''}>
                          <CardHeader className="flex flex-row items-center gap-3 pb-3">
                            <button type="button" className="cursor-grab touch-none" {...listeners}>
                              <GripVertical className="size-5 text-muted-foreground" />
                            </button>
                            <CollapsibleTrigger asChild>
                              <Button variant="ghost" size="icon" className="size-8">
                                {isOpen ? <ChevronDown className="size-4" /> : <ChevronRight className="size-4" />}
                              </Button>
                            </CollapsibleTrigger>
                            <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-sm font-bold text-primary">{index + 1}</div>
                            <div className="flex-1">
                              <CardTitle className="text-base">{module.title}</CardTitle>
                              {module.description && <CardDescription className="mt-0.5 text-xs">{module.description}</CardDescription>}
                            </div>
                            <div className="flex items-center gap-2">
                              <Badge variant={statusVariant[module.status] ?? 'outline'}>{module.status}</Badge>
                              <span className="text-xs text-muted-foreground">{children.length} items</span>
                              {!moduleIsVirtual && (
                                <div className="flex items-center gap-1">
                                  <ContentActionButton label="Edit module" icon={Edit} onClick={() => openEditModuleDialog(module)} />
                                  <ContentActionButton label="Duplicate module" icon={Copy} onClick={() => handleDuplicate(module)} disabled={isPending} />
                                  <ContentActionButton label="Add submodule" icon={Plus} onClick={() => openAddSubmoduleDialog(module.id)} />
                                  <ContentActionButton label="Move module up" icon={ArrowUp} onClick={() => handleMoveModule(module.id, 'up')} disabled={isPending || index === 0} />
                                  <ContentActionButton label="Move module down" icon={ArrowDown} onClick={() => handleMoveModule(module.id, 'down')} disabled={isPending || index === modules.length - 1} />
                                  <ContentActionButton label="Delete module" icon={Trash2} onClick={() => setDeleteTarget({ id: module.id, title: module.title, isModule: true })} destructive />
                                </div>
                              )}
                            </div>
                          </CardHeader>
                          <CollapsibleContent>
                            <CardContent className="pt-0">
                              {children.length === 0 ? (
                                <p className="py-4 text-center text-sm text-muted-foreground">No content items yet</p>
                              ) : (
                                <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={makeLessonDragEnd(module.id, children)}>
                                  <SortableContext items={children.map((c) => c.id)} strategy={verticalListSortingStrategy}>
                                    <div className="divide-y rounded-lg border">
                                      {children.map((item, itemIndex) => {
                                        const subchildren = allItems.filter((i) => i.parentId === item.id).sort((a, b) => a.order - b.order);
                                        const isSubmodule = subchildren.length > 0;
                                        const config = typeConfig[item.type] ?? { icon: FileText, label: item.type };
                                        const Icon = config.icon;
                                        const linkedAssessment = assessmentsByContentId.get(item.id);
                                        return (
                                          <SortableItem key={item.id} id={item.id}>
                                            {({ ref: itemRef, style: itemStyle, listeners: itemListeners, isDragging: itemDragging }) => (
                                              <div ref={itemRef} style={itemStyle}>
                                                <div className={`group flex items-center gap-3 px-4 py-3 transition-colors hover:bg-muted/50 ${itemDragging ? 'opacity-50' : ''}`}>
                                                  <button type="button" className="cursor-grab touch-none" {...itemListeners}>
                                                    <GripVertical className="size-4 text-muted-foreground/50" />
                                                  </button>
                                                  <div className="flex size-8 items-center justify-center rounded bg-muted">
                                                    <Icon className="size-4 text-muted-foreground" />
                                                  </div>
                                                  <div className="flex-1">
                                                    <p className="text-sm font-medium">{item.title}</p>
                                                    {isSubmodule && (
                                                      <p className="text-xs text-muted-foreground">{subchildren.length} sub-items</p>
                                                    )}
                                                    {linkedAssessment && (
                                                      <div className="mt-0.5 flex items-center gap-1">
                                                        <ClipboardList className="size-3 text-blue-500" />
                                                        <span className="text-xs text-blue-600">{linkedAssessment.title}</span>
                                                        <button type="button" className="ml-1 rounded p-0.5 text-muted-foreground hover:text-destructive" title="Detach assessment" onClick={(e) => { e.stopPropagation(); handleDetachAssessment(linkedAssessment.id); }} disabled={isPending}>
                                                          <Unlink className="size-3" />
                                                        </button>
                                                      </div>
                                                    )}
                                                  </div>
                                                  <Badge variant="outline" className="text-xs capitalize">
                                                    {config.label}
                                                  </Badge>
                                                  <Badge variant={statusVariant[item.status] ?? 'outline'} className="text-xs">
                                                    {item.status}
                                                  </Badge>
                                                  {item.duration != null && item.duration > 0 && (
                                                    <span className="flex items-center gap-1 text-xs text-muted-foreground">
                                                      <Clock className="size-3" />
                                                      {item.duration}m
                                                    </span>
                                                  )}
                                                  <div className="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100">
                                                    <ContentActionButton label={`Edit ${config.label}`} icon={Edit} onClick={() => navigateToContentItem(item.id)} className="size-7" />
                                                    <ContentActionButton label="Duplicate" icon={Copy} onClick={() => handleDuplicate(item)} disabled={isPending} className="size-7" />
                                                    {linkedAssessment ? (
                                                      <ContentActionButton label="Detach assessment" icon={Unlink} onClick={() => handleDetachAssessment(linkedAssessment.id)} disabled={isPending} className="size-7" />
                                                    ) : (
                                                      <ContentActionButton label="Attach assessment" icon={LinkIcon} onClick={() => setAssessmentPickerTarget(item.id)} disabled={assessments.length === 0} className="size-7" />
                                                    )}
                                                    <ContentActionButton label="Move up" icon={ArrowUp} onClick={() => handleMoveLesson(module.id, item.id, 'up')} disabled={isPending || itemIndex === 0} className="size-7" />
                                                    <ContentActionButton label="Move down" icon={ArrowDown} onClick={() => handleMoveLesson(module.id, item.id, 'down')} disabled={isPending || itemIndex === children.length - 1} className="size-7" />
                                                    <ContentActionButton label="Delete" icon={Trash2} onClick={() => setDeleteTarget({ id: item.id, title: item.title, isModule: false })} destructive className="size-7" />
                                                  </div>
                                                </div>
                                                {isSubmodule && (
                                                  <div className="ml-8 border-l pl-4 pb-2">
                                                    {subchildren.map((sub) => {
                                                      const subConfig = typeConfig[sub.type] ?? { icon: FileText, label: sub.type };
                                                      const SubIcon = subConfig.icon;
                                                      return (
                                                        <div key={sub.id} className="group flex items-center gap-3 px-4 py-2 transition-colors hover:bg-muted/30">
                                                          <div className="flex size-6 items-center justify-center rounded bg-muted">
                                                            <SubIcon className="size-3 text-muted-foreground" />
                                                          </div>
                                                          <div className="flex-1">
                                                            <p className="text-sm">{sub.title}</p>
                                                          </div>
                                                          <Badge variant="outline" className="text-xs">{subConfig.label}</Badge>
                                                          <Badge variant={statusVariant[sub.status] ?? 'outline'} className="text-xs">{sub.status}</Badge>
                                                          <div className="flex items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100">
                                                            <ContentActionButton label="Edit" icon={Edit} onClick={() => navigateToContentItem(sub.id)} className="size-6" />
                                                            <ContentActionButton label="Delete" icon={Trash2} onClick={() => setDeleteTarget({ id: sub.id, title: sub.title, isModule: false })} destructive className="size-6" />
                                                          </div>
                                                        </div>
                                                      );
                                                    })}
                                                    <Button variant="ghost" size="sm" className="mt-1 w-full text-xs text-muted-foreground" onClick={() => openAddLessonDialog(item.id)}>
                                                      <Plus className="mr-1 size-3" />
                                                      Add to {item.title}
                                                    </Button>
                                                  </div>
                                                )}
                                              </div>
                                            )}
                                          </SortableItem>
                                        );
                                      })}
                                    </div>
                                  </SortableContext>
                                </DndContext>
                              )}
                              <Button variant="ghost" size="sm" className="mt-2 w-full text-muted-foreground" onClick={() => openAddLessonDialog(module.id)}>
                                <Plus className="mr-2 size-4" />
                                {moduleIsVirtual ? 'Add Content Item' : 'Add Lesson'}
                              </Button>
                            </CardContent>
                          </CollapsibleContent>
                        </Card>
                      </Collapsible>
                    </div>
                  )}
                </SortableItem>
              );
            })}

            {/* Add Module button at the bottom */}
            <Button variant="outline" className="w-full border-dashed" onClick={() => { setModuleTitle(''); setModuleDescription(''); setError(''); setShowAddModule(true); }}>
              <Plus className="mr-2 size-4" />
              Add Module
            </Button>
          </div>
        </SortableContext>
      </DndContext>

      {/* ── Add Module Dialog ── */}
      <Dialog open={showAddModule} onOpenChange={setShowAddModule}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Module</DialogTitle>
            <DialogDescription>Create a new module to organize your course content.</DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-4 py-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="module-title">Title</Label>
              <Input id="module-title" placeholder="e.g. Introduction to Game Design" value={moduleTitle} onChange={(e) => setModuleTitle(e.target.value)} autoFocus />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="module-desc">Description (optional)</Label>
              <Input id="module-desc" placeholder="Brief description of this module" value={moduleDescription} onChange={(e) => setModuleDescription(e.target.value)} />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddModule(false)}>Cancel</Button>
            <Button onClick={handleAddModule} disabled={!moduleTitle.trim() || isPending}>
              {isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
              Add Module
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ── Add Lesson Dialog ── */}
      <Dialog open={showAddLesson} onOpenChange={setShowAddLesson}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Lesson</DialogTitle>
            <DialogDescription>Add a new content item to this module.</DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-4 py-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="lesson-title">Title</Label>
              <Input id="lesson-title" placeholder="e.g. Setting Up Your Environment" value={lessonTitle} onChange={(e) => setLessonTitle(e.target.value)} autoFocus />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="lesson-type">Type</Label>
              <Select value={lessonType} onValueChange={(v) => setLessonType(v as LearningCoursesProgramContentType)}>
                <SelectTrigger id="lesson-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {lessonTypes.map((t) => (
                    <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddLesson(false)}>Cancel</Button>
            <Button onClick={handleAddLesson} disabled={!lessonTitle.trim() || isPending}>
              {isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
              Add Lesson
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ── Delete Confirmation Dialog ── */}
      <Dialog open={!!deleteTarget} onOpenChange={(open) => { if (!open) setDeleteTarget(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete {deleteTarget?.isModule ? 'Module' : 'Item'}</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete &ldquo;{deleteTarget?.title}&rdquo;?
              {deleteTarget?.isModule && ' All lessons within this module will also be deleted.'}
              {' '}This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          {error && <p className="text-sm text-destructive">{error}</p>}
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>Cancel</Button>
            <Button variant="destructive" onClick={handleDelete} disabled={isPending}>
              {isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ── Edit Module Dialog ── */}
      <Dialog open={!!editTarget} onOpenChange={(open) => { if (!open) setEditTarget(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Module</DialogTitle>
            <DialogDescription>Update the module title and description.</DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-4 py-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="edit-module-title">Title</Label>
              <Input id="edit-module-title" value={editTitle} onChange={(e) => setEditTitle(e.target.value)} autoFocus />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="edit-module-desc">Description (optional)</Label>
              <Input id="edit-module-desc" value={editDescription} onChange={(e) => setEditDescription(e.target.value)} />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setEditTarget(null)}>Cancel</Button>
            <Button onClick={handleEditModule} disabled={!editTitle.trim() || isPending}>
              {isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ── Add Submodule Dialog ── */}
      <Dialog open={submoduleParentId !== null} onOpenChange={(open) => { if (!open) setSubmoduleParentId(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Submodule</DialogTitle>
            <DialogDescription>Create a submodule to further organize content within this module.</DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-4 py-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="submodule-title">Title</Label>
              <Input id="submodule-title" placeholder="e.g. Part A: Fundamentals" value={moduleTitle} onChange={(e) => setModuleTitle(e.target.value)} autoFocus />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="submodule-desc">Description (optional)</Label>
              <Input id="submodule-desc" placeholder="Brief description of this submodule" value={moduleDescription} onChange={(e) => setModuleDescription(e.target.value)} />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSubmoduleParentId(null)}>Cancel</Button>
            <Button onClick={handleAddSubmodule} disabled={!moduleTitle.trim() || isPending}>
              {isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
              Add Submodule
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* ── Attach Assessment Picker Dialog ── */}
      <Dialog open={assessmentPickerTarget !== null} onOpenChange={(open) => { if (!open) setAssessmentPickerTarget(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Attach Assessment</DialogTitle>
            <DialogDescription>Select an assessment to link to this content item.</DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-2 py-2">
            {assessments.length === 0 ? (
              <p className="py-4 text-center text-sm text-muted-foreground">No assessments available. Create one in the Assessments tab first.</p>
            ) : (
              assessments.map((a) => {
                const alreadyLinked = a.contentId !== null && a.contentId !== assessmentPickerTarget;
                return (
                  <button
                    key={a.id}
                    type="button"
                    disabled={isPending || alreadyLinked}
                    onClick={() => handleAttachAssessment(a.id)}
                    className="flex items-center gap-3 rounded-lg border p-3 text-left transition-colors hover:bg-muted/50 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    <ClipboardList className="size-4 text-muted-foreground" />
                    <div className="flex-1">
                      <p className="text-sm font-medium">{a.title}</p>
                      <p className="text-xs text-muted-foreground">
                        {a.type} &bull; {a.passingScore}/{a.maxScore} pts
                        {alreadyLinked && ' (already linked to another item)'}
                      </p>
                    </div>
                    {a.contentId === assessmentPickerTarget && (
                      <Badge variant="secondary" className="text-xs">Current</Badge>
                    )}
                  </button>
                );
              })
            )}
            {error && <p className="text-sm text-destructive">{error}</p>}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setAssessmentPickerTarget(null)}>Cancel</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
