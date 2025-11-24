'use server';

import { SignUpFormState } from '@/lib/auth/sign-up-form-state';
import { SignUpWithEmailAndPasswordGateway } from '@/lib/auth/sign-up-with-email-and-password.gateway';
import { httpClientFactory } from '@/lib/core/http';

export type SignUpWithEmailAndPassword = {
  signUpWithEmailAndPassword: (email: Readonly<string>, password: Readonly<string>) => Promise<any>;
};

export async function signUpWithEmailAndPassword(previousState: SignUpFormState, formData: FormData): Promise<SignUpFormState> {
  // TODO: Implement sign-up with email and password.
  const gateway = new SignUpWithEmailAndPasswordGateway(httpClientFactory());

  const email = formData.get('email') as string;
  const password = formData.get('password') as string;

  const response = await gateway.signUpWithEmailAndPassword(email, password);
  return Promise.resolve({});
}
