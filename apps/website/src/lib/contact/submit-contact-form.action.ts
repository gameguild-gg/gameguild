'use server';

import { ContactFormState } from '@/lib/contact/contact-form-state';

export async function submitContactForm(previousState: ContactFormState, formData: FormData): Promise<ContactFormState> {
  // TODO: Implement submitting the contact form.
  return Promise.resolve({});
}
