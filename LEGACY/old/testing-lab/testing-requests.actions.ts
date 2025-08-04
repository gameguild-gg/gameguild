'use server';

import { revalidatePath } from 'next/cache';

export interface TestingRequest {
  id: string;
  title: string;
  description: string;
  gameTitle: string;
  gameVersion: string;
  submittedBy: {
    id: string;
    name: string;
    email: string;
  };
  status: 'pending' | 'approved' | 'rejected' | 'in_testing';
  submittedAt: string;
  reviewedAt?: string;
  reviewedBy?: {
    id: string;
    name: string;
    email: string;
  };
  downloadUrl?: string;
  instructions?: string;
  testingPeriod?: {
    startDate: string;
    endDate: string;
  };
  priority: 'low' | 'medium' | 'high';
  tags: string[];
}

export interface TestingRequestsData {
  requests: TestingRequest[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

// Mock data for development
const mockTestingRequests: TestingRequest[] = [
  {
    id: '1',
    title: 'Space Adventure Alpha Build',
    description: 'First alpha build of our space exploration game. Need feedback on core mechanics and controls.',
    gameTitle: 'Space Adventure',
    gameVersion: '0.1.0-alpha',
    submittedBy: {
      id: 'dev1',
      name: 'John Doe',
      email: 'john.doe@mymail.champlain.edu',
    },
    status: 'pending',
    submittedAt: '2024-12-10T10:30:00Z',
    priority: 'high',
    tags: ['alpha', 'space', 'adventure'],
  },
  {
    id: '2',
    title: 'Puzzle Quest Beta Release',
    description: 'Beta version of our puzzle game. Focus on difficulty progression and user interface.',
    gameTitle: 'Puzzle Quest',
    gameVersion: '0.8.0-beta',
    submittedBy: {
      id: 'dev2',
      name: 'Jane Smith',
      email: 'jane.smith@mymail.champlain.edu',
    },
    status: 'approved',
    submittedAt: '2024-12-08T14:15:00Z',
    reviewedAt: '2024-12-09T09:00:00Z',
    reviewedBy: {
      id: 'prof1',
      name: 'Prof. Wilson',
      email: 'wilson@champlain.edu',
    },
    downloadUrl: 'https://example.com/puzzle-quest-beta.zip',
    instructions: 'Play through levels 1-10, focus on difficulty curve',
    testingPeriod: {
      startDate: '2024-12-15T00:00:00Z',
      endDate: '2024-12-20T23:59:59Z',
    },
    priority: 'medium',
    tags: ['beta', 'puzzle', 'ui'],
  },
  {
    id: '3',
    title: 'Racing Game Prototype',
    description: 'Early prototype of our racing game. Looking for feedback on physics and car handling.',
    gameTitle: 'Speed Racers',
    gameVersion: '0.0.3-prototype',
    submittedBy: {
      id: 'dev3',
      name: 'Mike Johnson',
      email: 'mike.johnson@mymail.champlain.edu',
    },
    status: 'rejected',
    submittedAt: '2024-12-05T16:45:00Z',
    reviewedAt: '2024-12-06T11:30:00Z',
    reviewedBy: {
      id: 'prof1',
      name: 'Prof. Wilson',
      email: 'wilson@champlain.edu',
    },
    priority: 'low',
    tags: ['prototype', 'racing', 'physics'],
  },
  {
    id: '4',
    title: 'RPG Combat System Test',
    description: 'Isolated combat system for our RPG. Need testing on balance and mechanics.',
    gameTitle: 'Fantasy Quest',
    gameVersion: '0.5.2',
    submittedBy: {
      id: 'dev4',
      name: 'Sarah Connor',
      email: 'sarah.connor@mymail.champlain.edu',
    },
    status: 'in_testing',
    submittedAt: '2024-12-07T12:00:00Z',
    reviewedAt: '2024-12-08T10:00:00Z',
    reviewedBy: {
      id: 'prof2',
      name: 'Prof. Anderson',
      email: 'anderson@champlain.edu',
    },
    downloadUrl: 'https://example.com/rpg-combat-test.zip',
    instructions: 'Test combat scenarios, focus on balance and responsiveness',
    testingPeriod: {
      startDate: '2024-12-12T00:00:00Z',
      endDate: '2024-12-18T23:59:59Z',
    },
    priority: 'high',
    tags: ['rpg', 'combat', 'balance'],
  },
];

export async function getTestingRequestsData(page: number = 1, limit: number = 20, search: string = '', status: string = ''): Promise<TestingRequestsData> {
  // Simulate API delay
  await new Promise((resolve) => setTimeout(resolve, 300));

  let filteredRequests = [...mockTestingRequests];

  // Apply search filter
  if (search) {
    const searchLower = search.toLowerCase();
    filteredRequests = filteredRequests.filter(
      (request) =>
        request.title.toLowerCase().includes(searchLower) ||
        request.gameTitle.toLowerCase().includes(searchLower) ||
        request.submittedBy.name.toLowerCase().includes(searchLower) ||
        request.tags.some((tag) => tag.toLowerCase().includes(searchLower)),
    );
  }

  // Apply status filter
  if (status && status !== 'all') {
    filteredRequests = filteredRequests.filter((request) => request.status === status);
  }

  // Calculate pagination
  const total = filteredRequests.length;
  const totalPages = Math.ceil(total / limit);
  const startIndex = (page - 1) * limit;
  const endIndex = startIndex + limit;
  const paginatedRequests = filteredRequests.slice(startIndex, endIndex);

  return {
    requests: paginatedRequests,
    pagination: {
      page,
      limit,
      total,
      totalPages,
    },
  };
}

export async function revalidateTestingRequestsDataAction() {
  revalidatePath('/dashboard/testing-lab/requests');
}
