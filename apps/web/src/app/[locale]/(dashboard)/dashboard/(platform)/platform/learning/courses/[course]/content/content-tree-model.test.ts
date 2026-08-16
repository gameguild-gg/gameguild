import { describe, expect, it } from 'vitest';
import type { ContentItem } from '@/lib/learning/types';
import { buildContentTreeModel } from './content-tree-model';

function item(overrides: Partial<ContentItem>): ContentItem {
  return {
    id: 'content-1',
    parentId: null,
    order: 0,
    type: 'Lesson',
    title: 'Content',
    description: null,
    status: 'draft',
    duration: null,
    metadata: {},
    gradingConfig: null,
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
    ...overrides,
  };
}

describe('buildContentTreeModel', () => {
  it('recognizes an empty explicit module before it has lessons', () => {
    const module = item({ id: 'module-1', type: 'Module', title: 'Production Foundations' });

    const model = buildContentTreeModel('course-1', [module], {
      title: 'Course',
      description: 'Course description',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
    });

    expect(model.modules).toEqual([module]);
    expect(model.treeItems).toEqual([module]);
    expect(model.virtualModuleIds).toEqual([]);
    expect(model.hasModules).toBe(true);
  });

  it('keeps legacy flat lessons inside a non-persisted compatibility module', () => {
    const lesson = item({ id: 'lesson-1', title: 'Imported lesson' });

    const model = buildContentTreeModel('course-1', [lesson], {
      title: 'Course',
      description: 'Imported course content',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
    });

    expect(model.hasModules).toBe(false);
    expect(model.virtualModuleIds).toEqual(['course-1-content']);
    expect(model.treeItems[0]?.parentId).toBe('course-1-content');
  });

  it('recognizes legacy top-level lesson containers that already have children', () => {
    const legacyModule = item({ id: 'module-1', title: 'Week 01' });
    const lesson = item({ id: 'lesson-1', parentId: 'module-1', title: 'Introduction' });

    const model = buildContentTreeModel('course-1', [legacyModule, lesson], {
      title: 'Course',
      description: '',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
    });

    expect(model.hasModules).toBe(true);
    expect(model.modules).toEqual([legacyModule]);
    expect(model.virtualModuleIds).toEqual([]);
  });
});
