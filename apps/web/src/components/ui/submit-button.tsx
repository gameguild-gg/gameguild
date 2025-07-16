'use client';

import React from 'react';
import { useFormStatus } from 'react-dom';
import { Button } from '@game-guild/ui/components';

type Props = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  children: React.ReactNode;
};

export function SubmitButton({ children = 'Submit', onClick }: Readonly<Props>) {
  const { pending } = useFormStatus();

  return (
    <Button type="button" aria-disabled={pending} onClick={onClick}>
      {children}
    </Button>
  );
}
