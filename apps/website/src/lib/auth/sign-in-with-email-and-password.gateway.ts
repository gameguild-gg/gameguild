import { HttpClient } from '@/lib/core/http';
import { SignInWithEmailAndPasswordAction } from '@/lib/auth/sign-in-with-email-and-password.action';

export class SignInWithEmailAndPasswordGateway implements SignInWithEmailAndPasswordAction {
  constructor(readonly httpClient: HttpClient) {}

  async signInWithEmailAndPassword(email: Readonly<string>, password: Readonly<string>): Promise<any> {
    const response = await this.httpClient.request({
      method: 'POST',
      // TODO: Use the correct URL for the sign-in endpoint.
      url: 'http://localhost:3000/auth/sign-in',
      body: {
        email: email,
        password: password,
      },
    });

    // TODO: Implement the response handling logic.
    return {};
  }
}
