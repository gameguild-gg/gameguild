/**
 * @game-guild/client - Health Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class HealthModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Application information endpoint
   *
   * Provides application version, build details, and runtime information for debugging and deployment monitoring.
   */
  async getInfo(): Promise<
    Result<Types.APIControllersApplicationInfoOutput, ApiError>
  > {
    const url = "/info";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersApplicationInfoOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Comprehensive application health check
   *
   * Performs a comprehensive health check of all registered services and dependencies. Returns detailed status information for monitoring systems, load balancers, and orchestration platforms.
   */
  async getHealth(): Promise<
    Result<Types.APIControllersHealthinessOutput, ApiError>
  > {
    const url = "/health";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersHealthinessOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Comprehensive application health check
   *
   * Performs a comprehensive health check of all registered services and dependencies. Returns detailed status information for monitoring systems, load balancers, and orchestration platforms.
   */
  async getApiHealth(): Promise<
    Result<Types.APIControllersHealthinessOutput, ApiError>
  > {
    const url = "/api/health";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersHealthinessOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Readiness probe for traffic routing decisions
   *
   * Kubernetes-style readiness probe that determines whether the application is ready to serve traffic. Checks all dependencies and services required for proper request handling.
   */
  async getReady(): Promise<
    Result<Types.APIControllersReadinessOutput, ApiError>
  > {
    const url = "/ready";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersReadinessOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Readiness probe for traffic routing decisions
   *
   * Kubernetes-style readiness probe that determines whether the application is ready to serve traffic. Checks all dependencies and services required for proper request handling.
   */
  async getApiReady(): Promise<
    Result<Types.APIControllersReadinessOutput, ApiError>
  > {
    const url = "/api/ready";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersReadinessOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Liveness probe for container restart decisions
   *
   * Kubernetes-style liveness probe that indicates whether the application process is running correctly. Used by orchestration platforms to determine if containers should be restarted.
   */
  async getLive(): Promise<
    Result<Types.APIControllersLivenessOutput, ApiError>
  > {
    const url = "/live";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersLivenessOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Liveness probe for container restart decisions
   *
   * Kubernetes-style liveness probe that indicates whether the application process is running correctly. Used by orchestration platforms to determine if containers should be restarted.
   */
  async getApiLive(): Promise<
    Result<Types.APIControllersLivenessOutput, ApiError>
  > {
    const url = "/api/live";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersLivenessOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Detailed dependency health check
   *
   * Provides comprehensive health status of all external dependencies including databases, APIs, caches, and message queues.
   */
  async getHealthDependencies(): Promise<
    Result<Types.APIControllersDependencyHealthOutput, ApiError>
  > {
    const url = "/health/dependencies";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersDependencyHealthOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Detailed dependency health check
   *
   * Provides comprehensive health status of all external dependencies including databases, APIs, caches, and message queues.
   */
  async getApiHealthDependencies(): Promise<
    Result<Types.APIControllersDependencyHealthOutput, ApiError>
  > {
    const url = "/api/health/dependencies";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.APIControllersDependencyHealthOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Prometheus metrics endpoint
   *
   * Exposes application metrics in Prometheus text format for monitoring, alerting, and observability dashboards.
   */
  async getMetrics(): Promise<Result<void, ApiError>> {
    const url = "/metrics";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createHealthModule(client: ApiClient): HealthModule {
  return new HealthModule(client);
}
