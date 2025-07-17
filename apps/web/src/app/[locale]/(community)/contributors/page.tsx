import React from 'react';
import { getContributors } from '@/lib/contributors';
import { ContributorCard } from '@/components/contributors';

export default async function Page(): Promise<React.JSX.Element> {
  const contributors = await getContributors();

  return (
    <div className="container">
      <main>
        <article>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6">
            {contributors.map((contributor) => (
              <ContributorCard key={contributor.username} contributor={contributor} />
            ))}
          </div>
        </article>
      </main>
    </div>
  );
}
