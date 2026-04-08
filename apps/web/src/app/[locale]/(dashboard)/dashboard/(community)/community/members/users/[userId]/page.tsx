import React from 'react';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { getMember } from '@/lib/community';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { ArrowLeft, Mail, MapPin, Globe, Clock, Phone, Calendar, User } from 'lucide-react';

interface Props {
  params: Promise<{ userId: string }>;
}

export default async function UserDetailPage({ params }: Props): Promise<React.JSX.Element> {
  const { userId } = await params;
  const member = await getMember(userId);

  if (!member) {
    notFound();
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link href="/dashboard/community/members/users">
            <ArrowLeft className="size-4" />
          </Link>
        </Button>
        <div className="flex-1">
          <h1 className="text-3xl font-bold tracking-tight">{member.displayName}</h1>
          <p className="text-muted-foreground">@{member.username}</p>
        </div>
        <Badge variant={member.status === 'active' ? 'default' : member.status === 'banned' ? 'destructive' : 'secondary'}>{member.status}</Badge>
        <Badge variant={member.role === 'admin' ? 'default' : member.role === 'moderator' ? 'secondary' : 'outline'}>{member.role}</Badge>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        {/* Profile Info */}
        <Card>
          <CardHeader>
            <CardTitle>Profile</CardTitle>
            <CardDescription>User account information</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-3">
              <User className="size-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">Display Name</p>
                <p className="text-sm text-muted-foreground">{member.displayName}</p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <Mail className="size-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">Email</p>
                <p className="text-sm text-muted-foreground">{member.email}</p>
              </div>
            </div>

            {member.phoneNumber && (
              <div className="flex items-center gap-3">
                <Phone className="size-4 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium">Phone</p>
                  <p className="text-sm text-muted-foreground">{member.phoneNumber}</p>
                </div>
              </div>
            )}

            {member.bio && (
              <div>
                <p className="text-sm font-medium">Bio</p>
                <p className="mt-1 text-sm text-muted-foreground">{member.bio}</p>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Details */}
        <Card>
          <CardHeader>
            <CardTitle>Details</CardTitle>
            <CardDescription>Location, links and preferences</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {member.location && (
              <div className="flex items-center gap-3">
                <MapPin className="size-4 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium">Location</p>
                  <p className="text-sm text-muted-foreground">{member.location}</p>
                </div>
              </div>
            )}

            {member.website && (
              <div className="flex items-center gap-3">
                <Globe className="size-4 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium">Website</p>
                  <a href={member.website} target="_blank" rel="noopener noreferrer" className="text-sm text-primary hover:underline">
                    {member.website}
                  </a>
                </div>
              </div>
            )}

            {member.timezone && (
              <div className="flex items-center gap-3">
                <Clock className="size-4 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium">Timezone</p>
                  <p className="text-sm text-muted-foreground">{member.timezone}</p>
                </div>
              </div>
            )}

            <div className="flex items-center gap-3">
              <Calendar className="size-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">Joined</p>
                <p className="text-sm text-muted-foreground">{new Date(member.joinedAt).toLocaleDateString()}</p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <Clock className="size-4 text-muted-foreground" />
              <div>
                <p className="text-sm font-medium">Last Active</p>
                <p className="text-sm text-muted-foreground">{new Date(member.lastActiveAt).toLocaleDateString()}</p>
              </div>
            </div>

            {member.updatedAt && (
              <div className="flex items-center gap-3">
                <Calendar className="size-4 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium">Last Updated</p>
                  <p className="text-sm text-muted-foreground">{new Date(member.updatedAt).toLocaleDateString()}</p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
