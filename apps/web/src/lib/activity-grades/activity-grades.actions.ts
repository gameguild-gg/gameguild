'use server';

import { revalidateTag } from 'next/cache';
import {
  postApiProgramsByProgramIdActivityGrades,
  getApiProgramsByProgramIdActivityGradesInteractionByContentInteractionId,
  getApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserId,
  getApiProgramsByProgramIdActivityGradesStudentByProgramUserId,
  deleteApiProgramsByProgramIdActivityGradesByGradeId,
  putApiProgramsByProgramIdActivityGradesByGradeId,
  getApiProgramsByProgramIdActivityGradesPending,
  getApiProgramsByProgramIdActivityGradesStatistics,
  getApiProgramsByProgramIdActivityGradesContentByContentId,
} from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type {
  PostApiProgramsByProgramIdActivityGradesData,
  GetApiProgramsByProgramIdActivityGradesInteractionByContentInteractionIdData,
  GetApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserIdData,
  GetApiProgramsByProgramIdActivityGradesStudentByProgramUserIdData,
  DeleteApiProgramsByProgramIdActivityGradesByGradeIdData,
  PutApiProgramsByProgramIdActivityGradesByGradeIdData,
  GetApiProgramsByProgramIdActivityGradesPendingData,
  GetApiProgramsByProgramIdActivityGradesStatisticsData,
  GetApiProgramsByProgramIdActivityGradesContentByContentIdData,
} from '@/lib/api/generated/types.gen';

/**
 * Create a new activity grade for a program
 */
export async function createActivityGrade(data: PostApiProgramsByProgramIdActivityGradesData) {
  await configureAuthenticatedClient();
  const result = await postApiProgramsByProgramIdActivityGrades({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate activity grades cache
  revalidateTag('activity-grades');
  revalidateTag(`program-${data.path.programId}-grades`);
  
  return result;
}

/**
 * Get activity grades by content interaction ID
 */
export async function getActivityGradesByInteraction(data: GetApiProgramsByProgramIdActivityGradesInteractionByContentInteractionIdData) {
  await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesInteractionByContentInteractionId({
    path: data.path,
  });
}

/**
 * Get activity grades by grader program user ID
 */
export async function getActivityGradesByGrader(data: GetApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserIdData) {
  await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserId({
    path: data.path,
  });
}

/**
 * Get activity grades by student program user ID
 */
export async function getActivityGradesByStudent(data: GetApiProgramsByProgramIdActivityGradesStudentByProgramUserIdData) {
  await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesStudentByProgramUserId({
    path: data.path,
  });
}

/**
 * Delete an activity grade by ID
 */
export async function deleteActivityGrade(data: DeleteApiProgramsByProgramIdActivityGradesByGradeIdData) {
  await configureAuthenticatedClient();
  const result = await deleteApiProgramsByProgramIdActivityGradesByGradeId({
    path: data.path,
  });
  
  // Revalidate activity grades cache
  revalidateTag('activity-grades');
  revalidateTag(`program-${data.path.programId}-grades`);
  
  return result;
}

/**
 * Update an activity grade by ID
 */
export async function updateActivityGrade(data: PutApiProgramsByProgramIdActivityGradesByGradeIdData) {
  await configureAuthenticatedClient();
  const result = await putApiProgramsByProgramIdActivityGradesByGradeId({
    path: data.path,
    body: data.body,
  });
  
  // Revalidate activity grades cache
  revalidateTag('activity-grades');
  revalidateTag(`program-${data.path.programId}-grades`);
  
  return result;
}

/**
 * Get pending activity grades for a program
 */
export async function getPendingActivityGrades(data: GetApiProgramsByProgramIdActivityGradesPendingData) {
  await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesPending({
    path: data.path,
  });
}

/**
 * Get activity grades statistics for a program
 */
export async function getActivityGradesStatistics(data: GetApiProgramsByProgramIdActivityGradesStatisticsData) {
  await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesStatistics({
    path: data.path,
  });
}

/**
 * Get activity grades by content ID for a program
 */
export async function getActivityGradesByContent(data: GetApiProgramsByProgramIdActivityGradesContentByContentIdData) {
  await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesContentByContentId({
    path: data.path,
  });
}
