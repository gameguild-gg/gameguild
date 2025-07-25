import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ArrowRight, BookOpen, CheckCircle, Code, Gamepad2, Headphones, Palette, PlayCircle, Star, Trophy, Users, Zap } from 'lucide-react';

export default function CoursesLandingPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
      {/* Hero Section */}
      <section className="relative py-20 lg:py-32">
        <div className="container mx-auto px-4">
          <div className="max-w-4xl mx-auto text-center">
            <Badge
              variant="outline"
              className="mb-6 text-sm font-medium bg-gradient-to-r from-purple-500/20 to-blue-500/20 border-purple-500/30 text-purple-300"
            >
              🎮 Master Game Development
            </Badge>
            <h1 className="text-4xl lg:text-6xl font-bold mb-6 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
              Learn from the Industry&apos;s Best
            </h1>
            <p className="text-xl lg:text-2xl text-slate-300 mb-8 max-w-3xl mx-auto">Game Development Shapes the World Around Us. Now, You Can Take Part.</p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center mb-12">
              <Button
                asChild
                size="lg"
                className="text-lg px-8 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 border-0 shadow-lg hover:shadow-xl hover:shadow-blue-500/25 transition-all text-white"
              >
                <Link href="/courses/catalog">
                  <BookOpen className="mr-2 h-5 w-5" />
                  Explore Courses
                </Link>
              </Button>
              <Button
                asChild
                variant="outline"
                size="lg"
                className="text-lg px-8 bg-slate-800/50 border-slate-600 text-slate-200 hover:bg-slate-700/50 hover:border-slate-500 backdrop-blur-sm hover:text-white"
              >
                <Link href="#pathways">
                  <PlayCircle className="mr-2 h-5 w-5" />
                  Find Your Path
                </Link>
              </Button>
            </div>

            {/* Key Features */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-6 max-w-4xl mx-auto">
              <div className="text-center">
                <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-4 w-16 h-16 mx-auto mb-3 flex items-center justify-center shadow-lg">
                  <Users className="h-6 w-6 text-blue-400" />
                </div>
                <p className="text-sm font-medium text-white">Expert Training</p>
                <p className="text-xs text-slate-400">Industry Professionals</p>
              </div>
              <div className="text-center">
                <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-4 w-16 h-16 mx-auto mb-3 flex items-center justify-center shadow-lg">
                  <Zap className="h-6 w-6 text-purple-400" />
                </div>
                <p className="text-sm font-medium text-white">Interactive Learning</p>
                <p className="text-xs text-slate-400">Hands-on Projects</p>
              </div>
              <div className="text-center">
                <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-4 w-16 h-16 mx-auto mb-3 flex items-center justify-center shadow-lg">
                  <Trophy className="h-6 w-6 text-yellow-400" />
                </div>
                <p className="text-sm font-medium text-white">Personalized Feedback</p>
                <p className="text-xs text-slate-400">Live Q&A Sessions</p>
              </div>
              <div className="text-center">
                <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg p-4 w-16 h-16 mx-auto mb-3 flex items-center justify-center shadow-lg">
                  <BookOpen className="h-6 w-6 text-green-400" />
                </div>
                <p className="text-sm font-medium text-white">Lifetime Access</p>
                <p className="text-xs text-slate-400">Learn at Your Pace</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Learning Pathways Section */}
      <section id="pathways" className="py-20">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Find Your Path</h2>
            <p className="text-xl text-slate-400 max-w-3xl mx-auto">One Campus. Multiple Disciplines. Endless Possibilities.</p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-7xl mx-auto">
            {/* Game Programming Track */}
            <Card className="group bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 hover:border-blue-500/50 transition-all duration-300 hover:scale-105 hover:shadow-xl hover:shadow-blue-500/10 shadow-lg">
              <CardContent className="p-8">
                <div className="bg-gradient-to-br from-blue-500/20 to-blue-600/20 rounded-lg p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform shadow-lg">
                  <Code className="h-10 w-10 text-blue-400" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4 text-white">Game Programming</h3>
                <p className="text-slate-400 text-center mb-6">
                  Master the technical foundations of game development with C#, Unity, and advanced programming concepts.
                </p>
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
                  <Button
                    asChild
                    className="w-full bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 border-0 shadow-lg hover:shadow-xl hover:shadow-blue-500/25 transition-all text-white"
                  >
                    <Link href="/courses/catalog?category=programming">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                  <Button
                    asChild
                    variant="outline"
                    className="w-full bg-slate-800/50 border-slate-600 text-slate-200 hover:bg-slate-700/50 hover:border-slate-500 hover:text-white"
                  >
                    <Link href="/tracks/beginner-track-game-dev-fundamentals">
                      View Learning Track <BookOpen className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>

            {/* Game Art & Design Track */}
            <Card className="group bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 hover:border-purple-500/50 transition-all duration-300 hover:scale-105 hover:shadow-xl hover:shadow-purple-500/10 shadow-lg">
              <CardContent className="p-8">
                <div className="bg-gradient-to-br from-purple-500/20 to-purple-600/20 rounded-lg p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform shadow-lg">
                  <Palette className="h-10 w-10 text-purple-400" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4 text-white">Game Art & Design</h3>
                <p className="text-slate-400 text-center mb-6">
                  Create stunning visuals and compelling game experiences through art, animation, and user interface design.
                </p>
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
                  <Button
                    asChild
                    className="w-full bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 border-0 shadow-lg hover:shadow-xl hover:shadow-purple-500/25 transition-all text-white"
                  >
                    <Link href="/courses/catalog?category=art">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                  <Button
                    asChild
                    variant="outline"
                    className="w-full bg-slate-800/50 border-slate-600 text-slate-200 hover:bg-slate-700/50 hover:border-slate-500 hover:text-white"
                  >
                    <Link href="/tracks/creative-track-game-art-animation">
                      View Learning Track <BookOpen className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>

            {/* Game Design Track */}
            <Card className="group bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 hover:border-green-500/50 transition-all duration-300 hover:scale-105 hover:shadow-xl hover:shadow-green-500/10 shadow-lg">
              <CardContent className="p-8">
                <div className="bg-gradient-to-br from-green-500/20 to-green-600/20 rounded-lg p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform shadow-lg">
                  <Gamepad2 className="h-10 w-10 text-green-400" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4 text-white">Game Design</h3>
                <p className="text-slate-400 text-center mb-6">
                  Learn the art of creating engaging gameplay mechanics, compelling narratives, and memorable player experiences.
                </p>
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
                  <Button
                    asChild
                    className="w-full bg-gradient-to-r from-green-600 to-teal-600 hover:from-green-700 hover:to-teal-700 border-0 shadow-lg hover:shadow-xl hover:shadow-green-500/25 transition-all text-white"
                  >
                    <Link href="/courses/catalog?category=design">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                  <Button
                    asChild
                    variant="outline"
                    className="w-full bg-slate-800/50 border-slate-600 text-slate-200 hover:bg-slate-700/50 hover:border-slate-500 hover:text-white"
                  >
                    <Link href="/tracks/beginner-track-game-dev-fundamentals">
                      View Learning Track <BookOpen className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* Learning Tracks Section */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <div className="text-center mb-12">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
              Structured Learning Tracks
            </h2>
            <p className="text-xl text-slate-400 max-w-3xl mx-auto">
              Follow our comprehensive learning tracks that guide you from beginner to expert in your chosen field.
            </p>
          </div>

          <div className="max-w-4xl mx-auto">
            <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg hover:shadow-xl hover:shadow-white/10 transition-all">
              <CardContent className="p-8">
                <div className="text-center mb-8">
                  <div className="bg-gradient-to-br from-blue-500/20 to-purple-500/20 rounded-lg p-4 w-20 h-20 mx-auto mb-4 flex items-center justify-center shadow-lg">
                    <BookOpen className="h-10 w-10 text-blue-400" />
                  </div>
                  <h3 className="text-2xl font-bold mb-4 text-white">Comprehensive Learning Paths</h3>
                  <p className="text-slate-400 mb-6">
                    Our learning tracks provide a structured approach to mastering game development, with carefully sequenced courses, hands-on projects, and
                    milestone assessments to track your progress.
                  </p>
                </div>

                <div className="grid md:grid-cols-3 gap-6 mb-8">
                  <div className="text-center">
                    <div className="bg-gradient-to-br from-blue-500/20 to-blue-600/20 rounded-lg p-4 mb-4 shadow-lg">
                      <Code className="h-8 w-8 text-blue-400 mx-auto" />
                    </div>
                    <h4 className="font-semibold mb-2 text-white">Programming Tracks</h4>
                    <p className="text-sm text-slate-400">From C++ fundamentals to advanced game architecture</p>
                  </div>
                  <div className="text-center">
                    <div className="bg-gradient-to-br from-purple-500/20 to-purple-600/20 rounded-lg p-4 mb-4 shadow-lg">
                      <Palette className="h-8 w-8 text-purple-400 mx-auto" />
                    </div>
                    <h4 className="font-semibold mb-2 text-white">Art & Design Tracks</h4>
                    <p className="text-sm text-slate-400">Master 2D/3D art, animation, and visual design</p>
                  </div>
                  <div className="text-center">
                    <div className="bg-gradient-to-br from-green-500/20 to-green-600/20 rounded-lg p-4 mb-4 shadow-lg">
                      <Gamepad2 className="h-8 w-8 text-green-400 mx-auto" />
                    </div>
                    <h4 className="font-semibold mb-2 text-white">Game Design Tracks</h4>
                    <p className="text-sm text-slate-400">Learn mechanics, level design, and player psychology</p>
                  </div>
                </div>

                <div className="text-center">
                  <Button
                    asChild
                    size="lg"
                    className="text-lg px-8 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 border-0 shadow-lg hover:shadow-xl hover:shadow-blue-500/25 transition-all"
                  >
                    <Link href="/tracks">
                      <BookOpen className="mr-2 h-5 w-5" />
                      Explore All Learning Tracks
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
            <div className="group">
              <div className="text-4xl lg:text-5xl font-bold mb-2 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent group-hover:scale-110 transition-transform">
                30+
              </div>
              <div className="text-slate-400">Courses Available</div>
            </div>
            <div className="group">
              <div className="text-4xl lg:text-5xl font-bold mb-2 bg-gradient-to-r from-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:scale-110 transition-transform">
                50+
              </div>
              <div className="text-slate-400">Expert Instructors</div>
            </div>
            <div className="group">
              <div className="text-4xl lg:text-5xl font-bold mb-2 bg-gradient-to-r from-green-400 to-teal-400 bg-clip-text text-transparent group-hover:scale-110 transition-transform">
                1K+
              </div>
              <div className="text-slate-400">Active Students</div>
            </div>
            <div className="group">
              <div className="text-4xl lg:text-5xl font-bold mb-2 bg-gradient-to-r from-yellow-400 to-orange-400 bg-clip-text text-transparent group-hover:scale-110 transition-transform">
                95%
              </div>
              <div className="text-slate-400">Success Rate</div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
              Why Choose Game Guild Academy
            </h2>
            <p className="text-xl text-slate-400 max-w-3xl mx-auto">
              We provide the tools, knowledge, and community you need to succeed in the game development industry.
            </p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-6xl mx-auto">
            <div className="text-center group">
              <div className="bg-gradient-to-br from-blue-500/20 to-blue-600/20 rounded-lg p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform">
                <Users className="h-12 w-12 text-blue-400" />
              </div>
              <h3 className="text-xl font-semibold mb-4 text-white">Industry Experts</h3>
              <p className="text-slate-400">Learn from professionals who have shipped AAA games and work at top studios worldwide.</p>
            </div>

            <div className="text-center group">
              <div className="bg-gradient-to-br from-purple-500/20 to-purple-600/20 rounded-lg p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform">
                <Zap className="h-12 w-12 text-purple-400" />
              </div>
              <h3 className="text-xl font-semibold mb-4 text-white">Hands-On Projects</h3>
              <p className="text-slate-400">Build real games and develop a portfolio that showcases your skills to potential employers.</p>
            </div>

            <div className="text-center group">
              <div className="bg-gradient-to-br from-green-500/20 to-green-600/20 rounded-lg p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform">
                <Headphones className="h-12 w-12 text-green-400" />
              </div>
              <h3 className="text-xl font-semibold mb-4 text-white">Personalized Support</h3>
              <p className="text-slate-400">Get individual feedback on your work and guidance from instructors throughout your learning journey.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Testimonials Section */}
      <section className="py-20">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
              What Our Students Are Saying
            </h2>
            <p className="text-xl text-slate-400">Join thousands of successful graduates working in the game industry.</p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-6xl mx-auto">
            <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg hover:shadow-xl hover:shadow-white/10 transition-all hover:scale-105">
              <CardContent className="p-8">
                <div className="flex mb-4">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="h-5 w-5 text-yellow-400 fill-current" />
                  ))}
                </div>
                <p className="text-slate-400 mb-6">
                  &quot;Game Guild Academy gave me the foundation I needed to transition from hobby programming to professional game development. The hands-on
                  approach and expert feedback were invaluable.&quot;
                </p>
                <div className="flex items-center">
                  <div className="bg-gradient-to-br from-blue-500/20 to-blue-600/20 rounded-lg p-2 w-12 h-12 flex items-center justify-center mr-4 shadow-lg">
                    <Users className="h-6 w-6 text-blue-400" />
                  </div>
                  <div>
                    <div className="font-semibold text-white">Sarah Chen</div>
                    <div className="text-sm text-slate-400">Unity Developer at Indie Studio</div>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg hover:shadow-xl hover:shadow-white/10 transition-all hover:scale-105">
              <CardContent className="p-8">
                <div className="flex mb-4">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="h-5 w-5 text-yellow-400 fill-current" />
                  ))}
                </div>
                <p className="text-slate-400 mb-6">
                  &quot;The art and design courses helped me develop a professional portfolio. Within 6 months of completing the program, I landed my dream job
                  as a 3D artist.&quot;
                </p>
                <div className="flex items-center">
                  <div className="bg-gradient-to-br from-purple-500/20 to-purple-600/20 rounded-lg p-2 w-12 h-12 flex items-center justify-center mr-4 shadow-lg">
                    <Palette className="h-6 w-6 text-purple-400" />
                  </div>
                  <div>
                    <div className="font-semibold text-white">Marcus Rodriguez</div>
                    <div className="text-sm text-slate-400">3D Artist at AAA Studio</div>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm border-slate-700/50 shadow-lg hover:shadow-xl hover:shadow-white/10 transition-all hover:scale-105">
              <CardContent className="p-8">
                <div className="flex mb-4">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="h-5 w-5 text-yellow-400 fill-current" />
                  ))}
                </div>
                <p className="text-slate-400 mb-6">
                  &quot;The game design courses taught me how to think like a designer. The community and networking opportunities were just as valuable as the
                  coursework.&quot;
                </p>
                <div className="flex items-center">
                  <div className="bg-gradient-to-br from-green-500/20 to-green-600/20 rounded-lg p-2 w-12 h-12 flex items-center justify-center mr-4 shadow-lg">
                    <Gamepad2 className="h-6 w-6 text-green-400" />
                  </div>
                  <div>
                    <div className="font-semibold text-white">Alex Thompson</div>
                    <div className="text-sm text-slate-400">Lead Designer at Mobile Games Co.</div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-3xl lg:text-4xl font-bold mb-6 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
            Ready to Start Your Game Development Journey?
          </h2>
          <p className="text-xl mb-8 max-w-2xl mx-auto text-slate-300">
            Join thousands of students who have transformed their passion for games into successful careers.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Button
              asChild
              size="lg"
              className="text-lg px-8 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 border-0 shadow-lg hover:shadow-xl hover:shadow-blue-500/25 transition-all text-white"
            >
              <Link href="/courses/catalog">
                <BookOpen className="mr-2 h-5 w-5" />
                Browse All Courses
              </Link>
            </Button>
            <Button
              asChild
              size="lg"
              variant="outline"
              className="text-lg px-8 bg-slate-800/50 border-slate-600 text-slate-200 hover:bg-slate-700/50 hover:border-slate-500 backdrop-blur-sm hover:text-white"
            >
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
