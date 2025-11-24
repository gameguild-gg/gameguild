'use server';

// Simplified projects API with mock data for now
// TODO: Replace with proper API integration once API types are fixed

export interface Project {
    id: string;
    version?: number;
    name: string;
    description: string;
    longDescription?: string;
    category: string;
    gameVersion?: string;
    status: 'development' | 'beta' | 'released' | 'archived';
    createdAt: string;
    updatedAt: string;
    deletedAt?: string;
    isDeleted: boolean;
    isPublic: boolean;
    rating?: number;
    tags: string[];
    downloadUrl?: string;
    sourceCodeUrl?: string;
    websiteUrl?: string;
    screenshots: string[];
    systemRequirements?: {
        minimum: string;
        recommended: string;
    };
    changelog?: Array<{
        version: string;
        date: string;
        changes: string[];
    }>;
    testingSessions?: Array<{
        id: string;
        title: string;
        status: 'pending' | 'active' | 'completed';
        participantCount: number;
        scheduledDate: string;
    }>;
    ownerId: string;
    ownerUsername?: string;
    ownerName?: string;
}

export interface CreateProjectRequest {
    name: string;
    description: string;
    longDescription?: string;
    category: string;
    gameVersion?: string;
    isPublic?: boolean;
    tags?: string[];
    sourceCodeUrl?: string;
    websiteUrl?: string;
}

export interface UpdateProjectRequest {
    name?: string;
    description?: string;
    longDescription?: string;
    category?: string;
    gameVersion?: string;
    isPublic?: boolean;
    tags?: string[];
    sourceCodeUrl?: string;
    websiteUrl?: string;
    expectedVersion?: number;
}

export interface ProjectSearchParams {
    query?: string;
    category?: string;
    status?: string;
    tags?: string[];
    sortBy?: 'name' | 'createdAt' | 'updatedAt' | 'rating';
    sortDirection?: 'asc' | 'desc';
    page?: number;
    pageSize?: number;
}

// Mock data - replace with actual API calls when API is fixed
const MOCK_PROJECTS: Project[] = [
    {
        id: '1',
        version: 1,
        name: 'Puzzle Adventure Game',
        description: 'A challenging puzzle game with unique mechanics and beautiful art style.',
        longDescription: 'This is an immersive puzzle adventure game that combines traditional puzzle-solving mechanics with modern storytelling. Players embark on a journey through mystical lands, solving intricate puzzles to progress through the story. The game features hand-drawn artwork, original soundtrack, and innovative gameplay mechanics that challenge players to think creatively.',
        category: 'Puzzle',
        gameVersion: '1.2.0',
        status: 'released',
        createdAt: '2024-01-15T00:00:00Z',
        updatedAt: '2024-08-20T00:00:00Z',
        isDeleted: false,
        isPublic: true,
        rating: 4.5,
        tags: ['puzzle', 'adventure', 'indie', 'single-player'],
        downloadUrl: 'https://example.com/download',
        sourceCodeUrl: 'https://github.com/user/puzzle-game',
        websiteUrl: 'https://puzzlegame.example.com',
        screenshots: [
            '/placeholder-screenshot1.jpg',
            '/placeholder-screenshot2.jpg',
            '/placeholder-screenshot3.jpg'
        ],
        systemRequirements: {
            minimum: 'Windows 10, 4GB RAM, DirectX 11',
            recommended: 'Windows 11, 8GB RAM, DirectX 12'
        },
        changelog: [
            {
                version: '1.2.0',
                date: '2024-08-20T00:00:00Z',
                changes: ['Added new puzzle mechanics', 'Fixed performance issues', 'Updated UI design']
            },
            {
                version: '1.1.0',
                date: '2024-06-15T00:00:00Z',
                changes: ['New levels added', 'Bug fixes', 'Improved sound effects']
            },
            {
                version: '1.0.0',
                date: '2024-01-15T00:00:00Z',
                changes: ['Initial release', 'Core gameplay mechanics', 'Basic UI implementation']
            }
        ],
        testingSessions: [
            {
                id: 'ts1',
                title: 'Beta Testing Round 2',
                status: 'completed',
                participantCount: 15,
                scheduledDate: '2024-08-15T14:00:00Z'
            },
            {
                id: 'ts2',
                title: 'UI/UX Feedback Session',
                status: 'completed',
                participantCount: 8,
                scheduledDate: '2024-08-10T16:00:00Z'
            }
        ],
        ownerId: 'user1',
        ownerUsername: 'super-admin',
        ownerName: 'Super Admin'
    }
];

