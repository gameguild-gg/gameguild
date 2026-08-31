/**
 * @game-guild/client - ApiProjectWork Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ApiProjectWorkModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getProjectsWork(projectId: string): Promise<Result<Types.APIProjectWorkProjectBoard, ApiError>> {
    const url = `/v1/projects/${projectId}/work`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectBoardSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postProjectsWorkColumns(
    projectId: string,
    body: Types.APIProjectWorkConfigureProjectWorkColumnInput,
  ): Promise<Result<Types.APIProjectWorkProjectWorkColumn, ApiError>> {
    const url = `/v1/projects/${projectId}/work/columns`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkConfigureProjectWorkColumnInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectWorkColumnSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putProjectsWorkColumns(
    projectId: string,
    columnId: string,
    body: Types.APIProjectWorkConfigureProjectWorkColumnInput,
  ): Promise<Result<Types.APIProjectWorkProjectWorkColumn, ApiError>> {
    const url = `/v1/projects/${projectId}/work/columns/${columnId}`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkConfigureProjectWorkColumnInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectWorkColumnSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjectsWorkColumns(projectId: string, columnId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/columns/${columnId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getProjectsWorkHistory(projectId: string, query?: { take?: number }): Promise<Result<Array<Types.APIProjectWorkProjectWorkHistory>, ApiError>> {
    const url = `/v1/projects/${projectId}/work/history`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.APIProjectWorkProjectWorkHistory>, ApiError>;
  }

  /**
   */
  async getProjectsWorkLabels(projectId: string): Promise<Result<Array<Types.APIProjectWorkProjectTaskLabel>, ApiError>> {
    const url = `/v1/projects/${projectId}/work/labels`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.APIProjectWorkProjectTaskLabel>, ApiError>;
  }

  /**
   */
  async postProjectsWorkLabels(
    projectId: string,
    body: Types.APIProjectWorkCreateProjectTaskLabelInput,
  ): Promise<Result<Types.APIProjectWorkProjectTaskLabel, ApiError>> {
    const url = `/v1/projects/${projectId}/work/labels`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkCreateProjectTaskLabelInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectTaskLabelSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjectsWorkLabels(projectId: string, labelId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/labels/${labelId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getProjectsWorkMilestones(projectId: string): Promise<Result<Array<Types.APIProjectWorkProjectMilestone>, ApiError>> {
    const url = `/v1/projects/${projectId}/work/milestones`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.APIProjectWorkProjectMilestone>, ApiError>;
  }

  /**
   */
  async postProjectsWorkMilestones(
    projectId: string,
    body: Types.APIProjectWorkCreateProjectMilestoneInput,
  ): Promise<Result<Types.APIProjectWorkProjectMilestone, ApiError>> {
    const url = `/v1/projects/${projectId}/work/milestones`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkCreateProjectMilestoneInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectMilestoneSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putProjectsWorkMilestones(
    projectId: string,
    milestoneId: string,
    body: Types.APIProjectWorkUpdateProjectMilestoneInput,
  ): Promise<Result<Types.APIProjectWorkProjectMilestone, ApiError>> {
    const url = `/v1/projects/${projectId}/work/milestones/${milestoneId}`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkUpdateProjectMilestoneInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectMilestoneSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjectsWorkMilestones(projectId: string, milestoneId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/milestones/${milestoneId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postProjectsWorkTasks(
    projectId: string,
    body: Types.APIProjectWorkCreateProjectWorkTaskInput,
  ): Promise<Result<Types.APIProjectWorkProjectWorkTask, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkCreateProjectWorkTaskInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectWorkTaskSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getProjectsWorkTasks(projectId: string, taskId: string): Promise<Result<Types.APIProjectWorkProjectWorkTaskDetails, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectWorkTaskDetailsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putProjectsWorkTasks(
    projectId: string,
    taskId: string,
    body: Types.APIProjectWorkUpdateProjectWorkTaskInput,
  ): Promise<Result<Types.APIProjectWorkProjectWorkTask, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkUpdateProjectWorkTaskInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectWorkTaskSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjectsWorkTasks(projectId: string, taskId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postProjectsWorkTasksChecklist(
    projectId: string,
    taskId: string,
    body: Types.APIProjectWorkAddProjectTaskChecklistInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/checklist`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkAddProjectTaskChecklistInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async putProjectsWorkTasksChecklist(
    projectId: string,
    taskId: string,
    itemId: string,
    body: Types.APIProjectWorkUpdateProjectTaskChecklistInput,
  ): Promise<Result<Types.APIProjectWorkProjectChecklistItem, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/checklist/${itemId}`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkUpdateProjectTaskChecklistInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectChecklistItemSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjectsWorkTasksChecklist(projectId: string, taskId: string, itemId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/checklist/${itemId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postProjectsWorkTasksComments(
    projectId: string,
    taskId: string,
    body: Types.APIProjectWorkAddProjectTaskCommentInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/comments`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkAddProjectTaskCommentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async putProjectsWorkTasksComments(
    projectId: string,
    taskId: string,
    commentId: string,
    body: Types.APIProjectWorkUpdateProjectTaskCommentInput,
  ): Promise<Result<Types.APIProjectWorkProjectTaskComment, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/comments/${commentId}`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkUpdateProjectTaskCommentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectTaskCommentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjectsWorkTasksComments(projectId: string, taskId: string, commentId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/comments/${commentId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postProjectsWorkTasksDependencies(
    projectId: string,
    taskId: string,
    body: Types.APIProjectWorkAddProjectTaskDependencyInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/dependencies`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkAddProjectTaskDependencyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async deleteProjectsWorkTasksDependencies(projectId: string, taskId: string, dependencyId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/dependencies/${dependencyId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postProjectsWorkTasksLabels(projectId: string, taskId: string, labelId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/labels/${labelId}`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async deleteProjectsWorkTasksLabels(projectId: string, taskId: string, labelId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/labels/${labelId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async putProjectsWorkTasksMove(
    projectId: string,
    taskId: string,
    body: Types.APIProjectWorkMoveProjectWorkTaskInput,
  ): Promise<Result<Types.APIProjectWorkProjectWorkTask, ApiError>> {
    const url = `/v1/projects/${projectId}/work/tasks/${taskId}/move`;

    // Validate request body
    const validatedBody = safeParse(Types.APIProjectWorkMoveProjectWorkTaskInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIProjectWorkProjectWorkTaskSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createApiProjectWorkModule(client: ApiClient): ApiProjectWorkModule {
  return new ApiProjectWorkModule(client);
}
