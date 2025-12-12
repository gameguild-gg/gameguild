'use server';
import { revalidateTag } from 'next/cache';

/**
 * Get feedback for a testing request
 */
export async function getTestingRequestFeedbackAction(_data: any) {
  // STUB: endpoint disabled
  revalidateTag('testing-feedback');
  return { data: [], error: { message: 'Not implemented (STUB)' }, response: { status: 501 } } as any;
}

/**
 * Submit feedback for a testing request
 */
export async function submitTestingRequestFeedbackAction(_data: any) {
  // STUB
  revalidateTag('testing-feedback');
  return { data: null, error: { message: 'Not implemented (STUB)' }, response: { status: 501 } } as any;
}

/**
 * Get feedback submitted by a specific user
 */
export async function getUserTestingFeedbackAction(_data: any) {
  // STUB
  revalidateTag('testing-feedback');
  return { data: [], error: { message: 'Not implemented (STUB)' }, response: { status: 501 } } as any;
}

/**
 * Submit general testing feedback
 */
export async function submitTestingFeedbackAction(_data: any) {
  // STUB
  revalidateTag('testing-feedback');
  return { data: null, error: { message: 'Not implemented (STUB)' }, response: { status: 501 } } as any;
}

/**
 * Report inappropriate feedback
 */
export async function reportTestingFeedbackAction(_data: any) {
  // STUB
  revalidateTag('testing-feedback');
  return { data: null, error: { message: 'Not implemented (STUB)' }, response: { status: 501 } } as any;
}

/**
 * Rate the quality of feedback
 */
export async function rateTestingFeedbackQualityAction(_data: any) {
  // STUB
  revalidateTag('testing-feedback');
  return { data: null, error: { message: 'Not implemented (STUB)' }, response: { status: 501 } } as any;
}
