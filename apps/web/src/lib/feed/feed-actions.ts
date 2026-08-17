'use server';

import { revalidatePath } from 'next/cache';

import {
  dismissPersonalFeedItem,
  markPersonalFeedItemViewed,
} from '@/lib/feed/personalized-feed';

export async function dismissFeedItemAction(itemId: string): Promise<void> {
  await dismissPersonalFeedItem(itemId);
  revalidatePath('/');
}

export async function markFeedItemViewedAction(itemId: string): Promise<void> {
  await markPersonalFeedItemViewed(itemId);
}
