import UserProfile from '@/components/legacy/user-profile';
import { getUserByUsername, userExists } from '@/lib/users/users.actions';
import { Metadata } from 'next';
import { notFound } from 'next/navigation';

interface PageProps {
  params: Promise<{
    username: string;
    locale: string;
  }>;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { username } = await params;
  const exists = await userExists(username);

  if (!exists) {
    return {
      title: 'User Not Found',
    };
  }

  return {
    title: `${username} - User Profile | Game Guild`,
    description: `View ${username}'s profile, courses, projects, and achievements on Game Guild.`,
  };
}

export default async function UserProfilePage({ params }: PageProps) {
  const { username } = await params;

  console.log('🔍 [USER PAGE] Loading profile for username:', username);

  // Get user data from the backend API
  const user = await getUserByUsername(username);

  if (!user) {
    console.log('❌ [USER PAGE] User not found:', username);
    notFound();
  }

  console.log('✅ [USER PAGE] User found:', user.name || user.email);

  return (
    <div className="container mx-auto px-4 py-8">
      <UserProfile user={user} />
    </div>
  );
}
