'use client';

import { ContactFormState, submitContactForm } from '@/lib/contact';
import { useFormState } from 'react-dom';

const initialState: ContactFormState = {};

export function useContactFormState() {
  return useFormState(submitContactForm, initialState);
}
