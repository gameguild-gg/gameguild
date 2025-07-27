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
import { configureAuthenticatedClient } from '@/lib/auth/utils';
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
  const client = await configureAuthenticatedClient();
  const result = await postApiProgramsByProgramIdActivityGrades({
    client,
    ...data,
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
  const client = await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesInteractionByContentInteractionId({
    client,
    ...data,
  });
}

/**
 * Get activity grades by grader program user ID
 */
export async function getActivityGradesByGrader(data: GetApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserIdData) {
  const client = await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserId({
    client,
    ...data,
  });
}

/**
 * Get activity grades by student program user ID
 */
export async function getActivityGradesByStudent(data: GetApiProgramsByProgramIdActivityGradesStudentByProgramUserIdData) {
  const client = await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesStudentByProgramUserId({
    client,
    ...data,
  });
}

/**
 * Delete an activity grade by ID
 */
export async function deleteActivityGrade(data: DeleteApiProgramsByProgramIdActivityGradesByGradeIdData) {
  const client = await configureAuthenticatedClient();
  const result = await deleteApiProgramsByProgramIdActivityGradesByGradeId({
    client,
    ...data,
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
  const client = await configureAuthenticatedClient();
  const result = await putApiProgramsByProgramIdActivityGradesByGradeId({
    client,
    ...data,
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
  const client = await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesPending({
    client,
    ...data,
  });
}

/**
 * Get activity grades statistics for a program
 */
export async function getActivityGradesStatistics(data: GetApiProgramsByProgramIdActivityGradesStatisticsData) {
  const client = await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesStatistics({
    client,
    ...data,
  });
}

/**
 * Get activity grades by content ID for a program
 */
export async function getActivityGradesByContent(data: GetApiProgramsByProgramIdActivityGradesContentByContentIdData) {
  const client = await configureAuthenticatedClient();
  return getApiProgramsByProgramIdActivityGradesContentByContentId({
    client,
    ...data,
  });
}
