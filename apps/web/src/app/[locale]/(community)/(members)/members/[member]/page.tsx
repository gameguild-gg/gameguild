import type {
  MemberActivity,
  MemberProjectSummary,
  MemberSkill,
} from '@game-guild/community-members';
import { ProfileHeader, ProfileTabs } from '@game-guild/community-members';
import React from 'react';

// TODO: Replace placeholder data with real fetch (getUserByUsername / portfolio / skills / activity).
const PORTFOLIO_PROJECTS: MemberProjectSummary[] = [
  { name: 'Game Mod', tech: 'Community', rating: 4.2 },
  { name: 'Tool Script', tech: 'Utility', rating: 4.0 },
  { name: 'Guide Content', tech: 'Educational', rating: 4.6 },
];

const FEATURED_PROJECT: MemberProjectSummary = {
  name: 'Community Project',
  description:
    'A showcase of community contributions and collaborative work within the Game Guild platform.',
  tech: 'Community',
  rating: 4.5,
  featured: true,
};

const TECHNICAL_SKILLS: MemberSkill[] = [
  { name: 'Game Development', level: 75 },
  { name: 'Community Management', level: 85 },
  { name: 'Content Creation', level: 70 },
  { name: 'Project Management', level: 65 },
];

const TOOLS_SKILLS: MemberSkill[] = [
  { name: 'Discord', level: 90 },
  { name: 'GitHub', level: 80 },
  { name: 'Game Guild Platform', level: 95 },
  { name: 'Community Tools', level: 85 },
];

const ACTIVITIES: MemberActivity[] = [
  { action: 'Joined', item: 'Game Development Discussion', time: '2 hours ago', type: 'community' },
  { action: 'Shared', item: 'Helpful Game Design Resource', time: '1 day ago', type: 'share' },
  { action: 'Commented on', item: 'Community Project Proposal', time: '2 days ago', type: 'comment' },
  { action: 'Participated in', item: 'Weekly Community Event', time: '3 days ago', type: 'event' },
  { action: 'Created', item: 'New Discussion Thread', time: '1 week ago', type: 'create' },
];

export default async function Page({
  params,
}: PageProps<'/[locale]/members/[member]'>): Promise<React.JSX.Element> {
  const { member } = await params;

  // TODO: fetch real user data
  const displayName = member;
  const initials =
    displayName
      .split(/[\s_-]+/)
      .map((part: string) => part[0])
      .filter(Boolean)
      .join('')
      .slice(0, 2)
      .toUpperCase() || 'U';
  const joinDate = new Date();

  return (
    <div className="flex flex-col min-h-screen">
      <ProfileHeader
        username={member}
        displayName={displayName}
        initials={initials}
        joinDate={joinDate}
      />
      <div className="max-w-7xl mx-auto w-full px-6 py-8">
        <ProfileTabs
          username={member}
          displayName={displayName}
          initials={initials}
          joinDate={joinDate}
          featuredProject={FEATURED_PROJECT}
          portfolioProjects={PORTFOLIO_PROJECTS}
          technicalSkills={TECHNICAL_SKILLS}
          toolsSkills={TOOLS_SKILLS}
          activities={ACTIVITIES}
        />
      </div>
    </div>
  );
}
