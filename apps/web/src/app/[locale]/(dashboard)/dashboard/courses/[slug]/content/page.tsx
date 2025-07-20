'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Plus, FileText, Video, Image, Link, GripVertical, Trash2 } from 'lucide-react';
import { useCourseEditor } from '@/lib/courses/course-editor.context';
import { useState } from 'react';

interface Lesson {
  id: string;
  title: string;
  description: string;
  type: 'video' | 'text' | 'quiz' | 'assignment';
  duration: number;
  isPreview: boolean;
  order: number;
}

interface Module {
  id: string;
  title: string;
  description: string;
  lessons: Lesson[];
  order: number;
}

const LESSON_TYPES = [
  { value: 'video', label: 'Video', icon: Video },
  { value: 'text', label: 'Article', icon: FileText },
  { value: 'quiz', label: 'Quiz', icon: Link },
  { value: 'assignment', label: 'Assignment', icon: Image },
] as const;

export default function CourseContentPage() {
  const { state } = useCourseEditor();
  const [modules, setModules] = useState<Module[]>([]);
  const [showAddModule, setShowAddModule] = useState(false);
  const [newModuleTitle, setNewModuleTitle] = useState('');
  const [newModuleDescription, setNewModuleDescription] = useState('');

  const addModule = () => {
    if (!newModuleTitle.trim()) return;

    const newModule: Module = {
      id: Date.now().toString(),
      title: newModuleTitle,
      description: newModuleDescription,
      lessons: [],
      order: modules.length + 1,
    };

    setModules([...modules, newModule]);
    setNewModuleTitle('');
    setNewModuleDescription('');
    setShowAddModule(false);
  };

  const addLesson = (moduleId: string) => {
    const newLesson: Lesson = {
      id: Date.now().toString(),
      title: 'New Lesson',
      description: '',
      type: 'video',
      duration: 0,
      isPreview: false,
      order: 1,
    };

    setModules(prev => prev.map(module => {
      if (module.id === moduleId) {
        return {
          ...module,
          lessons: [...module.lessons, { ...newLesson, order: module.lessons.length + 1 }]
        };
      }
      return module;
    }));
  };

  const updateLesson = (moduleId: string, lessonId: string, updates: Partial<Lesson>) => {
    setModules(prev => prev.map(module => {
      if (module.id === moduleId) {
        return {
          ...module,
          lessons: module.lessons.map(lesson => 
            lesson.id === lessonId ? { ...lesson, ...updates } : lesson
          )
        };
      }
      return module;
    }));
  };

  const deleteLesson = (moduleId: string, lessonId: string) => {
    setModules(prev => prev.map(module => {
      if (module.id === moduleId) {
        return {
          ...module,
          lessons: module.lessons.filter(lesson => lesson.id !== lessonId)
        };
      }
      return module;
    }));
  };

  const deleteModule = (moduleId: string) => {
    setModules(prev => prev.filter(module => module.id !== moduleId));
  };

  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Header */}
      <div className="border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">Course Content</h1>
              <p className="text-sm text-muted-foreground">Organize your course into modules and lessons</p>
            </div>
            <Button onClick={() => setShowAddModule(true)} className="gap-2">
              <Plus className="h-4 w-4" />
              Add Module
            </Button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto space-y-6">
          {/* Course Structure Overview */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">📚 Course Structure</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-3 gap-4 text-center">
                <div className="p-4 rounded-lg bg-primary/5 border border-primary/20">
                  <div className="text-2xl font-bold text-primary">{modules.length}</div>
                  <div className="text-sm text-muted-foreground">Modules</div>
                </div>
                <div className="p-4 rounded-lg bg-secondary/5 border border-secondary/20">
                  <div className="text-2xl font-bold text-secondary-foreground">
                    {modules.reduce((acc, module) => acc + module.lessons.length, 0)}
                  </div>
                  <div className="text-sm text-muted-foreground">Lessons</div>
                </div>
                <div className="p-4 rounded-lg bg-accent/5 border border-accent/20">
                  <div className="text-2xl font-bold text-accent-foreground">
                    {modules.reduce((acc, module) => 
                      acc + module.lessons.reduce((lessonAcc, lesson) => lessonAcc + lesson.duration, 0), 0
                    )}m
                  </div>
                  <div className="text-sm text-muted-foreground">Total Duration</div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Add Module Form */}
          {showAddModule && (
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle>Add New Module</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <Label htmlFor="module-title">Module Title</Label>
                  <Input
                    id="module-title"
                    value={newModuleTitle}
                    onChange={(e) => setNewModuleTitle(e.target.value)}
                    placeholder="Enter module title"
                  />
                </div>
                <div>
                  <Label htmlFor="module-description">Module Description</Label>
                  <Textarea
                    id="module-description"
                    value={newModuleDescription}
                    onChange={(e) => setNewModuleDescription(e.target.value)}
                    placeholder="Describe what this module covers"
                    rows={3}
                  />
                </div>
                <div className="flex gap-2">
                  <Button onClick={addModule} disabled={!newModuleTitle.trim()}>
                    Create Module
                  </Button>
                  <Button variant="outline" onClick={() => setShowAddModule(false)}>
                    Cancel
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Modules List */}
          {modules.map((module, moduleIndex) => (
            <Card key={module.id} className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className="flex items-center gap-2">
                      <GripVertical className="h-4 w-4 text-muted-foreground" />
                      <Badge variant="secondary">Module {moduleIndex + 1}</Badge>
                    </div>
                    <div>
                      <h3 className="font-semibold">{module.title}</h3>
                      <p className="text-sm text-muted-foreground">{module.description}</p>
                    </div>
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => addLesson(module.id)}
                      className="gap-2"
                    >
                      <Plus className="h-3 w-3" />
                      Add Lesson
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => deleteModule(module.id)}
                      className="gap-2 text-destructive"
                    >
                      <Trash2 className="h-3 w-3" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                {module.lessons.length === 0 ? (
                  <div className="text-center py-8 text-muted-foreground">
                    <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p>No lessons yet. Add your first lesson to get started.</p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {module.lessons.map((lesson, lessonIndex) => {
                      const LessonIcon = LESSON_TYPES.find(type => type.value === lesson.type)?.icon || FileText;
                      return (
                        <div
                          key={lesson.id}
                          className="flex items-center gap-3 p-3 rounded-lg border bg-background/50 hover:bg-background/80 transition-colors"
                        >
                          <GripVertical className="h-4 w-4 text-muted-foreground" />
                          <Badge variant="outline" className="min-w-fit">
                            {lessonIndex + 1}
                          </Badge>
                          <LessonIcon className="h-4 w-4 text-muted-foreground" />
                          <div className="flex-1">
                            <Input
                              value={lesson.title}
                              onChange={(e) => updateLesson(module.id, lesson.id, { title: e.target.value })}
                              className="border-none bg-transparent p-0 h-auto font-medium"
                            />
                          </div>
                          <div className="flex items-center gap-2">
                            <select
                              value={lesson.type}
                              onChange={(e) => updateLesson(module.id, lesson.id, { type: e.target.value as Lesson['type'] })}
                              className="px-2 py-1 text-sm border rounded bg-background"
                            >
                              {LESSON_TYPES.map(type => (
                                <option key={type.value} value={type.value}>
                                  {type.label}
                                </option>
                              ))}
                            </select>
                            <Input
                              type="number"
                              value={lesson.duration}
                              onChange={(e) => updateLesson(module.id, lesson.id, { duration: parseInt(e.target.value) || 0 })}
                              placeholder="0"
                              className="w-16 text-sm"
                            />
                            <span className="text-xs text-muted-foreground">min</span>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => deleteLesson(module.id, lesson.id)}
                              className="text-destructive hover:text-destructive"
                            >
                              <Trash2 className="h-3 w-3" />
                            </Button>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </CardContent>
            </Card>
          ))}

          {modules.length === 0 && !showAddModule && (
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CardContent className="text-center py-12">
                <FileText className="h-16 w-16 mx-auto mb-4 text-muted-foreground opacity-50" />
                <h3 className="text-lg font-semibold mb-2">No Content Yet</h3>
                <p className="text-muted-foreground mb-4">
                  Start building your course by creating your first module
                </p>
                <Button onClick={() => setShowAddModule(true)} className="gap-2">
                  <Plus className="h-4 w-4" />
                  Create First Module
                </Button>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
