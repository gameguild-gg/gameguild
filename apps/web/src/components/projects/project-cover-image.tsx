'use client';

import Image, { type ImageProps } from 'next/image';
import { useState } from 'react';

const FALLBACK_PROJECT_IMAGE =
  'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=1400&h=900&fit=crop';

export function ProjectCoverImage({ src, alt, onError, ...props }: ImageProps) {
  const [resolvedSource, setResolvedSource] = useState(src || FALLBACK_PROJECT_IMAGE);

  return (
    <Image
      {...props}
      src={resolvedSource}
      alt={alt}
      onError={(event) => {
        if (resolvedSource !== FALLBACK_PROJECT_IMAGE) setResolvedSource(FALLBACK_PROJECT_IMAGE);
        onError?.(event);
      }}
    />
  );
}
