'use client';

import type { AchievementDto } from '@/lib/core/api/generated/types.gen';
import { AchievementsList } from '../achievements-list';

interface AchievementManagementContentProps {
  achievements: AchievementDto[]
}

export function AchievementManagementContent({ achievements }: AchievementManagementContentProps) {
  console.log('AchievementManagementContent received achievements:', achievements.length);

  return (
    <AchievementsList achievements={achievements} />
  );
}
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
              <CardDescription>Create, edit, and manage achievements for your platform.</CardDescription>
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
            <Input placeholder="Search achievements..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="max-w-sm" />
          </div>

          {/* Achievements Grid */}
          {filteredAchievements.length === 0 ? (
            <div className="text-center py-8">
              <Trophy className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
              <p className="text-muted-foreground mb-4">{searchTerm ? 'No achievements found matching your search.' : 'No achievements created yet.'}</p>
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
                <AchievementCard key={achievement.id} achievement={achievement} onUpdate={handleAchievementUpdated} />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create Achievement Dialog */}
      <CreateAchievementDialog open={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen} onSuccess={handleAchievementCreated} />
    </div>
  );
}
