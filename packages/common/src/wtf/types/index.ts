// Common types shared across applications
export interface User {
  id: string;

  email: string;

  name: string;

  avatar?: string;

  createdAt: Date;

  updatedAt: Date;
}

export interface ApiResponse<T = any> {
  data: T;

  message?: string;

  success: boolean;
}

export interface ApiError {
  message: string;

  code: string;

  details?: Record<string, any>;
}

export interface PaginatedResponse<T> extends ApiResponse<T[]> {
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export type Theme = 'light' | 'dark' | 'system';

export type Environment = 'development' | 'staging' | 'production';
