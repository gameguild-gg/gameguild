'use server';

import { SignInFormState } from '@/lib/auth/sign-in-form-state';
import { SignInWithEmailAndPasswordGateway } from '@/lib/auth/sign-in-with-email-and-password.gateway';
import { httpClientFactory } from '@/lib/core/http';

export type SignInWithEmailAndPasswordAction = {
  signInWithEmailAndPassword: (email: Readonly<string>, password: Readonly<string>) => Promise<any>;
};

export async function signInWithEmailAndPassword(previousState: SignInFormState, formData: FormData): Promise<SignInFormState> {
  // TODO: Implement sign-up with email and password.
  const gateway = new SignInWithEmailAndPasswordGateway(httpClientFactory());

  const email = formData.get('email') as string;
  const password = formData.get('password') as string;

  const response = await gateway.signInWithEmailAndPassword(email, password);
  return Promise.resolve({});
}
