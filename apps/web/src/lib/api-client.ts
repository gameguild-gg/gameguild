/**
 * API Client for server-side actions
 * Used to make requests to the backend API
 */

// Default API base URL (configured based on environment)
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

/**
 * Enhanced fetch function with typings and defaults
 */
class ApiClient {
    private baseUrl: string;

    constructor(baseUrl: string = API_BASE_URL) {
        this.baseUrl = baseUrl;
    }

    /**
     * Make a GET request
     */
    async get(endpoint: string, options: RequestInit = {}): Promise<Response> {
        return this.request(endpoint, {
            ...options,
            method: 'GET'
        });
    }

    /**
     * Make a POST request
     */
    async post(endpoint: string, options: RequestInit = {}): Promise<Response> {
        return this.request(endpoint, {
            ...options,
            method: 'POST'
        });
    }

    /**
     * Make a PUT request
     */
    async put(endpoint: string, options: RequestInit = {}): Promise<Response> {
        return this.request(endpoint, {
            ...options,
            method: 'PUT'
        });
    }

    /**
     * Make a PATCH request
     */
    async patch(endpoint: string, options: RequestInit = {}): Promise<Response> {
        return this.request(endpoint, {
            ...options,
            method: 'PATCH'
        });
    }

    /**
     * Make a DELETE request
     */
    async delete(endpoint: string, options: RequestInit = {}): Promise<Response> {
        return this.request(endpoint, {
            ...options,
            method: 'DELETE'
        });
    }

    /**
     * Make a request with the given method and options
     */
    private async request(endpoint: string, options: RequestInit): Promise<Response> {
        const url = `${this.baseUrl}${endpoint}`;

        const headers = {
            'Accept': 'application/json',
            ...options.headers,
        };

        try {
            const response = await fetch(url, {
                ...options,
                headers,
                cache: 'no-store'  // Disable caching for server-side actions
            });

            return response;
        } catch (error) {
            console.error('API request error:', error);
            throw error;
        }
    }
}

// Export a singleton instance
export const apiClient = new ApiClient();
