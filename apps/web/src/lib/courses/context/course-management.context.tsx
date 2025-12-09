'use client';

// STUB: Course management context - placeholder for legacy/disabled functionality
// TODO: Implement actual course management when backend module is enabled

import { createContext, ReactNode, useContext } from 'react';

// Stub types for course management
interface Course {
    id: string;
    title: string;
    area: string;
    level: number;
    slug: string;
    image?: string;
    description?: string;
}

interface CoursesSyncState {
    isLoading: boolean;
    error: string | null;
    syncStatus: 'idle' | 'syncing' | 'synced' | 'error';
    lastSync: Date | null;
    lastSyncTime: Date | null;
    pendingChanges: Map<string, any>;
}

interface CoursesSyncContextType {
    state: CoursesSyncState;
    getEnhancedCourses: () => Course[];
    syncWithServer: () => Promise<void>;
}

const initialState: CoursesSyncState = {
    isLoading: false,
    error: null,
    syncStatus: 'idle',
    lastSync: null,
    lastSyncTime: null,
    pendingChanges: new Map(),
};

const CoursesSyncContext = createContext<CoursesSyncContextType | undefined>(undefined);

export function CoursesSyncProvider({ children }: { children: ReactNode }) {
    // Stub implementation
    const value: CoursesSyncContextType = {
        state: initialState,
        getEnhancedCourses: () => {
            // Return empty array as stub
            console.log('useCoursesSync: getEnhancedCourses is a stub - not implemented');
            return [];
        },
        syncWithServer: async () => {
            // Stub - do nothing
            console.log('useCoursesSync: syncWithServer is a stub - not implemented');
        },
    };

    return (
        <CoursesSyncContext.Provider value={value}>
            {children}
        </CoursesSyncContext.Provider>
    );
}

export function useCoursesSync(): CoursesSyncContextType {
    const context = useContext(CoursesSyncContext);

    // If no provider, return a default stub context
    if (!context) {
        console.warn('useCoursesSync: No provider found, using default stub');
        return {
            state: initialState,
            getEnhancedCourses: () => [],
            syncWithServer: async () => { },
        };
    }

    return context;
}
