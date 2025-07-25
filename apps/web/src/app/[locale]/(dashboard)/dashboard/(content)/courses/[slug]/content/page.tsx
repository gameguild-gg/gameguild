'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Collapsible, CollapsibleContent } from '@/components/ui/collapsible';
import {
  BookOpen,
  Plus,
  FileText,
  Video,
  PlayCircle,
  Clock,
  Edit,
  Trash2,
  MoreHorizontal,
  ChevronDown,
  ChevronRight,
  Eye,
  EyeOff,
  Copy,
  Move,
  ArrowUp,
  ArrowDown,
  GripVertical,
} from 'lucide-react';
import { useCourseEditor } from '@/lib/courses/course-editor.context';
import { useState } from 'react';

// Mock data types for content structure
interface MockLesson {
  id: string;
  title: string;
  description: string;
  type: 'text' | 'video' | 'quiz' | 'assignment' | 'file' | 'interactive';
  duration: number;
  status: 'draft' | 'published' | 'archived';
  visibility: 'public' | 'private' | 'premium';
}

interface MockModule {
  id: string;
  title: string;
  description: string;
  status: 'draft' | 'published' | 'archived';
  lessons: MockLesson[];
  estimatedDuration: number;
}

export default function CourseContentPage() {
  const { state } = useCourseEditor();
  const [expandedModules, setExpandedModules] = useState<Set<string>>(new Set());
  const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());
  const [bulkEditMode, setBulkEditMode] = useState(false);
  const [isAddingModule, setIsAddingModule] = useState(false);
  const [isAddingLesson, setIsAddingLesson] = useState<string | null>(null);
  const [modules, setModules] = useState<MockModule[]>([]);

  const toggleModule = (moduleId: string) => {
    const newExpanded = new Set(expandedModules);
    if (newExpanded.has(moduleId)) {
      newExpanded.delete(moduleId);
    } else {
      newExpanded.add(moduleId);
    }
    setExpandedModules(newExpanded);
  };

  const toggleSelection = (itemId: string) => {
    const newSelected = new Set(selectedItems);
    if (newSelected.has(itemId)) {
      newSelected.delete(itemId);
    } else {
      newSelected.add(itemId);
    }
    setSelectedItems(newSelected);
  };

  const handleAddModule = (moduleData: { title: string; description: string }) => {
    const newModule: MockModule = {
      id: `module-${Date.now()}`,
      title: moduleData.title || 'New Module',
      description: moduleData.description || '',
      status: 'draft',
      lessons: [],
      estimatedDuration: 0,
    };
    setModules((prev) => [...prev, newModule]);
    setIsAddingModule(false);
  };

  const handleAddLesson = (moduleId: string, lessonData: { title: string; description: string; type: MockLesson['type']; duration: number }) => {
    const newLesson: MockLesson = {
      id: `lesson-${Date.now()}`,
      title: lessonData.title || 'New Lesson',
      description: lessonData.description || '',
      type: lessonData.type || 'text',
      duration: lessonData.duration || 30,
      status: 'draft',
      visibility: 'public',
    };

    setModules((prev) => prev.map((moduleItem) => (moduleItem.id === moduleId ? { ...moduleItem, lessons: [...moduleItem.lessons, newLesson] } : moduleItem)));
    setIsAddingLesson(null);
  };

  const handleDeleteModule = (moduleId: string) => {
    setModules((prev) => prev.filter((moduleItem) => moduleItem.id !== moduleId));
  };

  const handleDeleteLesson = (moduleId: string, lessonId: string) => {
    setModules((prev) =>
      prev.map((moduleItem) =>
        moduleItem.id === moduleId ? { ...moduleItem, lessons: moduleItem.lessons.filter((lesson) => lesson.id !== lessonId) } : moduleItem,
      ),
    );
  };

  const getLessonTypeIcon = (type: MockLesson['type']) => {
    switch (type) {
      case 'video':
        return <Video className="h-4 w-4" />;
      case 'quiz':
        return <PlayCircle className="h-4 w-4" />;
      case 'assignment':
        return <Edit className="h-4 w-4" />;
      default:
        return <FileText className="h-4 w-4" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'published':
        return 'bg-green-100 text-green-800';
      case 'draft':
        return 'bg-yellow-100 text-yellow-800';
      case 'archived':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Sticky Header */}
      <div className="sticky top-0 z-10 border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">Course Content</h1>
              <p className="text-sm text-muted-foreground">Manage modules, lessons, and course materials for &ldquo;{state.title}&rdquo;</p>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => setBulkEditMode(!bulkEditMode)}>
                {bulkEditMode ? 'Exit Bulk Edit' : 'Bulk Edit'}
              </Button>
              <Button onClick={() => setIsAddingModule(true)} size="sm">
                <Plus className="h-4 w-4 mr-2" />
                Add Module
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-5xl mx-auto space-y-4">
          {/* Course Structure Overview */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BookOpen className="h-5 w-5" />
                Course Structure
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-4 gap-4 mb-6">
                <div className="text-center">
                  <div className="text-2xl font-bold text-primary">{modules.length}</div>
                  <div className="text-sm text-muted-foreground">Modules</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-primary">{modules.reduce((total, moduleItem) => total + moduleItem.lessons.length, 0)}</div>
                  <div className="text-sm text-muted-foreground">Lessons</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-primary">
                    {modules.reduce((total, moduleItem) => total + moduleItem.lessons.reduce((lessonTotal, lesson) => lessonTotal + lesson.duration, 0), 0)}m
                  </div>
                  <div className="text-sm text-muted-foreground">Total Duration</div>
                </div>
                <div className="text-center">
                  <div className="text-2xl font-bold text-primary">
                    {modules.reduce((total, moduleItem) => total + moduleItem.lessons.filter((lesson) => lesson.status === 'published').length, 0)}
                  </div>
                  <div className="text-sm text-muted-foreground">Published</div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Modules and Lessons */}
          <div className="space-y-4">
            {modules.map((moduleItem) => (
              <Card key={moduleItem.id} className="overflow-hidden">
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      {bulkEditMode && (
                        <input type="checkbox" checked={selectedItems.has(moduleItem.id)} onChange={() => toggleSelection(moduleItem.id)} className="rounded" />
                      )}
                      <Button variant="ghost" size="sm" onClick={() => toggleModule(moduleItem.id)} className="p-0 h-auto">
                        {expandedModules.has(moduleItem.id) ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                      </Button>
                      <div>
                        <div className="flex items-center gap-2">
                          <h3 className="font-semibold">{moduleItem.title}</h3>
                          <Badge className={getStatusColor(moduleItem.status)}>{moduleItem.status}</Badge>
                          <Badge variant="outline">{moduleItem.lessons.length} lessons</Badge>
                          <Badge variant="outline">
                            <Clock className="h-3 w-3 mr-1" />
                            {moduleItem.estimatedDuration}m
                          </Badge>
                        </div>
                        {moduleItem.description && <p className="text-sm text-muted-foreground mt-1">{moduleItem.description}</p>}
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Button variant="outline" size="sm" onClick={() => setIsAddingLesson(moduleItem.id)}>
                        <Plus className="h-4 w-4 mr-1" />
                        Add Lesson
                      </Button>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="sm">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem>
                            <Edit className="h-4 w-4 mr-2" />
                            Edit Module
                          </DropdownMenuItem>
                          <DropdownMenuItem>
                            <Copy className="h-4 w-4 mr-2" />
                            Duplicate Module
                          </DropdownMenuItem>
                          <DropdownMenuItem>
                            <Move className="h-4 w-4 mr-2" />
                            Move Module
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleDeleteModule(moduleItem.id)} className="text-red-600">
                            <Trash2 className="h-4 w-4 mr-2" />
                            Delete Module
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                  </div>
                </CardHeader>

                <Collapsible open={expandedModules.has(moduleItem.id)}>
                  <CollapsibleContent>
                    <CardContent className="pt-0">
                      <div className="space-y-2 pl-6 border-l-2 border-border">
                        {moduleItem.lessons.map((lesson, index) => (
                          <div key={lesson.id} className="flex items-center justify-between p-3 rounded-lg border bg-card hover:bg-accent/50">
                            <div className="flex items-center gap-3">
                              {bulkEditMode && (
                                <input type="checkbox" checked={selectedItems.has(lesson.id)} onChange={() => toggleSelection(lesson.id)} className="rounded" />
                              )}
                              <div className="flex items-center gap-2">
                                <GripVertical className="h-4 w-4 text-muted-foreground" />
                                <span className="text-sm text-muted-foreground font-mono">{index + 1}</span>
                              </div>
                              <div className="flex items-center gap-2">
                                {getLessonTypeIcon(lesson.type)}
                                <div>
                                  <div className="flex items-center gap-2">
                                    <span className="font-medium">{lesson.title}</span>
                                    <Badge className={getStatusColor(lesson.status)} variant="secondary">
                                      {lesson.status}
                                    </Badge>
                                    <Badge variant="outline">
                                      <Clock className="h-3 w-3 mr-1" />
                                      {lesson.duration}m
                                    </Badge>
                                    {lesson.visibility === 'private' && <EyeOff className="h-3 w-3 text-muted-foreground" />}
                                  </div>
                                  {lesson.description && <p className="text-sm text-muted-foreground">{lesson.description}</p>}
                                </div>
                              </div>
                            </div>
                            <div className="flex items-center gap-2">
                              <Button variant="ghost" size="sm">
                                <Eye className="h-4 w-4" />
                              </Button>
                              <DropdownMenu>
                                <DropdownMenuTrigger asChild>
                                  <Button variant="ghost" size="sm">
                                    <MoreHorizontal className="h-4 w-4" />
                                  </Button>
                                </DropdownMenuTrigger>
                                <DropdownMenuContent align="end">
                                  <DropdownMenuItem>
                                    <Edit className="h-4 w-4 mr-2" />
                                    Edit Lesson
                                  </DropdownMenuItem>
                                  <DropdownMenuItem>
                                    <Copy className="h-4 w-4 mr-2" />
                                    Duplicate Lesson
                                  </DropdownMenuItem>
                                  <DropdownMenuItem>
                                    <ArrowUp className="h-4 w-4 mr-2" />
                                    Move Up
                                  </DropdownMenuItem>
                                  <DropdownMenuItem>
                                    <ArrowDown className="h-4 w-4 mr-2" />
                                    Move Down
                                  </DropdownMenuItem>
                                  <DropdownMenuItem onClick={() => handleDeleteLesson(moduleItem.id, lesson.id)} className="text-red-600">
                                    <Trash2 className="h-4 w-4 mr-2" />
                                    Delete Lesson
                                  </DropdownMenuItem>
                                </DropdownMenuContent>
                              </DropdownMenu>
                            </div>
                          </div>
                        ))}
                        {moduleItem.lessons.length === 0 && (
                          <div className="text-center py-8 text-muted-foreground">
                            <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
                            <p>No lessons in this module yet</p>
                            <Button variant="outline" size="sm" onClick={() => setIsAddingLesson(moduleItem.id)} className="mt-2">
                              Add First Lesson
                            </Button>
                          </div>
                        )}
                      </div>
                    </CardContent>
                  </CollapsibleContent>
                </Collapsible>
              </Card>
            ))}

            {modules.length === 0 && (
              <Card>
                <CardContent className="text-center py-12">
                  <BookOpen className="h-12 w-12 mx-auto mb-4 text-muted-foreground opacity-50" />
                  <h3 className="text-lg font-semibold mb-2">No modules created yet</h3>
                  <p className="text-muted-foreground mb-4">Start building your course by creating your first module</p>
                  <Button onClick={() => setIsAddingModule(true)}>
                    <Plus className="h-4 w-4 mr-2" />
                    Create First Module
                  </Button>
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      </div>

      {/* Add Module Dialog */}
      <Dialog open={isAddingModule} onOpenChange={setIsAddingModule}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add New Module</DialogTitle>
            <DialogDescription>Create a new module to organize your course content</DialogDescription>
          </DialogHeader>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              const formData = new FormData(e.currentTarget);
              handleAddModule({
                title: formData.get('title') as string,
                description: formData.get('description') as string,
              });
            }}
          >
            <div className="space-y-4">
              <div>
                <Label htmlFor="module-title">Module Title</Label>
                <Input id="module-title" name="title" placeholder="e.g., Introduction to Game Development" required />
              </div>
              <div>
                <Label htmlFor="module-description">Description</Label>
                <Textarea id="module-description" name="description" placeholder="Brief description of what this module covers..." rows={3} />
              </div>
            </div>
            <DialogFooter className="mt-6">
              <Button type="button" variant="outline" onClick={() => setIsAddingModule(false)}>
                Cancel
              </Button>
              <Button type="submit">Create Module</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Add Lesson Dialog */}
      <Dialog open={!!isAddingLesson} onOpenChange={() => setIsAddingLesson(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add New Lesson</DialogTitle>
            <DialogDescription>Create a new lesson for your module</DialogDescription>
          </DialogHeader>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              if (!isAddingLesson) return;
              const formData = new FormData(e.currentTarget);
              handleAddLesson(isAddingLesson, {
                title: formData.get('title') as string,
                description: formData.get('description') as string,
                type: formData.get('type') as MockLesson['type'],
                duration: parseInt(formData.get('duration') as string) || 30,
              });
            }}
          >
            <div className="space-y-4">
              <div>
                <Label htmlFor="lesson-title">Lesson Title</Label>
                <Input id="lesson-title" name="title" placeholder="e.g., Setting up your development environment" required />
              </div>
              <div>
                <Label htmlFor="lesson-type">Lesson Type</Label>
                <Select name="type" defaultValue="text">
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="text">Text/Article</SelectItem>
                    <SelectItem value="video">Video</SelectItem>
                    <SelectItem value="quiz">Quiz</SelectItem>
                    <SelectItem value="assignment">Assignment</SelectItem>
                    <SelectItem value="file">File Download</SelectItem>
                    <SelectItem value="interactive">Interactive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label htmlFor="lesson-duration">Duration (minutes)</Label>
                <Input id="lesson-duration" name="duration" type="number" defaultValue={30} min={1} max={180} />
              </div>
              <div>
                <Label htmlFor="lesson-description">Description</Label>
                <Textarea id="lesson-description" name="description" placeholder="Brief description of what this lesson covers..." rows={3} />
              </div>
            </div>
            <DialogFooter className="mt-6">
              <Button type="button" variant="outline" onClick={() => setIsAddingLesson(null)}>
                Cancel
              </Button>
              <Button type="submit">Create Lesson</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
