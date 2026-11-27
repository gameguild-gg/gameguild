import { redirect } from 'next/navigation';

import { MAX_VERIFICATION_TOKEN_LENGTH } from '@/lib/auth/email-verification';

// The verification email links to /verify-email?token=… — the API builds that
// URL. The page that consumes the token is /verify; the proxy resolves the
// visitor's locale, exactly as it does for /login. Only the token travels on,
// so nothing else in the incoming query is ever reflected.
export default async function VerifyEmailRedirectPage({
  params,
  searchParams,
}: PageProps<'/[locale]/verify-email'>): Promise<never> {
  const [, query] = await Promise.all([params, searchParams]);
  const token = typeof query?.token === 'string' ? query.token.trim() : '';

  if (!token || token.length > MAX_VERIFICATION_TOKEN_LENGTH) {
    redirect('/verify');
  }

  redirect(`/verify?token=${encodeURIComponent(token)}`);
}
