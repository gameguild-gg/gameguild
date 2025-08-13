import { notFound } from 'next/navigation';
import { getUserByUsername } from '@/lib/api/users';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Calendar, Mail, User } from 'lucide-react';

interface UserProfilePageProps {
  params: {
    locale: string;
    username: string;
  };
}

export default async function UserProfilePage({ params }: UserProfilePageProps) {
  const { username } = params;
  
  try {
    const user = await getUserByUsername(username);
    
    if (!user) {
      notFound();
    }

    // Don't show deleted/inactive users
    if (user.isDeleted || !user.isActive) {
      notFound();
    }

    return (
      <div className="container mx-auto py-8 px-4">
        <div className="max-w-4xl mx-auto">
          <Card>
            <CardHeader>
              <div className="flex items-center space-x-4">
                <Avatar className="h-20 w-20">
                  <AvatarImage src={`/avatars/${user.username}.png`} alt={user.name} />
                  <AvatarFallback className="text-lg">
                    {user.name?.charAt(0)?.toUpperCase() || 'U'}
                  </AvatarFallback>
                </Avatar>
                <div className="space-y-2">
                  <div>
                    <h1 className="text-2xl font-bold">{user.name}</h1>
                    <p className="text-muted-foreground">@{user.username}</p>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Badge variant={user.isActive ? 'default' : 'secondary'}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </div>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-6">
                <Separator />
                
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <div className="flex items-center space-x-2">
                      <User className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Profile Information</span>
                    </div>
                    <div className="pl-6 space-y-1">
                      <p className="text-sm">
                        <span className="font-medium">Name:</span> {user.name}
                      </p>
                      <p className="text-sm">
                        <span className="font-medium">Username:</span> @{user.username}
                      </p>
                    </div>
                  </div>
                  
                  <div className="space-y-2">
                    <div className="flex items-center space-x-2">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Member Since</span>
                    </div>
                    <div className="pl-6">
                      <p className="text-sm">
                        {new Date(user.createdAt).toLocaleDateString('en-US', {
                          year: 'numeric',
                          month: 'long',
                          day: 'numeric'
                        })}
                      </p>
                    </div>
                  </div>
                </div>

                <Separator />
                
                <div className="text-center text-muted-foreground">
                  <p className="text-sm">
                    This is a public profile page for {user.name}. 
                    Additional profile information may be available to authenticated users.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  } catch (error) {
    console.error('Error loading user profile:', error);
    notFound();
  }
}

// Generate metadata for SEO
export async function generateMetadata({ params }: UserProfilePageProps) {
  const { username } = params;
  
  try {
    const user = await getUserByUsername(username);
    
    if (!user || user.isDeleted || !user.isActive) {
      return {
        title: 'User Not Found - Game Guild',
        description: 'The requested user profile could not be found.',
      };
    }

    return {
      title: `${user.name} (@${user.username}) - Game Guild`,
      description: `View ${user.name}'s profile on Game Guild. Member since ${new Date(user.createdAt).getFullYear()}.`,
    };
  } catch (error) {
    return {
      title: 'User Profile - Game Guild',
      description: 'User profile page on Game Guild platform.',
    };
  }
}
