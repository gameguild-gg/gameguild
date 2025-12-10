/**
 * Stub for content navigation sidebar component.
 * This component is disabled in production.
 */

'use client';

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
    // New props used by course-content-viewer
    courseId?: string;
    modules?: CourseModule[];
    currentContentId?: string;
    onContentSelect?: (contentId: string) => void;
    // Legacy props
    items?: ContentNavigationItem[];
    currentItemId?: string;
    onItemClick?: (item: ContentNavigationItem) => void;
}

export function ContentNavigationSidebar({
    courseId,
    modules,
    currentContentId,
    onContentSelect,
    items,
    currentItemId,
    onItemClick
}: ContentNavigationSidebarProps) {
    const effectiveItems = items || modules?.flatMap(m => m.items || m.contentItems || []) || [];
    const effectiveCurrentId = currentContentId || currentItemId;

    return (
        <div className="w-80 bg-muted/40 border-r p-4 h-full overflow-y-auto">
            <p className="text-sm text-muted-foreground mb-4">Course Navigation</p>
            {courseId && <p className="text-xs text-muted-foreground">Course: {courseId}</p>}
            <div className="space-y-2">
                {effectiveItems.map((item) => (
                    <button
                        key={item.id}
                        onClick={() => {
                            onContentSelect?.(item.id);
                            onItemClick?.(item);
                        }}
                        className={`w-full text-left px-3 py-2 rounded text-sm ${effectiveCurrentId === item.id
                                ? 'bg-primary text-primary-foreground'
                                : 'hover:bg-muted'
                            }`}
                    >
                        {item.title}
                    </button>
                ))}
            </div>
        </div>
    );
}

export default ContentNavigationSidebar;
