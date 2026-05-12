// Auth types matching the CMS API
export interface User {
  id: string;
  username: string;
  email: string;
}

export interface TenantInfo {
  id: string;
  name: string;
  isActive: boolean;
}

export interface SignInResponse {
  accessToken: string;
  refreshToken: string;
  expires: string;
  user: User;
  tenantId?: string;
  availableTenants?: TenantInfo[];
}

export interface LocalSignInRequest {
  email: string;
  password: string;
  tenantId?: string;
}

export interface LocalSignUpRequest {
  email: string;
  password: string;
  username: string;
  tenantId?: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expires: string;
}

export interface OAuthSignInRequest {
  code: string;
  state?: string;
  redirectUri?: string;
  tenantId?: string;
}

// Google OAuth specific
export interface GoogleTokenRequest {
  id_token: string;
  tenantId?: string;
}
