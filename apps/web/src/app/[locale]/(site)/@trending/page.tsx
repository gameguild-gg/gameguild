import React from 'react';
import { FeedSection } from '../feed-section';

export default async function TrendingSlot(): Promise<React.JSX.Element> {
  return <FeedSection kind="trending" />;
}
