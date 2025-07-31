import { NotFound } from '@/components/common/error/not-found';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  return <NotFound />;
}
