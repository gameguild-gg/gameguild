import React, { Suspense } from 'react';
import { getAchievements, getAchievementStatistics, getUserAchievements } from '@/lib/achievements/achievements.actions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Award, Edit, Eye, Plus, Star, Target, TrendingUp, Trophy } from 'lucide-react';
import Link from 'next/link';
import Image from 'next/image';

interface AchievementsPageProps {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

// Loading component
function AchievementsLoading() {
  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div className="h-8 w-48 bg-gray-200 rounded animate-pulse" />
        <div className="h-10 w-32 bg-gray-200 rounded animate-pulse" />
      </div>

      {/* Statistics skeleton */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <Card key={i}>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <div className="h-4 w-24 bg-gray-200 rounded animate-pulse" />
                <div className="h-6 w-6 bg-gray-200 rounded animate-pulse" />
              </div>
              <div className="h-8 w-16 bg-gray-200 rounded animate-pulse" />
              <div className="h-3 w-20 bg-gray-200 rounded animate-pulse mt-2" />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}

// Error component
function AchievementsError({ error }: { error: string }) {
  return (
    <Card>
      <CardContent className="p-6">
        <div className="text-center">
          <Trophy className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Unable to load achievements</h3>
          <p className="text-gray-600 mb-4">{error}</p>
          <Button variant="outline" asChild>
            <Link href="/dashboard/achievements">Try again</Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

// Helper functions
function getAchievementTypeColor(type?: string) {
  switch (type) {
    case 'milestone':
      return 'default';
    case 'progress':
      return 'secondary';
    case 'special':
      return 'destructive';
    default:
      return 'outline';
  }
}

// Main achievements content
async function AchievementsContent({ searchParams }: AchievementsPageProps) {
  const params = await searchParams;
  const page = typeof params.page === 'string' ? parseInt(params.page) : 1;
  const limit = typeof params.limit === 'string' ? parseInt(params.limit) : 20;
  const tab = typeof params.tab === 'string' ? params.tab : 'all';

  // Fetch data
  const [achievementsResult, userAchievementsResult, statisticsResult] = await Promise.all([
    getAchievements(page, limit),
    getUserAchievements(undefined, 1, 10),
    getAchievementStatistics(),
  ]);

  if (!achievementsResult.success) {
    return <AchievementsError error={achievementsResult.error || 'Unknown error'} />;
  }

  const achievements = achievementsResult.data || [];
  const userAchievements = userAchievementsResult.data || [];
  const statistics = statisticsResult.data;

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Achievements</h1>
          <p className="text-gray-600">Manage achievements and user progress</p>
        </div>
        <Button asChild>
          <Link href="/dashboard/achievements/create">
            <Plus className="h-4 w-4 mr-2" />
            Create Achievement
          </Link>
        </Button>
      </div>

      {/* Statistics */}
      {statistics && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Total Achievements</p>
                <Trophy className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.totalAchievements}</div>
              <p className="text-xs text-muted-foreground">{statistics.activeAchievements} active</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">User Achievements</p>
                <Award className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.userAchievements}</div>
              <p className="text-xs text-muted-foreground">{statistics.completionRate.toFixed(1)}% completion</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Average Points</p>
                <Target className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.averagePoints}</div>
              <p className="text-xs text-muted-foreground">{statistics.secretAchievements} secret</p>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between space-y-0 pb-2">
                <p className="text-sm font-medium">Recent Awards</p>
                <TrendingUp className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="text-2xl font-bold">{statistics.recentAchievements}</div>
              <p className="text-xs text-muted-foreground">This week</p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Tabs */}
      <Tabs value={tab} className="space-y-4">
        <TabsList>
          <TabsTrigger value="all" asChild>
            <Link href="/dashboard/achievements?tab=all">All Achievements</Link>
          </TabsTrigger>
          <TabsTrigger value="user" asChild>
            <Link href="/dashboard/achievements?tab=user">User Achievements</Link>
          </TabsTrigger>
        </TabsList>

        {/* All Achievements Tab */}
        <TabsContent value="all" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>All Achievements</CardTitle>
            </CardHeader>
            <CardContent>
              {achievements.length === 0 ? (
                <div className="text-center py-12">
                  <Trophy className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">No achievements found</h3>
                  <p className="text-gray-600 mb-4">Create your first achievement to get started.</p>
                  <Button asChild>
                    <Link href="/dashboard/achievements/create">
                      <Plus className="h-4 w-4 mr-2" />
                      Create Achievement
                    </Link>
                  </Button>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {achievements.map((achievement) => (
                    <Card key={achievement.id} className="hover:shadow-md transition-shadow">
                      <CardContent className="p-6">
                        <div className="flex items-start justify-between mb-4">
                          <div className="flex items-center space-x-3">
                            {achievement.badgeUrl ? (
                              <Image src={achievement.badgeUrl} alt={achievement.name} width={40} height={40} className="rounded-full" />
                            ) : (
                              <div className="h-10 w-10 rounded-full bg-yellow-100 flex items-center justify-center">
                                <Trophy className="h-5 w-5 text-yellow-600" />
                              </div>
                            )}
                            <div>
                              <h3 className="font-semibold text-gray-900">{achievement.name}</h3>
                              <p className="text-sm text-gray-600">{achievement.points} points</p>
                            </div>
                          </div>
                          <div className="flex space-x-1">
                            <Button variant="ghost" size="sm" asChild>
                              <Link href={`/dashboard/achievements/${achievement.id}`}>
                                <Eye className="h-4 w-4" />
                              </Link>
                            </Button>
                            <Button variant="ghost" size="sm" asChild>
                              <Link href={`/dashboard/achievements/${achievement.id}/edit`}>
                                <Edit className="h-4 w-4" />
                              </Link>
                            </Button>
                          </div>
                        </div>
                        <p className="text-sm text-gray-600 mb-3">{achievement.description}</p>
                        <div className="flex flex-wrap gap-2">
                          <Badge variant={achievement.isActive ? 'default' : 'secondary'}>{achievement.isActive ? 'Active' : 'Inactive'}</Badge>
                          {achievement.isSecret && <Badge variant="outline">Secret</Badge>}
                          {achievement.category && <Badge variant="secondary">{achievement.category}</Badge>}
                          {achievement.type && <Badge variant={getAchievementTypeColor(achievement.type)}>{achievement.type}</Badge>}
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* User Achievements Tab */}
        <TabsContent value="user" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Recent User Achievements</CardTitle>
            </CardHeader>
            <CardContent>
              {userAchievements.length === 0 ? (
                <div className="text-center py-12">
                  <Award className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">No user achievements yet</h3>
                  <p className="text-gray-600">User achievements will appear here when earned.</p>
                </div>
              ) : (
                <div className="space-y-4">
                  {userAchievements.map((userAchievement) => (
                    <div key={userAchievement.id} className="flex items-center justify-between p-4 border rounded-lg">
                      <div className="flex items-center space-x-4">
                        {userAchievement.achievement?.badgeUrl ? (
                          <Image
                            src={userAchievement.achievement.badgeUrl}
                            alt={userAchievement.achievement.name}
                            width={48}
                            height={48}
                            className="rounded-full"
                          />
                        ) : (
                          <div className="h-12 w-12 rounded-full bg-yellow-100 flex items-center justify-center">
                            <Trophy className="h-6 w-6 text-yellow-600" />
                          </div>
                        )}
                        <div>
                          <h3 className="font-semibold text-gray-900">{userAchievement.achievement?.name || 'Unknown Achievement'}</h3>
                          <p className="text-sm text-gray-600">{userAchievement.achievement?.description}</p>
                          <div className="flex items-center space-x-2 mt-1">
                            <Badge variant={userAchievement.isCompleted ? 'default' : 'secondary'}>
                              {userAchievement.isCompleted ? 'Completed' : `${userAchievement.progress}% Progress`}
                            </Badge>
                            {userAchievement.achievement?.points && <span className="text-xs text-gray-500">{userAchievement.achievement.points} points</span>}
                          </div>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className="text-sm text-gray-600">Earned {new Date(userAchievement.earnedAt).toLocaleDateString()}</p>
                        {userAchievement.isCompleted && <Star className="h-5 w-5 text-yellow-500 ml-auto mt-1" />}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export default async function AchievementsPage({ searchParams }: AchievementsPageProps) {
  return (
    <Suspense fallback={<AchievementsLoading />}>
      <AchievementsContent searchParams={searchParams} />
    </Suspense>
  );
}
