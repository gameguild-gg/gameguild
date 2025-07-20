'use client';

import type React from 'react';

import { useState } from 'react';

interface ResizeOptions {
  minHeight?: number;
  maxHeight?: number;
}

export function useResize(initialHeight: number, options: ResizeOptions = {}) {
  const [height, setHeight] = useState(initialHeight);
  const { minHeight = 50, maxHeight = Number.POSITIVE_INFINITY } = options;

  const handleResize = (e: React.MouseEvent, initialY: number) => {
    e.preventDefault();

    const startResize = (e: MouseEvent) => {
      const newHeight = height + (e.clientY - initialY);
      if (newHeight >= minHeight && newHeight <= maxHeight) {
        setHeight(newHeight);
      }
    };

    const stopResize = () => {
      document.removeEventListener('mousemove', startResize);
      document.removeEventListener('mouseup', stopResize);
    };

    document.addEventListener('mousemove', startResize);
    document.addEventListener('mouseup', stopResize);
  };

  return {
    height,
    setHeight,
    handleResize,
  };
}
