import { getCourseBySlug } from '@/actions/courses';
import Image from 'next/image';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { ArrowLeft, BookOpen, Clock, Users, Trophy, Star, CheckCircle, Play, Award, Target } from 'lucide-react';

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

export default async function CourseDetailPage({ params }: { params: { slug: string } }) {
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
              <Link href="/tracks">Learning Tracks</Link>
            </Button>
            <span className="text-gray-500">/</span>
            <span className="text-gray-400">{course.title}</span>
          </div>
          <Button asChild variant="ghost" className="text-gray-300 hover:text-white mt-2">
            <Link href="/tracks">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Learning Tracks
            </Link>
          </Button>
        </div>
      </div>

      <div className="container mx-auto px-4 py-8">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-8">
            {/* Hero Section */}
            <section className="relative">
              <div className="aspect-video relative overflow-hidden rounded-xl bg-gray-800">
                <Image src={course.image || '/placeholder.svg'} alt={course.title} fill className="object-cover" />
                <div className="absolute inset-0 bg-gradient-to-t from-gray-900/80 to-transparent" />
                <div className="absolute bottom-6 left-6 right-6">
                  <div className="flex flex-wrap gap-2 mb-4">
                    <Badge className={`border ${levelColor}`}>{levelName}</Badge>
                    <Badge variant="outline" className="border-gray-600 text-gray-300">
                      {course.area?.charAt(0).toUpperCase() + course.area?.slice(1)}
                    </Badge>
                  </div>
                  <h1 className="text-4xl font-bold mb-2">{course.title}</h1>
                  <p className="text-xl text-gray-300 leading-relaxed">{course.description}</p>
                </div>
              </div>
            </section>

            {/* Course Overview */}
            <section>
              <h2 className="text-2xl font-bold mb-6 flex items-center">
                <Target className="mr-3 h-6 w-6 text-blue-400" />
                Course Overview
              </h2>
              <div className="bg-gray-800/50 rounded-xl p-6 border border-gray-700">
                <p className="text-lg text-gray-300 leading-relaxed mb-6">{course.description}</p>
                
                <div className="grid md:grid-cols-2 gap-6">
                  <div>
                    <h3 className="text-lg font-semibold mb-3 text-blue-400">What You&apos;ll Learn</h3>
                    <ul className="space-y-2">
                      <li className="flex items-start">
                        <CheckCircle className="mr-2 h-5 w-5 text-green-400 mt-0.5 flex-shrink-0" />
                        <span className="text-gray-300">Master the fundamentals of {course.area} development</span>
                      </li>
                      <li className="flex items-start">
                        <CheckCircle className="mr-2 h-5 w-5 text-green-400 mt-0.5 flex-shrink-0" />
                        <span className="text-gray-300">Build practical projects to reinforce learning</span>
                      </li>
                      <li className="flex items-start">
                        <CheckCircle className="mr-2 h-5 w-5 text-green-400 mt-0.5 flex-shrink-0" />
                        <span className="text-gray-300">Apply industry best practices and workflows</span>
                      </li>
                      <li className="flex items-start">
                        <CheckCircle className="mr-2 h-5 w-5 text-green-400 mt-0.5 flex-shrink-0" />
                        <span className="text-gray-300">Prepare for real-world development challenges</span>
                      </li>
                    </ul>
                  </div>
                  
                  <div>
                    <h3 className="text-lg font-semibold mb-3 text-blue-400">Prerequisites</h3>
                    <ul className="space-y-2 text-gray-300">
                      <li>• Basic computer literacy</li>
                      <li>• Enthusiasm for learning</li>
                      <li>• {course.level > 1 ? 'Some programming experience recommended' : 'No prior experience required'}</li>
                      <li>• Access to development tools</li>
                    </ul>
                  </div>
                </div>
              </div>
            </section>

            {/* Tools & Technologies */}
            {course.tools && course.tools.length > 0 && (
              <section>
                <h2 className="text-2xl font-bold mb-6 flex items-center">
                  <BookOpen className="mr-3 h-6 w-6 text-purple-400" />
                  Tools & Technologies
                </h2>
                <div className="bg-gray-800/50 rounded-xl p-6 border border-gray-700">
                  <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                    {course.tools.map((tool, index) => (
                      <div key={index} className="flex items-center p-3 bg-gray-700/50 rounded-lg border border-gray-600">
                        <div className="w-8 h-8 bg-gray-600 rounded mr-3 flex items-center justify-center">
                          <BookOpen className="h-4 w-4 text-gray-300" />
                        </div>
                        <span className="text-sm font-medium text-gray-300">{tool}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </section>
            )}

            {/* Course Features */}
            <section>
              <h2 className="text-2xl font-bold mb-6 flex items-center">
                <Award className="mr-3 h-6 w-6 text-yellow-400" />
                Course Features
              </h2>
              <div className="grid md:grid-cols-2 gap-6">
                <Card className="bg-gray-800 border-gray-700">
                  <CardHeader>
                    <CardTitle className="text-lg flex items-center">
                      <Play className="mr-2 h-5 w-5 text-blue-400" />
                      Interactive Learning
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-gray-300">Hands-on exercises and projects that reinforce theoretical concepts.</p>
                  </CardContent>
                </Card>

                <Card className="bg-gray-800 border-gray-700">
                  <CardHeader>
                    <CardTitle className="text-lg flex items-center">
                      <Users className="mr-2 h-5 w-5 text-green-400" />
                      Community Support
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-gray-300">Join a community of learners and get help when you need it.</p>
                  </CardContent>
                </Card>

                <Card className="bg-gray-800 border-gray-700">
                  <CardHeader>
                    <CardTitle className="text-lg flex items-center">
                      <Trophy className="mr-2 h-5 w-5 text-yellow-400" />
                      Certification
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-gray-300">Earn a certificate of completion to showcase your achievements.</p>
                  </CardContent>
                </Card>

                <Card className="bg-gray-800 border-gray-700">
                  <CardHeader>
                    <CardTitle className="text-lg flex items-center">
                      <Clock className="mr-2 h-5 w-5 text-purple-400" />
                      Self-Paced
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-gray-300">Learn at your own pace with lifetime access to course materials.</p>
                  </CardContent>
                </Card>
              </div>
            </section>
          </div>

          {/* Right Sidebar */}
          <div className="lg:col-span-1">
            <div className="sticky top-8 space-y-6">
              {/* Enrollment Card */}
              <Card className="bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-xl text-center">Start Learning</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  {typeof course.progress === 'number' && (
                    <div>
                      <div className="flex justify-between items-center mb-2">
                        <span className="text-sm font-medium">Progress</span>
                        <span className="text-sm text-gray-400">{course.progress}%</span>
                      </div>
                      <Progress value={course.progress} className="mb-4" />
                    </div>
                  )}
                  
                  <div className="space-y-3 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-400">Level:</span>
                      <Badge className={`border ${levelColor}`}>{levelName}</Badge>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-400">Area:</span>
                      <span className="capitalize">{course.area}</span>
                    </div>
                    {course.tools && course.tools.length > 0 && (
                      <div className="flex justify-between">
                        <span className="text-gray-400">Tools:</span>
                        <span>{course.tools.length} tools</span>
                      </div>
                    )}
                  </div>

                  <div className="space-y-2 pt-4">
                    {typeof course.progress === 'number' && course.progress > 0 ? (
                      <Button className="w-full bg-green-600 hover:bg-green-700">Continue Learning</Button>
                    ) : (
                      <Button className="w-full bg-blue-600 hover:bg-blue-700">Start Course</Button>
                    )}
                    <Button variant="outline" className="w-full border-gray-600 hover:bg-gray-700">
                      Add to Favorites
                    </Button>
                  </div>
                </CardContent>
              </Card>

              {/* Course Stats */}
              <Card className="bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-lg">Course Stats</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center">
                      <Users className="mr-2 h-4 w-4 text-blue-400" />
                      <span className="text-sm">Students</span>
                    </div>
                    <span className="text-sm font-medium">1,247</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center">
                      <Star className="mr-2 h-4 w-4 text-yellow-400" />
                      <span className="text-sm">Rating</span>
                    </div>
                    <div className="flex items-center">
                      <span className="text-sm font-medium mr-1">4.8</span>
                      <div className="flex">
                        {[...Array(5)].map((_, i) => (
                          <Star key={i} className="h-3 w-3 fill-yellow-400 text-yellow-400" />
                        ))}
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center">
                      <Trophy className="mr-2 h-4 w-4 text-green-400" />
                      <span className="text-sm">Completed</span>
                    </div>
                    <span className="text-sm font-medium">89%</span>
                  </div>
                </CardContent>
              </Card>

              {/* Part of Track */}
              <Card className="bg-gray-800 border-gray-700">
                <CardHeader>
                  <CardTitle className="text-lg">Part of Learning Track</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 bg-blue-600 rounded-lg flex items-center justify-center">
                        <BookOpen className="h-5 w-5 text-white" />
                      </div>
                      <div>
                        <h4 className="font-semibold">Game Development Fundamentals</h4>
                        <p className="text-sm text-gray-400">Complete learning track</p>
                      </div>
                    </div>
                    <p className="text-sm text-gray-300">
                      This course is part of a comprehensive learning track designed to take you from beginner to professional game developer.
                    </p>
                    <Button asChild variant="outline" className="w-full border-gray-600 hover:bg-gray-700">
                      <Link href="/tracks/game-development-fundamentals">View Full Track</Link>
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
