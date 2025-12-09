'use server';

// STUB: Projects content-management actions are stubbed; endpoints unavailable in current SDK.

export type GetApiProjectsData = any;
export type PostApiProjectsData = any;
export type DeleteApiProjectsByIdData = any;
export type GetApiProjectsByIdData = any;
export type PutApiProjectsByIdData = any;
export type GetApiProjectsSlugBySlugData = any;
export type PostApiProjectsByIdPublishData = any;
export type PostApiProjectsByIdUnpublishData = any;
export type PostApiProjectsByIdArchiveData = any;
export type GetApiProjectsSearchData = any;
export type GetApiProjectsPopularData = any;
export type GetApiProjectsRecentData = any;
export type GetApiProjectsFeaturedData = any;
export type GetApiProjectsCategoryByCategoryIdData = any;
export type GetApiProjectsCreatorByCreatorIdData = any;
export type GetApiProjectsByIdStatisticsData = any;

export async function getProjects(_data?: GetApiProjectsData): Promise<any> {
  throw new Error('Not implemented (STUB): getProjects');
}

export async function createProject(_data?: PostApiProjectsData): Promise<any> {
  throw new Error('Not implemented (STUB): createProject');
}

export async function deleteProject(_data: DeleteApiProjectsByIdData): Promise<any> {
  throw new Error('Not implemented (STUB): deleteProject');
}

export async function getProjectById(_data: GetApiProjectsByIdData): Promise<any> {
  throw new Error('Not implemented (STUB): getProjectById');
}

export async function updateProject(_data: PutApiProjectsByIdData): Promise<any> {
  throw new Error('Not implemented (STUB): updateProject');
}

export async function getProjectBySlug(_data: any): Promise<any> {
  throw new Error('Not implemented (STUB): getProjectBySlug');
}

export async function publishProject(_data: PostApiProjectsByIdPublishData): Promise<any> {
  throw new Error('Not implemented (STUB): publishProject');
}

export async function unpublishProject(_data: PostApiProjectsByIdUnpublishData): Promise<any> {
  throw new Error('Not implemented (STUB): unpublishProject');
}

export async function archiveProject(_data: PostApiProjectsByIdArchiveData): Promise<any> {
  throw new Error('Not implemented (STUB): archiveProject');
}

export async function searchProjects(_data?: GetApiProjectsSearchData): Promise<any> {
  throw new Error('Not implemented (STUB): searchProjects');
}

export async function getPopularProjects(_data?: GetApiProjectsPopularData): Promise<any> {
  throw new Error('Not implemented (STUB): getPopularProjects');
}

export async function getRecentProjects(_data?: GetApiProjectsRecentData): Promise<any> {
  throw new Error('Not implemented (STUB): getRecentProjects');
}

export async function getFeaturedProjects(_data?: GetApiProjectsFeaturedData): Promise<any> {
  throw new Error('Not implemented (STUB): getFeaturedProjects');
}

export async function getProjectsByCategory(_data: GetApiProjectsCategoryByCategoryIdData): Promise<any> {
  throw new Error('Not implemented (STUB): getProjectsByCategory');
}

export async function getProjectsByCreator(_data: GetApiProjectsCreatorByCreatorIdData): Promise<any> {
  throw new Error('Not implemented (STUB): getProjectsByCreator');
}

export async function getProjectStatistics(_data: GetApiProjectsByIdStatisticsData): Promise<any> {
  throw new Error('Not implemented (STUB): getProjectStatistics');
}
