'use server';

/**
 * Stub implementations for dashboard refresh actions.
 */

export async function refreshDashboardData() {
  return { success: true, message: 'Dashboard data refreshed' };
}

export async function refreshProjectsData() {
  return { success: true, data: [] };
}

export async function refreshUserStats() {
  return { success: true, data: {} };
}
