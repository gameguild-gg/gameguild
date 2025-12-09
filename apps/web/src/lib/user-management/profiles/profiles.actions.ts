'use server';

// STUB: User profiles actions are stubbed; endpoints are unavailable in current SDK.

export async function getUserProfilesData(_params?: any): Promise<any> {
  throw new Error('Not implemented (STUB): getUserProfilesData');
}

export async function getUserProfileById(_id: string, _includeDeleted = false): Promise<any> {
  throw new Error('Not implemented (STUB): getUserProfileById');
}

export async function getUserProfileByUserId(_userId: string, _includeDeleted = false): Promise<any> {
  throw new Error('Not implemented (STUB): getUserProfileByUserId');
}

export async function createUserProfile(_profileData: any): Promise<any> {
  throw new Error('Not implemented (STUB): createUserProfile');
}

export async function updateUserProfile(_id: string, _profileData: any, _ifMatch?: number): Promise<any> {
  throw new Error('Not implemented (STUB): updateUserProfile');
}

export async function deleteUserProfile(_id: string): Promise<any> {
  throw new Error('Not implemented (STUB): deleteUserProfile');
}

export async function restoreUserProfile(_id: string): Promise<any> {
  throw new Error('Not implemented (STUB): restoreUserProfile');
}

export async function getOrCreateUserProfile(_userId: string, _defaultProfileData: any): Promise<any> {
  throw new Error('Not implemented (STUB): getOrCreateUserProfile');
}

export async function updateUserProfileWithVersion(_id: string, _profileData: any, _expectedVersion: number): Promise<any> {
  throw new Error('Not implemented (STUB): updateUserProfileWithVersion');
}
