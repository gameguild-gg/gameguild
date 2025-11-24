import { ContentFilterState, defaultContentFilterState } from '@/lib/content/types';
import { cookies } from 'next/headers';

const CONTENT_FILTER_COOKIE_KEY = 'content-filter';

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
