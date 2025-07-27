'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getTestingRequestsByRequestIdFeedback,
  postTestingRequestsByRequestIdFeedback,
  getTestingFeedbackByUserByUserId,
  postTestingFeedback,
  postTestingFeedbackByFeedbackIdReport,
  postTestingFeedbackByFeedbackIdQuality,
} from '@/lib/api/generated/sdk.gen';

import type {
  GetTestingRequestsByRequestIdFeedbackData,
  PostTestingRequestsByRequestIdFeedbackData,
  GetTestingFeedbackByUserByUserIdData,
  PostTestingFeedbackData,
  PostTestingFeedbackByFeedbackIdReportData,
  PostTestingFeedbackByFeedbackIdQualityData,
} from '@/lib/api/generated/types.gen';

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
