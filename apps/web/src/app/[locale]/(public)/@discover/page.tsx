import React from 'react';
import { FeedSection } from '../feed-section';

export default async function DiscoverSlot(): Promise<React.JSX.Element> {
  return <FeedSection kind="discover" />;
}
