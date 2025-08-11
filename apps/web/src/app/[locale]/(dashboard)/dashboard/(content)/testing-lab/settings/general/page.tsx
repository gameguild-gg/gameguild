import React from 'react';
import { TestingLabSettings } from '@/components/testing-lab/testing-lab-settings';

export default async function Page(): Promise<React.JSX.Element> {
  return <TestingLabSettings initialSection="general" />;
}
