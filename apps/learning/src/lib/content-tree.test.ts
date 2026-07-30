import { describe, expect, it } from 'vitest';
import { flattenUniqueContent } from './content-tree';

describe('flattenUniqueContent', () => {
    it('deduplicates content returned both as nested children and flat rows', () => {
        const child = { id: 'reflection-1', parentId: 'module-1', title: 'Production reflection', sortOrder: 2 };
        const result = flattenUniqueContent([
            { id: 'module-1', title: 'Production', sortOrder: 1, children: [child] },
            child,
        ]);

        expect(result.map((item) => item.id)).toEqual(['module-1', 'reflection-1']);
    });

    it('keeps anonymous content because it has no stable identity to deduplicate', () => {
        const anonymous = { title: 'Draft note', sortOrder: 1 };
        expect(flattenUniqueContent([anonymous, anonymous])).toHaveLength(2);
    });
});