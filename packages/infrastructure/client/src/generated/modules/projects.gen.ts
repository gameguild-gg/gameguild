/**
 * @game-guild/client - Projects Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ProjectsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getProjects(query?: {
    type?: Types.ProjectsProjectType;
    status?: Types.ContentStatus;
    visibility?: Types.ContentVisibility;
    creatorId?: string;
    categoryId?: string;
    searchTerm?: string;
    featured?: boolean;
    popular?: boolean;
    recent?: boolean;
    skip?: number;
    take?: number;
    sortBy?: string;
    sortDirection?: string;
  }): Promise<Result<Array<Types.ProjectsProject>, ApiError>> {
    const url = '/v1/projects';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.ProjectsProject>, ApiError>;
  }

  /**
   */
  async postProjects(body: Types.ProjectsCreateProjectInput): Promise<Result<Types.ProjectsProject, ApiError>> {
    const url = '/v1/projects';

    // Validate request body
    const validatedBody = safeParse(Types.ProjectsCreateProjectInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getProjectsById(
    id: string,
    query?: { includeTeam?: boolean; includeReleases?: boolean; includeCollaborators?: boolean; includeStatistics?: boolean },
  ): Promise<Result<Types.ProjectsProject, ApiError>> {
    const url = `/v1/projects/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putProjects(id: string, body: Types.ProjectsUpdateProjectInput): Promise<Result<Types.ProjectsProject, ApiError>> {
    const url = `/v1/projects/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.ProjectsUpdateProjectInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjects(id: string, query?: { softDelete?: boolean; reason?: string }): Promise<Result<boolean, ApiError>> {
    const url = `/v1/projects/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   */
  async getProjectsSlug(
    slug: string,
    query?: { includeTeam?: boolean; includeReleases?: boolean; includeCollaborators?: boolean },
  ): Promise<Result<Types.ProjectsProject, ApiError>> {
    const url = `/v1/projects/slug/${slug}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postProjectsPublish(id: string): Promise<Result<Types.ProjectsProject, ApiError>> {
    const url = `/v1/projects/${id}:publish`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postProjectsUnpublish(id: string): Promise<Result<Types.ProjectsProject, ApiError>> {
    const url = `/v1/projects/${id}:unpublish`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postProjectsArchive(id: string): Promise<Result<Types.ProjectsProject, ApiError>> {
    const url = `/v1/projects/${id}:archive`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getProjectsSearch(query?: {
    searchTerm?: string;
    type?: Types.ProjectsProjectType;
    categoryId?: string;
    status?: Types.ContentStatus;
    visibility?: Types.ContentVisibility;
    skip?: number;
    take?: number;
    sortBy?: string;
    sortDirection?: string;
  }): Promise<Result<Array<Types.ProjectsProject>, ApiError>> {
    const url = '/v1/projects/search';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.ProjectsProject>, ApiError>;
  }

  /**
   */
  async getProjectsPopular(query?: { type?: Types.ProjectsProjectType; take?: number }): Promise<Result<Array<Types.ProjectsProject>, ApiError>> {
    const url = '/v1/projects/popular';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.ProjectsProject>, ApiError>;
  }

  /**
   */
  async getProjectsRecent(query?: { type?: Types.ProjectsProjectType; take?: number }): Promise<Result<Array<Types.ProjectsProject>, ApiError>> {
    const url = '/v1/projects/recent';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.ProjectsProject>, ApiError>;
  }

  /**
   */
  async getProjectsFeatured(query?: { type?: Types.ProjectsProjectType; take?: number }): Promise<Result<Array<Types.ProjectsProject>, ApiError>> {
    const url = '/v1/projects/featured';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.ProjectsProject>, ApiError>;
  }

  /**
   */
  async getProjectsStatistics(id: string, query?: { fromDate?: string; toDate?: string }): Promise<Result<Types.ProjectsProjectStatistics, ApiError>> {
    const url = `/v1/projects/${id}/statistics`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectStatisticsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getProjectsCategory(
    categoryId: string,
    query?: { status?: Types.ContentStatus; skip?: number; take?: number },
  ): Promise<Result<Array<Types.ProjectsProject>, ApiError>> {
    const url = `/v1/projects/category/${categoryId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.ProjectsProject>, ApiError>;
  }

  /**
   */
  async getProjectsCreator(
    creatorId: string,
    query?: { status?: Types.ContentStatus; skip?: number; take?: number },
  ): Promise<Result<Array<Types.ProjectsProject>, ApiError>> {
    const url = `/v1/projects/creator/${creatorId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.ProjectsProject>, ApiError>;
  }

  /**
   */
  async getProjectsRoleTemplates(): Promise<Result<Array<Record<string, unknown>>, ApiError>> {
    const url = '/v1/projects/role-templates';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Array<Record<string, unknown>>, ApiError>;
  }

  /**
   */
  async getProjectsMyInvitations(): Promise<Result<Array<Types.ProjectsProjectInvitation>, ApiError>> {
    const url = '/v1/projects/my-invitations';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.ProjectsProjectInvitation>, ApiError>;
  }

  /**
   */
  async getProjectsRolesPermissions(roleName: string): Promise<Result<Array<Types.IdentityAuthorizationPermissionType>, ApiError>> {
    const url = `/v1/projects/roles/${roleName}/permissions`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Array<Types.IdentityAuthorizationPermissionType>, ApiError>;
  }

  /**
   */
  async postProjectsInvitationsAccept(invitationToken: string): Promise<Result<Types.ProjectsProjectInvitation, ApiError>> {
    const url = `/v1/projects/invitations/${invitationToken}:accept`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectInvitationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postProjectsInvitationsDecline(invitationToken: string): Promise<Result<Types.ProjectsProjectInvitation, ApiError>> {
    const url = `/v1/projects/invitations/${invitationToken}:decline`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectInvitationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getProjectsCollaborators(id: string): Promise<Result<Array<Types.ProjectsCollaborator>, ApiError>> {
    const url = `/v1/projects/${id}/collaborators`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.ProjectsCollaborator>, ApiError>;
  }

  /**
   */
  async postProjectsCollaborators(id: string, body: Types.ProjectsAddProjectCollaboratorInput): Promise<Result<Types.ProjectsCollaborator, ApiError>> {
    const url = `/v1/projects/${id}/collaborators`;

    // Validate request body
    const validatedBody = safeParse(Types.ProjectsAddProjectCollaboratorInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsCollaboratorSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putProjectsCollaborators(
    id: string,
    collaboratorId: string,
    body: Types.ProjectsUpdateProjectCollaboratorInput,
  ): Promise<Result<Types.ProjectsCollaborator, ApiError>> {
    const url = `/v1/projects/${id}/collaborators/${collaboratorId}`;

    // Validate request body
    const validatedBody = safeParse(Types.ProjectsUpdateProjectCollaboratorInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsCollaboratorSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjectsCollaborators(id: string, collaboratorId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${id}/collaborators/${collaboratorId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postProjectsShare(id: string, body: Types.ProjectsShareProjectInput): Promise<Result<Types.ProjectsCollaborator, ApiError>> {
    const url = `/v1/projects/${id}:share`;

    // Validate request body
    const validatedBody = safeParse(Types.ProjectsShareProjectInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsCollaboratorSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postProjectsInvitations(id: string, body: Types.ProjectsInviteProjectCollaboratorInput): Promise<Result<Types.ProjectsProjectInvitation, ApiError>> {
    const url = `/v1/projects/${id}/invitations`;

    // Validate request body
    const validatedBody = safeParse(Types.ProjectsInviteProjectCollaboratorInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectInvitationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createProjectsModule(client: ApiClient): ProjectsModule {
  return new ProjectsModule(client);
}
