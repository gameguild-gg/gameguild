'use server';

import { CONTENT_FILTER_COOKIE_KEY } from '@/lib/content/content-filter.constant';
import { ContentFilterState, defaultContentFilterState } from '@/lib/content/types';
import { cookies } from 'next/headers';

export async function getInitialContentFilterState(): Promise<ContentFilterState> {
  const cookieStore = await cookies();
  const cookie = cookieStore.get(CONTENT_FILTER_COOKIE_KEY);

  let state: ContentFilterState = defaultContentFilterState;

  if (cookie) {
    try {
      const parsedCookie = JSON.parse(cookie.value);
      state = { ...parsedCookie };
    } catch (error) {
      // TODO: Log the error to a logging service.
      console.error('Failed to parse content filter cookie', error);
    }
  }

  return state;
}
