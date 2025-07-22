import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Activity, BookOpen, DollarSign, Settings, User, UserMinus, UserPlus } from 'lucide-react';
import { getRecentActivity, type ActivityItem } from '@/lib/feed/feed.actions';

const activityIcons = {
  user_joined: UserPlus,
  post_created: BookOpen,
  course_completed: Activity,
  achievement_earned: Settings,
  user_created: UserPlus,
  user_updated: User,
  user_deleted: UserMinus,
  course_enrolled: BookOpen,
  payment_received: DollarSign,
  content_interaction: Activity,
  settings_changed: Settings,
};

const activityColors = {
  user_joined: 'text-green-400',
  post_created: 'text-orange-400',
  course_completed: 'text-blue-400',
  achievement_earned: 'text-yellow-400',
  user_created: 'text-green-400',
  user_updated: 'text-blue-400',
  user_deleted: 'text-red-400',
  course_enrolled: 'text-purple-400',
  payment_received: 'text-green-400',
  content_interaction: 'text-indigo-400',
  settings_changed: 'text-slate-400',
};

const activityBadgeColors = {
  user_joined: 'bg-green-500/10 text-green-400 border-green-500/20',
  post_created: 'bg-orange-500/10 text-orange-400 border-orange-500/20',
  course_completed: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
  achievement_earned: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/20',
  user_created: 'bg-green-500/10 text-green-400 border-green-500/20',
  user_updated: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
  user_deleted: 'bg-red-500/10 text-red-400 border-red-500/20',
  course_enrolled: 'bg-purple-500/10 text-purple-400 border-purple-500/20',
  payment_received: 'bg-green-500/10 text-green-400 border-green-500/20',
  content_interaction: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20',
  settings_changed: 'bg-slate-500/10 text-slate-400 border-slate-500/20',
};

function ActivityIcon({ type, className }: { type: ActivityItem['type']; className?: string }) {
  const Icon = activityIcons[type];
  const colorClass = activityColors[type];
  return <Icon className={`${colorClass} ${className}`} />;
}

function ActivityBadge({ type }: { type: ActivityItem['type'] }) {
  const colorClass = activityBadgeColors[type];
  const label = type.replace('_', ' ').replace(/\b\w/g, (l) => l.toUpperCase());
  
  return (
    <Badge variant="outline" className={`${colorClass} text-xs`}>
      {label}
    </Badge>
  );
}

interface ServerRecentActivityProps {
  limit?: number;
}

export async function ServerRecentActivity({ limit = 10 }: ServerRecentActivityProps) {
  const result = await getRecentActivity(limit);
  
  if (!result.success) {
    return (
      <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
        <CardHeader>
          <CardTitle className="text-base text-white flex items-center gap-2">
            <Activity className="h-4 w-4" />
            Recent Activity
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8">
            <Activity className="mx-auto h-8 w-8 mb-2 text-red-400" />
            <p className="text-slate-400">Failed to load recent activity</p>
            <p className="text-xs text-slate-500 mt-1">{result.error}</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const { activities } = result.data!;

  return (
    <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="text-base text-white flex items-center gap-2">
            <Activity className="h-4 w-4" />
            Recent Activity
          </CardTitle>
          <Badge variant="outline" className="bg-slate-700/50 text-slate-300 border-slate-600">
            {activities.length} items
          </Badge>
        </div>
      </CardHeader>
      <CardContent>
        {activities.length === 0 ? (
          <div className="text-center py-8">
            <Activity className="mx-auto h-8 w-8 mb-2 text-slate-400" />
            <p className="text-slate-400">No recent activity</p>
            <p className="text-xs text-slate-500 mt-1">Activity will appear here as users interact with the platform</p>
          </div>
        ) : (
          <ScrollArea className="h-[400px] pr-4">
            <div className="space-y-4">
              {activities.map((activity) => (
                <div key={activity.id} className="flex items-start gap-3 p-3 rounded-lg bg-slate-700/20 hover:bg-slate-700/30 transition-colors">
                  <div className="flex-shrink-0">
                    {activity.user?.avatar ? (
                      <Avatar className="h-8 w-8">
                        <AvatarImage src={activity.user.avatar} alt={activity.user.name} />
                        <AvatarFallback className="bg-slate-600 text-slate-200 text-xs">{activity.user.name.charAt(0).toUpperCase()}</AvatarFallback>
                      </Avatar>
                    ) : (
                      <div className="h-8 w-8 rounded-full bg-slate-600 flex items-center justify-center">
                        <ActivityIcon type={activity.type} className="h-4 w-4" />
                      </div>
                    )}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <h4 className="text-sm font-medium text-white truncate">{activity.title}</h4>
                      <ActivityBadge type={activity.type} />
                    </div>
                    
                    <p className="text-xs text-slate-400 mb-2 line-clamp-2">{activity.description}</p>
                    
                    {activity.metadata && (
                      <div className="flex items-center gap-3 text-xs text-slate-500 mb-1">
                        {activity.metadata.postTitle && (
                          <span className="flex items-center gap-1">
                            <BookOpen className="h-3 w-3" />
                            {activity.metadata.postTitle}
                          </span>
                        )}
                        {activity.metadata.courseName && (
                          <span className="flex items-center gap-1">
                            <BookOpen className="h-3 w-3" />
                            {activity.metadata.courseName}
                          </span>
                        )}
                        {activity.metadata.achievementName && (
                          <span className="flex items-center gap-1 text-yellow-400">
                            <Settings className="h-3 w-3" />
                            {activity.metadata.achievementName}
                          </span>
                        )}
                      </div>
                    )}
                    
                    <span className="text-xs text-slate-500">{activity.timestamp}</span>
                  </div>
                </div>
              ))}
            </div>
          </ScrollArea>
        )}
      </CardContent>
    </Card>
  );
}

export function RecentActivityLoading() {
  return (
    <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
      <CardHeader>
        <CardTitle className="text-base text-white flex items-center gap-2">
          <Activity className="h-4 w-4" />
          Recent Activity
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="flex items-start gap-3 p-3 rounded-lg bg-slate-700/20">
              <div className="h-8 w-8 rounded-full bg-slate-600 animate-pulse" />
              <div className="flex-1 space-y-2">
                <div className="h-4 bg-slate-600 rounded animate-pulse" />
                <div className="h-3 bg-slate-700 rounded animate-pulse w-3/4" />
                <div className="h-3 bg-slate-700 rounded animate-pulse w-1/2" />
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
