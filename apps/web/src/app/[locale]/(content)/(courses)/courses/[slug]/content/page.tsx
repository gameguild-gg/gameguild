'use client';

import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { BookOpen, CheckCircle, ChevronRight, Circle, Code, FileText, Play } from 'lucide-react';
import { useParams } from 'next/navigation';
import { useEffect, useState } from 'react';

// Import actual markdown content from Python course
import week01Content from '@/data/courses/python/chapters/week01/lecture.md';
import week02Content from '@/data/courses/python/chapters/week02/lecture.md';
import week03Content from '@/data/courses/python/chapters/week03/lecture.md';
import week04Content from '@/data/courses/python/chapters/week04/lecture.md';
import week05Exercise01Content from '@/data/courses/python/chapters/week05/exercise-lists-01.md';
import week05Exercise02Content from '@/data/courses/python/chapters/week05/exercise-lists-02.md';
import week05ListsContent from '@/data/courses/python/chapters/week05/lists.md';
import week06Content from '@/data/courses/python/chapters/week06/lecture.md';
import week07Content from '@/data/courses/python/chapters/week07/lecture.md';
import week10DictionariesContent from '@/data/courses/python/chapters/week10/dictionaries.md';
import week10SetsContent from '@/data/courses/python/chapters/week10/sets.md';
import week11Content from '@/data/courses/python/chapters/week11/lecture.md';
import week12ApisContent from '@/data/courses/python/chapters/week12/lecture.md';
import week12LocalLlmContent from '@/data/courses/python/chapters/week12/local-llm.md';
import syllabusContent from '@/data/courses/python/syllabus.md';

interface CourseModule {
  id: string;
  title: string;
  description: string;
  duration: string;
  completed: boolean;
  lessons: CourseLesson[];
}

interface CourseLesson {
  id: string;
  title: string;
  type: 'text' | 'video' | 'exercise' | 'code';
  duration: string;
  completed: boolean;
  content?: string;
}

