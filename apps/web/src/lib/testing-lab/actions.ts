'use server';

import { getToken } from '@/auth';
import { revalidatePath } from 'next/cache';

export async function submitTestingBuild(formData: FormData): Promise<void> {
  const token = await getToken();
  if (!token) return;

  const title = String(formData.get('title') ?? '').trim();
  const teamIdentifier = String(formData.get('teamIdentifier') ?? '').trim();
  const versionNumber = String(formData.get('versionNumber') ?? '').trim();
  if (!title || !teamIdentifier || !versionNumber) return;

  const description = String(formData.get('description') ?? '').trim();
  const downloadUrl = String(formData.get('downloadUrl') ?? '').trim();
  const instructionsContent = String(formData.get('instructionsContent') ?? '').trim();
  const feedbackFormContent = String(formData.get('feedbackFormContent') ?? '').trim();
  const maxTestersRaw = String(formData.get('maxTesters') ?? '').trim();
  const startDate = String(formData.get('startDate') ?? '').trim();
  const endDate = String(formData.get('endDate') ?? '').trim();
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

  const response = await fetch(`${apiUrl}/v1/testing/submit-simple`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      title,
      teamIdentifier,
      versionNumber,
      description: description || null,
      downloadUrl: downloadUrl || null,
      instructionsType: 0,
      instructionsContent: instructionsContent || null,
      feedbackFormContent: feedbackFormContent || null,
      maxTesters: maxTestersRaw ? Number(maxTestersRaw) : null,
      startDate: startDate ? new Date(startDate).toISOString() : null,
      endDate: endDate ? new Date(endDate).toISOString() : null,
    }),
  });

  if (response.ok) revalidatePath('/dashboard/testing-lab');
}
