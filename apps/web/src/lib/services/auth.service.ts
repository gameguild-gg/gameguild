import { apiClient } from '../api-client';
import { 
  SignInResponse, 
  LocalSignInRequest, 
  LocalSignUpRequest, 
  RefreshTokenRequest,
  RefreshTokenResponse 
} from '@/types/auth';

export class AuthService {
  // Local sign in (email/password)
  static async localSignIn(credentials: LocalSignInRequest): Promise<SignInResponse> {
    return apiClient.request<SignInResponse>('/auth/sign-in', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
  }

  // Local sign up
  static async localSignUp(userData: LocalSignUpRequest): Promise<SignInResponse> {
    return apiClient.request<SignInResponse>('/auth/sign-up', {
      method: 'POST',
      body: JSON.stringify(userData),
    });
  }

  // Refresh access token
  static async refreshToken(refreshData: RefreshTokenRequest): Promise<RefreshTokenResponse> {
    return apiClient.refreshToken(refreshData);
  }

  // Revoke refresh token (sign out)
  static async revokeToken(refreshToken: string): Promise<void> {
    return apiClient.revokeToken(refreshToken);
  }

  // Send email verification
  static async sendEmailVerification(email: string, tenantId?: string): Promise<void> {
    await apiClient.request('/auth/send-email-verification', {
      method: 'POST',
      body: JSON.stringify({ email, tenantId }),
    });
  }

  // Verify email
  static async verifyEmail(token: string): Promise<void> {
    await apiClient.request('/auth/verify-email', {
      method: 'POST',
      body: JSON.stringify({ token }),
    });
  }

  // Forgot password
  static async forgotPassword(email: string, tenantId?: string): Promise<void> {
    await apiClient.request('/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify({ email, tenantId }),
    });
  }

  // Reset password
  static async resetPassword(token: string, newPassword: string): Promise<void> {
    await apiClient.request('/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify({ token, newPassword }),
    });
  }

  // Change password (authenticated)
  static async changePassword(
    oldPassword: string, 
    newPassword: string, 
    accessToken: string
  ): Promise<void> {
    await apiClient.authenticatedRequest('/auth/change-password', accessToken, {
      method: 'POST',
      body: JSON.stringify({ oldPassword, newPassword }),
    });
  }
}
