/**
 * API utilities and types shared across applications
 */
import { API_CONFIG, HTTP_STATUS } from '../constants';
import type { ApiError, ApiResponse, PaginatedResponse } from '../types';

export class ApiClient {
  private baseUrl: string;

  private timeout: number;

  constructor(baseUrl = API_CONFIG.baseUrl, timeout = API_CONFIG.timeout) {
    this.baseUrl = baseUrl;
    this.timeout = timeout;
  }

  async get<T>(endpoint: string, options?: RequestInit): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...options, method: 'GET' });
  }

  async post<T>(endpoint: string, data?: any, options?: RequestInit): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async put<T>(endpoint: string, data?: any, options?: RequestInit): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async patch<T>(endpoint: string, data?: any, options?: RequestInit): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PATCH',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async delete<T>(endpoint: string, options?: RequestInit): Promise<ApiResponse<T>> {
    return this.request<T>(endpoint, { ...options, method: 'DELETE' });
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<ApiResponse<T>> {
    const url = `${this.baseUrl}${endpoint}`;
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), this.timeout);

    try {
      const response = await fetch(url, {
        ...options,
        signal: controller.signal,
        headers: {
          'Content-Type': 'application/json',
          ...options.headers,
        },
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new ApiClientError(errorData.message || `HTTP ${response.status}: ${response.statusText}`, response.status.toString(), errorData);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      clearTimeout(timeoutId);

      if (error instanceof ApiClientError) {
        throw error;
      }

      if (error instanceof Error) {
        if (error.name === 'AbortError') {
          throw new ApiClientError('Request timeout', 'TIMEOUT');
        }
        throw new ApiClientError(error.message, 'NETWORK_ERROR');
      }

      throw new ApiClientError('Unknown error occurred', 'UNKNOWN_ERROR');
    }
  }
}

export class ApiClientError extends Error implements ApiError {
  public code: string;

  public details?: Record<string, any>;

  constructor(message: string, code: string, details?: Record<string, any>) {
    super(message);
    this.name = 'ApiClientError';
    this.code = code;
    this.details = details;
  }
}

// Helper functions for common API patterns
export function createApiResponse<T>(data: T, message?: string, success = true): ApiResponse<T> {
  return { data, message, success };
}

export function createPaginatedResponse<T>(data: T[], page: number, limit: number, total: number, message?: string): PaginatedResponse<T> {
  return {
    data,
    message,
    success: true,
    pagination: {
      page,
      limit,
      total,
      totalPages: Math.ceil(total / limit),
    },
  };
}

export function isApiError(error: any): error is ApiError {
  return error && typeof error.message === 'string' && typeof error.code === 'string';
}

// Default API client instance
export const apiClient = new ApiClient();

// Export HTTP status codes for convenience
export { HTTP_STATUS };
