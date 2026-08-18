import { redirect } from 'next/navigation';

const MAP: Array<[string, string]> = [
  ['community', '/console/community'],
  ['platform', '/console/platform'],
  ['search', '/console/search'],
  ['learning', '/workspace/learning'],
  ['projects', '/workspace/projects'],
  ['teams', '/workspace/teams'],
  ['invitations', '/workspace/invitations'],
  ['settings', '/workspace/settings'],
  ['work', '/workspace/work'],
];

export default async function LegacyDashboardPathPage({
  params,
}: {
  params: Promise<{ locale: string; path: string[] }>;
}): Promise<never> {
  const { locale, path } = await params;
  const head = path[0] ?? '';
  const entry = MAP.find(([prefix]) => head === prefix);
  const suffix = path.map((segment) => encodeURIComponent(segment)).join('/');

  if (!entry) redirect(`/${locale}/workspace`);
  const rest = path.slice(1).map((segment) => encodeURIComponent(segment)).join('/');
  redirect(`/${locale}${entry[1]}${rest ? `/${rest}` : ''}${suffix ? '' : ''}`);
}
