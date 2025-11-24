import React from 'react';
import { PropsWithSlugParams } from '@/types';

export default async function Page({ params }: PropsWithSlugParams): Promise<React.JSX.Element> {
  const { slug } = await params;
  
  return (
    <div>
      <h1>Course: {slug}</h1>
    </div>
  );
}
