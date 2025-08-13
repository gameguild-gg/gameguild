import { notFound } from 'next/navigation';
import { getUserByUsername } from '@/lib/api/users';
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Progress } from "@/components/ui/progress"
import Image from "next/image"
import {
  Github,
  Twitter,
  Linkedin,
  Globe,
  MapPin,
  Calendar,
  Star,
  Users,
  Trophy,
  Gamepad2,
  Code,
  Palette,
  Settings,
  MessageSquare,
  Heart,
  Share2,
  ExternalLink,
} from "lucide-react"

interface UserProfilePageProps {
  params: {
    locale: string;
    username: string;
  };
}

export default async function UserProfilePage({ params }: UserProfilePageProps) {
  const { username } = await params;
  
  try {
    const user = await getUserByUsername(username);
    
    if (!user) {
      notFound();
    }

    // Don't show deleted/inactive users
    if (user.isDeleted || !user.isActive) {
      notFound();
    }

    // Extract display name and initials from user data
    const displayName = user.name || username;
    const initials = displayName.split(' ').map(n => n[0]).join('').toUpperCase() || 'U';
    const joinDate = new Date(user.createdAt);

    return (
      <div className="flex flex-col min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
        {/* Cover Image */}
        <div className="relative h-80 md:h-96 overflow-hidden">
          <Image 
            src="/placeholder.svg?height=384&width=1200" 
            alt="Profile Cover" 
            className="w-full h-full object-cover"
            width={1200}
            height={384}
          />
          <div className="absolute inset-0 bg-gradient-to-t from-slate-900/80 via-slate-900/40 to-transparent" />
          <div className="absolute inset-0 bg-gradient-to-r from-blue-600/20 to-purple-600/20" />
          <div className="absolute inset-0 flex items-end">
            <div className="max-w-7xl mx-auto w-full px-6 pb-6">
              {/* Profile Info - Left Side */}
              <div className="flex items-end justify-between gap-8">
                <div className="flex items-end gap-4">
                  <Avatar className="w-28 h-28 border-4 border-purple-500/30 shadow-2xl">
                    <AvatarImage src={`/avatars/${username}.png`} />
                    <AvatarFallback className="text-lg bg-gradient-to-br from-indigo-600 to-blue-600 text-white">
                      {initials}
                    </AvatarFallback>
                  </Avatar>
                  <div className="space-y-2 flex-1">
                    <h1 className="text-2xl font-bold text-white">{displayName}</h1>
                    <p className="text-purple-300">Game Developer & Community Member</p>
                    <div className="flex flex-wrap gap-2">
                      <Badge className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-gradient-to-r from-blue-500/20 to-purple-500/20 border border-blue-400/30 text-blue-200 backdrop-blur-sm">
                        <Code className="w-3 h-3" />
                        Developer
                      </Badge>
                      <Badge className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-gradient-to-r from-purple-500/20 to-indigo-500/20 border border-purple-400/30 text-purple-200 backdrop-blur-sm">
                        <Palette className="w-3 h-3" />
                        Creator
                      </Badge>
                      <Badge className="inline-flex items-center gap-1 px-3 py-1 rounded-full bg-gradient-to-r from-emerald-500/20 to-teal-500/20 border border-emerald-400/30 text-emerald-200 backdrop-blur-sm">
                        <Gamepad2 className="w-3 h-3" />
                        Gamer
                      </Badge>
                    </div>
                    <div className="flex flex-wrap gap-4 text-sm text-gray-400 mt-2">
                      <div className="flex items-center gap-1">
                        <MapPin className="w-4 h-4" />
                        Member Location
                      </div>
                      <div className="flex items-center gap-1">
                        <Calendar className="w-4 h-4" />
                        Joined {joinDate.toLocaleDateString('en-US', { year: 'numeric', month: 'long' })}
                      </div>
                      <div className="flex items-center gap-1">
                        <Users className="w-4 h-4" />
                        Active member
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-2 mt-3">
                      <Button size="sm" className="px-4 py-2 bg-gradient-to-r from-blue-600/80 to-purple-600/80 text-white rounded-lg hover:from-blue-700/80 hover:to-purple-700/80 transition-all duration-300 font-medium shadow-lg border border-white/10 backdrop-blur-sm">
                        <MessageSquare className="w-4 h-4 mr-2" />
                        Message
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        className="px-4 py-2 bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-white/20 text-white rounded-lg hover:from-purple-500/30 hover:to-blue-500/30 hover:border-white/30 transition-all duration-300 font-medium backdrop-blur-sm shadow-lg"
                      >
                        <Users className="w-4 h-4 mr-2" />
                        Follow
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        className="px-4 py-2 bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-white/20 text-white rounded-lg hover:from-purple-500/30 hover:to-blue-500/30 hover:border-white/30 transition-all duration-300 font-medium backdrop-blur-sm shadow-lg"
                      >
                        <Share2 className="w-4 h-4 mr-2" />
                        Share
                      </Button>
                    </div>
                  </div>
                </div>

                {/* Small Stats Cards - Right Side */}
                <div className="hidden md:flex gap-3">
                  <div className="bg-gradient-to-br from-yellow-500/10 to-transparent border border-yellow-400/30 rounded-lg p-3 text-center backdrop-blur-sm shadow-lg">
                    <Trophy className="w-5 h-5 text-yellow-500 mx-auto mb-1" />
                    <div className="text-lg font-bold text-white">42</div>
                    <div className="text-xs text-gray-400">Points</div>
                  </div>
                  <div className="bg-gradient-to-br from-purple-500/10 to-transparent border border-purple-400/30 rounded-lg p-3 text-center backdrop-blur-sm shadow-lg">
                    <Star className="w-5 h-5 text-purple-400 mx-auto mb-1" />
                    <div className="text-lg font-bold text-white">4.7</div>
                    <div className="text-xs text-gray-400">Rating</div>
                  </div>
                  <div className="bg-gradient-to-br from-sky-500/10 to-transparent border border-sky-400/30 rounded-lg p-3 text-center backdrop-blur-sm shadow-lg">
                    <Gamepad2 className="w-5 h-5 text-sky-400 mx-auto mb-1" />
                    <div className="text-lg font-bold text-white">7</div>
                    <div className="text-xs text-gray-400">Games</div>
                  </div>
                  <div className="bg-gradient-to-br from-red-500/10 to-transparent border border-red-400/30 rounded-lg p-3 text-center backdrop-blur-sm shadow-lg">
                    <Heart className="w-5 h-5 text-red-400 mx-auto mb-1" />
                    <div className="text-lg font-bold text-white">1.2k</div>
                    <div className="text-xs text-gray-400">Likes</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

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
                {/* Featured Project */}
                <Card className="md:col-span-2 lg:col-span-2 bg-slate-800/50 border-purple-500/20 overflow-hidden">
                  <div className="relative h-48 bg-gradient-to-br from-blue-600 to-purple-600">
                    <Image
                      src="/placeholder.svg?height=192&width=400"
                      alt="Featured Project"
                      className="w-full h-full object-cover"
                      width={400}
                      height={192}
                    />
                    <Badge className="absolute top-4 left-4 bg-yellow-600 text-yellow-100">Featured</Badge>
                  </div>
                  <CardContent className="p-6">
                    <h3 className="text-xl font-bold text-white mb-2">Community Project</h3>
                    <p className="text-gray-300 mb-4">
                      A showcase of community contributions and collaborative work within the Game Guild platform.
                    </p>
                    <div className="flex flex-wrap gap-2 mb-4">
                      <Badge className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
                        Community
                      </Badge>
                      <Badge className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
                        Collaborative
                      </Badge>
                      <Badge className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
                        Open Source
                      </Badge>
                    </div>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-4 text-sm text-gray-400">
                        <span className="flex items-center gap-1">
                          <Star className="w-4 h-4" />
                          4.5
                        </span>
                        <span>Community project</span>
                      </div>
                      <Button size="sm" className="px-4 py-2 bg-gradient-to-r from-blue-600/80 to-purple-600/80 text-white rounded-lg hover:from-blue-700/80 hover:to-purple-700/80 transition-all duration-300 font-medium shadow-lg border border-white/10 backdrop-blur-sm">
                        <ExternalLink className="w-4 h-4 mr-2" />
                        View Project
                      </Button>
                    </div>
                  </CardContent>
                </Card>

                {/* Other Projects */}
                {[
                  { name: "Game Mod", tech: "Community", rating: 4.2 },
                  { name: "Tool Script", tech: "Utility", rating: 4.0 },
                  { name: "Guide Content", tech: "Educational", rating: 4.6 },
                ].map((project, index) => (
                  <Card key={index} className="bg-slate-800/50 border-purple-500/20 overflow-hidden">
                    <div className="relative h-32 bg-gradient-to-br from-slate-700 to-slate-600">
                      <Image
                        src={`/placeholder.svg?height=128&width=300`}
                        alt={project.name}
                        className="w-full h-full object-cover"
                        width={300}
                        height={128}
                      />
                    </div>
                    <CardContent className="p-4">
                      <h4 className="font-semibold text-white mb-2">{project.name}</h4>
                      <Badge className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 mb-3 backdrop-blur-sm shadow-md">
                        {project.tech}
                      </Badge>
                      <div className="flex items-center justify-between text-sm text-gray-400">
                        <span className="flex items-center gap-1">
                          <Star className="w-3 h-3" />
                          {project.rating}
                        </span>
                        <span>Community</span>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </TabsContent>

            <TabsContent value="skills" className="space-y-6">
              <div className="grid md:grid-cols-2 gap-6">
                <Card className="bg-slate-800/50 border-purple-500/20">
                  <CardHeader>
                    <h3 className="text-lg font-semibold text-white flex items-center gap-2">
                      <Code className="w-5 h-5 text-purple-400" />
                      Technical Skills
                    </h3>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    {[
                      { name: "Game Development", level: 75 },
                      { name: "Community Management", level: 85 },
                      { name: "Content Creation", level: 70 },
                      { name: "Project Management", level: 65 },
                    ].map((skill, index) => (
                      <div key={index}>
                        <div className="flex justify-between text-sm mb-1">
                          <span className="text-gray-300">{skill.name}</span>
                          <span className="text-purple-400">{skill.level}%</span>
                        </div>
                        <Progress value={skill.level} className="h-2" />
                      </div>
                    ))}
                  </CardContent>
                </Card>

                <Card className="bg-slate-800/50 border-purple-500/20">
                  <CardHeader>
                    <h3 className="text-lg font-semibold text-white flex items-center gap-2">
                      <Settings className="w-5 h-5 text-indigo-400" />
                      Tools & Platforms
                    </h3>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    {[
                      { name: "Discord", level: 90 },
                      { name: "GitHub", level: 80 },
                      { name: "Game Guild Platform", level: 95 },
                      { name: "Community Tools", level: 85 },
                    ].map((skill, index) => (
                      <div key={index}>
                        <div className="flex justify-between text-sm mb-1">
                          <span className="text-gray-300">{skill.name}</span>
                          <span className="text-indigo-400">{skill.level}%</span>
                        </div>
                        <Progress value={skill.level} className="h-2" />
                      </div>
                    ))}
                  </CardContent>
                </Card>
              </div>

              <Card className="bg-slate-800/50 border-purple-500/20">
                <CardHeader>
                  <h3 className="text-lg font-semibold text-white">Specializations</h3>
                </CardHeader>
                <CardContent>
                  <div className="flex flex-wrap gap-3">
                    {[
                      "Community Building",
                      "Game Testing",
                      "Content Creation",
                      "Collaboration",
                      "Project Documentation",
                      "User Experience",
                      "Team Coordination",
                      "Creative Problem Solving",
                    ].map((spec, index) => (
                      <Badge key={index} className="rounded-full bg-gradient-to-r from-purple-500/15 to-blue-500/15 border border-purple-400/25 text-white px-3 py-1 backdrop-blur-sm shadow-sm">
                        {spec}
                      </Badge>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="activity" className="space-y-6">
              <div className="space-y-4">
                {[
                  { action: "Joined", item: "Game Development Discussion", time: "2 hours ago", type: "community" },
                  { action: "Shared", item: "Helpful Game Design Resource", time: "1 day ago", type: "share" },
                  { action: "Commented on", item: "Community Project Proposal", time: "2 days ago", type: "comment" },
                  { action: "Participated in", item: "Weekly Community Event", time: "3 days ago", type: "event" },
                  { action: "Created", item: "New Discussion Thread", time: "1 week ago", type: "create" },
                ].map((activity, index) => (
                  <Card key={index} className="bg-slate-800/50 border-purple-500/20">
                    <CardContent className="p-4">
                      <div className="flex items-center gap-3">
                        <Avatar className="w-8 h-8">
                          <AvatarImage src={`/avatars/${username}.png`} />
                          <AvatarFallback className="bg-purple-600 text-white text-xs">{initials}</AvatarFallback>
                        </Avatar>
                        <div className="flex-1">
                          <p className="text-gray-300">
                            <span className="text-white font-medium">{displayName}</span> {activity.action}{" "}
                            <span className="text-purple-400">{activity.item}</span>
                          </p>
                          <p className="text-sm text-gray-500">{activity.time}</p>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </TabsContent>

            <TabsContent value="about" className="space-y-6">
              <div className="grid md:grid-cols-2 gap-6">
                <Card className="bg-slate-800/50 border-purple-500/20">
                  <CardHeader>
                    <h3 className="text-lg font-semibold text-white">About {displayName}</h3>
                  </CardHeader>
                  <CardContent className="space-y-4 text-gray-300">
                    <p>
                      Welcome to {displayName}&apos;s profile! This community member has been part of Game Guild since {joinDate.getFullYear()}, 
                      contributing to various discussions and projects within our gaming community.
                    </p>
                    <p>
                      As an active member of Game Guild, {displayName} participates in community events, shares knowledge, 
                      and collaborates on exciting gaming projects with fellow community members.
                    </p>
                    <p>
                      Feel free to connect and explore the various projects and contributions this member has shared 
                      with the Game Guild community.
                    </p>
                  </CardContent>
                </Card>

                <Card className="bg-slate-800/50 border-purple-500/20">
                  <CardHeader>
                    <h3 className="text-lg font-semibold text-white">Community Stats</h3>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div className="flex items-center gap-3 p-2 rounded-lg">
                      <Calendar className="w-5 h-5 text-purple-400" />
                      <div>
                        <div className="text-sm text-gray-400">Member Since</div>
                        <div className="text-white">{joinDate.toLocaleDateString('en-US', { 
                          year: 'numeric', 
                          month: 'long',
                          day: 'numeric'
                        })}</div>
                      </div>
                    </div>
                    <div className="flex items-center gap-3 p-2 rounded-lg">
                      <Users className="w-5 h-5 text-green-400" />
                      <div>
                        <div className="text-sm text-gray-400">Status</div>
                        <div className="text-white">Active Member</div>
                      </div>
                    </div>
                    <div className="flex items-center gap-3 p-2 rounded-lg">
                      <Trophy className="w-5 h-5 text-yellow-400" />
                      <div>
                        <div className="text-sm text-gray-400">Community Level</div>
                        <div className="text-white">Contributor</div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </TabsContent>
            </Tabs>
          </div>
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
  const { username } = await params;
  
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
