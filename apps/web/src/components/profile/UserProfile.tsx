'use client';

import React from 'react';
import Image from 'next/image';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Progress } from '@/components/ui/progress';
import Link from 'next/link';

interface UserProfileProps {
  username: string;
}

// Mock data - in a real app, this would come from an API
const getUserData = (username: string) => ({
  id: '1',
  username: username,
  name: username.charAt(0).toUpperCase() + username.slice(1) + ' User',
  email: `${username}@example.com`,
  avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
  bio: 'Passionate game developer and educator. Love creating interactive experiences and teaching others.',
  joinDate: '2023-01-15',
  location: 'San Francisco, CA',
  website: `https://${username}.dev`,
  github: username,
  twitter: `@${username}`,
  stats: {
    coursesCompleted: 12,
    projectsCreated: 8,
    totalPoints: 2450,
    rank: 'Expert Developer',
  },
});

const mockCourses = [
  {
    id: '1',
    title: 'Unity Game Development Fundamentals',
    progress: 85,
    status: 'In Progress',
    thumbnail: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?w=300&h=200&fit=crop',
    completedAt: null,
    enrolledAt: '2024-06-01',
  },
  {
    id: '2',
    title: 'Advanced JavaScript for Games',
    progress: 100,
    status: 'Completed',
    thumbnail: 'https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=300&h=200&fit=crop',
    completedAt: '2024-05-15',
    enrolledAt: '2024-04-01',
  },
];

const UserProfile: React.FC<UserProfileProps> = ({ username }) => {
  // In a real app, you would fetch user data based on the username
  // For now, we'll use mock data but display the username from the URL
  const mockUser = getUserData(username);
  return (
    <div className="container mx-auto max-w-6xl px-4 py-8">
      {/* Profile Header */}
      <div className="mb-8">
        <Card>
          <CardContent className="p-8">
            <div className="flex flex-col md:flex-row gap-6 items-start">
              <div className="flex-shrink-0">
                <Image
                  src={mockUser.avatar}
                  alt={mockUser.name}
                  width={128}
                  height={128}
                  className="w-32 h-32 rounded-full object-cover border-4 border-green-500"
                />
              </div>
              
              <div className="flex-1">
                <div className="flex flex-col md:flex-row md:items-center md:justify-between mb-4">
                  <div>
                    <h1 className="text-3xl font-bold mb-2">{mockUser.name}</h1>
                    <p className="text-zinc-600 dark:text-zinc-400 mb-2">{mockUser.bio}</p>
                    <div className="flex flex-wrap gap-2 text-sm text-zinc-500">
                      <span>📍 {mockUser.location}</span>
                      <span>📅 Joined {new Date(mockUser.joinDate).toLocaleDateString()}</span>
                    </div>
                  </div>
                  
                  <div className="flex gap-2 mt-4 md:mt-0">
                    <Button variant="outline" size="sm">
                      Edit Profile
                    </Button>
                    <Button variant="outline" size="sm">
                      Share Profile
                    </Button>
                  </div>
                </div>
                
                {/* Stats */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div className="text-center p-3 bg-zinc-50 dark:bg-zinc-800 rounded-lg">
                    <div className="text-2xl font-bold text-green-600">{mockUser.stats.coursesCompleted}</div>
                    <div className="text-sm text-zinc-600 dark:text-zinc-400">Courses</div>
                  </div>
                  <div className="text-center p-3 bg-zinc-50 dark:bg-zinc-800 rounded-lg">
                    <div className="text-2xl font-bold text-blue-600">{mockUser.stats.projectsCreated}</div>
                    <div className="text-sm text-zinc-600 dark:text-zinc-400">Projects</div>
                  </div>
                  <div className="text-center p-3 bg-zinc-50 dark:bg-zinc-800 rounded-lg">
                    <div className="text-2xl font-bold text-purple-600">{mockUser.stats.totalPoints}</div>
                    <div className="text-sm text-zinc-600 dark:text-zinc-400">Points</div>
                  </div>
                  <div className="text-center p-3 bg-zinc-50 dark:bg-zinc-800 rounded-lg">
                    <div className="text-sm font-bold text-orange-600">{mockUser.stats.rank}</div>
                    <div className="text-sm text-zinc-600 dark:text-zinc-400">Rank</div>
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tabs Content */}
      <Tabs defaultValue="courses" className="w-full">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="courses">Courses</TabsTrigger>
          <TabsTrigger value="projects">Projects</TabsTrigger>
          <TabsTrigger value="achievements">Achievements</TabsTrigger>
          <TabsTrigger value="activity">Activity</TabsTrigger>
        </TabsList>

        {/* Courses Tab */}
        <TabsContent value="courses" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {mockCourses.map((course) => (
              <Card key={course.id} className="overflow-hidden">
                <div className="aspect-video">
                  <Image src={course.thumbnail} alt={course.title} width={300} height={200} className="w-full h-full object-cover" />
                </div>
                <CardContent className="p-4">
                  <div className="flex justify-between items-start mb-2">
                    <h3 className="font-semibold text-lg line-clamp-2">{course.title}</h3>
                    <Badge variant={course.status === 'Completed' ? 'default' : 'secondary'}>{course.status}</Badge>
                  </div>
                  
                  <div className="space-y-2">
                    <div className="flex justify-between text-sm text-zinc-600 dark:text-zinc-400">
                      <span>Progress</span>
                      <span>{course.progress}%</span>
                    </div>
                    <Progress value={course.progress} className="h-2" />
                  </div>
                  
                  <div className="mt-4 flex justify-between items-center">
                    <span className="text-sm text-zinc-500">Enrolled: {new Date(course.enrolledAt).toLocaleDateString()}</span>
                    <Button asChild size="sm">
                      <Link href={`/courses/catalog`}>{course.status === 'Completed' ? 'Review' : 'Continue'}</Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        {/* Projects Tab */}
        <TabsContent value="projects" className="space-y-6">
          <div className="flex justify-between items-center">
            <h2 className="text-2xl font-bold">My Projects</h2>
            <Button asChild>
              <Link href="/projects/new">Create New Project</Link>
            </Button>
          </div>
          
          <div className="text-center py-12">
            <p className="text-zinc-500">No projects yet. Create your first project!</p>
          </div>
        </TabsContent>

        {/* Achievements Tab */}
        <TabsContent value="achievements" className="space-y-6">
          <div>
            <h2 className="text-2xl font-bold mb-6">Achievements</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <Card className="text-center p-6">
                <div className="text-4xl mb-2">🎓</div>
                <h3 className="font-semibold mb-2">First Course Completed</h3>
                <p className="text-sm text-zinc-500">Unlocked: Apr 15, 2024</p>
              </Card>
              <Card className="text-center p-6">
                <div className="text-4xl mb-2">🚀</div>
                <h3 className="font-semibold mb-2">Project Creator</h3>
                <p className="text-sm text-zinc-500">Unlocked: May 20, 2024</p>
              </Card>
            </div>
          </div>
        </TabsContent>

        {/* Activity Tab */}
        <TabsContent value="activity" className="space-y-6">
          <div>
            <h2 className="text-2xl font-bold mb-6">Recent Activity</h2>
            <div className="space-y-4">
              <Card className="p-4">
                <div className="flex items-center gap-3">
                  <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                  <div className="flex-1">
                    <p className="text-sm">
                      <span className="font-medium">Completed course:</span> Advanced JavaScript for Games
                    </p>
                    <p className="text-xs text-zinc-500">2 days ago</p>
                  </div>
                </div>
              </Card>
            </div>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default UserProfile;
