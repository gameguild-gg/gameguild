'use client';

import React from 'react';
import { Slider } from '@/components/ui/slider';
import { Label } from '@/components/ui/label';

export interface ImageSizeControlProps {
  size: number;
  onSizeChange: (size: number) => void;
  className?: string;
}

export function ImageSizeControl({ size, onSizeChange, className }: ImageSizeControlProps) {
  return (
    <div className={className}>
      <Label className="text-sm font-medium">Size: {size}%</Label>
      <Slider
        value={[size]}
        onValueChange={(value) => onSizeChange(value[0])}
        max={200}
        min={10}
        step={10}
        className="mt-2"
      />
    </div>
  );
}
