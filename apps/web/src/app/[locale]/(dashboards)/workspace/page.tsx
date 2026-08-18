import { WorkspaceHub } from '@/components/workspace/workspace-hub';
import React from 'react';

export default async function Page({}: PageProps<'/[locale]/workspace'>): Promise<React.JSX.Element> {
  return <WorkspaceHub />;
}
