import React from 'react';
import { FeedSection } from '../feed-section';

export default async function FollowingSlot(): Promise<React.JSX.Element> {
  return <FeedSection kind="following" />;
}
