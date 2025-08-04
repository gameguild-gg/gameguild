import React from 'react';
import { NotFound } from '@/components/common/errors/not-found';

export default async function Page(): Promise<React.JSX.Element> {
  return <NotFound />;
}
