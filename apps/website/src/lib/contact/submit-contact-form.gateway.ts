import { HttpClient } from '@/lib/core/http';
import { SubmitContactForm } from '@/lib/contact/submit-contact-form';

export class SubmitContactFormGateway implements SubmitContactForm {
  constructor(readonly httpClient: HttpClient) {}

  async submitContactForm(): Promise<any> {
    const response = await this.httpClient.request({
      method: 'POST',
      // TODO: Use the correct URL for the contact endpoint.
      url: 'http://localhost:3000/contact',
      body: {},
    });

    // TODO: Implement the response handling logic.
    return {};
  }
}
