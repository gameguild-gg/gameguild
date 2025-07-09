import { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { UserProfile } from '@/components/profile';

interface PageProps {
  params: {
    username: string;
    locale: string;
  };
}

// Mock function to check if user exists
async function getUserExists(username: string): Promise<boolean> {
  // In a real app, this would check your database
  const validUsernames = ['johndoe', 'janedoe', 'alexsmith', 'admin', 'user'];
  return validUsernames.includes(username.toLowerCase());
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { username } = await params;
  const userExists = await getUserExists(username);
  
  if (!userExists) {
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
  const userExists = await getUserExists(username);
  
  if (!userExists) {
    notFound();
  }

  return (
    <div className="container mx-auto max-w-6xl px-4 py-8">
      <UserProfile username={username} />
    </div>
  );
}
