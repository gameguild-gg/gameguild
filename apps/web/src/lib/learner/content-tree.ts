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

// Private content is author-only: hide the item and its entire subtree from learners.
export function collectHiddenContentIds(
    items: LearningCoursesProgramContent[],
    hidden: Set<string> = new Set<string>(),
    parentHidden = false,
): Set<string> {
    for (const item of items) {
        const itemHidden = parentHidden || item.visibility === 'Private';
        if (itemHidden && item.id) hidden.add(item.id);
        if (item.children?.length) {
            collectHiddenContentIds(item.children, hidden, itemHidden);
        }
    }
    return hidden;
}