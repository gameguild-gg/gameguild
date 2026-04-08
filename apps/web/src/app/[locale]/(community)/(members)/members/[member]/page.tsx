// TODO: Restore member profile page when ProfileHeader, ProfileTabs, and getUserByUsername are reimplemented
import { notFound } from 'next/navigation';

interface Props {
  params: Promise<{ member: string }>;
}

export default async function Page({ params }: Props) {
  const { member } = await params;

  return (
    <div className="flex flex-col min-h-screen">
      <div className="flex-1 flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold mb-2">Member Profile</h1>
          <p className="text-muted-foreground">Profile page for {member} is under reconstruction.</p>
        </div>
      </div>
    </div>
  );
}
