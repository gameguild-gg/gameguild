'use server';

import type { AchievementDto } from '@/lib/core/api/generated/types.gen';

export interface AchievementActionResult<T = unknown> {
    success: boolean;
    data?: T;
    error?: string;
}

export async function getAchievementsAction(): Promise<AchievementActionResult<AchievementDto[]>> {
    try {
        // For now, return empty array to avoid API errors
        // This can be updated later when the API endpoint is properly configured
        console.log('Loading achievements...');

        const achievements: AchievementDto[] = [
            {
                id: 'sample-achievement-1',
                name: 'First Steps',
                description: 'Complete your first task in the platform',
                category: 'Getting Started',
                isActive: true,
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                points: 10,
                displayOrder: 1,
                isSecret: false
            },
            {
                id: 'sample-achievement-2',
                name: 'Team Player',
                description: 'Join your first team or project',
                category: 'Social',
                isActive: true,
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                points: 25,
                displayOrder: 2,
                isSecret: false
            },
            {
                id: 'sample-achievement-3',
                name: 'Power User',
                description: 'Use advanced features of the platform',
                category: 'Advanced',
                isActive: false,
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                points: 50,
                displayOrder: 3,
                isSecret: true
            }
        ];

        return {
            success: true,
            data: achievements,
        };
    } catch (error) {
        console.error('Failed to load achievements:', error);
        return {
            success: false,
            error: 'Failed to load achievements',
        };
    }
}

export async function searchAchievementsAction({ query }: { query: { searchTerm: string } }): Promise<AchievementActionResult<AchievementDto[]>> {
    try {
        const result = await getAchievementsAction();

        if (!result.success || !result.data) {
            return result;
        }

        const filteredAchievements = result.data.filter(achievement =>
            achievement.name?.toLowerCase().includes(query.searchTerm.toLowerCase()) ||
            achievement.description?.toLowerCase().includes(query.searchTerm.toLowerCase()) ||
            achievement.category?.toLowerCase().includes(query.searchTerm.toLowerCase())
        );

        return {
            success: true,
            data: filteredAchievements,
        };
    } catch (error) {
        console.error('Failed to search achievements:', error);
        return {
            success: false,
            error: 'Failed to search achievements',
        };
    }
}
