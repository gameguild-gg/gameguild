import UserDetailPageClient from './UserDetailPage.client';

export default async function Page({ params }: { params: Promise<{ userId: string }> }) {
  const { userId } = await params;
  return <UserDetailPageClient userId={userId} />;
}