import type { ContentItem } from '@/lib/learning/types';

interface CourseContentIdentity {
  title: string;
  description: string;
  createdAt: string;
  updatedAt: string;
}

export interface ContentTreeModel {
  hasModules: boolean;
  modules: ContentItem[];
  treeItems: ContentItem[];
  virtualModuleIds: string[];
}

export function buildContentTreeModel(
  courseId: string,
  items: ContentItem[],
  course: CourseContentIdentity,
): ContentTreeModel {
  const topLevelItems = items.filter((item) => !item.parentId).sort((left, right) => left.order - right.order);
  const hasModules = items.some((item) => item.type === 'Module' || Boolean(item.parentId));

  if (hasModules) {
    return {
      hasModules: true,
      modules: topLevelItems,
      treeItems: items,
      virtualModuleIds: [],
    };
  }

  const legacyFlatModuleId = `${courseId}-content`;
  const compatibilityModule: ContentItem = {
    id: legacyFlatModuleId,
    parentId: null,
    order: 0,
    type: 'Module',
    title: 'Course Content',
    description: course.description || 'Imported course content',
    status: 'published',
    duration: null,
    metadata: {},
    gradingMethod: null,
    maxPoints: null,
    gradingConfig: null,
    createdAt: course.createdAt,
    updatedAt: course.updatedAt,
  };

  return {
    hasModules: false,
    modules: [compatibilityModule],
    treeItems: topLevelItems.map((item) => ({ ...item, parentId: legacyFlatModuleId })),
    virtualModuleIds: [legacyFlatModuleId],
  };
}
