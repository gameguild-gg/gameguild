import React from 'react';
import Image from 'next/image';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { Contributor } from '@/lib/contributors';

interface ContributorCardProps {
  contributor: Contributor;
}

export const ContributorCard = async ({ contributor }: ContributorCardProps): Promise<React.JSX.Element> => {
  return (
    <article>
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader className="pb-2">
          <div className="flex items-center gap-3">
            <Image src={contributor.avatar_url} alt={contributor.name || contributor.login} width={40} height={40} className="rounded-full" />
            <div>
              <h3 className="text-white font-semibold">{contributor.name || contributor.login}</h3>
              <p className="text-gray-400 text-sm">@{contributor.login}</p>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-2">
            <div className="flex justify-between text-sm">
              <span className="text-gray-400">Contributions:</span>
              <span className="text-white">{contributor.contributions || 0}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-400">Additions:</span>
              <span className="text-green-400">+{contributor.additions || 0}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-gray-400">Deletions:</span>
              <span className="text-red-400">-{contributor.deletions || 0}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </article>
  );
};
