import '@testing-library/jest-dom/vitest';
import { act, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ContentTree } from './content-tree';
import type { ContentItem } from '@/lib/learning/types';
import { TooltipProvider } from '@game-guild/ui/components/tooltip';
import { addContent, deleteContent, reorderContent, updateContent } from '@/lib/learning/actions';

const dndHarness = vi.hoisted(() => ({
  handlers: [] as Array<(event: { active: { id: string }; over: { id: string } | null }) => void>,
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
  closestCenter: vi.fn(),
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
    vi.mocked(reorderContent).mockResolvedValue({ success: true, data: null });
    vi.mocked(updateContent).mockResolvedValue({ success: true, data: null });
  });

  it('covers module drag guards, successful reorder, and server errors', async () => {
    renderTree();
    const moduleDragEnd = dndHarness.handlers[0]!;

    await act(async () => {
      moduleDragEnd({ active: { id: 'module-1' }, over: null });
      moduleDragEnd({ active: { id: 'module-1' }, over: { id: 'module-1' } });
      moduleDragEnd({ active: { id: 'missing' }, over: { id: 'module-2' } });
      moduleDragEnd({ active: { id: 'module-1' }, over: { id: 'missing' } });
    });
    expect(reorderContent).not.toHaveBeenCalled();

    await act(async () => {
      moduleDragEnd({ active: { id: 'module-1' }, over: { id: 'module-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['module-2', 'module-1']);
    });
    expect(navigationMocks.refresh).toHaveBeenCalled();

    vi.clearAllMocks();
    vi.mocked(reorderContent).mockResolvedValueOnce({ success: false, error: 'Cannot reorder modules.' });
    await act(async () => {
      moduleDragEnd({ active: { id: 'module-1' }, over: { id: 'module-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['module-2', 'module-1']);
    });
    expect(navigationMocks.refresh).not.toHaveBeenCalled();
  });

  it('covers lesson drag guards, successful reorder, and server errors', async () => {
    renderTree();
    const lessonDragEnd = dndHarness.handlers[1]!;

    await act(async () => {
      lessonDragEnd({ active: { id: 'lesson-1' }, over: null });
      lessonDragEnd({ active: { id: 'lesson-1' }, over: { id: 'lesson-1' } });
      lessonDragEnd({ active: { id: 'missing' }, over: { id: 'lesson-2' } });
      lessonDragEnd({ active: { id: 'lesson-1' }, over: { id: 'missing' } });
    });
    expect(reorderContent).not.toHaveBeenCalled();

    await act(async () => {
      lessonDragEnd({ active: { id: 'lesson-1' }, over: { id: 'lesson-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['lesson-2', 'lesson-1']);
    });
    expect(navigationMocks.refresh).toHaveBeenCalled();

    vi.clearAllMocks();
    vi.mocked(reorderContent).mockResolvedValueOnce({ success: false, error: 'Cannot reorder lessons.' });
    await act(async () => {
      lessonDragEnd({ active: { id: 'lesson-1' }, over: { id: 'lesson-2' } });
    });
    await waitFor(() => {
      expect(reorderContent).toHaveBeenCalledWith('course-1', ['lesson-2', 'lesson-1']);
    });
    expect(navigationMocks.refresh).not.toHaveBeenCalled();
  });
});
