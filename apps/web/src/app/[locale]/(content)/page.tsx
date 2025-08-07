import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  ArrowRight,
  BookOpen,
  Code,
  TestTube,
  Trophy,
  Users,
  Zap
} from 'lucide-react';
import Link from 'next/link';
import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
      {/* Hero Section */}
      <section className="relative py-16 lg:py-24 px-4 overflow-hidden">
        <div className="container mx-auto max-w-6xl">
          <div className="text-center max-w-4xl mx-auto">
            <Badge variant="outline" className="mb-6 text-sm font-medium bg-gradient-to-r from-purple-500/20 to-blue-500/20 border-purple-500/30 text-purple-300">
              🎮 Master Game Development
            </Badge>
            <h1 className="text-4xl lg:text-6xl font-bold mb-6 bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent leading-tight">
              Learn, Build & Connect
            </h1>
            <p className="text-lg lg:text-xl text-slate-300 mb-8 max-w-3xl mx-auto leading-relaxed">
              Join the ultimate gaming community dedicated to education, collaboration, and innovation.
              Master game development skills and shape the future of gaming.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Button asChild size="lg" className="bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white">
                <Link href="/courses">
                  Start Learning
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
              <Button asChild variant="outline" size="lg" className="border-slate-600 text-slate-300 hover:bg-slate-800">
                <Link href="/community">
                  Join Community
                </Link>
              </Button>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-16 lg:py-24 px-4">
        <div className="container mx-auto max-w-6xl">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold text-white mb-4">
              Everything You Need to Succeed
            </h2>
            <p className="text-lg text-slate-400 max-w-2xl mx-auto">
              Comprehensive tools and resources to accelerate your game development journey
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 lg:gap-8">
            {/* Interactive Courses */}
            <Card className="bg-slate-800/50 border-slate-700 hover:border-slate-600 transition-colors">
              <CardContent className="p-6">
                <div className="flex items-center mb-4">
                  <div className="p-3 bg-blue-500/20 rounded-lg mr-4">
                    <BookOpen className="h-6 w-6 text-blue-400" />
                  </div>
                  <h3 className="text-xl font-semibold text-white">Interactive Courses</h3>
                </div>
                <p className="text-slate-400 leading-relaxed">
                  Learn game development through hands-on projects and real-world examples.
                  From beginner to advanced, we&apos;ve got you covered.
                </p>
              </CardContent>
            </Card>

            {/* Live Testing Lab */}
            <Card className="bg-slate-800/50 border-slate-700 hover:border-slate-600 transition-colors">
              <CardContent className="p-6">
                <div className="flex items-center mb-4">
                  <div className="p-3 bg-purple-500/20 rounded-lg mr-4">
                    <TestTube className="h-6 w-6 text-purple-400" />
                  </div>
                  <h3 className="text-xl font-semibold text-white">Testing Lab</h3>
                </div>
                <p className="text-slate-400 leading-relaxed">
                  Test games in development and provide valuable feedback.
                  Earn rewards while helping developers improve their creations.
                </p>
              </CardContent>
            </Card>

            {/* Community */}
            <Card className="bg-slate-800/50 border-slate-700 hover:border-slate-600 transition-colors">
              <CardContent className="p-6">
                <div className="flex items-center mb-4">
                  <div className="p-3 bg-green-500/20 rounded-lg mr-4">
                    <Users className="h-6 w-6 text-green-400" />
                  </div>
                  <h3 className="text-xl font-semibold text-white">Vibrant Community</h3>
                </div>
                <p className="text-slate-400 leading-relaxed">
                  Connect with fellow developers, share knowledge, and collaborate on projects.
                  Build lasting relationships in the gaming industry.
                </p>
              </CardContent>
            </Card>

            {/* Game Jams */}
            <Card className="bg-slate-800/50 border-slate-700 hover:border-slate-600 transition-colors">
              <CardContent className="p-6">
                <div className="flex items-center mb-4">
                  <div className="p-3 bg-yellow-500/20 rounded-lg mr-4">
                    <Trophy className="h-6 w-6 text-yellow-400" />
                  </div>
                  <h3 className="text-xl font-semibold text-white">Game Jams</h3>
                </div>
                <p className="text-slate-400 leading-relaxed">
                  Participate in exciting game jams and competitions.
                  Challenge yourself, showcase your skills, and win amazing prizes.
                </p>
              </CardContent>
            </Card>

            {/* Job Board */}
            <Card className="bg-slate-800/50 border-slate-700 hover:border-slate-600 transition-colors">
              <CardContent className="p-6">
                <div className="flex items-center mb-4">
                  <div className="p-3 bg-indigo-500/20 rounded-lg mr-4">
                    <Code className="h-6 w-6 text-indigo-400" />
                  </div>
                  <h3 className="text-xl font-semibold text-white">Job Board</h3>
                </div>
                <p className="text-slate-400 leading-relaxed">
                  Find your dream job in the gaming industry.
                  Connect with top studios and opportunities worldwide.
                </p>
              </CardContent>
            </Card>

            {/* Resources */}
            <Card className="bg-slate-800/50 border-slate-700 hover:border-slate-600 transition-colors">
              <CardContent className="p-6">
                <div className="flex items-center mb-4">
                  <div className="p-3 bg-red-500/20 rounded-lg mr-4">
                    <Zap className="h-6 w-6 text-red-400" />
                  </div>
                  <h3 className="text-xl font-semibold text-white">Rich Resources</h3>
                </div>
                <p className="text-slate-400 leading-relaxed">
                  Access tutorials, documentation, and tools to accelerate your development.
                  Everything you need in one place.
                </p>
              </CardContent>
            </Card>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-16 lg:py-24 px-4">
        <div className="container mx-auto max-w-4xl text-center">
          <div className="bg-gradient-to-r from-blue-600/20 to-purple-600/20 rounded-2xl p-8 lg:p-12 border border-blue-500/30">
            <h2 className="text-3xl lg:text-4xl font-bold text-white mb-4">
              Ready to Start Your Journey?
            </h2>
            <p className="text-lg text-slate-300 mb-8 max-w-2xl mx-auto">
              Join thousands of developers who are already building amazing games with Game Guild
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Button asChild size="lg" className="bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white">
                <Link href="/courses">
                  Explore Courses
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
              <Button asChild variant="outline" size="lg" className="border-slate-600 text-slate-300 hover:bg-slate-800">
                <Link href="/about">
                  Learn More
                </Link>
              </Button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
