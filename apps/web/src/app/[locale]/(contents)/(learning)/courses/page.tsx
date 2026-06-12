import { PublicCourseCatalog } from '@/components/courses/public-course-catalog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Link } from '@/i18n/navigation';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import { ArrowRight, BookOpen, CheckCircle, Code, Gamepad2, Headphones, Palette, PlayCircle, Trophy, Users, Zap } from 'lucide-react';
import NextLink from 'next/link';

export default async function ProgramsPage() {
  const catalog = await getPublicCourseCatalog();
  const courses = catalog.data;
  const catalogUnavailable = !catalog.success;
  const liveCourseCount = courses.length;
  const openEnrollmentCount = courses.filter((course) => course.isEnrollmentOpen).length;
  const categoryCount = new Set(courses.map((course) => String(course.category ?? '').trim()).filter(Boolean)).size;
  const totalEstimatedHours = courses.reduce((total, course) => total + (course.estimatedHours ?? 0), 0);

  const catalogStats = catalogUnavailable
    ? [
      {
        value: '--',
        label: 'Live Courses',
        color: 'from-blue-400 to-purple-400',
      },
      {
        value: '--',
        label: 'Open Enrollments',
        color: 'from-purple-400 to-pink-400',
      },
      {
        value: '--',
        label: 'Active Disciplines',
        color: 'from-green-400 to-teal-400',
      },
      {
        value: '--',
        label: 'Planned Learning Time',
        color: 'from-yellow-400 to-orange-400',
      },
    ]
    : [
      {
        value: liveCourseCount,
        label: 'Live Courses',
        color: 'from-blue-400 to-purple-400',
      },
      {
        value: openEnrollmentCount,
        label: 'Open Enrollments',
        color: 'from-purple-400 to-pink-400',
      },
      {
        value: categoryCount,
        label: 'Active Disciplines',
        color: 'from-green-400 to-teal-400',
      },
      {
        value: `${totalEstimatedHours}h`,
        label: 'Planned Learning Time',
        color: 'from-yellow-400 to-orange-400',
      },
    ];

  return (
    <div className="min-h-screen bg-linear-to-br from-slate-900 via-slate-800 to-slate-900">
      {/* Hero Section */}
      <section className="relative py-20 lg:py-32">
        <div className="container mx-auto px-4">
          <div className="max-w-4xl mx-auto text-center">
            <Badge variant="outline" className="mb-6 text-sm font-medium bg-linear-to-r from-purple-500/20 to-blue-500/20 border-purple-500/30 text-purple-300">
              {catalogUnavailable ? 'Catalog Temporarily Unavailable' : liveCourseCount > 0 ? `🎮 ${liveCourseCount} Live Courses` : '🎮 Game Guild Academy'}
            </Badge>
            <h1 className="text-4xl lg:text-6xl font-bold mb-6 bg-linear-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Learn from the Industry&apos;s Best</h1>
            <p className="text-xl lg:text-2xl text-slate-300 mb-8 max-w-3xl mx-auto">Game Development Shapes the World Around Us. Now, You Can Take Part.</p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center mb-12">
              <Button asChild size="lg" className="text-lg px-8 bg-linear-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 border-0 shadow-lg hover:shadow-xl hover:shadow-blue-500/25 transition-all text-white">
                <NextLink href="#catalog">
                  <BookOpen className="mr-2 h-5 w-5" />
                  Explore Courses
                </NextLink>
              </Button>
              <Button asChild variant="outline" size="lg" className="text-lg px-8 bg-slate-800/50 border-slate-600 text-slate-200 hover:bg-slate-700/50 hover:border-slate-500 backdrop-blur-sm hover:text-white">
                <NextLink href="#catalog">
                  <PlayCircle className="mr-2 h-5 w-5" />
                  Browse Disciplines
                </NextLink>
              </Button>
            </div>

            {/* Key Features */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-6 max-w-4xl mx-auto">
              <div className="text-center">
                <div className="bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-4 w-16 h-16 mx-auto mb-3 flex items-center justify-center shadow-lg">
                  <Users className="h-6 w-6 text-blue-400" />
                </div>
                <p className="text-sm font-medium text-white">Expert Training</p>
                <p className="text-xs text-slate-400">Industry Professionals</p>
              </div>
              <div className="text-center">
                <div className="bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-4 w-16 h-16 mx-auto mb-3 flex items-center justify-center shadow-lg">
                  <Zap className="h-6 w-6 text-purple-400" />
                </div>
                <p className="text-sm font-medium text-white">Interactive Learning</p>
                <p className="text-xs text-slate-400">Hands-on Projects</p>
              </div>
              <div className="text-center">
                <div className="bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-4 w-16 h-16 mx-auto mb-3 flex items-center justify-center shadow-lg">
                  <Trophy className="h-6 w-6 text-yellow-400" />
                </div>
                <p className="text-sm font-medium text-white">Personalized Feedback</p>
                <p className="text-xs text-slate-400">Live Q&A Sessions</p>
              </div>
              <div className="text-center">
                <div className="bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-4 w-16 h-16 mx-auto mb-3 flex items-center justify-center shadow-lg">
                  <BookOpen className="h-6 w-6 text-green-400" />
                </div>
                <p className="text-sm font-medium text-white">Lifetime Access</p>
                <p className="text-xs text-slate-400">Learn at Your Pace</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Discipline Highlights */}
      <section id="pathways" className="py-20">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4 bg-linear-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Browse by Discipline</h2>
            <p className="text-xl text-slate-400 max-w-3xl mx-auto">One Campus. Multiple Disciplines. Endless Possibilities.</p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-7xl mx-auto">
            {/* Game Programming Track */}
            <Card className="group bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 hover:border-blue-500/50 transition-all duration-300 hover:scale-105 hover:shadow-xl hover:shadow-blue-500/10 shadow-lg">
              <CardContent className="p-8">
                <div className="bg-linear-to-br from-blue-500/20 to-blue-600/20 rounded-lg p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform shadow-lg">
                  <Code className="h-10 w-10 text-blue-400" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4 text-white">Game Programming</h3>
                <p className="text-slate-400 text-center mb-6">Master the technical foundations of game development with C#, Unity, and advanced programming concepts.</p>
                <div className="space-y-2 mb-6">
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">C# Programming Fundamentals</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">Unity Engine Development</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">Game Architecture & Design Patterns</span>
                  </div>
                </div>
                <div className="space-y-3">
                  <Button asChild className="w-full bg-linear-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 border-0 shadow-lg hover:shadow-xl hover:shadow-blue-500/25 transition-all text-white">
                    <Link href="/courses?category=programming">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>

            {/* Game Art & Design Track */}
            <Card className="group bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 hover:border-purple-500/50 transition-all duration-300 hover:scale-105 hover:shadow-xl hover:shadow-purple-500/10 shadow-lg">
              <CardContent className="p-8">
                <div className="bg-linear-to-br from-purple-500/20 to-purple-600/20 rounded-lg p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform shadow-lg">
                  <Palette className="h-10 w-10 text-purple-400" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4 text-white">Game Art & Design</h3>
                <p className="text-slate-400 text-center mb-6">Create stunning visuals and compelling game experiences through art, animation, and user interface design.</p>
                <div className="space-y-2 mb-6">
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">2D & 3D Art Creation</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">Character & Environment Design</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">UI/UX Design for Games</span>
                  </div>
                </div>
                <div className="space-y-3">
                  <Button asChild className="w-full bg-linear-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 border-0 shadow-lg hover:shadow-xl hover:shadow-purple-500/25 transition-all text-white">
                    <Link href="/courses?category=art">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>

            {/* Game Design Track */}
            <Card className="group bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 hover:border-green-500/50 transition-all duration-300 hover:scale-105 hover:shadow-xl hover:shadow-green-500/10 shadow-lg">
              <CardContent className="p-8">
                <div className="bg-linear-to-br from-green-500/20 to-green-600/20 rounded-lg p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform shadow-lg">
                  <Gamepad2 className="h-10 w-10 text-green-400" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4 text-white">Game Design</h3>
                <p className="text-slate-400 text-center mb-6">Learn the art of creating engaging gameplay mechanics, compelling narratives, and memorable player experiences.</p>
                <div className="space-y-2 mb-6">
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">Game Mechanics & Systems</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">Level Design & Pacing</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-400 mr-2" />
                    <span className="text-slate-300">Narrative & Storytelling</span>
                  </div>
                </div>
                <div className="space-y-3">
                  <Button asChild className="w-full bg-linear-to-r from-green-600 to-teal-600 hover:from-green-700 hover:to-teal-700 border-0 shadow-lg hover:shadow-xl hover:shadow-green-500/25 transition-all text-white">
                    <Link href="/courses?category=design">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-8 text-center">
            {catalogStats.map((stat) => (
              <div key={stat.label} className="group">
                <div className={`text-4xl lg:text-5xl font-bold mb-2 bg-linear-to-r ${stat.color} bg-clip-text text-transparent group-hover:scale-110 transition-transform`}>
                  {stat.value}
                </div>
                <div className="text-slate-400">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4 bg-linear-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Why Choose Game Guild Academy</h2>
            <p className="text-xl text-slate-400 max-w-3xl mx-auto">We provide the tools, knowledge, and community you need to succeed in the game development industry.</p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-6xl mx-auto">
            <div className="text-center group">
              <div className="bg-linear-to-br from-blue-500/20 to-blue-600/20 rounded-lg p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform">
                <Users className="h-12 w-12 text-blue-400" />
              </div>
              <h3 className="text-xl font-semibold mb-4 text-white">Industry Experts</h3>
              <p className="text-slate-400">Learn from professionals who have shipped AAA games and work at top studios worldwide.</p>
            </div>

            <div className="text-center group">
              <div className="bg-linear-to-br from-purple-500/20 to-purple-600/20 rounded-lg p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform">
                <Zap className="h-12 w-12 text-purple-400" />
              </div>
              <h3 className="text-xl font-semibold mb-4 text-white">Hands-On Projects</h3>
              <p className="text-slate-400">Build real games and develop a portfolio that showcases your skills to potential employers.</p>
            </div>

            <div className="text-center group">
              <div className="bg-linear-to-br from-green-500/20 to-green-600/20 rounded-lg p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform">
                <Headphones className="h-12 w-12 text-green-400" />
              </div>
              <h3 className="text-xl font-semibold mb-4 text-white">Personalized Support</h3>
              <p className="text-slate-400">Get individual feedback on your work and guidance from instructors throughout your learning journey.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Learning Experience Section */}
      <section className="py-20">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4 bg-linear-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">What Learners Can Expect</h2>
            <p className="text-xl text-slate-400">This catalog is already live. As real learner reviews come in, this section can shift from expectations to verified outcomes.</p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-6xl mx-auto">
            <Card className="bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg hover:shadow-xl hover:shadow-white/10 transition-all hover:scale-105">
              <CardContent className="p-8">
                <div className="bg-linear-to-br from-blue-500/20 to-blue-600/20 rounded-lg p-2 w-12 h-12 flex items-center justify-center mb-4 shadow-lg">
                  <BookOpen className="h-6 w-6 text-blue-400" />
                </div>
                <p className="text-slate-400 mb-6">Each course is organized around a clear learning path, so learners can move from discovery in the public catalog to focused study inside the dedicated learning app.</p>
                <div className="flex items-center">
                  <div>
                    <div className="font-semibold text-white">Structured Progression</div>
                    <div className="text-sm text-slate-400">Catalog, course detail, then classroom handoff</div>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card className="bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg hover:shadow-xl hover:shadow-white/10 transition-all hover:scale-105">
              <CardContent className="p-8">
                <div className="bg-linear-to-br from-purple-500/20 to-purple-600/20 rounded-lg p-2 w-12 h-12 flex items-center justify-center mb-4 shadow-lg">
                  <Zap className="h-6 w-6 text-purple-400" />
                </div>
                <p className="text-slate-400 mb-6">The learning experience is built around practical content, not just marketing pages, so published courses can lead directly into lessons, assignments, and guided work.</p>
                <div className="flex items-center">
                  <div>
                    <div className="font-semibold text-white">Practical Delivery</div>
                    <div className="text-sm text-slate-400">Lessons, course content, and classroom flow</div>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card className="bg-linear-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg hover:shadow-xl hover:shadow-white/10 transition-all hover:scale-105">
              <CardContent className="p-8">
                <div className="bg-linear-to-br from-green-500/20 to-green-600/20 rounded-lg p-2 w-12 h-12 flex items-center justify-center mb-4 shadow-lg">
                  <Headphones className="h-6 w-6 text-green-400" />
                </div>
                <p className="text-slate-400 mb-6">The public site now describes the experience honestly: what is live, what is open for enrollment, and where learners go next once they start attending a course.</p>
                <div className="flex items-center">
                  <div>
                    <div className="font-semibold text-white">Truthful Positioning</div>
                    <div className="text-sm text-slate-400">No invented reviews, only live product signals</div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* Course Catalog Section */}
      <section id="catalog" className="py-20 scroll-mt-16">
        {catalogUnavailable ? (
          <div className="container mx-auto px-4">
            <Card className="border-amber-500/40 bg-slate-900/80 text-white shadow-lg shadow-slate-950/40">
              <CardContent className="space-y-4 p-8">
                <div className="space-y-2">
                  <h3 className="text-2xl font-semibold">The live course catalog is temporarily unavailable</h3>
                  <p className="text-slate-300">
                    The public storefront could not reach the learning API, so this page is intentionally hiding unavailable catalog data instead of showing a misleading empty state.
                  </p>
                </div>
                {catalog.error ? <p className="text-sm text-amber-300">Latest error: {catalog.error}</p> : null}
                <div className="flex flex-wrap gap-3">
                  <Button asChild className="bg-blue-600 text-white hover:bg-blue-500">
                    <Link href="/courses#catalog">Try again</Link>
                  </Button>
                  <Button asChild variant="outline" className="border-slate-600 bg-slate-800/50 text-slate-100 hover:bg-slate-700/50 hover:text-white">
                    <Link href="/dashboard/learning/courses">Open dashboard catalog</Link>
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        ) : (
          <PublicCourseCatalog initialCourses={courses} />
        )}
      </section>

      {/* CTA Section */}
      <section className="py-20">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-3xl lg:text-4xl font-bold mb-6 bg-linear-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Ready to Start Your Game Development Journey?</h2>
          <p className="text-xl mb-8 max-w-2xl mx-auto text-slate-300">Start with the live catalog, pick a discipline, and move into the dedicated learning experience when you are ready.</p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Button asChild size="lg" className="text-lg px-8 bg-linear-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 border-0 shadow-lg hover:shadow-xl hover:shadow-blue-500/25 transition-all text-white">
              <Link href="#catalog">
                <BookOpen className="mr-2 h-5 w-5" />
                Browse All Courses
              </Link>
            </Button>
            <Button asChild size="lg" variant="outline" className="text-lg px-8 bg-slate-800/50 border-slate-600 text-slate-200 hover:bg-slate-700/50 hover:border-slate-500 backdrop-blur-sm hover:text-white">
              <Link href="#contact">
                <Users className="mr-2 h-5 w-5" />
                Get Guidance
              </Link>
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
}
