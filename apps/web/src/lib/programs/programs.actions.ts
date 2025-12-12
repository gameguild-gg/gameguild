'use server';

/**
 * Stub implementations for programs actions.
 * This module is disabled in production.
 */

// Type export for Program
export type Program = any;

export async function getPrograms() {
    return { data: [], error: null };
}

export async function getProgramById(_id: string) {
    return { data: null, error: null };
}

export async function createProgram(_data: any) {
    return { success: false, error: 'Programs module is disabled' };
}

export async function updateProgram(_id: string, _data: any) {
    return { success: false, error: 'Programs module is disabled' };
}

export async function deleteProgram(_id: string) {
    return { success: false, error: 'Programs module is disabled' };
}

export async function getProgramContents(_programId: string) {
    return { data: [], error: null };
}
