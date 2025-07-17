'use client';

import React from 'react';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { Progress } from '@game-guild/ui/components/progress';
// import { ScrollArea } from '@game-guild/ui/components/scroll-area';
import { 
  BookOpen, 
  CheckCircle, 
  Clock, 
  Play,
  FileText,
  Upload,
  MessageSquare,
  Lock,
  ChevronDown,
  ChevronRight
} from 'lucide-react';
import { useState } from 'react';

interface ContentItem {
  id: string;
  title: string;
  type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
  status: 'locked' | 'available' | 'in-progress' | 'completed';
  duration?: number;
  order: number;
  isRequired: boolean;
  progress?: number;
  score?: number;
}

interface Module {
  id: string;
  title: string;
  description: string;
  order: number;
  items: ContentItem[];
  isLocked: boolean;
  progress: number;
}

interface ContentNavigationSidebarProps {
  modules: Module[];
  currentItem: ContentItem | null;
  onItemSelect: (item: ContentItem) => void;
}

export function ContentNavigationSidebar({ modules, currentItem, onItemSelect }: ContentNavigationSidebarProps) {
  const [expandedModules, setExpandedModules] = useState<Set<string>>(
    new Set(modules.map(m => m.id))
  );

  const toggleModule = (moduleId: string) => {
    const newExpanded = new Set(expandedModules);
    if (newExpanded.has(moduleId)) {
      newExpanded.delete(moduleId);
    } else {
      newExpanded.add(moduleId);
    }
    setExpandedModules(newExpanded);
  };

  const getStatusIcon = (item: ContentItem) => {
    switch (item.status) {
      case 'completed':
        return <CheckCircle className="h-4 w-4 text-green-500" />;
      case 'in-progress':
        return <Clock className="h-4 w-4 text-blue-500" />;
      case 'available':
        return getTypeIcon(item.type);
      case 'locked':
        return <Lock className="h-4 w-4 text-gray-500" />;
      default:
        return getTypeIcon(item.type);
    }
  };

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'lesson':
        return <BookOpen className="h-4 w-4 text-gray-400" />;
      case 'activity':
        return <Play className="h-4 w-4 text-gray-400" />;
      case 'quiz':
        return <FileText className="h-4 w-4 text-gray-400" />;
      case 'assignment':
        return <Upload className="h-4 w-4 text-gray-400" />;
      case 'peer-review':
        return <MessageSquare className="h-4 w-4 text-gray-400" />;
      default:
        return <BookOpen className="h-4 w-4 text-gray-400" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'completed':
        return 'text-green-400';
      case 'in-progress':
        return 'text-blue-400';
      case 'available':
        return 'text-white';
      case 'locked':
        return 'text-gray-500';
      default:
        return 'text-gray-400';
    }
  };

  return (
    <div className="fixed left-0 top-0 h-full w-80 bg-gray-900 border-r border-gray-800 pt-20">
      <div className="p-4 border-b border-gray-800">
        <h2 className="text-lg font-semibold text-white">Course Contents</h2>
      </div>
      
      <div className="flex-1 p-4 overflow-y-auto">
        <div className="space-y-4">
          {modules.map((module) => (
            <Card key={module.id} className="bg-gray-800 border-gray-700">
              <div className="p-4">
                <Button
                  variant="ghost"
                  className="w-full justify-between p-0 h-auto text-left hover:bg-transparent"
                  onClick={() => toggleModule(module.id)}
                >
                  <div className="flex-1">
                    <div className="flex items-center justify-between">
                      <h3 className="font-medium text-white">{module.title}</h3>
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-gray-400">
                          {Math.round(module.progress)}%
                        </span>
                        {expandedModules.has(module.id) ? (
                          <ChevronDown className="h-4 w-4 text-gray-400" />
                        ) : (
                          <ChevronRight className="h-4 w-4 text-gray-400" />
                        )}
                      </div>
                    </div>
                    <Progress value={module.progress} className="mt-2 h-1" />
                  </div>
                </Button>

                {expandedModules.has(module.id) && (
                  <div className="mt-4 space-y-2">
                    {module.items.map((item) => (
                      <Button
                        key={item.id}
                        variant="ghost"
                        className={`w-full justify-start p-2 h-auto text-left hover:bg-gray-700 ${
                          currentItem?.id === item.id ? 'bg-gray-700 border border-blue-500' : ''
                        } ${item.status === 'locked' ? 'cursor-not-allowed opacity-50' : ''}`}
                        onClick={() => item.status !== 'locked' && onItemSelect(item)}
                        disabled={item.status === 'locked'}
                      >
                        <div className="flex items-center gap-3 w-full">
                          {getStatusIcon(item)}
                          <div className="flex-1 min-w-0">
                            <div className={`text-sm font-medium ${getStatusColor(item.status)}`}>
                              {item.title}
                            </div>
                            <div className="flex items-center gap-2 mt-1">
                              {item.duration && (
                                <span className="text-xs text-gray-500 flex items-center gap-1">
                                  <Clock className="h-3 w-3" />
                                  {item.duration}m
                                </span>
                              )}
                              {item.isRequired && (
                                <Badge variant="secondary" className="text-xs px-1 py-0">
                                  Required
                                </Badge>
                              )}
                              {item.score !== undefined && (
                                <span className="text-xs text-green-400">
                                  Score: {item.score}
                                </span>
                              )}
                            </div>
                            {item.status === 'in-progress' && item.progress !== undefined && (
                              <Progress value={item.progress} className="mt-2 h-1" />
                            )}
                          </div>
                        </div>
                      </Button>
                    ))}
                  </div>
                )}
              </div>
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}
