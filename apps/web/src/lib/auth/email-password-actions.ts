'use server';

import { httpClientFactory } from '@/lib/core/http';
import { LocalSignInRequest, LocalSignUpRequest, SignInResponse } from '@/types/auth';
import { environment } from '@/configs/environment';

export type SignInFormState = {
  error?: string;
  success?: boolean;
  data?: SignInResponse;
};

export type SignUpFormState = {
  error?: string;
  success?: boolean;
  data?: SignInResponse;
};

export async function signInWithEmailAndPassword(
  previousState: SignInFormState,
  formData: FormData,
): Promise<SignInFormState> {
  try {
    const httpClient = httpClientFactory();
    
    const email = formData.get('email') as string;
    const password = formData.get('password') as string;
    const tenantId = formData.get('tenantId') as string | undefined;

    if (!email || !password) {
      return {
        error: 'Email and password are required',
        success: false,
      };
    }

    const requestData: LocalSignInRequest = {
      email,
      password,
      tenantId: tenantId || undefined,
    };

    const response = await httpClient.request<SignInResponse>({
      method: 'POST',
      url: `${environment.apiBaseUrl}/auth/sign-in`,
      body: requestData,
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Sign in failed',
        success: false,
      };
    }

    return {
      success: true,
      data: response.body,
    };
  } catch (error) {
    console.error('Sign in error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}

export async function signUpWithEmailAndPassword(
  previousState: SignUpFormState,
  formData: FormData,
): Promise<SignUpFormState> {
  try {
    const httpClient = httpClientFactory();
    
    const email = formData.get('email') as string;
    const password = formData.get('password') as string;
    const username = formData.get('username') as string;
    const tenantId = formData.get('tenantId') as string | undefined;

    if (!email || !password || !username) {
      return {
        error: 'Email, password, and username are required',
        success: false,
      };
    }

    const requestData: LocalSignUpRequest = {
      email,
      password,
      username,
      tenantId: tenantId || undefined,
    };

    const response = await httpClient.request<SignInResponse>({
      method: 'POST',
      url: `${environment.apiBaseUrl}/auth/sign-up`,
      body: requestData,
    });

    if (response.statusCode !== 200) {
      return {
        error: (response.body as any)?.message || 'Sign up failed',
        success: false,
      };
    }

    return {
      success: true,
      data: response.body,
    };
  } catch (error) {
    console.error('Sign up error:', error);
    return {
      error: error instanceof Error ? error.message : 'An unexpected error occurred',
      success: false,
    };
  }
}
