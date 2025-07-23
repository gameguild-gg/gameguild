'use client';

import React, { useState } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export interface CaptionInputProps {
  caption: string;
  onCaptionChange: (caption: string) => void;
  placeholder?: string;
  className?: string;
}

export function CaptionInput({ caption, onCaptionChange, placeholder = 'Enter caption...', className }: CaptionInputProps) {
  return (
    <div className={className}>
      <Label className="text-sm font-medium">Caption</Label>
      <Input value={caption} onChange={(e) => onCaptionChange(e.target.value)} placeholder={placeholder} className="mt-1" />
    </div>
  );
}
