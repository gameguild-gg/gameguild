import Link from 'next/link';
import { Button } from '@game-guild/ui/components';
import { Card, CardContent } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components';
import { BookOpen, Users, Trophy, Zap, Code, Palette, Gamepad2, Headphones, Star, CheckCircle, ArrowRight, PlayCircle } from 'lucide-react';

export default function CoursesLandingPage() {
  return (
    <div className="min-h-screen">
      {/* Hero Section */}
      <section className="relative bg-gradient-to-br from-primary/10 via-primary/5 to-background py-20 lg:py-32">
        <div className="container mx-auto px-4">
          <div className="max-w-4xl mx-auto text-center">
            <Badge variant="outline" className="mb-6 text-sm font-medium">
              🎮 Master Game Development
            </Badge>
            <h1 className="text-4xl lg:text-6xl font-bold mb-6 bg-gradient-to-r from-primary to-primary/60 bg-clip-text text-transparent">
              Learn from the Industry&apos;s Best
            </h1>
            <p className="text-xl lg:text-2xl text-muted-foreground mb-8 max-w-3xl mx-auto">
              Game Development Shapes the World Around Us. Now, You Can Take Part.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center mb-12">
              <Button asChild size="lg" className="text-lg px-8">
                <Link href="/courses/catalog">
                  <BookOpen className="mr-2 h-5 w-5" />
                  Explore Courses
                </Link>
              </Button>
              <Button asChild variant="outline" size="lg" className="text-lg px-8">
                <Link href="#pathways">
                  <PlayCircle className="mr-2 h-5 w-5" />
                  Find Your Path
                </Link>
              </Button>
            </div>

            {/* Key Features */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-6 max-w-4xl mx-auto">
              <div className="text-center">
                <div className="bg-primary/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Users className="h-6 w-6 text-primary" />
                </div>
                <p className="text-sm font-medium">Expert Training</p>
                <p className="text-xs text-muted-foreground">Industry Professionals</p>
              </div>
              <div className="text-center">
                <div className="bg-primary/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Zap className="h-6 w-6 text-primary" />
                </div>
                <p className="text-sm font-medium">Interactive Learning</p>
                <p className="text-xs text-muted-foreground">Hands-on Projects</p>
              </div>
              <div className="text-center">
                <div className="bg-primary/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <Trophy className="h-6 w-6 text-primary" />
                </div>
                <p className="text-sm font-medium">Personalized Feedback</p>
                <p className="text-xs text-muted-foreground">Live Q&A Sessions</p>
              </div>
              <div className="text-center">
                <div className="bg-primary/10 rounded-full p-3 w-16 h-16 mx-auto mb-3 flex items-center justify-center">
                  <BookOpen className="h-6 w-6 text-primary" />
                </div>
                <p className="text-sm font-medium">Lifetime Access</p>
                <p className="text-xs text-muted-foreground">Learn at Your Pace</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Learning Pathways Section */}
      <section id="pathways" className="py-20 bg-muted/30">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4">Find Your Path</h2>
            <p className="text-xl text-muted-foreground max-w-3xl mx-auto">One Campus. Multiple Disciplines. Endless Possibilities.</p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-7xl mx-auto">
            {/* Game Programming Track */}
            <Card className="group hover:shadow-lg transition-all duration-300 border-2 hover:border-primary/20">
              <CardContent className="p-8">
                <div className="bg-gradient-to-br from-blue-500/10 to-blue-600/10 rounded-full p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform">
                  <Code className="h-10 w-10 text-blue-600" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4">Game Programming</h3>
                <p className="text-muted-foreground text-center mb-6">
                  Master the technical foundations of game development with C#, Unity, and advanced programming concepts.
                </p>
                <div className="space-y-2 mb-6">
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>C# Programming Fundamentals</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>Unity Engine Development</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>Game Architecture & Design Patterns</span>
                  </div>
                </div>
                <div className="space-y-3">
                  <Button asChild className="w-full">
                    <Link href="/courses/catalog?category=programming">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                  <Button asChild variant="outline" className="w-full">
                    <Link href="/tracks/beginner-track-game-dev-fundamentals">
                      View Learning Track <BookOpen className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>

            {/* Game Art & Design Track */}
            <Card className="group hover:shadow-lg transition-all duration-300 border-2 hover:border-primary/20">
              <CardContent className="p-8">
                <div className="bg-gradient-to-br from-purple-500/10 to-purple-600/10 rounded-full p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform">
                  <Palette className="h-10 w-10 text-purple-600" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4">Game Art & Design</h3>
                <p className="text-muted-foreground text-center mb-6">
                  Create stunning visuals and compelling game experiences through art, animation, and user interface design.
                </p>
                <div className="space-y-2 mb-6">
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>2D & 3D Art Creation</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>Character & Environment Design</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>UI/UX Design for Games</span>
                  </div>
                </div>
                <div className="space-y-3">
                  <Button asChild className="w-full">
                    <Link href="/courses/catalog?category=art">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                  <Button asChild variant="outline" className="w-full">
                    <Link href="/tracks/creative-track-game-art-animation">
                      View Learning Track <BookOpen className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                </div>
              </CardContent>
            </Card>

            {/* Game Design Track */}
            <Card className="group hover:shadow-lg transition-all duration-300 border-2 hover:border-primary/20">
              <CardContent className="p-8">
                <div className="bg-gradient-to-br from-green-500/10 to-green-600/10 rounded-full p-4 w-20 h-20 mx-auto mb-6 flex items-center justify-center group-hover:scale-110 transition-transform">
                  <Gamepad2 className="h-10 w-10 text-green-600" />
                </div>
                <h3 className="text-2xl font-bold text-center mb-4">Game Design</h3>
                <p className="text-muted-foreground text-center mb-6">
                  Learn the art of creating engaging gameplay mechanics, compelling narratives, and memorable player experiences.
                </p>
                <div className="space-y-2 mb-6">
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>Game Mechanics & Systems</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>Level Design & Pacing</span>
                  </div>
                  <div className="flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 text-green-500 mr-2" />
                    <span>Narrative & Storytelling</span>
                  </div>
                </div>
                <div className="space-y-3">
                  <Button asChild className="w-full">
                    <Link href="/courses/catalog?category=design">
                      Browse Courses <ArrowRight className="ml-2 h-4 w-4" />
                    </Link>
                  </Button>
                  <Button asChild variant="outline" className="w-full">
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
      <section className="py-16 bg-background">
        <div className="container mx-auto px-4">
          <div className="text-center mb-12">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4">Structured Learning Tracks</h2>
            <p className="text-xl text-muted-foreground max-w-3xl mx-auto">
              Follow our comprehensive learning tracks that guide you from beginner to expert in your chosen field.
            </p>
          </div>

          <div className="max-w-4xl mx-auto">
            <Card className="border-2 border-primary/20 shadow-lg">
              <CardContent className="p-8">
                <div className="text-center mb-8">
                  <div className="bg-primary/10 rounded-full p-4 w-20 h-20 mx-auto mb-4 flex items-center justify-center">
                    <BookOpen className="h-10 w-10 text-primary" />
                  </div>
                  <h3 className="text-2xl font-bold mb-4">Comprehensive Learning Paths</h3>
                  <p className="text-muted-foreground mb-6">
                    Our learning tracks provide a structured approach to mastering game development, with carefully sequenced courses, hands-on projects, and
                    milestone assessments to track your progress.
                  </p>
                </div>

                <div className="grid md:grid-cols-3 gap-6 mb-8">
                  <div className="text-center">
                    <div className="bg-blue-500/10 rounded-lg p-4 mb-4">
                      <Code className="h-8 w-8 text-blue-600 mx-auto" />
                    </div>
                    <h4 className="font-semibold mb-2">Programming Tracks</h4>
                    <p className="text-sm text-muted-foreground">From C++ fundamentals to advanced game architecture</p>
                  </div>
                  <div className="text-center">
                    <div className="bg-purple-500/10 rounded-lg p-4 mb-4">
                      <Palette className="h-8 w-8 text-purple-600 mx-auto" />
                    </div>
                    <h4 className="font-semibold mb-2">Art & Design Tracks</h4>
                    <p className="text-sm text-muted-foreground">Master 2D/3D art, animation, and visual design</p>
                  </div>
                  <div className="text-center">
                    <div className="bg-green-500/10 rounded-lg p-4 mb-4">
                      <Gamepad2 className="h-8 w-8 text-green-600 mx-auto" />
                    </div>
                    <h4 className="font-semibold mb-2">Game Design Tracks</h4>
                    <p className="text-sm text-muted-foreground">Learn mechanics, level design, and player psychology</p>
                  </div>
                </div>

                <div className="text-center">
                  <Button asChild size="lg" className="text-lg px-8">
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
      <section className="py-16 bg-primary text-primary-foreground">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-8 text-center">
            <div>
              <div className="text-4xl lg:text-5xl font-bold mb-2">30+</div>
              <div className="text-primary-foreground/80">Courses Available</div>
            </div>
            <div>
              <div className="text-4xl lg:text-5xl font-bold mb-2">50+</div>
              <div className="text-primary-foreground/80">Expert Instructors</div>
            </div>
            <div>
              <div className="text-4xl lg:text-5xl font-bold mb-2">1K+</div>
              <div className="text-primary-foreground/80">Active Students</div>
            </div>
            <div>
              <div className="text-4xl lg:text-5xl font-bold mb-2">95%</div>
              <div className="text-primary-foreground/80">Success Rate</div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4">Why Choose Game Guild Academy</h2>
            <p className="text-xl text-muted-foreground max-w-3xl mx-auto">
              We provide the tools, knowledge, and community you need to succeed in the game development industry.
            </p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-6xl mx-auto">
            <div className="text-center">
              <div className="bg-primary/10 rounded-full p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center">
                <Users className="h-12 w-12 text-primary" />
              </div>
              <h3 className="text-xl font-semibold mb-4">Industry Experts</h3>
              <p className="text-muted-foreground">Learn from professionals who have shipped AAA games and work at top studios worldwide.</p>
            </div>

            <div className="text-center">
              <div className="bg-primary/10 rounded-full p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center">
                <Zap className="h-12 w-12 text-primary" />
              </div>
              <h3 className="text-xl font-semibold mb-4">Hands-On Projects</h3>
              <p className="text-muted-foreground">Build real games and develop a portfolio that showcases your skills to potential employers.</p>
            </div>

            <div className="text-center">
              <div className="bg-primary/10 rounded-full p-6 w-24 h-24 mx-auto mb-6 flex items-center justify-center">
                <Headphones className="h-12 w-12 text-primary" />
              </div>
              <h3 className="text-xl font-semibold mb-4">Personalized Support</h3>
              <p className="text-muted-foreground">Get individual feedback on your work and guidance from instructors throughout your learning journey.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Testimonials Section */}
      <section className="py-20 bg-muted/30">
        <div className="container mx-auto px-4">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold mb-4">What Our Students Are Saying</h2>
            <p className="text-xl text-muted-foreground">Join thousands of successful graduates working in the game industry.</p>
          </div>

          <div className="grid lg:grid-cols-3 gap-8 max-w-6xl mx-auto">
            <Card className="border-0 shadow-md">
              <CardContent className="p-8">
                <div className="flex mb-4">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="h-5 w-5 text-yellow-400 fill-current" />
                  ))}
                </div>
                <p className="text-muted-foreground mb-6">
                  &quot;Game Guild Academy gave me the foundation I needed to transition from hobby programming to professional game development. The hands-on
                  approach and expert feedback were invaluable.&quot;
                </p>
                <div className="flex items-center">
                  <div className="bg-primary/10 rounded-full p-2 w-12 h-12 flex items-center justify-center mr-4">
                    <Users className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <div className="font-semibold">Sarah Chen</div>
                    <div className="text-sm text-muted-foreground">Unity Developer at Indie Studio</div>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card className="border-0 shadow-md">
              <CardContent className="p-8">
                <div className="flex mb-4">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="h-5 w-5 text-yellow-400 fill-current" />
                  ))}
                </div>
                <p className="text-muted-foreground mb-6">
                  &quot;The art and design courses helped me develop a professional portfolio. Within 6 months of completing the program, I landed my dream job
                  as a 3D artist.&quot;
                </p>
                <div className="flex items-center">
                  <div className="bg-primary/10 rounded-full p-2 w-12 h-12 flex items-center justify-center mr-4">
                    <Palette className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <div className="font-semibold">Marcus Rodriguez</div>
                    <div className="text-sm text-muted-foreground">3D Artist at AAA Studio</div>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card className="border-0 shadow-md">
              <CardContent className="p-8">
                <div className="flex mb-4">
                  {[...Array(5)].map((_, i) => (
                    <Star key={i} className="h-5 w-5 text-yellow-400 fill-current" />
                  ))}
                </div>
                <p className="text-muted-foreground mb-6">
                  &quot;The game design courses taught me how to think like a designer. The community and networking opportunities were just as valuable as the
                  coursework.&quot;
                </p>
                <div className="flex items-center">
                  <div className="bg-primary/10 rounded-full p-2 w-12 h-12 flex items-center justify-center mr-4">
                    <Gamepad2 className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <div className="font-semibold">Alex Thompson</div>
                    <div className="text-sm text-muted-foreground">Lead Designer at Mobile Games Co.</div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20 bg-gradient-to-r from-primary to-primary/80 text-primary-foreground">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-3xl lg:text-4xl font-bold mb-6">Ready to Start Your Game Development Journey?</h2>
          <p className="text-xl mb-8 max-w-2xl mx-auto text-primary-foreground/90">
            Join thousands of students who have transformed their passion for games into successful careers.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Button asChild size="lg" variant="secondary" className="text-lg px-8">
              <Link href="/courses/catalog">
                <BookOpen className="mr-2 h-5 w-5" />
                Browse All Courses
              </Link>
            </Button>
            <Button
              asChild
              size="lg"
              variant="outline"
              className="text-lg px-8 border-primary-foreground/20 text-primary-foreground hover:bg-primary-foreground/10"
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
