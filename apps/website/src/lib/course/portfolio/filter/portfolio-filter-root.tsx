import React, { PropsWithChildren } from 'react';

type Props = {};

export function PortfolioFilterRoot({ children }: Readonly<PropsWithChildren<Props>>) {
  return <div>{children}</div>;
}
