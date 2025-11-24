import React from 'react';
import { PortfolioFilterCanvas } from '@/components/portfolio/filter/portfolio-filter-canvas';
import { PortfolioFilterRoot } from '@/components/portfolio/filter/portfolio-filter-root';

type Props = {};

const PortfolioFilter: React.FunctionComponent<Props> & {
  //
} = ({}: Readonly<Props>) => {
  return (
    <PortfolioFilterRoot>
      <PortfolioFilterCanvas />
      {/*<PortfolioFilterContent>*/}

      {/*</PortfolioFilterContent>*/}
    </PortfolioFilterRoot>
  );
};

export { PortfolioFilter };