export async function getProjects(params?: ProjectSearchParams): Promise<Project[]> {
    // TODO: Implement real API call
    // For now, return mock data
    return MOCK_PROJECTS;
}

export async function getProjectById(id: string): Promise<Project | null> {
    // TODO: Implement real API call
    // For now, return mock data
    const project = MOCK_PROJECTS.find(p => p.id === id);
    return project || null;
}

export async function createProject(project: CreateProjectRequest): Promise<Project | null> {
    // TODO: Implement real API call
    // For now, return mock data
    const newProject: Project = {
        id: Math.random().toString(36).substr(2, 9),
        version: 1,
        name: project.name,
        description: project.description,
        longDescription: project.longDescription,
        category: project.category,
        gameVersion: project.gameVersion,
        status: 'development',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
        isPublic: project.isPublic ?? true,
        rating: 0,
        tags: project.tags || [],
        sourceCodeUrl: project.sourceCodeUrl,
        websiteUrl: project.websiteUrl,
        screenshots: [],
        systemRequirements: {
            minimum: 'Not specified',
            recommended: 'Not specified'
        },
        changelog: [],
        testingSessions: [],
        ownerId: 'current-user',
        ownerUsername: 'current-user',
        ownerName: 'Current User'
    };

    return newProject;
}

export async function updateProject(id: string, updates: UpdateProjectRequest): Promise<Project | null> {
    // TODO: Implement real API call
    // For now, return updated mock data
    const project = MOCK_PROJECTS.find(p => p.id === id);
    if (!project) return null;

    const updatedProject: Project = {
        ...project,
        ...updates,
        tags: updates.tags || project.tags,
        updatedAt: new Date().toISOString()
    };

    return updatedProject;
}

export async function deleteProject(id: string): Promise<boolean> {
    // TODO: Implement real API call
    return true;
}

export async function searchProjects(params: ProjectSearchParams): Promise<Project[]> {
    // TODO: Implement real API call
    // For now, return filtered mock data
    return MOCK_PROJECTS.filter(project =>
        !params.query ||
        project.name.toLowerCase().includes(params.query.toLowerCase()) ||
        project.description.toLowerCase().includes(params.query.toLowerCase())
    );
}

export async function getPopularProjects(limit: number = 10): Promise<Project[]> {
    // TODO: Implement real API call
    return MOCK_PROJECTS.slice(0, limit);
}

export async function getRecentProjects(limit: number = 10): Promise<Project[]> {
    // TODO: Implement real API call
    return MOCK_PROJECTS.slice(0, limit);
}

export async function getFeaturedProjects(limit: number = 10): Promise<Project[]> {
    // TODO: Implement real API call
    return MOCK_PROJECTS.slice(0, limit);
}

export async function getProjectStatistics(id: string): Promise<any> {
    // TODO: Implement real API call
    return {
        views: 1234,
        downloads: 567,
        followers: 89,
        stars: 42
    };
}

export async function publishProject(id: string): Promise<Project | null> {
    // TODO: Implement real API call
    const project = MOCK_PROJECTS.find(p => p.id === id);
    if (!project) return null;

    return {
        ...project,
        status: 'released',
        updatedAt: new Date().toISOString()
    };
}

export async function unpublishProject(id: string): Promise<Project | null> {
    // TODO: Implement real API call
    const project = MOCK_PROJECTS.find(p => p.id === id);
    if (!project) return null;

    return {
        ...project,
        status: 'development',
        updatedAt: new Date().toISOString()
    };
}

export async function archiveProject(id: string): Promise<Project | null> {
    // TODO: Implement real API call
    const project = MOCK_PROJECTS.find(p => p.id === id);
    if (!project) return null;

    return {
        ...project,
        status: 'archived',
        updatedAt: new Date().toISOString()
    };
}
