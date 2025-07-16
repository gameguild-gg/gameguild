'use client';

import React from 'react';
import Image from 'next/image';
import { Card, CardContent } from '@game-guild/ui/components';
import { Button } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components';
import { Progress } from '@game-guild/ui/components';
import Link from 'next/link';
import { User } from '@/types/user';

interface UserProfileProps {
  user: User;
}

// Transform backend User to display format
const transformUserData = (user: User) => ({
  id: user.id,
  username: user.name, // Use name as username since that's what we have
  name: user.name,
  email: user.email,
  avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
  bio: 'Game developer and learner on Game Guild platform.',
  joinDate: new Date(user.createdAt).toLocaleDateString(),
  location: 'Unknown', // Not provided by backend
  website: '', // Not provided by backend
  github: '', // Not provided by backend
  twitter: '', // Not provided by backend
  isActive: user.isActive,
  balance: user.balance,
  stats: {
    coursesCompleted: 0, // Would need to be fetched from courses API
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

const UserProfile: React.FC<UserProfileProps> = ({ user }) => {
  // Transform backend User data to display format
  const userData = transformUserData(user);
  return (
    <div className="container mx-auto max-w-6xl px-4 py-8">
      {/* Profile Header */}
      <div className="mb-8">
        <Card>
          <CardContent className="p-8">
            <div className="flex flex-col md:flex-row gap-6 items-start">
              <div className="flex-shrink-0">
                <Image
                  src={userData.avatar}
                  alt={userData.name}
                  width={128}
                  height={128}
                  className="w-32 h-32 rounded-full object-cover border-4 border-green-500"
                />
              </div>

              <div className="flex-1">
                <div className="flex flex-col md:flex-row md:items-center md:justify-between mb-4">
                  <div>
                    <h1 className="text-3xl font-bold mb-2">{userData.name}</h1>
                    <p className="text-zinc-600 dark:text-zinc-400 mb-2">{userData.bio}</p>
                    <div className="flex flex-wrap gap-2 text-sm text-zinc-500">
                      <span>📍 {userData.location || 'Unknown'}</span>
                      <span>📅 Joined {userData.joinDate}</span>
                      <span>💰 Balance: ${userData.balance}</span>
                      <span>🎯 Status: {userData.isActive ? 'Active' : 'Inactive'}</span>
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
                    <div className="text-2xl font-bold text-green-600">{userData.stats.coursesCompleted}</div>
                    <div className="text-sm text-zinc-600 dark:text-zinc-400">Courses</div>
                  </div>
                  <div className="text-center p-3 bg-zinc-50 dark:bg-zinc-800 rounded-lg">
                    <div className="text-2xl font-bold text-blue-600">0</div>
                    <div className="text-sm text-zinc-600 dark:text-zinc-400">Projects</div>
                  </div>
                  <div className="text-center p-3 bg-zinc-50 dark:bg-zinc-800 rounded-lg">
                    <div className="text-2xl font-bold text-purple-600">0</div>
                    <div className="text-sm text-zinc-600 dark:text-zinc-400">Points</div>
                  </div>
                  <div className="text-center p-3 bg-zinc-50 dark:bg-zinc-800 rounded-lg">
                    <div className="text-sm font-bold text-orange-600">Beginner</div>
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
