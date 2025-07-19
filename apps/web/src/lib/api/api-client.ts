import { environment } from '@/configs/environment';
import { OAuthSignInRequest, RefreshTokenRequest, RefreshTokenResponse, SignInResponse } from '@/components/legacy/types/auth';

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
