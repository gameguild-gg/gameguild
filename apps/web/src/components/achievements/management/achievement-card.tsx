'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { MoreHorizontal, Edit, Trash2, Award } from 'lucide-react';
import Image from 'next/image';
import type { AchievementDto } from '@/lib/core/api/generated/types.gen';
import { deleteAchievement } from '@/lib/achievements/achievements.actions';
import { useState } from 'react';
import { EditAchievementDialog } from './edit-achievement-dialog';

interface AchievementCardProps {
  achievement: AchievementDto;
  onUpdate: () => void;
}

export function AchievementCard({ achievement, onUpdate }: AchievementCardProps) {
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const handleDelete = async () => {
    if (!achievement.id) return;

    if (!confirm('Are you sure you want to delete this achievement? This action cannot be undone.')) {
      return;
    }

    try {
      setIsDeleting(true);
      await deleteAchievement({
        path: { achievementId: achievement.id },
      });
      onUpdate();
    } catch (error) {
      console.error('Error deleting achievement:', error);
      alert('Failed to delete achievement. Please try again.');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleEditSuccess = () => {
    setIsEditDialogOpen(false);
    onUpdate();
  };

  return (
    <>
      <Card className="hover:shadow-md transition-shadow">
        <CardHeader className="pb-3">
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <CardTitle className="text-lg flex items-center gap-2">
                {achievement.iconUrl ? <Image src={achievement.iconUrl} alt={achievement.name || 'Achievement'} width={24} height={24} className="rounded" /> : <Award className="w-5 h-5" />}
                {achievement.name}
              </CardTitle>
              <div className="flex items-center gap-2 mt-2">
                <Badge variant={achievement.isActive ? 'default' : 'secondary'}>{achievement.isActive ? 'Active' : 'Inactive'}</Badge>
                {achievement.isSecret && <Badge variant="outline">Secret</Badge>}
                {achievement.isRepeatable && <Badge variant="outline">Repeatable</Badge>}
              </div>
            </div>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => setIsEditDialogOpen(true)}>
                  <Edit className="h-4 w-4 mr-2" />
                  Edit
                </DropdownMenuItem>
                <DropdownMenuItem onClick={handleDelete} disabled={isDeleting} className="text-destructive">
                  <Trash2 className="h-4 w-4 mr-2" />
                  {isDeleting ? 'Deleting...' : 'Delete'}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </CardHeader>
        <CardContent className="pt-0">
          <p className="text-sm text-muted-foreground mb-3 line-clamp-2">{achievement.description || 'No description provided.'}</p>

          <div className="space-y-2">
            {achievement.category && (
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Category:</span>
                <Badge variant="outline">{achievement.category}</Badge>
              </div>
            )}

            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Points:</span>
              <span className="font-medium">{achievement.points || 0}</span>
            </div>

            {achievement.type && (
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Type:</span>
                <Badge variant="outline">{achievement.type}</Badge>
              </div>
            )}

            {achievement.levels && achievement.levels.length > 0 && (
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Levels:</span>
                <span className="font-medium">{achievement.levels.length}</span>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      <EditAchievementDialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen} achievement={achievement} onSuccess={handleEditSuccess} />
    </>
  );
}
