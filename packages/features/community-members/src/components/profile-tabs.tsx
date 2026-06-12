'use client';

import { Card, CardContent, CardHeader } from '@game-guild/ui/components/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import { Code, Settings } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import type { ReactNode } from 'react';
import type { MemberActivity, MemberProjectSummary, MemberSkill } from '../types';
import { AboutSection } from './about-section';
import { ActivityFeed } from './activity-feed';
import { CommunityStats } from './community-stats';
import { ProjectCard } from './project-card';
import { SkillsSection } from './skills-section';
import { SpecializationsBadges } from './specializations-badges';

interface ProfileTabsProps {
  username: string;
  displayName: string;
  initials: string;
  joinDate: Date;
  bio?: string;
  featuredProject?: MemberProjectSummary;
  portfolioProjects: MemberProjectSummary[];
  technicalSkills: MemberSkill[];
  toolsSkills: MemberSkill[];
  activities: MemberActivity[];
  myProjectsSlot?: ReactNode;
}

export function ProfileTabs({
  username,
  displayName,
  initials,
  joinDate,
  bio,
  featuredProject,
  portfolioProjects,
  technicalSkills,
  toolsSkills,
  activities,
  myProjectsSlot,
}: ProfileTabsProps) {
  const searchParams = useSearchParams();
  const defaultTab = searchParams.get('tab') ?? 'portfolio';

  return (
    <Tabs defaultValue={defaultTab} className="space-y-6">
      <TabsList className="bg-slate-800/50 border-purple-500/20">
        <TabsTrigger value="portfolio" className="data-[state=active]:bg-purple-600">
          Portfolio
        </TabsTrigger>
        {myProjectsSlot ? (
          <TabsTrigger value="projects" className="data-[state=active]:bg-purple-600">
            My Projects
          </TabsTrigger>
        ) : null}
        <TabsTrigger value="skills" className="data-[state=active]:bg-purple-600">
          Skills
        </TabsTrigger>
        <TabsTrigger value="activity" className="data-[state=active]:bg-purple-600">
          Activity
        </TabsTrigger>
        <TabsTrigger value="about" className="data-[state=active]:bg-purple-600">
          About
        </TabsTrigger>
      </TabsList>

      <TabsContent value="portfolio" className="space-y-6">
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
          {featuredProject ? (
            <ProjectCard
              name={featuredProject.name}
              description={featuredProject.description}
              tech={featuredProject.tech}
              rating={featuredProject.rating}
              imageUrl={featuredProject.imageUrl}
              url={featuredProject.url}
              featured
              imageHeight="h-48"
              colSpan="md:col-span-2 lg:col-span-2"
            />
          ) : null}
          {portfolioProjects.map((project, index) => (
            <ProjectCard
              key={project.id ?? project.slug ?? index}
              name={project.name}
              description={project.description}
              tech={project.tech}
              rating={project.rating}
              imageUrl={project.imageUrl}
              url={project.url}
            />
          ))}
        </div>
      </TabsContent>

      {myProjectsSlot ? (
        <TabsContent value="projects" className="space-y-6">
          {myProjectsSlot}
        </TabsContent>
      ) : null}

      <TabsContent value="skills" className="space-y-6">
        <div className="grid md:grid-cols-2 gap-6">
          <SkillsSection
            title="Technical Skills"
            icon={Code}
            iconColor="text-purple-400"
            skills={technicalSkills}
            levelColor="text-purple-400"
          />
          <SkillsSection
            title="Tools & Platforms"
            icon={Settings}
            iconColor="text-indigo-400"
            skills={toolsSkills}
            levelColor="text-indigo-400"
          />
        </div>

        <Card className="bg-slate-800/50 border-purple-500/20">
          <CardHeader>
            <h3 className="text-lg font-semibold text-white">Specializations</h3>
          </CardHeader>
          <CardContent>
            <SpecializationsBadges />
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="activity" className="space-y-6">
        <ActivityFeed
          activities={activities}
          username={username}
          displayName={displayName}
          initials={initials}
        />
      </TabsContent>

      <TabsContent value="about" className="space-y-6">
        <div className="grid md:grid-cols-2 gap-6">
          <AboutSection displayName={displayName} joinDate={joinDate} bio={bio} />
          <CommunityStats displayName={displayName} joinDate={joinDate} />
        </div>
      </TabsContent>
    </Tabs>
  );
}
