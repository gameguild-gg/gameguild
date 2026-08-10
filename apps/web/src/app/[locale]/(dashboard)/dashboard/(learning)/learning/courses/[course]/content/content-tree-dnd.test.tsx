import '@testing-library/jest-dom/vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentTree } from './content-tree';
import type { ContentItem } from '@/lib/learning/types';
import { TooltipProvider } from '@game-guild/ui/components/tooltip';
import { addContent, deleteContent, moveContent, reorderContent, updateContent } from '@/lib/learning/actions';

const dndHarness = vi.hoisted(() => ({
  handlers: [] as Array<(event: { active: { id: string }; over: { id: string } | null }) => void>,
  droppables: new Map<string, (el: HTMLElement | null) => void>(),
}));

const navigationMocks = vi.hoisted(() => ({
  refresh: vi.fn(),
  push: vi.fn(),
}));

vi.mock('@dnd-kit/core', () => ({
  DndContext: ({ children, onDragEnd }: { children: unknown; onDragEnd?: (event: { active: { id: string }; over: { id: string } | null }) => void }) => {
    if (onDragEnd) dndHarness.handlers.push(onDragEnd);
    return children;
  },
  PointerSensor: vi.fn(),
  closestCorners: vi.fn(),
  useDroppable: ({ id }: { id: string }) => ({
    setNodeRef: (el: HTMLElement | null) => {
      dndHarness.droppables.set(id, () => el);
    },
    isOver: false,
  }),
  useSensor: vi.fn(() => ({})),
  useSensors: vi.fn(() => []),
}));

vi.mock('@dnd-kit/sortable', () => ({
  SortableContext: ({ children }: { children: unknown }) => children,
  arrayMove: (items: string[], from: number, to: number) => {
    const copy = [...items];
    const [moved] = copy.splice(from, 1);
    copy.splice(to, 0, moved!);
    return copy;
  },
  useSortable: () => ({
    attributes: {},
    listeners: {},
    setNodeRef: vi.fn(),
    transform: null,
    transition: undefined,
    isDragging: false,
  }),
  verticalListSortingStrategy: {},
}));

vi.mock('@dnd-kit/utilities', () => ({
  CSS: {
    Transform: {
      toString: () => undefined,
    },
  },
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/en-US/dashboard/learning/courses/course-1/content',
  useRouter: () => ({
    push: navigationMocks.push,
    refresh: navigationMocks.refresh,
  }),
}));

vi.mock('@/lib/learning/actions', () => ({
  addContent: vi.fn(),
  deleteContent: vi.fn(),
  moveContent: vi.fn(),
  reorderContent: vi.fn(),
  updateContent: vi.fn(),
}));

const moduleOne = {
  id: 'module-1',
  parentId: null,
  order: 0,
  type: 'Lesson',
  title: 'Week 01',
  description: null,
  status: 'published',
  duration: null,
  metadata: {},
  gradingMethod: null,
  maxPoints: null,
  gradingConfig: null,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
} satisfies ContentItem;

const moduleTwo = {
  ...moduleOne,
  id: 'module-2',
  order: 1,
  title: 'Week 02',
} satisfies ContentItem;

const lessonOne = {
  ...moduleOne,
  id: 'lesson-1',
  parentId: 'module-1',
  title: 'Intro lesson',
  duration: 20,
} satisfies ContentItem;

const lessonTwo = {
  ...lessonOne,
  id: 'lesson-2',
  order: 1,
  title: 'Follow-up lesson',
} satisfies ContentItem;

function renderTree() {
  dndHarness.handlers.length = 0;
  dndHarness.droppables.clear();
  return render(
    <TooltipProvider>
      <ContentTree
        courseId="course-1"
        modules={[moduleOne, moduleTwo]}
        allItems={[moduleOne, moduleTwo, lessonOne, lessonTwo]}
      />
    </TooltipProvider>,
  );
}

