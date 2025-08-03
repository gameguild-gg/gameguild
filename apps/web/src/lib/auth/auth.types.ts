export interface SignInResponse {
  user: {
    id: string;
    username?: string;
    email?: string;
  };

  accessToken: string;

  refreshToken: string;

  expiresAt: string;

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

  expiresAt: string;
}
