import { ProfileHeader, ProfileTabs } from '@game-guild/community-members';
import { notFound } from 'next/navigation';
import React from 'react';
import { getPublicMemberProfile } from '@/lib/community/queries/members';

export default async function Page({
  params,
}: PageProps<'/[locale]/members/[member]'>): Promise<React.JSX.Element> {
  const { member } = await params;
  const profile = await getPublicMemberProfile(member);

  if (!profile) notFound();

  return (
    <div className="flex min-h-screen flex-col">
      <ProfileHeader
        username={profile.username}
        displayName={profile.displayName}
        initials={profile.initials}
        joinDate={profile.joinDate}
        headline={profile.headline}
        location={profile.location}
        avatarUrl={profile.avatarUrl}
        bannerUrl={profile.bannerUrl}
        stats={profile.stats}
      />
      <div className="mx-auto w-full max-w-7xl px-6 py-8">
        <ProfileTabs
          username={profile.username}
          displayName={profile.displayName}
          initials={profile.initials}
          joinDate={profile.joinDate}
          bio={profile.bio}
          featuredProject={profile.featuredProject}
          portfolioProjects={profile.portfolioProjects}
          technicalSkills={profile.technicalSkills}
          toolsSkills={profile.toolsSkills}
          activities={profile.activities}
        />
      </div>
    </div>
  );
}
