'use server';

// STUB: Users actions are stubbed; endpoints are unavailable in current SDK.

export type DeleteApiUsersByIdData = any;
export type GetApiUsersByIdData = any;
export type GetApiUsersByUserIdAchievementsAvailableData = any;
export type GetApiUsersByUserIdAchievementsByAchievementIdPrerequisitesData = any;
export type GetApiUsersByUserIdAchievementsData = any;
export type GetApiUsersByUserIdAchievementsProgressData = any;
export type GetApiUsersByUserIdAchievementsSummaryData = any;
export type GetApiUsersData = any;
export type GetApiUsersSearchData = any;
export type GetApiUsersStatisticsData = any;
export type PatchApiUsersBulkActivateData = any;
export type PatchApiUsersBulkDeactivateData = any;
export type PostApiUsersBulkData = any;
export type PostApiUsersByIdRestoreData = any;
export type PostApiUsersByUserIdAchievementsByAchievementIdProgressData = any;
export type PostApiUsersData = any;
export type PutApiUsersByIdBalanceData = any;
export type PutApiUsersByIdData = any;

export async function getUsers(_data?: GetApiUsersData): Promise<any> { throw new Error('Not implemented (STUB): getUsers'); }
export async function createUser(_data?: PostApiUsersData): Promise<any> { throw new Error('Not implemented (STUB): createUser'); }
export async function deleteUser(_data: Omit<DeleteApiUsersByIdData, 'url'>): Promise<any> { throw new Error('Not implemented (STUB): deleteUser'); }
export async function getUserById(_data: Omit<GetApiUsersByIdData, 'url'>): Promise<any> { throw new Error('Not implemented (STUB): getUserById'); }
export async function updateUser(_data: Omit<PutApiUsersByIdData, 'url'>): Promise<any> { throw new Error('Not implemented (STUB): updateUser'); }
export async function restoreUser(_data: PostApiUsersByIdRestoreData): Promise<any> { throw new Error('Not implemented (STUB): restoreUser'); }
export async function updateUserBalance(_data: Omit<PutApiUsersByIdBalanceData, 'url'>): Promise<any> { throw new Error('Not implemented (STUB): updateUserBalance'); }
export async function getUserStatistics(_data?: GetApiUsersStatisticsData): Promise<any> { throw new Error('Not implemented (STUB): getUserStatistics'); }
export async function searchUsers(_data?: GetApiUsersSearchData): Promise<any> { throw new Error('Not implemented (STUB): searchUsers'); }
export async function createUsersBulk(_data?: PostApiUsersBulkData): Promise<any> { throw new Error('Not implemented (STUB): createUsersBulk'); }
export async function activateUsersBulk(_data?: Omit<PatchApiUsersBulkActivateData, 'url'>): Promise<any> { throw new Error('Not implemented (STUB): activateUsersBulk'); }
export async function deactivateUsersBulk(_data?: Omit<PatchApiUsersBulkDeactivateData, 'url'>): Promise<any> { throw new Error('Not implemented (STUB): deactivateUsersBulk'); }
export async function getUserAchievements(_data: GetApiUsersByUserIdAchievementsData): Promise<any> { throw new Error('Not implemented (STUB): getUserAchievements'); }
export async function getUserAchievementProgress(_data: GetApiUsersByUserIdAchievementsProgressData): Promise<any> { throw new Error('Not implemented (STUB): getUserAchievementProgress'); }
export async function getUserAchievementSummary(_data: GetApiUsersByUserIdAchievementsSummaryData): Promise<any> { throw new Error('Not implemented (STUB): getUserAchievementSummary'); }
export async function getUserAvailableAchievements(_data: GetApiUsersByUserIdAchievementsAvailableData): Promise<any> { throw new Error('Not implemented (STUB): getUserAvailableAchievements'); }
export async function updateUserAchievementProgress(_data: PostApiUsersByUserIdAchievementsByAchievementIdProgressData): Promise<any> { throw new Error('Not implemented (STUB): updateUserAchievementProgress'); }
export async function getUserAchievementPrerequisites(_data: GetApiUsersByUserIdAchievementsByAchievementIdPrerequisitesData): Promise<any> { throw new Error('Not implemented (STUB): getUserAchievementPrerequisites'); }

export const bulkActivateUsers = activateUsersBulk;
export const bulkDeactivateUsers = deactivateUsersBulk;

export async function getUsersData(_page: number = 1, _limit: number = 10): Promise<any> {
  throw new Error('Not implemented (STUB): getUsersData');
}

export async function refreshUserStatistics(_data?: GetApiUsersStatisticsData): Promise<any> {
  throw new Error('Not implemented (STUB): refreshUserStatistics');
}
