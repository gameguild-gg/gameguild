import { TestingLabSettings } from '@/components/testing-lab/testing-lab-settings';
import React from 'react';


export default async function Page(): Promise<React.JSX.Element> {
  return <TestingLabSettings initialSection="locations" sectionOnly />;
}
