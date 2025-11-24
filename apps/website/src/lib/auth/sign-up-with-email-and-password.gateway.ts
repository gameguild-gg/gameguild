import { HttpClient } from '@/lib/core/http';
import { SignUpWithEmailAndPassword } from '@/lib/auth/sign-up-with-email-and-password.action';

export class SignUpWithEmailAndPasswordGateway implements SignUpWithEmailAndPassword {
  constructor(readonly httpClient: HttpClient) {}

  async signUpWithEmailAndPassword(email: Readonly<string>, password: Readonly<string>): Promise<any> {
    const response = await this.httpClient.request({
      method: 'POST',
      // TODO: Use the correct URL for the sign-up endpoint.
      url: 'http://localhost:3000/auth/sign-up',
      body: {
        email: email,
        password: password,
      },
    });

    // TODO: Implement the response handling logic.
    return {};
  }
}
