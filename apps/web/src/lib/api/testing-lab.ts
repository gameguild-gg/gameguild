import { assertSafeServiceUrl } from '@/lib/security/safe-remote-url';
import type { TestingLocation } from './testing-types';

function getApiBaseUrl(): string {
  return process.env.NEXT_PUBLIC_API_URL ?? '';
}

export async function getTestingLocations(skip = 0, take = 50): Promise<TestingLocation[]> {
  const apiBaseUrl = getApiBaseUrl();
  const endpoint = apiBaseUrl
    ? `${apiBaseUrl}/v1/testing/locations?skip=${skip}&take=${take}`
    : `/v1/testing/locations?skip=${skip}&take=${take}`;

  try {
    const response = await fetch(assertSafeServiceUrl(endpoint, apiBaseUrl), {
      redirect: 'error',
      headers: {
        Accept: 'application/json',
      },
      credentials: 'include',
      cache: 'no-store',
    });

    if (!response.ok) {
      return [];
    }

    const data = await response.json();
    return Array.isArray(data) ? (data as TestingLocation[]) : [];
  } catch {
    return [];
  }
}

/**
 * Active testing locations for server components (issue #106 phase 1): pages
 * fetch this on the server and hand the list to client widgets as a prop, so
 * the browser never calls the locations endpoint from a mount effect.
 */
export async function getActiveTestingLocations(skip = 0, take = 50): Promise<TestingLocation[]> {
  const locations = await getTestingLocations(skip, take);
  return locations.filter((location: TestingLocation) => location.status === 1);
}
