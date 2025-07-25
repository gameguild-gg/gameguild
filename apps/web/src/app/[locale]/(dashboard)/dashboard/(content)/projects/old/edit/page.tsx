type Props = {
  params: Promise<{
    slug: string;
  }>;
};

import React from 'react';
import ProjectForm from '@/components/legacy/projects/project-form';

export default async function Component({ params }: Readonly<Props>) {
  const { slug } = await params;
  return <ProjectForm action={'update'} slug={slug} />;
}
