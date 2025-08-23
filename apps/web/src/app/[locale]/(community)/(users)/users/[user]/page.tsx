import {Card, CardContent, CardHeader} from "@/components/ui/card"
import {Tabs, TabsContent, TabsList, TabsTrigger} from "@/components/ui/tabs"
import {getUserByUsername} from '@/lib/api/users';
import {Code, Settings} from "lucide-react"
import {notFound} from 'next/navigation';
import {ProfileHeader} from "@/components/profile/profile-header"
import {ProjectCard} from "@/components/profile/project-card"
import {SkillsSection} from "@/components/profile/skills-section"
import {ActivityFeed} from "@/components/profile/activity-feed"
import {AboutSection} from "@/components/profile/about-section"
import {CommunityStats} from "@/components/profile/community-stats"
import {SpecializationsBadges} from "@/components/profile/specializations-badges"

const PROJECTS = [
  {name: "Game Mod", tech: "Community", rating: 4.2},
  {name: "Tool Script", tech: "Utility", rating: 4.0},
  {name: "Guide Content", tech: "Educational", rating: 4.6},
]

const TECHNICAL_SKILLS = [
  {name: "Game Development", level: 75},
  {name: "Community Management", level: 85},
  {name: "Content Creation", level: 70},
  {name: "Project Management", level: 65},
]

const TOOLS_SKILLS = [
  {name: "Discord", level: 90},
  {name: "GitHub", level: 80},
  {name: "Game Guild Platform", level: 95},
  {name: "Community Tools", level: 85},
]

const ACTIVITIES = [
  {action: "Joined", item: "Game Development Discussion", time: "2 hours ago", type: "community"},
  {action: "Shared", item: "Helpful Game Design Resource", time: "1 day ago", type: "share"},
  {action: "Commented on", item: "Community Project Proposal", time: "2 days ago", type: "comment"},
  {action: "Participated in", item: "Weekly Community Event", time: "3 days ago", type: "event"},
  {action: "Created", item: "New Discussion Thread", time: "1 week ago", type: "create"},
]

interface Props {
  params: Promise<{ username: string; }>;
}

export async function generateStaticParams(): Promise<{ username: string; }[]> {
  // TODO: Use an API Key to fetch usernames from your database.
  // TODO: Replace with actual usernames from your database.
  const users = ['john_doe', 'jane_smith', 'gamer123'];

  return users.map(username => ({username}));
}

export default async function Page({params}: Props) {
  const {username} = await params;
  const user = await getUserByUsername(username);

  if (!user || user.isDeleted || !user.isActive) notFound();

  // Extract display name and initials from user data
  const displayName = user.name || username;
  const initials = displayName.split(' ').map(n => n[0]).join('').toUpperCase() || 'U';
  const joinDate = new Date(user.createdAt);

  return (
    <div className="flex flex-col min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      <ProfileHeader
        username={username}
        displayName={displayName}
        initials={initials}
        joinDate={joinDate}
      />

      {/* Profile Section */}
      <div className="flex-1">
        <div className="relative overflow-hidden bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
          <div className="absolute inset-0 bg-gradient-to-r from-blue-600/10 to-indigo-600/10"/>
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
          <Tabs defaultValue="portfolio" className="space-y-6">
            <TabsList className="bg-slate-800/50 border-purple-500/20">
              <TabsTrigger value="portfolio" className="data-[state=active]:bg-purple-600">
                Portfolio
              </TabsTrigger>
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
                <ProjectCard
                  name="Community Project"
                  description="A showcase of community contributions and collaborative work within the Game Guild platform."
                  tech="Community"
                  rating={4.5}
                  featured={true}
                  imageHeight="h-48"
                  colSpan="md:col-span-2 lg:col-span-2"
                />

                {PROJECTS.map((project, index) => (
                  <ProjectCard
                    key={index}
                    name={project.name}
                    tech={project.tech}
                    rating={project.rating}
                  />
                ))}
              </div>
            </TabsContent>

            <TabsContent value="skills" className="space-y-6">
              <div className="grid md:grid-cols-2 gap-6">
                <SkillsSection
                  title="Technical Skills"
                  icon={Code}
                  iconColor="text-purple-400"
                  skills={TECHNICAL_SKILLS}
                  levelColor="text-purple-400"
                />

                <SkillsSection
                  title="Tools & Platforms"
                  icon={Settings}
                  iconColor="text-indigo-400"
                  skills={TOOLS_SKILLS}
                  levelColor="text-indigo-400"
                />
              </div>

              <Card className="bg-slate-800/50 border-purple-500/20">
                <CardHeader>
                  <h3 className="text-lg font-semibold text-white">Specializations</h3>
                </CardHeader>
                <CardContent>
                  <SpecializationsBadges/>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="activity" className="space-y-6">
              <ActivityFeed
                activities={ACTIVITIES}
                username={username}
                displayName={displayName}
                initials={initials}
              />
            </TabsContent>

            <TabsContent value="about" className="space-y-6">
              <div className="grid md:grid-cols-2 gap-6">
                <AboutSection
                  displayName={displayName}
                  joinDate={joinDate}
                />

                <CommunityStats
                  displayName={displayName}
                  joinDate={joinDate}
                />
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </div>
  );
}
