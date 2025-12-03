'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getContentInteractionUserByProgramUserId,
  getContentInteractionUserByProgramUserIdContentByContentId,
  postContentInteractionByInteractionIdComplete,
  postContentInteractionByInteractionIdSubmit,
  postContentInteractionStart,
  putContentInteractionByInteractionIdProgress,
  putContentInteractionByInteractionIdTimeSpent,
} from '@/lib/api/generated';
import type {
  GetContentInteractionUserByProgramUserIdContentByContentIdData,
  GetContentInteractionUserByProgramUserIdData,
  PostContentInteractionByInteractionIdCompleteData,
  PostContentInteractionByInteractionIdSubmitData,
  PostContentInteractionStartData,
  PutContentInteractionByInteractionIdProgressData,
  PutContentInteractionByInteractionIdTimeSpentData,
} from '@/lib/api/generated';
import { revalidateTag } from 'next/cache';

export async function startContentInteraction(data?: PostContentInteractionStartData) {
  await configureAuthenticatedClient();
  const result = await postContentInteractionStart({
    body: data?.body,
  });
  revalidateTag('content-interactions');
  return result;
}

export async function updateContentInteractionProgress(data: PutContentInteractionByInteractionIdProgressData) {
  await configureAuthenticatedClient();
  const result = await putContentInteractionByInteractionIdProgress({
    path: data.path,
    body: data.body,
  });
  revalidateTag('content-interactions');
  return result;
}

export async function submitContentInteraction(data: PostContentInteractionByInteractionIdSubmitData) {
  await configureAuthenticatedClient();
  const result = await postContentInteractionByInteractionIdSubmit({
    path: data.path,
    body: data.body,
  });
  revalidateTag('content-interactions');
  return result;
}

export async function completeContentInteraction(data: PostContentInteractionByInteractionIdCompleteData) {
  await configureAuthenticatedClient();
  const result = await postContentInteractionByInteractionIdComplete({
    path: data.path,
    body: data.body,
  });
  revalidateTag('content-interactions');
  return result;
}

export async function updateContentInteractionTimeSpent(data: PutContentInteractionByInteractionIdTimeSpentData) {
  await configureAuthenticatedClient();
  const result = await putContentInteractionByInteractionIdTimeSpent({
    path: data.path,
    body: data.body,
  });
  revalidateTag('content-interactions');
  return result;
}

export async function getContentInteractionByUserAndContent(data: GetContentInteractionUserByProgramUserIdContentByContentIdData) {
  await configureAuthenticatedClient();
  return getContentInteractionUserByProgramUserIdContentByContentId({
    path: data.path,
  });
}

export async function getContentInteractionsByUser(data: GetContentInteractionUserByProgramUserIdData) {
  await configureAuthenticatedClient();
  return getContentInteractionUserByProgramUserId({
    path: data.path,
  });
}
