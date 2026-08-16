/**
 * @game-guild/client - Notifications Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class NotificationsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getApiNotificationsForGetApiNotifications(query?: {
    skip?: number;
    take?: number;
    isRead?: boolean;
  }): Promise<
    Result<Array<Types.NotificationsControllersNotification>, ApiError>
  > {
    const url = "/api/notifications";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.NotificationsControllersNotification>,
      ApiError
    >;
  }

  /**
   */
  async getApiNotificationsUnreadCount(): Promise<
    Result<Types.NotificationsControllersUnreadCountOutput, ApiError>
  > {
    const url = "/api/notifications/unread-count";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.NotificationsControllersUnreadCountOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiNotificationsForGetApiNotificationsById(
    id: string,
  ): Promise<Result<Types.NotificationsControllersNotification, ApiError>> {
    const url = `/api/notifications/${id}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.NotificationsControllersNotificationSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteApiNotifications(id: string): Promise<Result<void, ApiError>> {
    const url = `/api/notifications/${id}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postApiNotificationsRead(id: string): Promise<Result<void, ApiError>> {
    const url = `/api/notifications/${id}/read`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postApiNotificationsReadAll(): Promise<Result<void, ApiError>> {
    const url = "/api/notifications/read-all";

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postApiNotificationsUnread(
    id: string,
  ): Promise<Result<void, ApiError>> {
    const url = `/api/notifications/${id}/unread`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async deleteApiNotificationsRead(): Promise<
    Result<Types.NotificationsControllersDeletedCountOutput, ApiError>
  > {
    const url = "/api/notifications/read";

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.NotificationsControllersDeletedCountOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiNotificationsPreferences(): Promise<
    Result<Types.NotificationsControllersNotificationPreference, ApiError>
  > {
    const url = "/api/notifications/preferences";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.NotificationsControllersNotificationPreferenceSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiNotificationsPreferences(
    body: Types.NotificationsControllersUpdatePreferencesInput,
  ): Promise<
    Result<Types.NotificationsControllersNotificationPreference, ApiError>
  > {
    const url = "/api/notifications/preferences";

    // Validate request body
    const validatedBody = safeParse(
      Types.NotificationsControllersUpdatePreferencesInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.NotificationsControllersNotificationPreferenceSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiNotificationsPreferencesQuietHours(
    body: Types.NotificationsControllersSetQuietHoursInput,
  ): Promise<Result<void, ApiError>> {
    const url = "/api/notifications/preferences/quiet-hours";

    // Validate request body
    const validatedBody = safeParse(
      Types.NotificationsControllersSetQuietHoursInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createNotificationsModule(
  client: ApiClient,
): NotificationsModule {
  return new NotificationsModule(client);
}
