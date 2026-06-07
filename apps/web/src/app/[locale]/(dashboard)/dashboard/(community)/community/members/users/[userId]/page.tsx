import { Link } from '@/i18n/navigation';
import { getMember } from '@/lib/community';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowLeft, Award, BriefcaseBusiness, Calendar, Clock, Globe, Mail, MapPin, Phone, User, Users } from 'lucide-react';
import { notFound } from 'next/navigation';
import React from 'react';

interface Props {
  params: Promise<{ userId: string }>;
}

export default async function UserDetailPage({ params }: Props): Promise<React.JSX.Element> {
  const { userId } = await params;
  const member = await getMember(userId);

  if (!member) {
    notFound();
  }

  const profileStats = [
    { label: 'Followers', value: member.followerCount, icon: Users },
    { label: 'Following', value: member.followingCount, icon: Users },
    { label: 'Posts', value: member.postCount, icon: Calendar },
    { label: 'Projects', value: member.projectCount, icon: BriefcaseBusiness },
  ].filter((stat) => typeof stat.value === 'number');

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
          <p className="text-muted-foreground">@{member.handle ?? member.username}</p>
          {member.headline && <p className="mt-1 text-sm text-muted-foreground">{member.headline}</p>}
        </div>
        {member.availabilityStatus && member.availabilityStatus !== 'NotSet' && <Badge variant="outline">{member.availabilityStatus}</Badge>}
        <Badge variant={member.status === 'active' ? 'default' : member.status === 'banned' ? 'destructive' : 'secondary'}>{member.status}</Badge>
        <Badge variant={member.role === 'admin' ? 'default' : member.role === 'moderator' ? 'secondary' : 'outline'}>{member.role}</Badge>
      </div>

      {profileStats.length > 0 && (
        <div className="grid gap-4 md:grid-cols-4">
          {profileStats.map((stat) => (
            <Card key={stat.label}>
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium">{stat.label}</CardTitle>
                <stat.icon className="size-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{stat.value}</div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

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

      {member.skills.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Award className="size-5" />
              Skills
            </CardTitle>
            <CardDescription>Public skills from the member social profile</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {member.skills.map((skill) => (
                <Badge key={skill.id ?? skill.name} variant="secondary">
                  {skill.name}
                  {skill.proficiency ? ` · ${skill.proficiency}` : ''}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {member.portfolioItems.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BriefcaseBusiness className="size-5" />
              Portfolio
            </CardTitle>
            <CardDescription>Public work attached to the member social profile</CardDescription>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            {member.portfolioItems.map((item) => (
              <div key={item.id ?? item.title} className="rounded-lg border p-4">
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <p className="font-medium">{item.title}</p>
                    {item.description && <p className="mt-1 text-sm text-muted-foreground">{item.description}</p>}
                  </div>
                  {item.isPinned && <Badge variant="outline">Pinned</Badge>}
                </div>
                {item.url && (
                  <a href={item.url} target="_blank" rel="noopener noreferrer" className="mt-3 inline-flex text-sm text-primary hover:underline">
                    View project
                  </a>
                )}
              </div>
            ))}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
