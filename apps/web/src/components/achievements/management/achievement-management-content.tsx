'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Plus, Search, Trophy, Users, Award, Target } from 'lucide-react';
import { useEffect, useState } from 'react';
import { getAchievements, getAchievementsStatistics } from '@/lib/achievements/achievements.actions';
import type { AchievementDto, AchievementStatisticsDto } from '@/lib/core/api/generated/types.gen';
import { CreateAchievementDialog } from './create-achievement-dialog';
import { AchievementCard } from './achievement-card';

export function AchievementManagementContent() {
  const [achievements, setAchievements] = useState<AchievementDto[]>([]);
  const [statistics, setStatistics] = useState<AchievementStatisticsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);

  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      // Load achievements and statistics in parallel
      const [achievementsResponse, statisticsResponse] = await Promise.all([
        getAchievements(),
        getAchievementsStatistics(),
      ]);

      // Handle achievements response
      if (achievementsResponse.success && achievementsResponse.data?.achievements) {
        setAchievements(achievementsResponse.data.achievements);
      } else if (!achievementsResponse.success) {
        setError(achievementsResponse.error || 'Failed to load achievements');
        return;
      }

      // Handle statistics response
      if (statisticsResponse.success && statisticsResponse.data) {
        setStatistics(statisticsResponse.data);
      } else if (!statisticsResponse.success) {
        console.warn('Failed to load statistics:', statisticsResponse.error);
        // Don't fail the entire page if just statistics fail
      }
    } catch (error) {
      console.error('Error loading achievements data:', error);
      setError('An unexpected error occurred while loading data');
    } finally {
      setLoading(false);
    }
  };

  const filteredAchievements = achievements.filter(achievement =>
    achievement.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    achievement.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    achievement.category?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleAchievementCreated = () => {
    setIsCreateDialogOpen(false);
    loadData(); // Refresh the data
  };

  const handleAchievementUpdated = () => {
    loadData(); // Refresh the data
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <div className="text-muted-foreground">Loading achievements...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col justify-center items-center min-h-[400px] space-y-4">
        <div className="text-destructive text-center">
          <h3 className="text-lg font-semibold mb-2">Error Loading Achievements</h3>
          <p className="text-sm">{error}</p>
        </div>
        <Button onClick={loadData} variant="outline">
          Try Again
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Statistics Overview */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Achievements</CardTitle>
            <Trophy className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{statistics?.totalAchievements || 0}</div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Achievements</CardTitle>
            <Award className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{statistics?.activeAchievements || 0}</div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Users with Achievements</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{statistics?.usersWithAchievements || 0}</div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Awarded</CardTitle>
            <Target className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{statistics?.totalAchievementsAwarded || 0}</div>
          </CardContent>
        </Card>
      </div>

      {/* Achievement Management */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Achievement Management</CardTitle>
              <CardDescription>
                Create, edit, and manage achievements for your platform.
              </CardDescription>
            </div>
            <Button onClick={() => setIsCreateDialogOpen(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Create Achievement
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {/* Search */}
          <div className="flex items-center space-x-2 mb-6">
            <Search className="h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search achievements..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="max-w-sm"
            />
          </div>

          {/* Achievements Grid */}
          {filteredAchievements.length === 0 ? (
            <div className="text-center py-8">
              <Trophy className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
              <p className="text-muted-foreground mb-4">
                {searchTerm ? 'No achievements found matching your search.' : 'No achievements created yet.'}
              </p>
              {!searchTerm && (
                <Button onClick={() => setIsCreateDialogOpen(true)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Create Your First Achievement
                </Button>
              )}
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {filteredAchievements.map((achievement) => (
                <AchievementCard
                  key={achievement.id}
                  achievement={achievement}
                  onUpdate={handleAchievementUpdated}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create Achievement Dialog */}
      <CreateAchievementDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onSuccess={handleAchievementCreated}
      />
    </div>
  );
}
