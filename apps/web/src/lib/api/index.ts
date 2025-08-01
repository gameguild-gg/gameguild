// Client wrapper for API calls with error handling
export interface ApiError {
  message: string;
  status?: number;
  details?: any;
}

export class ApiClient {
  getProjectsMetadata: any;
  getProject: any;
  checkProjectHash: any;
  static handleError(error: any): ApiError {
    if (error instanceof Response) {
      return {
        message: error.statusText || 'Request failed',
        status: error.status,
        details: error,
      };
    }

    if (error?.message) {
      return {
        message: error.message,
        status: error.status,
        details: error,
      };
    }

    return {
      message: 'Unknown error occurred',
      details: error,
    };
  }
}

// Re-export everything from the generated SDK
export * from './generated';
