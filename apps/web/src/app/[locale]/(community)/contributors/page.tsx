import React from 'react';
import { getContributors } from '@/lib/contributors';

export default async function Page(): Promise<React.JSX.Element> {
  const contributors = await getContributors();

  return (
    <div className="container">
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6">
        {contributors.map((contributor, index) => (
          <ContributorCard key={index} {...contributor} />
        ))}
      </div>
    </div>
  );
}
