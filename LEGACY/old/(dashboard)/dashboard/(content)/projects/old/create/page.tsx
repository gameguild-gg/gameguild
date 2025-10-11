import React from 'react';
import ProjectForm from '@/components/legacy/projects/project-form';

export const dynamic = 'force-dynamic';

export default async function Page() {
  return <ProjectForm action={'create'} />;
}
