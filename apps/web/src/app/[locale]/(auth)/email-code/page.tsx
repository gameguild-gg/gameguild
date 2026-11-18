import { redirect } from '@/i18n/navigation';
import { resolveAllowedAuthRedirect } from '@/lib/auth/cross-domain-auth';

// Email-code is no longer a sign-in method (issue #1236): a typed code and a
// magic link both require mailbox access, so offering both at sign-in was
// redundant — the email link is the single passwordless entry. Typed codes
// remain the mechanism for re-verification flows (step-up #1191, password
// change) and the EmailCodeFlow component is preserved for a future
// revalidation surface. This route now lands legacy links and bookmarks on
// the surviving passwordless entry.
export default async function LegacyEmailCodeRedirectPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}): Promise<never> {
  const [{ locale }, query] = await Promise.all([params, searchParams]);
  const rawRedirect = Array.isArray(query?.redirectTo)
    ? query.redirectTo[0]
    : query?.redirectTo;
  const redirectTo = resolveAllowedAuthRedirect(
    typeof rawRedirect === 'string' ? rawRedirect : undefined,
  );
  const href =
    redirectTo && redirectTo !== '/'
      ? `/magic-link?redirectTo=${encodeURIComponent(redirectTo)}`
      : '/magic-link';
  redirect({ href, locale });
  throw new Error('unreachable');
}
