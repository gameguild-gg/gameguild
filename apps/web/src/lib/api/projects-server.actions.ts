'use server';

// STUB: Projects server actions are stubbed because backend endpoints are not available.
// These stubs preserve compilation while mappings to enabled modules are clarified.

import { AccessLevel, ContentStatus } from '@/lib/api/generated/types.gen';

export type ProjectAccessLevel = AccessLevel;
export type ProjectContentStatus = ContentStatus;

export async function getProjects(_params?: any): Promise<any[]> {
    throw new Error('Not implemented (STUB): getProjects');
}

export async function getProjectById(_id: string): Promise<any | null> {
    throw new Error('Not implemented (STUB): getProjectById');
}

export async function getProjectsByUser(_userId: string): Promise<any[]> {
    throw new Error('Not implemented (STUB): getProjectsByUser');
}

export async function createProject(_project: any): Promise<any | null> {
    throw new Error('Not implemented (STUB): createProject');
}

export async function updateProject(_id: string, _updates: any): Promise<any | null> {
    throw new Error('Not implemented (STUB): updateProject');
}

export async function deleteProject(_id: string): Promise<boolean> {
    throw new Error('Not implemented (STUB): deleteProject');
}