describe('ContentTree deterministic drag handlers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(addContent).mockResolvedValue({ success: true, data: { id: 'created-content' } });
    vi.mocked(deleteContent).mockResolvedValue({ success: true, data: null });
    vi.mocked(moveContent).mockResolvedValue({ success: true, data: null });
    vi.mocked(reorderContent).mockResolvedValue({ success: true, data: null });
    vi.mocked(updateContent).mockResolvedValue({ success: true, data: null });
  });

  it('covers module drag guards, successful reorder, and server errors', async () => {
    renderTree();
    const dragEnd = dndHarness.handlers[0]!;

    await act(async () => {
      dragEnd({ active: { id: 'module-1' }, over: null });
      dragEnd({ active: { id: 'module-1' }, over: { id: 'module-1' } });
      dragEnd({ active: { id: 'missing' }, over: { id: 'module-2' } });
      dragEnd({ active: { id: 'module-1' }, over: { id: 'missing' } });
    });
    expect(reorderContent).not.toHaveBeenCalled();

    await act(async () => {
      dragEnd({ active: { id: 'module-1' }, over: { id: 'module-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['module-2', 'module-1']);
    });
    expect(navigationMocks.refresh).toHaveBeenCalled();

    vi.clearAllMocks();
    vi.mocked(reorderContent).mockResolvedValueOnce({ success: false, error: 'Cannot reorder modules.' });
    await act(async () => {
      dragEnd({ active: { id: 'module-1' }, over: { id: 'module-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['module-2', 'module-1']);
    });
    expect(navigationMocks.refresh).not.toHaveBeenCalled();
  });

  it('covers lesson drag guards, successful same-module reorder, and server errors', async () => {
    renderTree();
    const dragEnd = dndHarness.handlers[0]!;

    await act(async () => {
      dragEnd({ active: { id: 'lesson-1' }, over: null });
      dragEnd({ active: { id: 'lesson-1' }, over: { id: 'lesson-1' } });
      dragEnd({ active: { id: 'missing' }, over: { id: 'lesson-2' } });
      dragEnd({ active: { id: 'lesson-1' }, over: { id: 'missing' } });
    });
    expect(reorderContent).not.toHaveBeenCalled();
    expect(moveContent).not.toHaveBeenCalled();

    await act(async () => {
      dragEnd({ active: { id: 'lesson-1' }, over: { id: 'lesson-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['lesson-2', 'lesson-1']);
    });
    expect(navigationMocks.refresh).toHaveBeenCalled();

    vi.clearAllMocks();
    vi.mocked(reorderContent).mockResolvedValueOnce({ success: false, error: 'Cannot reorder lessons.' });
    await act(async () => {
      dragEnd({ active: { id: 'lesson-1' }, over: { id: 'lesson-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['lesson-2', 'lesson-1']);
    });
    expect(navigationMocks.refresh).not.toHaveBeenCalled();
  });

  it('excludes virtual modules from module-reorder payloads', async () => {
    const virtualModuleId = 'course-1-unassigned';
    const virtualModule: ContentItem = {
      ...moduleOne,
      id: virtualModuleId,
      title: 'Unassigned',
      order: 2,
    };
    dndHarness.handlers.length = 0;
    dndHarness.droppables.clear();
    render(
      <TooltipProvider>
        <ContentTree
          courseId="course-1"
          modules={[moduleOne, moduleTwo, virtualModule]}
          allItems={[moduleOne, moduleTwo, virtualModule]}
          virtualModuleIds={[virtualModuleId]}
        />
      </TooltipProvider>,
    );
    const dragEnd = dndHarness.handlers[0]!;

    await act(async () => {
      dragEnd({ active: { id: 'module-1' }, over: { id: virtualModuleId } });
    });
    expect(reorderContent).not.toHaveBeenCalled();

    await act(async () => {
      dragEnd({ active: { id: 'module-1' }, over: { id: 'module-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['module-2', 'module-1']);
    });
  });

  it('moves a lesson across modules through the shared drag handler', async () => {
    const virtualModuleId = 'course-1-unassigned';
    const virtualModule: ContentItem = {
      ...moduleOne,
      id: virtualModuleId,
      title: 'Unassigned',
      order: 1,
    };
    const orphanLesson: ContentItem = {
      ...lessonOne,
      id: 'lesson-orphan',
      parentId: virtualModuleId,
      order: 0,
      title: 'Orphan lesson',
    };

    dndHarness.handlers.length = 0;
    dndHarness.droppables.clear();
    render(
      <TooltipProvider>
        <ContentTree
          courseId="course-1"
          modules={[moduleOne, virtualModule]}
          allItems={[moduleOne, virtualModule, orphanLesson]}
          virtualModuleIds={[virtualModuleId]}
        />
      </TooltipProvider>,
    );

    expect(screen.getByText('Week 01')).toBeInTheDocument();
    expect(screen.getByText('Unassigned')).toBeInTheDocument();
    expect(screen.getByText('Orphan lesson')).toBeInTheDocument();

    const dragEnd = dndHarness.handlers[0]!;
    await act(async () => {
      dragEnd({ active: { id: 'lesson-orphan' }, over: { id: 'module-drop-module-1' } });
    });
    await waitFor(() => {
      expect(moveContent).toHaveBeenCalledWith('course-1', 'lesson-orphan', 'module-1', 0);
    });
    expect(navigationMocks.refresh).toHaveBeenCalled();
  });

  it('maps virtual dest modules to a null newParentId when moving a lesson', async () => {
    const virtualModuleId = 'course-1-unassigned';
    const virtualModule: ContentItem = {
      ...moduleOne,
      id: virtualModuleId,
      title: 'Unassigned',
      order: 1,
    };
    const realLesson: ContentItem = {
      ...lessonOne,
      id: 'lesson-real',
      parentId: 'module-1',
      order: 0,
      title: 'Real lesson',
    };

    dndHarness.handlers.length = 0;
    dndHarness.droppables.clear();
    render(
      <TooltipProvider>
        <ContentTree
          courseId="course-1"
          modules={[moduleOne, virtualModule]}
          allItems={[moduleOne, virtualModule, realLesson]}
          virtualModuleIds={[virtualModuleId]}
        />
      </TooltipProvider>,
    );

    const dragEnd = dndHarness.handlers[0]!;
    await act(async () => {
      dragEnd({ active: { id: 'lesson-real' }, over: { id: `module-drop-${virtualModuleId}` } });
    });
    await waitFor(() => {
      expect(moveContent).toHaveBeenCalledWith('course-1', 'lesson-real', null, 0);
    });
  });

  it('surfaces move-content server errors without refreshing', async () => {
    const virtualModuleId = 'course-1-unassigned';
    const virtualModule: ContentItem = {
      ...moduleOne,
      id: virtualModuleId,
      title: 'Unassigned',
      order: 1,
    };
    const orphanLesson: ContentItem = {
      ...lessonOne,
      id: 'lesson-orphan',
      parentId: virtualModuleId,
      order: 0,
      title: 'Orphan lesson',
    };

    dndHarness.handlers.length = 0;
    dndHarness.droppables.clear();
    render(
      <TooltipProvider>
        <ContentTree
          courseId="course-1"
          modules={[moduleOne, virtualModule]}
          allItems={[moduleOne, virtualModule, orphanLesson]}
          virtualModuleIds={[virtualModuleId]}
        />
      </TooltipProvider>,
    );

    vi.mocked(moveContent).mockResolvedValueOnce({ success: false, error: 'Move blocked.' });
    const dragEnd = dndHarness.handlers[0]!;
    await act(async () => {
      dragEnd({ active: { id: 'lesson-orphan' }, over: { id: 'module-drop-module-1' } });
    });
    await waitFor(() => {
      expect(moveContent).toHaveBeenCalledWith('course-1', 'lesson-orphan', 'module-1', 0);
    });
    expect(navigationMocks.refresh).not.toHaveBeenCalled();
  });
});
