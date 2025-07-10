'use server';

import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';

export async function adminLogin() {
  const cookieStore = await cookies();

  // Set a simple admin session cookie for development
  cookieStore.set('admin-session', 'true', {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    maxAge: 60 * 60 * 24, // 24 hours
  });

  // Redirect to tenant management
  redirect('/dashboard/tenant?admin=true');
}
