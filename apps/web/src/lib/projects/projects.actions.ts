// Legacy wrapper adapting old projects.actions API to new content-management implementation.
'use server';

import type {PostApiProjectsData, Project, PutApiProjectsByIdData} from '@/lib/api/generated/types.gen';
import {
    archiveProject as archiveProjectNew,
    createProject as createProjectNew,
    deleteProject as deleteProjectNew,
    getProjects as getProjectsRaw,
    publishProject as publishProjectNew,
    unpublishProject as unpublishProjectNew,
    updateProject as updateProjectNew,
} from '@/lib/content-management/projects/projects.actions';

// -----------------------------------------------------------------------------
// GET PROJECTS (legacy shape)
// -----------------------------------------------------------------------------
export async function getProjectsData(params?: {
  searchTerm?: string;
  categoryId?: string;
  creatorId?: string;
  type?: string;
  status?: string;
  visibility?: number;
  skip?: number;
  take?: number;
}) {
  const response = await getProjectsRaw({
    query: {
      searchTerm: params?.searchTerm,
      categoryId: params?.categoryId,
      creatorId: params?.creatorId,
      type: params?.type ? (Number(params.type) as any) : undefined,
      status: params?.status ? (Number(params.status) as any) : undefined,
      visibility: params?.visibility as any,
      skip: params?.skip,
      take: params?.take,
    },
  });

  const projects: Project[] = (response as any)?.data || [];
  return {
    projects,
    total: projects.length,
  };
}

// -----------------------------------------------------------------------------
// CREATE PROJECT (legacy signature)
// -----------------------------------------------------------------------------
export async function createProject(projectData: {
  title: string;
  description?: string;
  shortDescription?: string;
  visibility: number;
  category?: string;
  type?: string;
  developmentStatus?: string; // not mapped currently
  websiteUrl?: string;
  repositoryUrl?: string;
  downloadUrl?: string;
  tags?: string[];
  imageUrl?: string;
}) {
  const body: PostApiProjectsData['body'] = {
    title: projectData.title,
    description: projectData.description,
    shortDescription: projectData.shortDescription,
    visibility: projectData.visibility as any,
    categoryId: projectData.category,
    type: projectData.type ? (Number(projectData.type) as any) : undefined,
    websiteUrl: projectData.websiteUrl,
    repositoryUrl: projectData.repositoryUrl,
    downloadUrl: projectData.downloadUrl,
    tags: projectData.tags,
    imageUrl: projectData.imageUrl,
  };

  const result: any = await createProjectNew({ body });
  return result?.data?.project as Project | undefined;
}

// -----------------------------------------------------------------------------
// UPDATE PROJECT (legacy signature)
// -----------------------------------------------------------------------------
export async function updateProject(
  id: string,
  projectData: {
    title?: string;
    description?: string;
    shortDescription?: string;
    visibility?: number;
    category?: string;
    type?: string;
    developmentStatus?: string; // not mapped currently
    websiteUrl?: string;
    repositoryUrl?: string;
    downloadUrl?: string;
    tags?: string[];
    imageUrl?: string;
  },
) {
  const body: PutApiProjectsByIdData['body'] = {
    title: projectData.title,
    description: projectData.description,
    shortDescription: projectData.shortDescription,
    visibility: projectData.visibility as any,
    categoryId: projectData.category,
    type: projectData.type ? (Number(projectData.type) as any) : undefined,
    websiteUrl: projectData.websiteUrl,
    repositoryUrl: projectData.repositoryUrl,
    downloadUrl: projectData.downloadUrl,
    tags: projectData.tags,
    imageUrl: projectData.imageUrl,
  };

  return updateProjectNew({ path: { id }, body });
}

// -----------------------------------------------------------------------------
// DELETE / PUBLISH / UNPUBLISH / ARCHIVE wrappers
// -----------------------------------------------------------------------------
export async function deleteProject(id: string) {
  return deleteProjectNew({ path: { id } });
}

export async function publishProject(id: string) {
  return publishProjectNew({ path: { id } });
}

export async function unpublishProject(id: string) {
  return unpublishProjectNew({ path: { id } });
}

export async function archiveProject(id: string) {
  return archiveProjectNew({ path: { id } });
}
