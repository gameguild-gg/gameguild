import { environment } from '@/configs/environment';
import { OAuthSignInRequest, RefreshTokenRequest, RefreshTokenResponse, SignInResponse } from '@/types/auth';

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

    const response = await fetch(url, config);

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Auth endpoints
  async googleSignIn(tokenData: OAuthSignInRequest): Promise<SignInResponse> {
    return this.request<SignInResponse>('/auth/google/callback', {
      method: 'POST',
      body: JSON.stringify(tokenData),
    });
  }

  // Google ID Token validation for NextAuth.js integration
  async googleIdTokenSignIn(tokenData: { idToken: string; tenantId?: string }): Promise<SignInResponse> {
    return this.request<SignInResponse>('/auth/google/id-token', {
      method: 'POST',
      body: JSON.stringify(tokenData),
    });
  }

  async refreshToken(data: RefreshTokenRequest): Promise<RefreshTokenResponse> {
    return this.request<RefreshTokenResponse>('/auth/refresh-token', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  // Admin login for development/testing (uses regular sign-in endpoint)
  async adminLogin(credentials: { email: string; password: string }): Promise<SignInResponse> {
    return this.request<SignInResponse>('/auth/sign-in', {
      method: 'POST',
      body: JSON.stringify({
        email: credentials.email,
        password: credentials.password,
        tenantId: null, // Let backend choose the tenant
      }),
    });
  }

  async revokeToken(refreshToken: string): Promise<void> {
    await this.request('/auth/revoke-token', {
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
