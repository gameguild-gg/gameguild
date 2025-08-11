'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteCredentialsById,
  deleteCredentialsByIdHard,
  getCredentials,
  getCredentialsById,
  getCredentialsDeleted,
  getCredentialsUserByUserId,
  getCredentialsUserByUserIdTypeByType,
  postCredentials,
  postCredentialsByIdActivate,
  postCredentialsByIdDeactivate,
  postCredentialsByIdMarkUsed,
  postCredentialsByIdRestore,
  putCredentialsById,
} from '@/lib/api/generated/sdk.gen';

import type {
  DeleteCredentialsByIdData,
  DeleteCredentialsByIdHardData,
  GetCredentialsByIdData,
  GetCredentialsData,
  GetCredentialsDeletedData,
  GetCredentialsUserByUserIdData,
  GetCredentialsUserByUserIdTypeByTypeData,
  PostCredentialsByIdActivateData,
  PostCredentialsByIdDeactivateData,
  PostCredentialsByIdMarkUsedData,
  PostCredentialsByIdRestoreData,
  PostCredentialsData,
  PutCredentialsByIdData,
} from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

/**
 * Get all credentials
 */
export async function getCredentialsAction(params?: GetCredentialsData) {
  await configureAuthenticatedClient();
  const result = await getCredentials({
    ...params,
  });

  revalidateTag('credentials');
  return result;
}

/**
 * Create a new credential
 */
export async function createCredentialAction(data: PostCredentialsData) {
  await configureAuthenticatedClient();
  const result = await postCredentials({
    body: data.body,
  });

  revalidateTag('credentials');
  return result;
}

/**
 * Get credentials for a specific user
 */
export async function getUserCredentialsAction(data: GetCredentialsUserByUserIdData) {
  await configureAuthenticatedClient();
  const result = await getCredentialsUserByUserId({
    path: { userId: data.path.userId },
  });

  revalidateTag('credentials');
  revalidateTag(`user-credentials-${data.path.userId}`);
  return result;
}

/**
 * Delete a credential by ID
 */
export async function deleteCredentialAction(data: DeleteCredentialsByIdData) {
  await configureAuthenticatedClient();
  const result = await deleteCredentialsById({
    path: { id: data.path.id },
  });

  revalidateTag('credentials');
  revalidateTag(`credential-${data.path.id}`);
  return result;
}

/**
 * Get a credential by ID
 */
export async function getCredentialByIdAction(data: GetCredentialsByIdData) {
  await configureAuthenticatedClient();
  const result = await getCredentialsById({
    path: { id: data.path.id },
  });

  revalidateTag(`credential-${data.path.id}`);
  return result;
}

/**
 * Update a credential by ID
 */
export async function updateCredentialAction(data: PutCredentialsByIdData) {
  await configureAuthenticatedClient();
  const result = await putCredentialsById({
    path: { id: data.path.id },
    body: data.body,
  });

  revalidateTag('credentials');
  revalidateTag(`credential-${data.path.id}`);
  return result;
}

/**
 * Get credentials for a user by type
 */
export async function getUserCredentialsByTypeAction(data: GetCredentialsUserByUserIdTypeByTypeData) {
  await configureAuthenticatedClient();
  const result = await getCredentialsUserByUserIdTypeByType({
    path: {
      userId: data.path.userId,
      type: data.path.type,
    },
  });

  revalidateTag('credentials');
  revalidateTag(`user-credentials-${data.path.userId}`);
  revalidateTag(`user-credentials-${data.path.userId}-${data.path.type}`);
  return result;
}

/**
 * Restore a deleted credential
 */
export async function restoreCredentialAction(data: PostCredentialsByIdRestoreData) {
  await configureAuthenticatedClient();
  const result = await postCredentialsByIdRestore({
    path: { id: data.path.id },
  });

  revalidateTag('credentials');
  revalidateTag('credentials-deleted');
  revalidateTag(`credential-${data.path.id}`);
  return result;
}

/**
 * Permanently delete a credential (hard delete)
 */
export async function hardDeleteCredentialAction(data: DeleteCredentialsByIdHardData) {
  await configureAuthenticatedClient();
  const result = await deleteCredentialsByIdHard({
    path: { id: data.path.id },
  });

  revalidateTag('credentials');
  revalidateTag('credentials-deleted');
  revalidateTag(`credential-${data.path.id}`);
  return result;
}

/**
 * Mark a credential as used
 */
export async function markCredentialUsedAction(data: PostCredentialsByIdMarkUsedData) {
  await configureAuthenticatedClient();
  const result = await postCredentialsByIdMarkUsed({
    path: { id: data.path.id },
  });

  revalidateTag('credentials');
  revalidateTag(`credential-${data.path.id}`);
  return result;
}

/**
 * Deactivate a credential
 */
export async function deactivateCredentialAction(data: PostCredentialsByIdDeactivateData) {
  await configureAuthenticatedClient();
  const result = await postCredentialsByIdDeactivate({
    path: { id: data.path.id },
  });

  revalidateTag('credentials');
  revalidateTag(`credential-${data.path.id}`);
  return result;
}

/**
 * Activate a credential
 */
export async function activateCredentialAction(data: PostCredentialsByIdActivateData) {
  await configureAuthenticatedClient();
  const result = await postCredentialsByIdActivate({
    path: { id: data.path.id },
  });

  revalidateTag('credentials');
  revalidateTag(`credential-${data.path.id}`);
  return result;
}

/**
 * Get all deleted credentials
 */
export async function getDeletedCredentialsAction(params?: GetCredentialsDeletedData) {
  await configureAuthenticatedClient();
  const result = await getCredentialsDeleted({
    ...params,
  });

  revalidateTag('credentials-deleted');
  return result;
}
