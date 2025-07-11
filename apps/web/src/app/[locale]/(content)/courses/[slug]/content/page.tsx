import { auth } from '@/auth';
import { getCourseBySlug } from '@/lib/courses/actions';
import { redirect } from 'next/navigation';
import { notFound } from 'next/navigation';
import Image from 'next/image';
import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { ArrowLeft, Clock, Play, CheckCircle, FileText, Video, Code, Award } from 'lucide-react';

const levelColors = {
  1: 'bg-green-500/10 border-green-500 text-green-400',
  2: 'bg-blue-500/10 border-blue-500 text-blue-400',
  3: 'bg-orange-500/10 border-orange-500 text-orange-400',
  4: 'bg-red-500/10 border-red-500 text-red-400',
};

const levelNames = {
  1: 'Beginner',
  2: 'Intermediate',
  3: 'Advanced',
  4: 'Arcane',
};

// Mock course content structure
const mockCourseContent = {
  modules: [
    {
      id: '1',
      title: 'Getting Started',
      description: 'Introduction to the fundamentals',
      duration: '45 min',
      completed: true,
      lessons: [
        { id: '1-1', title: 'Welcome & Course Overview', type: 'video', duration: '5 min', completed: true },
        { id: '1-2', title: 'Setting Up Your Environment', type: 'video', duration: '15 min', completed: true },
        { id: '1-3', title: 'Basic Concepts', type: 'text', duration: '10 min', completed: true },
        { id: '1-4', title: 'First Hands-on Exercise', type: 'exercise', duration: '15 min', completed: false },
      ],
    },
    {
      id: '2',
      title: 'Core Concepts',
      description: 'Deep dive into essential topics',
      duration: '2h 30min',
      completed: false,
      lessons: [
        { id: '2-1', title: 'Advanced Techniques', type: 'video', duration: '25 min', completed: false },
        { id: '2-2', title: 'Best Practices', type: 'text', duration: '20 min', completed: false },
        { id: '2-3', title: 'Code Examples', type: 'code', duration: '30 min', completed: false },
        { id: '2-4', title: 'Practical Project', type: 'exercise', duration: '45 min', completed: false },
        { id: '2-5', title: 'Project Review', type: 'video', duration: '20 min', completed: false },
      ],
    },
    {
      id: '3',
      title: 'Advanced Topics',
      description: 'Master advanced techniques and patterns',
      duration: '3h 15min',
      completed: false,
      lessons: [
        { id: '3-1', title: 'Advanced Architecture', type: 'video', duration: '35 min', completed: false },
        { id: '3-2', title: 'Performance Optimization', type: 'text', duration: '25 min', completed: false },
        { id: '3-3', title: 'Complex Scenarios', type: 'code', duration: '40 min', completed: false },
        { id: '3-4', title: 'Final Project Setup', type: 'exercise', duration: '30 min', completed: false },
        { id: '3-5', title: 'Final Project Implementation', type: 'exercise', duration: '60 min', completed: false },
        { id: '3-6', title: 'Course Completion & Certificate', type: 'video', duration: '5 min', completed: false },
      ],
    },
  ],
};

const contentTypeIcons = {
  video: Video,
  text: FileText,
  code: Code,
  exercise: Award,
};

