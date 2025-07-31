import { OAuthSignInRequest, RefreshTokenRequest, RefreshTokenResponse, SignInResponse } from '@/components/legacy/types/auth';
import { environment } from '@/configs/environment';

class ApiClient {
    private baseUrl: string;

    constructor() {
        this.baseUrl = environment.apiBaseUrl;
    }

    async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
        const url = `${this.baseUrl}${endpoint}`;
        const config: RequestInit = {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers,
            },
            ...options,
        };

        console.log('🌐 [API CLIENT] Making request:', {
            url,
            method: options.method || 'GET',
            hasBody: !!options.body,
            baseUrl: this.baseUrl,
        });

        try {
            const response = await fetch(url, config);

            console.log('🌐 [API CLIENT] Response received:', {
                url,
                status: response.status,
                statusText: response.statusText,
                ok: response.ok,
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                const errorMessage = errorData.message || `HTTP error! status: ${response.status}`;
                console.error('🌐 [API CLIENT] Request failed:', {
                    url,
                    status: response.status,
                    statusText: response.statusText,
                    errorData,
                    errorMessage,
                });
                throw new Error(errorMessage);
            }

            return response.json();
        } catch (error) {
            console.error('🌐 [API CLIENT] Request error:', {
                url,
                error: error instanceof Error ? error.message : 'Unknown error',
                errorType: error?.constructor?.name || 'Unknown',
                cause: (error as any)?.cause,
                stack: error instanceof Error ? error.stack : undefined,
                baseUrl: this.baseUrl,
                endpoint,
            });
            throw error;
        }
    }

    // Auth endpoints
    async googleSignIn(tokenData: OAuthSignInRequest): Promise<SignInResponse> {
        return this.request<SignInResponse>('/api/auth/google/callback', {
            method: 'POST',
            body: JSON.stringify(tokenData),
        });
    }

    // Google ID Token validation for NextAuth.js integration
    async googleIdTokenSignIn(tokenData: { idToken: string; tenantId?: string }): Promise<SignInResponse> {
        return this.request<SignInResponse>('/api/auth/google/id-token', {
            method: 'POST',
            body: JSON.stringify(tokenData),
        });
    }

    async refreshToken(data: RefreshTokenRequest): Promise<RefreshTokenResponse> {
        const tokenPrefix = data.refreshToken?.length > 20 ? data.refreshToken.substring(0, 20) : (data.refreshToken ?? 'null');
        console.log('🔥 [API CLIENT] refreshToken called with:', {
            tokenPrefix: `${tokenPrefix}...`,
            tokenLength: data.refreshToken?.length ?? 0,
            hasToken: !!data.refreshToken,
            endpoint: '/api/auth/refresh',
        });

        return this.request<RefreshTokenResponse>('/api/auth/refresh', {
            method: 'POST',
            body: JSON.stringify(data),
        });
    }

    // Admin login for development/testing (uses regular sign-in endpoint)
    async adminLogin(credentials: { email: string; password: string }): Promise<SignInResponse> {
        return this.request<SignInResponse>('/api/auth/signin', {
            method: 'POST',
            body: JSON.stringify({
                email: credentials.email,
                password: credentials.password,
                tenantId: null, // Let backend choose the tenant
            }),
        });
    }

    async revokeToken(refreshToken: string): Promise<void> {
        await this.request('/api/auth/revoke', {
            method: 'POST',
            body: JSON.stringify({ refreshToken }),
        });
    }

    // Authenticated requests
    async authenticatedRequest<T>(endpoint: string, accessToken: string, options: RequestInit = {}): Promise<T> {
        return this.request<T>(endpoint, {
            ...options,
            headers: {
                Authorization: `Bearer ${accessToken}`,
                ...options.headers,
            },
        });
    }
}

export const apiClient = new ApiClient();
