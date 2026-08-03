/**
 * @game-guild/client - UsersNotifications Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class UsersNotificationsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get user notifications with pagination, search, and sorting
   */
  async getUsersNotifications(
    userId: string,
    query?: {
      page?: number;
      pageSize?: number;
      search?: string;
      sortBy?: string;
      sortDirection?: string;
      isRead?: boolean;
      isArchived?: boolean;
      type?: string;
      priority?: string;
      fromDate?: string;
      toDate?: string;
    },
  ): Promise<Result<Types.PagedResultOfGameGuildIdentityUsersUserNotificationDto, ApiError>> {
    const url = `/v1/users/${userId}/notifications`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.PagedResultOfGameGuildIdentityUsersUserNotificationDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get detailed notification by ID
   */
  async getUsersNotifications1(userId: string, notificationId: string): Promise<Result<Types.IdentityUsersUserNotificationDetail, ApiError>> {
    const url = `/v1/users/${userId}/notifications/${notificationId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityUsersUserNotificationDetailSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Check if user notification exists
   */
  async headUsersNotifications(userId: string, notificationId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications/${notificationId}`;

    const result = await this.client.request({
      method: 'HEAD',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Archive notification
   */
  async postUsersNotificationsArchive(userId: string, notificationId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications/${notificationId}:archive`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Mark notification as read
   */
  async postUsersNotificationsMarkAsRead(userId: string, notificationId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications/${notificationId}:mark-as-read`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Mark notification as unread
   */
  async postUsersNotificationsMarkAsUnread(userId: string, notificationId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications/${notificationId}:mark-as-unread`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Unarchive notification
   */
  async postUsersNotificationsUnarchive(userId: string, notificationId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications/${notificationId}:unarchive`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Archive multiple notifications for a user
   */
  async postUsersNotificationsArchive1(userId: string, body: Types.IdentityUsersBulkNotificationInput): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications:archive`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityUsersBulkNotificationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Mark multiple notifications as read for a user
   */
  async postUsersNotificationsMarkAsRead1(userId: string, body: Types.IdentityUsersBulkNotificationInput): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications:mark-as-read`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityUsersBulkNotificationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Mark multiple notifications as unread for a user
   */
  async postUsersNotificationsMarkAsUnread1(userId: string, body: Types.IdentityUsersBulkNotificationInput): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications:mark-as-unread`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityUsersBulkNotificationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Unarchive multiple notifications for a user
   */
  async postUsersNotificationsUnarchive1(userId: string, body: Types.IdentityUsersBulkNotificationInput): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/notifications:unarchive`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityUsersBulkNotificationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createUsersNotificationsModule(client: ApiClient): UsersNotificationsModule {
  return new UsersNotificationsModule(client);
}
