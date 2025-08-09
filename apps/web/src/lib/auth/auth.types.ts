export interface SignInResponse {
  user: {
    id: string;
    username?: string;
    email?: string;
  };

  accessToken: string;

  refreshToken: string;

  // Legacy combined expiry (refresh token). Prefer using accessTokenExpiresAt / refreshTokenExpiresAt when available.
  expiresAt: string;
  accessTokenExpiresAt?: string;
  refreshTokenExpiresAt?: string;

  tenantId?: string;

  availableTenants?: Array<{
    id: string;
    name: string;
    isActive: boolean;
  }>;
}

export interface RefreshTokenResponse {
  tenantId?: string;

  accessToken: string;

  refreshToken: string;

  // Legacy combined expiry (refresh token). Prefer using accessTokenExpiresAt / refreshTokenExpiresAt.
  expiresAt: string;
  accessTokenExpiresAt?: string;
  refreshTokenExpiresAt?: string;
}
