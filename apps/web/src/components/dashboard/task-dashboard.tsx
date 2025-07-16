'use client';

import { CheckCircle } from 'lucide-react';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components';
import { Progress } from '@game-guild/ui/components';

interface TaskItem {
  id: string;
  title: string;
  category: 'Todo' | 'WIP' | 'Bugs' | 'Complete';
  progress: number;
  total: number;
  percentage: number;
  color: string;
}

interface Comment {
  id: string;
  user: {
    name: string;
    avatar?: string;
    initials: string;
  };
  message: string;
  timestamp: string;
}

interface Feature {
  title: string;
  description: string;
  icon: React.ReactNode;
}

export function TaskDashboard() {
  const tasks: TaskItem[] = [
    {
      id: '1',
      title: 'Todo',
      category: 'Todo',
      progress: 49,
      total: 150,
      percentage: 33,
      color: 'bg-orange-500',
    },
    {
      id: '2',
      title: 'WIP',
      category: 'WIP',
      progress: 49,
      total: 150,
      percentage: 33,
      color: 'bg-yellow-500',
    },
    {
      id: '3',
      title: 'Bugs',
      category: 'Bugs',
      progress: 42,
      total: 150,
      percentage: 28,
      color: 'bg-blue-500',
    },
    {
      id: '4',
      title: 'Complete',
      category: 'Complete',
      progress: 10,
      total: 150,
      percentage: 6,
      color: 'bg-gray-500',
    },
  ];

  const comments: Comment[] = [
    {
      id: '1',
      user: {
        name: 'User',
        initials: 'U',
        avatar: undefined,
      },
      message: "There's no light model!",
      timestamp: '2 min ago',
    },
    {
      id: '2',
      user: {
        name: 'User',
        initials: 'U',
        avatar: undefined,
      },
      message: "After creating a new task, if I create a new list, the new task's name is changed to the name of the list.",
      timestamp: '5 min ago',
    },
    {
      id: '3',
      user: {
        name: 'User',
        initials: 'U',
        avatar: undefined,
      },
      message: 'I NEED LIGHT MODE!!!',
      timestamp: '1 hour ago',
    },
  ];

  const features: Feature[] = [
    {
      title: 'Owned by you',
      description: 'You get the code, host it anywhere. No recurring fees or upkeep.',
      icon: <CheckCircle className="w-12 h-12 text-yellow-500" />,
    },
    {
      title: 'Progress tracking',
      description: "Easily track and analyse your team's progress.",
      icon: (
        <div className="w-12 h-12 bg-primary rounded-lg flex items-center justify-center">
          <div className="grid grid-cols-2 gap-1">
            <div className="w-2 h-2 bg-orange-500 rounded"></div>
            <div className="w-2 h-2 bg-yellow-500 rounded"></div>
            <div className="w-2 h-2 bg-blue-500 rounded"></div>
            <div className="w-2 h-2 bg-gray-500 rounded"></div>
          </div>
        </div>
      ),
    },
    {
      title: 'Public issue links',
      description: 'Create forms and accept tickets/issues from anyone via your custom domain.',
      icon: (
        <div className="w-12 h-12 bg-background border-2 border-muted rounded-lg flex items-center justify-center">
          <div className="space-y-1">
            <div className="h-1 bg-muted rounded w-8"></div>
            <div className="h-1 bg-muted rounded w-6"></div>
            <div className="h-1 bg-muted rounded w-7"></div>
          </div>
        </div>
      ),
    },
  ];

  return (
    <div className="bg-background text-foreground p-6 space-y-8">
      {/* Main Content Area */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left Side - Task Progress */}
        <div className="lg:col-span-2 space-y-6">
          {/* Code Badge */}
          <div className="relative">
            <div className="bg-muted/20 rounded-lg p-6 border border-border">
              <div className="absolute top-4 right-4">
                <div className="bg-yellow-500 rounded-lg p-3">
                  <CheckCircle className="w-6 h-6 text-white" />
                </div>
              </div>
              <div className="space-y-4">
                <div className="space-y-2">
                  <div className="h-3 bg-muted rounded w-48"></div>
                  <div className="h-2 bg-muted/60 rounded w-32"></div>
                </div>
                <div className="space-y-2">
                  <div className="h-2 bg-muted/40 rounded w-40"></div>
                  <div className="h-2 bg-muted/40 rounded w-36"></div>
                </div>
              </div>
            </div>
          </div>

          {/* Task Progress Bars */}
          <div className="space-y-4">
            {tasks.map((task) => (
              <div key={task.id} className="flex items-center gap-4">
                <div className={`w-3 h-3 rounded-full ${task.color}`}></div>
                <span className="text-sm font-medium text-foreground min-w-[60px]">{task.title}</span>
                <div className="flex-1">
                  <Progress value={task.percentage} className="h-2" />
                </div>
                <span className="text-sm text-muted-foreground min-w-[80px] text-right">
                  {task.progress}/{task.total} {task.percentage}%
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Right Side - Comments */}
        <div className="space-y-4">
          {comments.map((comment) => (
            <div key={comment.id} className="flex gap-3">
              <Avatar className="w-8 h-8 flex-shrink-0">
                <AvatarImage src={comment.user.avatar} alt={comment.user.name} />
                <AvatarFallback className="bg-muted text-muted-foreground text-xs">{comment.user.initials}</AvatarFallback>
              </Avatar>
              <div className="flex-1 min-w-0">
                <p className="text-sm text-foreground leading-relaxed">{comment.message}</p>
                <span className="text-xs text-muted-foreground">{comment.timestamp}</span>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Bottom Features Section */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-8 border-t border-border">
        {features.map((feature, index) => (
          <div key={index} className="text-center space-y-4">
            <div className="flex justify-center">{feature.icon}</div>
            <div className="space-y-2">
              <h3 className="font-semibold text-foreground">{feature.title}</h3>
              <p className="text-sm text-muted-foreground leading-relaxed">{feature.description}</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
