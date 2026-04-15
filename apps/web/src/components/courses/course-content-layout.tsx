'use client';

import { ProgramContent } from '@/lib/api/generated/types.gen';
import { cn } from '@/lib/utils';
import { CourseContentSidebar } from './course-content-sidebar';
import { useSidebar } from './sidebar-context';
import { SidebarToggle } from './sidebar-toggle';

interface CourseContentLayoutClientProps {
    courseSlug: string;
    courseTitle?: string;
    content: ProgramContent[];
    children: React.ReactNode;
}

export function CourseContentLayoutClient({ courseSlug, courseTitle, content, children }: CourseContentLayoutClientProps) {
    const { isSidebarOpen, mounted } = useSidebar();

    return (
        <div className="flex h-screen relative overflow-hidden">
            {/* Sidebar */}
            <CourseContentSidebar
                courseSlug={courseSlug}
                courseTitle={courseTitle}
                content={content}
            />

            {/* Main Content Area */}
            <main className={cn(
                "flex-1 flex flex-col transition-all duration-300 ease-in-out",
                // Start with desktop layout (sidebar open) to prevent layout shift
                "lg:ml-80",
                // Only hide margin after mount if sidebar is closed
                mounted && !isSidebarOpen && "lg:ml-0",
                // Ensure no horizontal overflow
                "min-w-0"
            )}>
                {/* Sidebar Toggle Button */}
                <SidebarToggle />

                {/* Content Container */}
                <div className="flex-1 flex flex-col overflow-auto min-w-0">
                    {children}
                </div>
            </main>
        </div>
    );
}
