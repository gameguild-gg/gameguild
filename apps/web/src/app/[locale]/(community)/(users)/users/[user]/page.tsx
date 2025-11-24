import { ProfileHeader } from "@/components/profile/profile-header";
import { ProfileTabs } from '@/components/profile/profile-tabs';
import { getUserByUsername } from '@/lib/api/users';
import { notFound } from 'next/navigation';

interface Props {
  params: Promise<{ user: string; }>;
}

export async function generateStaticParams(): Promise<{ user: string; }[]> {
  // TODO: Use an API Key to fetch usernames from your database.
  // TODO: Replace with actual usernames from your database.
  const users = ['john_doe', 'jane_smith', 'gamer123'];

  return users.map(user => ({ user }));
}

export default async function Page({ params }: Props) {
  const { user } = await params;
  const userData = await getUserByUsername(user);

  if (!userData || userData.isDeleted || !userData.isActive) notFound();

  // Extract display name and initials from user data
  const displayName = userData.name || user;
  const initials = displayName.split(' ').map(n => n[0]).join('').toUpperCase() || 'U';
  const joinDate = new Date(userData.createdAt);

  return (
    <div className="flex flex-col min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <ProfileHeader
        username={user}
        displayName={displayName}
        initials={initials}
        joinDate={joinDate}
      />

      {/* Profile Section */}
      <div className="flex-1">
        <div className="relative overflow-hidden bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
          <div className="absolute inset-0 bg-gradient-to-r from-blue-600/10 to-indigo-600/10" />
          <div className="relative max-w-7xl mx-auto px-6 py-8">
            <div className="space-y-4">
              <p className="text-gray-300">
                Community member and game enthusiast. Part of the Game Guild community since {joinDate.getFullYear()}.
                Active participant in discussions and collaborative projects.
              </p>
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="max-w-7xl mx-auto px-6 py-8">
          <ProfileTabs
            userId={userData.id || ''}
            username={user}
            displayName={displayName}
            initials={initials}
            joinDate={joinDate}
          />
        </div>
      </div>
    </div>
  );
}
