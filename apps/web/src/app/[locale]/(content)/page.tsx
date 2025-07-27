import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <div className="flex items-center justify-center h-screen">
      <h1 className="text-2xl font-bold">This is the home page</h1>
    </div>
  );
}
