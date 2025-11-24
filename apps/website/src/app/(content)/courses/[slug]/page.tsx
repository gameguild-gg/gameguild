import { PropsWithSlugParams } from '@/types';
import React from 'react';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;

  return (
    <div>
      <h1>Course: {slug}</h1>
    </div>
  );
}
