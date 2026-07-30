import type { LearningCoursesProgramContent } from '@game-guild/client';

export function flattenUniqueContent(items: LearningCoursesProgramContent[]): LearningCoursesProgramContent[] {
    const flattened: LearningCoursesProgramContent[] = [];
    const seenIds = new Set<string>();

    const visit = (item: LearningCoursesProgramContent) => {
        if (item.id && seenIds.has(item.id)) return;
        if (item.id) seenIds.add(item.id);
        flattened.push(item);
        for (const child of item.children ?? []) visit(child);
    };

    for (const item of items) visit(item);
    return flattened.sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0));
}