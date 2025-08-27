'use client';

import { ProgramContent } from '@/lib/api/generated/types.gen';
import { CourseContentSidebar } from './course-content-sidebar';
import { useSidebar } from './sidebar-context';
import { SidebarToggle } from './sidebar-toggle';
import { cn } from '@/lib/utils';

interface CourseContentLayoutClientProps {
    courseSlug: string;
    courseTitle?: string;
    content: ProgramContent[];
    children: React.ReactNode;
}

export function CourseContentLayoutClient({ courseSlug, courseTitle, content, children }: CourseContentLayoutClientProps) {
    const { isSidebarOpen, mounted } = useSidebar();

    return (
        <div className="flex min-h-screen relative overflow-hidden">
            {/* Sidebar */}
            <CourseContentSidebar
                courseSlug={courseSlug}
                courseTitle={courseTitle}
                content={content}
            />
            
            {/* Main Content Area */}
            <main className={cn(
                "flex-1 transition-all duration-300 ease-in-out",
                // Large screens: adjust margin based on sidebar state
                "lg:ml-0",
                // Only apply conditional styling after hydration
                mounted && isSidebarOpen && "lg:ml-80",
                // Ensure no horizontal overflow
                "min-w-0"
            )}>
                {/* Sidebar Toggle Button */}
                <SidebarToggle />
                
                {/* Content Container */}
                <div className="h-full overflow-auto min-w-0">
                    {children}
                </div>
            </main>
        </div>
    );
}
