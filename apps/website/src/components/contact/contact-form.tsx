'use client';

import { useContactFormState } from '@/hooks/contact/use-contact-form-state.hook';
import React from 'react';

function ContactForm() {
  const [, formAction] = useContactFormState();

  return <form action={formAction}></form>;
}

export { ContactForm };
