import React from 'react';
import { PropsWithSlugParams } from '@/types';

export default async function Page({ params: { slug } }: PropsWithSlugParams) {
  return (
    <div>
      <h1>Course</h1>
    </div>
  );
}
