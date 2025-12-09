'use server';

/**
 * Stub implementations for testing lab actions.
 * This module is disabled in production.
 */

export async function getTestingSessions() {
    return { data: [], error: null };
}

export async function getTestingSessionById(_id: string) {
    return { data: null, error: null };
}

export async function createTestingSession(_data: any) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function updateTestingSession(_id: string, _data: any) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function deleteTestingSession(_id: string) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function getTestingRequests() {
    return { data: [], error: null };
}

export async function getTestingRequestsData() {
    return { data: [], error: null };
}

export async function getTestingRequestById(_id: string) {
    return { data: null, error: null };
}

export async function createTestingRequest(_data: any) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function updateTestingRequest(_id: string, _data: any) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function joinTestingRequest(_requestId: string, _userId?: string) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function leaveTestingRequest(_requestId: string, _userId?: string) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function submitTestingRequestFeedback(_requestId: string, _data: any) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function getTestingLocations() {
    return { data: [], error: null };
}

export async function getTestingFeedback(_sessionId: string) {
    return { data: [], error: null };
}

export async function submitTestingFeedback(_sessionId: string, _data: any) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

// Additional stub functions for testing lab overview
export async function getMyTestingRequests() {
    return [];
}

export async function getAvailableTestingRequests() {
    return [];
}

export async function createSimpleTestingRequest(_data: any) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function deleteTestingRequest(_id: string) {
    return { success: false, error: 'Testing Lab module is disabled' };
}