// Mock course data using actual markdown content
const getCourseContent = (slug: string): CourseModule[] => {
  if (slug === 'python') {
    return [
      {
        id: 'syllabus',
        title: 'Course Syllabus',
        description: 'Course overview, objectives, and schedule',
        duration: '15 min',
        completed: true,
        lessons: [
          {
            id: 'syllabus-1',
            title: 'Python Programming Course Overview',
            type: 'text',
            duration: '15 min',
            completed: true,
            content: syllabusContent,
          },
        ],
      },
      {
        id: '1',
        title: 'Week 01: Introduction to Python',
        description: 'Introduction to algorithms and algorithmic thinking',
        duration: '45 min',
        completed: true,
        lessons: [
          {
            id: '1-1',
            title: 'What is an algorithm?',
            type: 'text',
            duration: '15 min',
            completed: true,
            content: week01Content,
          },
        ],
      },
      {
        id: '2',
        title: 'Week 02: Python Basics',
        description: 'Introduction to Python programming basics',
        duration: '2h 30min',
        completed: false,
        lessons: [
          {
            id: '2-1',
            title: 'Python REPL and Basic Concepts',
            type: 'text',
            duration: '2h 30min',
            completed: false,
            content: week02Content,
          },
        ],
      },
      {
        id: '3',
        title: 'Week 03: Functions and Math',
        description: 'Functions and math in Python programming',
        duration: '3h 15min',
        completed: false,
        lessons: [
          {
            id: '3-1',
            title: 'Functions and Mathematical Operations',
            type: 'text',
            duration: '3h 15min',
            completed: false,
            content: week03Content,
          },
        ],
      },
      {
        id: '4',
        title: 'Week 04: Python Conditionals and Loops',
        description: 'Flow control in Python programming',
        duration: '2h 45min',
        completed: false,
        lessons: [
          {
            id: '4-1',
            title: 'Conditionals and Loop Structures',
            type: 'text',
            duration: '2h 45min',
            completed: false,
            content: week04Content,
          },
        ],
      },
      {
        id: '5',
        title: 'Week 05: Lists and Data Structures',
        description: 'Lists, tuples, and string manipulation',
        duration: '3h 30min',
        completed: false,
        lessons: [
          {
            id: '5-1',
            title: 'Lists and Basic Data Structures',
            type: 'text',
            duration: '2h',
            completed: false,
            content: week05ListsContent,
          },
          {
            id: '5-2',
            title: 'Exercise: Two Sum',
            type: 'exercise',
            duration: '45 min',
            completed: false,
            content: week05Exercise01Content,
          },
          {
            id: '5-3',
            title: 'Exercise: Search Insert Position',
            type: 'exercise',
            duration: '45 min',
            completed: false,
            content: week05Exercise02Content,
          },
        ],
      },
      {
        id: '6',
        title: 'Week 06: Advanced Loops',
        description: 'Advanced looping techniques and patterns',
        duration: '3h 30min',
        completed: false,
        lessons: [
          {
            id: '6-1',
            title: 'Loop Patterns and Advanced Techniques',
            type: 'text',
            duration: '3h 30min',
            completed: false,
            content: week06Content,
          },
        ],
      },
      {
        id: '7',
        title: 'Week 07: Nested Loops',
        description: 'Nested loops and advanced loop control',
        duration: '2h 15min',
        completed: false,
        lessons: [
          {
            id: '7-1',
            title: 'Nested Loops and Connect-4 Game',
            type: 'text',
            duration: '2h 15min',
            completed: false,
            content: week07Content,
          },
        ],
      },
      {
        id: '10',
        title: 'Week 10: Dictionaries and Sets',
        description: 'Advanced data structures in Python',
        duration: '3h 45min',
        completed: false,
        lessons: [
          {
            id: '10-1',
            title: 'Dictionaries and Key-Value Pairs',
            type: 'text',
            duration: '2h',
            completed: false,
            content: week10DictionariesContent,
          },
          {
            id: '10-2',
            title: 'Sets and Set Operations',
            type: 'text',
            duration: '1h 45min',
            completed: false,
            content: week10SetsContent,
          },
        ],
      },
      {
        id: '11',
        title: 'Week 11: Files and Exceptions',
        description: 'File handling and exception management',
        duration: '3h 20min',
        completed: false,
        lessons: [
          {
            id: '11-1',
            title: 'File I/O and Exception Handling',
            type: 'text',
            duration: '3h 20min',
            completed: false,
            content: week11Content,
          },
        ],
      },
      {
        id: '12',
        title: 'Week 12: APIs and Web Services',
        description: 'Working with APIs and web services',
        duration: '2h 30min',
        completed: false,
        lessons: [
          {
            id: '12-1',
            title: 'APIs and Web Services',
            type: 'text',
            duration: '1h',
            completed: false,
            content: week12ApisContent,
          },
          {
            id: '12-2',
            title: 'Local LLMs with Ollama',
            type: 'text',
            duration: '1h 30min',
            completed: false,
            content: week12LocalLlmContent,
          },
        ],
      },
    ];
  }
  return [];
};

const getLessonIcon = (type: string) => {
  switch (type) {
    case 'video':
      return <Play className="h-4 w-4" />;
    case 'exercise':
      return <Code className="h-4 w-4" />;
    case 'code':
      return <Code className="h-4 w-4" />;
    default:
      return <FileText className="h-4 w-4" />;
  }
};

const getLessonTypeColor = (type: string) => {
  switch (type) {
    case 'video':
      return 'bg-blue-100 text-blue-800 dark:bg-blue-900/20 dark:text-blue-300';
    case 'exercise':
      return 'bg-green-100 text-green-800 dark:bg-green-900/20 dark:text-green-300';
    case 'code':
      return 'bg-purple-100 text-purple-800 dark:bg-purple-900/20 dark:text-purple-300';
    default:
      return 'bg-muted text-muted-foreground';
  }
};

