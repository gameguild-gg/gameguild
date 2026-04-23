import React from 'react';

export default async function Page({
  params,
}: PageProps<'/[locale]/members/[member]/projects/[project]'>): Promise<React.JSX.Element> {
  const { member, project } = await params;

  // TODO: implement real project detail page (uses authenticated client API).
  return (
    <div className="flex flex-col min-h-screen">
      <div className="flex-1 flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold mb-2">Project</h1>
          <p className="text-muted-foreground">
            Project <span className="font-mono">{project}</span> for member{' '}
            <span className="font-mono">{member}</span> is under construction.
          </p>
        </div>
      </div>
    </div>
  );
}
