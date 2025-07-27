'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient, getAuthenticatedSession } from '@/lib/api/authenticated-client';
import {
  getCredentials,
  postCredentials,
  getCredentialsUserByUserId,
  deleteCredentialsById,
  getCredentialsById,
  putCredentialsById,
  getCredentialsUserByUserIdTypeByType,
  postCredentialsByIdRestore,
  deleteCredentialsByIdHard,
  postCredentialsByIdMarkUsed,
  postCredentialsByIdDeactivate,
  postCredentialsByIdActivate,
  getCredentialsDeleted,
} from '@/lib/api/generated/sdk.gen';
import type { CreateCredentialDto, UpdateCredentialDto } from '@/lib/api/generated/types.gen';

// Get all credentials with pagination and filtering
export async function getCredentialsData() {
  await configureAuthenticatedClient();

  try {
    const result = await getCredentials();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get credentials:', error);
    return { success: false, error: 'Failed to get credentials' };
  }
}

// Create a new credential
export async function createCredential(data: {
  userId?: string;
  type: string;
  value: string;
  metadata?: Record<string, unknown> | string | null;
  expiresAt?: string | null;
  isActive?: boolean;
}) {
  await configureAuthenticatedClient();
  const session = await getAuthenticatedSession();

  try {
    const requestData: CreateCredentialDto = {
      userId: data.userId || session.user.id,
      type: data.type,
      value: data.value,
      metadata: typeof data.metadata === 'object' && data.metadata !== null ? JSON.stringify(data.metadata) : data.metadata || null,
      expiresAt: data.expiresAt || null,
      isActive: data.isActive ?? true,
    };

    const result = await postCredentials({
      body: requestData,
    });

    if (result.data) {
      revalidateTag('credentials');
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to create credential:', error);
    return { success: false, error: 'Failed to create credential' };
  }
}

// Get credentials for a specific user
export async function getUserCredentials(userId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getCredentialsUserByUserId({
      path: { userId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get user credentials:', error);
    return { success: false, error: 'Failed to get user credentials' };
  }
}

// Delete a credential (soft delete)
export async function deleteCredential(credentialId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await deleteCredentialsById({
      path: { id: credentialId },
    });

    if (result.data) {
      revalidateTag('credentials');
      revalidateTag(`credential-${credentialId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to delete credential:', error);
    return { success: false, error: 'Failed to delete credential' };
  }
}

// Get a credential by ID
export async function getCredentialById(credentialId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getCredentialsById({
      path: { id: credentialId },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get credential:', error);
    return { success: false, error: 'Failed to get credential' };
  }
}

// Update a credential
export async function updateCredential(credentialId: string, data: {
  type: string;
  value: string;
  metadata?: Record<string, unknown> | string | null;
  expiresAt?: string | null;
  isActive?: boolean;
}) {
  await configureAuthenticatedClient();

  try {
    const requestData: UpdateCredentialDto = {
      type: data.type,
      value: data.value,
      metadata: typeof data.metadata === 'object' && data.metadata !== null ? JSON.stringify(data.metadata) : data.metadata || null,
      expiresAt: data.expiresAt || null,
      isActive: data.isActive,
    };

    const result = await putCredentialsById({
      path: { id: credentialId },
      body: requestData,
    });

    if (result.data) {
      revalidateTag('credentials');
      revalidateTag(`credential-${credentialId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to update credential:', error);
    return { success: false, error: 'Failed to update credential' };
  }
}

// Get user credentials by type
export async function getUserCredentialsByType(userId: string, type: string) {
  await configureAuthenticatedClient();

  try {
    const result = await getCredentialsUserByUserIdTypeByType({
      path: { userId, type },
    });

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get user credentials by type:', error);
    return { success: false, error: 'Failed to get user credentials by type' };
  }
}

// Restore a deleted credential
export async function restoreCredential(credentialId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await postCredentialsByIdRestore({
      path: { id: credentialId },
    });

    if (result.data) {
      revalidateTag('credentials');
      revalidateTag(`credential-${credentialId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to restore credential:', error);
    return { success: false, error: 'Failed to restore credential' };
  }
}

// Hard delete a credential (permanent)
export async function hardDeleteCredential(credentialId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await deleteCredentialsByIdHard({
      path: { id: credentialId },
    });

    if (result.data) {
      revalidateTag('credentials');
      revalidateTag(`credential-${credentialId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to hard delete credential:', error);
    return { success: false, error: 'Failed to hard delete credential' };
  }
}

// Mark credential as used
export async function markCredentialAsUsed(credentialId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await postCredentialsByIdMarkUsed({
      path: { id: credentialId },
    });

    if (result.data) {
      revalidateTag('credentials');
      revalidateTag(`credential-${credentialId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to mark credential as used:', error);
    return { success: false, error: 'Failed to mark credential as used' };
  }
}

// Deactivate a credential
export async function deactivateCredential(credentialId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await postCredentialsByIdDeactivate({
      path: { id: credentialId },
    });

    if (result.data) {
      revalidateTag('credentials');
      revalidateTag(`credential-${credentialId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to deactivate credential:', error);
    return { success: false, error: 'Failed to deactivate credential' };
  }
}

// Activate a credential
export async function activateCredential(credentialId: string) {
  await configureAuthenticatedClient();

  try {
    const result = await postCredentialsByIdActivate({
      path: { id: credentialId },
    });

    if (result.data) {
      revalidateTag('credentials');
      revalidateTag(`credential-${credentialId}`);
    }

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to activate credential:', error);
    return { success: false, error: 'Failed to activate credential' };
  }
}

// Get deleted credentials
export async function getDeletedCredentials() {
  await configureAuthenticatedClient();

  try {
    const result = await getCredentialsDeleted();

    return { success: true, data: result.data };
  } catch (error) {
    console.error('Failed to get deleted credentials:', error);
    return { success: false, error: 'Failed to get deleted credentials' };
  }
}
