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
