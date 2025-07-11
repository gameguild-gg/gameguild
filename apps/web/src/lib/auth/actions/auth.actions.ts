'use server';

import { httpClientFactory } from '@/lib/core/http';
import { environment } from '@/configs/environment';
import { auth } from '@/auth';

export type AuthActionState = {
  error?: string;
  success?: boolean;
  message?: string;
};

export async function sendEmailVerification(previousState: AuthActionState, formData: FormData): Promise<AuthActionState> {
  try {
    const email = formData.get('email') as string;
    const tenantId = formData.get('tenantId') as string | undefined;

    if (!email) {
      return {
        error: 'Email is required',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const response = await httpClient.request({
      method: 'POST',
      url: `${environment.apiBaseUrl}/auth/send-email-verification`,
      body: {
        email,
        tenantId: tenantId || undefined,
      },
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Failed to send verification email',
        success: false,
      };
    }

    return {
      success: true,
      message: 'Verification email sent successfully',
    };
  } catch (error) {
    console.error('Send email verification error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function verifyEmail(previousState: AuthActionState, formData: FormData): Promise<AuthActionState> {
  try {
    const token = formData.get('token') as string;

    if (!token) {
      return {
        error: 'Verification token is required',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const response = await httpClient.request({
      method: 'POST',
      url: `${environment.apiBaseUrl}/auth/verify-email`,
      body: { token },
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Failed to verify email',
        success: false,
      };
    }

    return {
      success: true,
      message: 'Email verified successfully',
    };
  } catch (error) {
    console.error('Verify email error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function forgotPassword(previousState: AuthActionState, formData: FormData): Promise<AuthActionState> {
  try {
    const email = formData.get('email') as string;
    const tenantId = formData.get('tenantId') as string | undefined;

    if (!email) {
      return {
        error: 'Email is required',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const response = await httpClient.request({
      method: 'POST',
      url: `${environment.apiBaseUrl}/auth/forgot-password`,
      body: {
        email,
        tenantId: tenantId || undefined,
      },
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Failed to send reset email',
        success: false,
      };
    }

    return {
      success: true,
      message: 'Password reset email sent successfully',
    };
  } catch (error) {
    console.error('Forgot password error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function resetPassword(previousState: AuthActionState, formData: FormData): Promise<AuthActionState> {
  try {
    const token = formData.get('token') as string;
    const newPassword = formData.get('newPassword') as string;
    const confirmPassword = formData.get('confirmPassword') as string;

    if (!token || !newPassword) {
      return {
        error: 'Reset token and new password are required',
        success: false,
      };
    }

    if (newPassword !== confirmPassword) {
      return {
        error: 'Passwords do not match',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const response = await httpClient.request({
      method: 'POST',
      url: `${environment.apiBaseUrl}/auth/reset-password`,
      body: {
        token,
        newPassword,
      },
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Failed to reset password',
        success: false,
      };
    }

    return {
      success: true,
      message: 'Password reset successfully',
    };
  } catch (error) {
    console.error('Reset password error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function changePassword(previousState: AuthActionState, formData: FormData): Promise<AuthActionState> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return {
        error: 'Not authenticated',
        success: false,
      };
    }

    const oldPassword = formData.get('oldPassword') as string;
    const newPassword = formData.get('newPassword') as string;
    const confirmPassword = formData.get('confirmPassword') as string;

    if (!oldPassword || !newPassword) {
      return {
        error: 'Old password and new password are required',
        success: false,
      };
    }

    if (newPassword !== confirmPassword) {
      return {
        error: 'New passwords do not match',
        success: false,
      };
    }

    const httpClient = httpClientFactory();

    const response = await httpClient.request({
      method: 'POST',
      url: `${environment.apiBaseUrl}/auth/change-password`,
      body: {
        oldPassword,
        newPassword,
      },
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Failed to change password',
        success: false,
      };
    }

    return {
      success: true,
      message: 'Password changed successfully',
    };
  } catch (error) {
    console.error('Change password error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}
