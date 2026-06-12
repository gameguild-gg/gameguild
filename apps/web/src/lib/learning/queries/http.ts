import { getToken } from '@/auth';

export async function learningApiGet<T>(path: string, revalidate = 60): Promise<T | null> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  const token = await getToken();
  const response = await fetch(`${apiUrl}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    next: { revalidate },
  });

  if (response.status === 404 || response.status === 403 || response.status === 401) {
    return null;
  }

  if (!response.ok) {
    console.error(`[learningApiGet] ${path} failed with ${response.status}`);
    return null;
  }

  return (await response.json()) as T;
}