export default function CourseContentPage() {
  const params = useParams();
  const slug = params.slug as string;
  const [selectedLesson, setSelectedLesson] = useState<CourseLesson | null>(null);
  const [courseContent] = useState(() => getCourseContent(slug));

  const totalLessons = courseContent.reduce((acc, module) => acc + module.lessons.length, 0);
  const completedLessons = courseContent.reduce(
    (acc, module) => acc + module.lessons.filter(lesson => lesson.completed).length,
    0
  );
  const progressPercentage = totalLessons > 0 ? (completedLessons / totalLessons) * 100 : 0;

  useEffect(() => {
    // Set the first lesson as selected by default
    const firstModule = courseContent[0];
    if (firstModule && firstModule.lessons && firstModule.lessons.length > 0) {
      const firstLesson = firstModule.lessons[0];
      if (firstLesson) {
        setSelectedLesson(firstLesson);
      }
    }
  }, [courseContent]);

  const handleLessonClick = (lesson: CourseLesson) => {
    setSelectedLesson(lesson);
  };

  const handleLessonComplete = (moduleId: string, lessonId: string) => {
    // In a real implementation, this would update the backend
    console.log(`Marking lesson ${lessonId} in module ${moduleId} as completed`);
  };

  if (!courseContent.length) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-foreground mb-4">Course Not Found</h1>
          <p className="text-muted-foreground">The requested course could not be found.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Course Header */}
      <div className="mb-8">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h1 className="text-3xl font-bold text-foreground">Python Programming</h1>
            <p className="text-muted-foreground mt-2">
              Master Python programming fundamentals and advanced concepts
            </p>
          </div>
          <div className="text-right">
            <div className="text-2xl font-bold text-foreground">{completedLessons}/{totalLessons}</div>
            <div className="text-sm text-muted-foreground">Lessons completed</div>
          </div>
        </div>
        <Progress value={progressPercentage} className="h-2" />
        <div className="text-sm text-muted-foreground mt-2">
          {Math.round(progressPercentage)}% complete
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Course Modules */}
        <div className="lg:col-span-1">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BookOpen className="h-5 w-5" />
                Course Content
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {courseContent.map((module) => (
                <div key={module.id} className="space-y-2">
                  <div className="flex items-center justify-between">
                    <h3 className="font-semibold text-foreground">{module.title}</h3>
                    <Badge variant={module.completed ? 'default' : 'secondary'}>
                      {module.completed ? 'Completed' : 'In Progress'}
                    </Badge>
                  </div>
                  <p className="text-sm text-muted-foreground">{module.description}</p>
                  <div className="space-y-1">
                    {module.lessons.map((lesson) => (
                      <button
                        key={lesson.id}
                        onClick={() => handleLessonClick(lesson)}
                        className={`w-full flex items-center justify-between p-3 rounded-lg border transition-colors ${selectedLesson?.id === lesson.id
                          ? 'border-primary bg-primary/10'
                          : 'border-border hover:border-border/80 hover:bg-muted/50'
                          }`}
                      >
                        <div className="flex items-center gap-3">
                          {lesson.completed ? (
                            <CheckCircle className="h-4 w-4 text-green-600 dark:text-green-400" />
                          ) : (
                            <Circle className="h-4 w-4 text-muted-foreground" />
                          )}
                          <div className="flex items-center gap-2">
                            {getLessonIcon(lesson.type)}
                            <span className="text-sm font-medium text-foreground">
                              {lesson.title}
                            </span>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <Badge className={getLessonTypeColor(lesson.type)}>
                            {lesson.type}
                          </Badge>
                          <span className="text-xs text-muted-foreground">{lesson.duration}</span>
                          <ChevronRight className="h-4 w-4 text-muted-foreground" />
                        </div>
                      </button>
                    ))}
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </div>

        {/* Lesson Content */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                {selectedLesson && getLessonIcon(selectedLesson.type)}
                {selectedLesson?.title || 'Select a lesson to begin'}
              </CardTitle>
              {selectedLesson && (
                <div className="flex items-center gap-4 text-sm text-muted-foreground">
                  <Badge className={getLessonTypeColor(selectedLesson.type)}>
                    {selectedLesson.type}
                  </Badge>
                  <span>{selectedLesson.duration}</span>
                  {!selectedLesson.completed && (
                    <Button
                      size="sm"
                      onClick={() => handleLessonComplete('', selectedLesson.id)}
                      className="ml-auto"
                    >
                      Mark as Complete
                    </Button>
                  )}
                </div>
              )}
            </CardHeader>
            <CardContent>
              {selectedLesson?.content ? (
                <div className="prose max-w-none">
                  <MarkdownRenderer content={selectedLesson.content} />
                </div>
              ) : (
                <div className="text-center py-12">
                  <BookOpen className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-foreground mb-2">
                    Select a lesson to begin learning
                  </h3>
                  <p className="text-muted-foreground">
                    Choose a lesson from the course content to start your learning journey.
                  </p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
