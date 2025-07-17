import React from 'react';
import { Card, CardContent, CardHeader } from '@game-guild/ui/components/card';

type Props = {
  contributor: Contributor;
};

export const ContributorCard = async ({ contributor }: Props): Promise<React.JSX.Element> => {
  return (
    <article>
      <Card>
        <CardHeader></CardHeader>
        <CardContent></CardContent>
      </Card>
    </article>
  );
};
