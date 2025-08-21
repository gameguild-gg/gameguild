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
    const { isSidebarOpen } = useSidebar();

    return (
        <div className="flex min-h-screen relative">
            {/* Sidebar */}
            <CourseContentSidebar
                courseSlug={courseSlug}
                courseTitle={courseTitle}
                content={content}
            />
            
            {/* Main Content Area */}
            <main className={cn(
                "flex-1 transition-all duration-300 ease-in-out overflow-hidden",
                // Large screens: adjust margin based on sidebar state
                "lg:ml-0",
                isSidebarOpen && "lg:ml-80",
                // Ensure content uses full width
                "w-full"
            )}>
                {/* Sidebar Toggle Button */}
                <SidebarToggle />
                
                {/* Content Container */}
                <div className="h-full overflow-auto">
                    {children}
                </div>
            </main>
        </div>
    );
}