export default async function CourseContentPage({ params }: { params: { slug: string } }) {
  // Check authentication
  const session = await auth();
  if (!session?.user) {
    redirect(`/connect?returnUrl=/course/${params.slug}/content`);
  }

  const { slug } = await params;

  let course;
  try {
    course = await getCourseBySlug(slug);
  } catch (error) {
    console.error('Error fetching course:', error);
    course = null;
  }

  if (!course) {
    notFound();
  }

  const levelColor = levelColors[course.level as keyof typeof levelColors] || levelColors[1];
  const levelName = levelNames[course.level as keyof typeof levelNames] || 'Beginner';

  // Calculate progress
  const totalLessons = mockCourseContent.modules.reduce((total, module) => total + module.lessons.length, 0);
  const completedLessons = mockCourseContent.modules.reduce((total, module) => total + module.lessons.filter((lesson) => lesson.completed).length, 0);
  const progressPercentage = Math.round((completedLessons / totalLessons) * 100);

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      {/* Navigation */}
      <div className="border-b border-gray-800">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center gap-2 text-sm">
            <Button asChild variant="ghost" className="text-gray-300 hover:text-white p-0">
              <Link href="/courses">Courses</Link>
            </Button>
            <span className="text-gray-500">/</span>
            <Button asChild variant="ghost" className="text-gray-300 hover:text-white p-0">
              <Link href={`/course/${slug}`}>{course.title}</Link>
            </Button>
            <span className="text-gray-500">/</span>
            <span className="text-gray-400">Content</span>
          </div>
          <Button asChild variant="ghost" className="text-gray-300 hover:text-white mt-2">
            <Link href={`/course/${slug}`}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Course Details
            </Link>
          </Button>
        </div>
      </div>

      <div className="container mx-auto px-4 py-8">
        <div className="grid lg:grid-cols-4 gap-8">
          {/* Course Progress Sidebar */}
          <div className="lg:col-span-1">
            <Card className="bg-gray-800/50 border-gray-700 sticky top-8">
              <CardHeader>
                <div className="flex items-center gap-3">
                  <div className="w-12 h-12 relative rounded-lg overflow-hidden">
                    <Image src={course.image || '/placeholder.svg'} alt={course.title} fill className="object-cover" />
                  </div>
                  <div>
                    <h3 className="font-semibold text-sm">{course.title}</h3>
                    <Badge className={`text-xs ${levelColor}`}>{levelName}</Badge>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <div className="flex justify-between text-sm mb-2">
                      <span>Progress</span>
                      <span>{progressPercentage}%</span>
                    </div>
                    <Progress value={progressPercentage} className="h-2" />
                    <p className="text-xs text-gray-400 mt-1">
                      {completedLessons} of {totalLessons} lessons completed
                    </p>
                  </div>

                  <div className="pt-4 border-t border-gray-700">
                    <h4 className="font-medium mb-3 text-sm">Course Modules</h4>
                    <div className="space-y-2">
                      {mockCourseContent.modules.map((module) => (
                        <div key={module.id} className="flex items-center gap-2 text-sm">
                          {module.completed ? (
                            <CheckCircle className="w-4 h-4 text-green-400" />
                          ) : (
                            <div className="w-4 h-4 rounded-full border-2 border-gray-600" />
                          )}
                          <span className={module.completed ? 'text-green-400' : 'text-gray-400'}>{module.title}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Main Content */}
          <div className="lg:col-span-3">
            <div className="space-y-6">
              {/* Welcome Header */}
              <div className="bg-gradient-to-r from-blue-600/10 to-purple-600/10 border border-blue-500/20 rounded-xl p-6">
                <h1 className="text-3xl font-bold mb-2">Welcome back, {session.user.name || 'Student'}!</h1>
                <p className="text-gray-300">Continue your learning journey in {course.title}</p>
              </div>

              {/* Course Modules */}
              {mockCourseContent.modules.map((module) => (
                <Card key={module.id} className="bg-gray-800/50 border-gray-700">
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        {module.completed ? (
                          <CheckCircle className="w-6 h-6 text-green-400" />
                        ) : (
                          <div className="w-6 h-6 rounded-full border-2 border-gray-600 flex items-center justify-center">
                            <div className="w-2 h-2 rounded-full bg-gray-600" />
                          </div>
                        )}
                        <div>
                          <CardTitle className="text-xl">{module.title}</CardTitle>
                          <p className="text-gray-400 text-sm">{module.description}</p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2 text-sm text-gray-400">
                        <Clock className="w-4 h-4" />
                        {module.duration}
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-3">
                      {module.lessons.map((lesson) => {
                        const IconComponent = contentTypeIcons[lesson.type as keyof typeof contentTypeIcons];
                        return (
                          <div
                            key={lesson.id}
                            className={`flex items-center justify-between p-3 rounded-lg border transition-colors ${
                              lesson.completed ? 'bg-green-500/5 border-green-500/20' : 'bg-gray-700/30 border-gray-600 hover:bg-gray-700/50'
                            }`}
                          >
                            <div className="flex items-center gap-3">
                              {lesson.completed ? <CheckCircle className="w-5 h-5 text-green-400" /> : <IconComponent className="w-5 h-5 text-gray-400" />}
                              <div>
                                <h4 className={`font-medium ${lesson.completed ? 'text-green-400' : 'text-white'}`}>{lesson.title}</h4>
                                <div className="flex items-center gap-2 text-sm text-gray-400">
                                  <Clock className="w-3 h-3" />
                                  {lesson.duration}
                                </div>
                              </div>
                            </div>
                            <Button
                              variant={lesson.completed ? 'outline' : 'default'}
                              size="sm"
                              className={lesson.completed ? 'border-green-500 text-green-400' : ''}
                            >
                              {lesson.completed ? 'Review' : 'Start'}
                              <Play className="ml-2 w-4 h-4" />
                            </Button>
                          </div>
                        );
                      })}
                    </div>
                  </CardContent>
                </Card>
              ))}

              {/* Next Steps */}
              {progressPercentage < 100 && (
                <Card className="bg-gradient-to-r from-blue-600/10 to-purple-600/10 border border-blue-500/20">
                  <CardContent className="p-6">
                    <h3 className="text-xl font-bold mb-2">Keep Learning!</h3>
                    <p className="text-gray-300 mb-4">
                      You're {progressPercentage}% through the course. Continue with the next lesson to maintain your momentum.
                    </p>
                    <Button className="bg-blue-600 hover:bg-blue-700">
                      Continue Learning
                      <Play className="ml-2 w-4 h-4" />
                    </Button>
                  </CardContent>
                </Card>
              )}

              {progressPercentage === 100 && (
                <Card className="bg-gradient-to-r from-green-600/10 to-emerald-600/10 border border-green-500/20">
                  <CardContent className="p-6 text-center">
                    <Award className="w-12 h-12 text-green-400 mx-auto mb-4" />
                    <h3 className="text-2xl font-bold mb-2">Congratulations!</h3>
                    <p className="text-gray-300 mb-4">You've completed the entire course. Download your certificate and continue with advanced topics.</p>
                    <div className="flex gap-4 justify-center">
                      <Button className="bg-green-600 hover:bg-green-700">Download Certificate</Button>
                      <Button variant="outline">View Related Courses</Button>
                    </div>
                  </CardContent>
                </Card>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
