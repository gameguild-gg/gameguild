'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getTestingFeedbackByUserByUserId,
  getTestingRequestsByRequestIdFeedback,
  postTestingFeedback,
  postTestingFeedbackByFeedbackIdQuality,
  postTestingFeedbackByFeedbackIdReport,
  postTestingRequestsByRequestIdFeedback,
} from '@/lib/api/generated/sdk.gen';

import type {
  GetTestingFeedbackByUserByUserIdData,
  GetTestingRequestsByRequestIdFeedbackData,
  PostTestingFeedbackByFeedbackIdQualityData,
  PostTestingFeedbackByFeedbackIdReportData,
  PostTestingFeedbackData,
  PostTestingRequestsByRequestIdFeedbackData,
} from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

/**
 * Get feedback for a testing request
 */
export async function getTestingRequestFeedbackAction(data: GetTestingRequestsByRequestIdFeedbackData) {
  await configureAuthenticatedClient();
  const result = await getTestingRequestsByRequestIdFeedback({
    path: { requestId: data.path.requestId },
    query: data.query,
  });

  revalidateTag('testing-feedback');
  revalidateTag(`testing-request-${data.path.requestId}-feedback`);
  return result;
}

/**
 * Submit feedback for a testing request
 */
export async function submitTestingRequestFeedbackAction(data: PostTestingRequestsByRequestIdFeedbackData) {
  await configureAuthenticatedClient();
  const result = await postTestingRequestsByRequestIdFeedback({
    path: { requestId: data.path.requestId },
    body: data.body,
  });

  revalidateTag('testing-feedback');
  revalidateTag(`testing-request-${data.path.requestId}-feedback`);
  return result;
}

/**
 * Get feedback submitted by a specific user
 */
export async function getUserTestingFeedbackAction(data: GetTestingFeedbackByUserByUserIdData) {
  await configureAuthenticatedClient();
  const result = await getTestingFeedbackByUserByUserId({
    path: { userId: data.path.userId },
    query: data.query,
  });

  revalidateTag('testing-feedback');
  revalidateTag(`user-testing-feedback-${data.path.userId}`);
  return result;
}

/**
 * Submit general testing feedback
 */
export async function submitTestingFeedbackAction(data: PostTestingFeedbackData) {
  await configureAuthenticatedClient();
  const result = await postTestingFeedback({
    body: data.body,
  });

  revalidateTag('testing-feedback');
  return result;
}

/**
 * Report inappropriate feedback
 */
export async function reportTestingFeedbackAction(data: PostTestingFeedbackByFeedbackIdReportData) {
  await configureAuthenticatedClient();
  const result = await postTestingFeedbackByFeedbackIdReport({
    path: { feedbackId: data.path.feedbackId },
    body: data.body,
  });

  revalidateTag('testing-feedback');
  revalidateTag(`testing-feedback-${data.path.feedbackId}`);
  return result;
}

/**
 * Rate the quality of feedback
 */
export async function rateTestingFeedbackQualityAction(data: PostTestingFeedbackByFeedbackIdQualityData) {
  await configureAuthenticatedClient();
  const result = await postTestingFeedbackByFeedbackIdQuality({
    path: { feedbackId: data.path.feedbackId },
    body: data.body,
  });

  revalidateTag('testing-feedback');
  revalidateTag(`testing-feedback-${data.path.feedbackId}`);
  return result;
}
