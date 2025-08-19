import { redirect } from 'next/navigation';
import { routing } from '@/i18n/routing';

interface Props {
  params: { username: string };
}

export default function UserRedirect({ params }: Props) {
  const { username } = params;
  // Redirect to default-locale public profile page
  redirect(`/${routing.defaultLocale}/users/${encodeURIComponent(username)}`);
}