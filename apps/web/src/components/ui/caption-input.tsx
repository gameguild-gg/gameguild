'use client';

import React from 'react';
import { Textarea } from '@/components/ui/textarea';

export interface CaptionInputProps {
  caption: string;
  onChange: (newCaption: string) => void;
  autoFocus?: boolean;
}

export function CaptionInput({ caption, onChange, autoFocus = false }: CaptionInputProps) {
  return (
    <Textarea
      value={caption}
      onChange={(e) => onChange(e.target.value)}
      placeholder="Add a caption"
      autoFocus={autoFocus}
      className="w-full"
    />
  );
}