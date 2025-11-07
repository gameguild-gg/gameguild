'use client';

import React from 'react';
import { Slider } from '@/components/ui/slider';

export interface ImageSizeControlProps {
  size: number;
  onChange: (newSize: number) => void;
}

export function ImageSizeControl({ size, onChange }: ImageSizeControlProps) {
  const handleChange = (values: number[]) => {
    const value = values[0] ?? size;
    onChange(value);
  };

  return (
    <div className="flex items-center gap-3">
      <span className="text-sm w-16">Size</span>
      <Slider value={[size]} min={10} max={100} step={1} onValueChange={handleChange} className="flex-1" />
      <span className="text-sm w-10 text-right">{size}%</span>
    </div>
  );
}