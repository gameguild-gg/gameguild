'use client';

import { Button } from '@game-guild/ui/components';
import { ArrowRight, Play } from 'lucide-react';

export default function CoursesBanner() {
  const scrollToCourses = () => {
    const coursesSection = document.querySelector('#courses-section');
    if (coursesSection) {
      coursesSection.scrollIntoView({ behavior: 'smooth' });
    }
  };

  return (
    <section className="relative w-screen h-[600px] overflow-hidden left-1/2 right-1/2 -ml-[50vw] -mr-[50vw]">
      {/* Background with gradient overlay */}
      <div className="absolute inset-0 bg-gradient-to-r from-purple-900/90 via-blue-900/80 to-cyan-900/90" />

      {/* Animated background pattern with more elements */}
      <div className="absolute inset-0 opacity-20">
        <div className="absolute top-10 left-10 w-32 h-32 bg-purple-500/20 rounded-full blur-xl animate-pulse" />
        <div className="absolute top-40 right-20 w-48 h-48 bg-blue-500/20 rounded-full blur-xl animate-pulse delay-1000" />
        <div className="absolute bottom-20 left-1/4 w-40 h-40 bg-cyan-500/20 rounded-full blur-xl animate-pulse delay-2000" />
        <div className="absolute bottom-10 right-10 w-24 h-24 bg-pink-500/20 rounded-full blur-xl animate-pulse delay-500" />

        {/* Additional floating particles */}
        <div className="absolute top-1/3 left-1/3 w-16 h-16 bg-yellow-500/10 rounded-full blur-lg animate-pulse delay-3000" />
        <div className="absolute top-1/2 right-1/3 w-20 h-20 bg-green-500/10 rounded-full blur-lg animate-pulse delay-1500" />
      </div>

      {/* Floating elements */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute top-16 left-16 w-4 h-4 bg-yellow-400 rounded-full animate-bounce" style={{ animationDelay: '0.5s' }} />
        <div className="absolute top-32 right-32 w-3 h-3 bg-green-400 rounded-full animate-bounce" style={{ animationDelay: '1.5s' }} />
        <div className="absolute bottom-32 left-32 w-5 h-5 bg-purple-400 rounded-full animate-bounce" style={{ animationDelay: '2s' }} />
        <div className="absolute bottom-16 right-16 w-4 h-4 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '0.8s' }} />
      </div>

      {/* Main content */}
      <div className="relative z-10 flex items-center justify-center h-full">
        <div className="text-center text-white px-6 max-w-4xl">
          {/* Badge */}
          <div className="inline-flex items-center px-4 py-2 bg-white/10 backdrop-blur-sm border border-white/20 rounded-full text-sm font-medium mb-6">
            <span className="w-2 h-2 bg-green-400 rounded-full mr-2 animate-pulse" />
            Learn from Industry Professionals
          </div>

          {/* Main heading */}
          <h1 className="text-5xl md:text-7xl font-bold mb-6 bg-gradient-to-r from-white via-purple-200 to-cyan-200 bg-clip-text text-transparent">
            Master the Art of
            <br />
            <span className="text-6xl md:text-8xl font-extrabold bg-gradient-to-r from-purple-400 via-pink-400 to-cyan-400 bg-clip-text text-transparent">
              Game Development
            </span>
          </h1>

          {/* Subtitle */}
          <p className="text-xl md:text-2xl text-gray-200 mb-8 max-w-3xl mx-auto leading-relaxed">
            Innovative online education for a successful career in the creative industries. Learn from the industry&apos;s best with hands-on projects and
            expert mentorship.
          </p>

          {/* Stats */}
          <div className="flex flex-wrap justify-center gap-8 mb-10">
            <div className="text-center">
              <div className="text-3xl md:text-4xl font-bold text-white">500+</div>
              <div className="text-sm text-gray-300">Expert Instructors</div>
            </div>
            <div className="text-center">
              <div className="text-3xl md:text-4xl font-bold text-white">10k+</div>
              <div className="text-sm text-gray-300">Students Worldwide</div>
            </div>
            <div className="text-center">
              <div className="text-3xl md:text-4xl font-bold text-white">95%</div>
              <div className="text-sm text-gray-300">Success Rate</div>
            </div>
          </div>

          {/* CTA buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
            <Button
              size="lg"
              onClick={scrollToCourses}
              className="bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 text-white px-8 py-4 text-lg font-semibold rounded-full shadow-lg hover:shadow-xl transition-all duration-300 transform hover:scale-105"
            >
              Explore Courses
              <ArrowRight className="ml-2 h-5 w-5" />
            </Button>

            <Button
              variant="outline"
              size="lg"
              className="border-white/30 text-white hover:bg-white/10 backdrop-blur-sm px-8 py-4 text-lg font-semibold rounded-full transition-all duration-300"
            >
              <Play className="mr-2 h-5 w-5" />
              Watch Demo
            </Button>
          </div>

          {/* Trust indicators */}
          <div className="mt-12 pt-8 border-t border-white/20">
            <p className="text-sm text-gray-300 mb-4">Trusted by professionals at</p>
            <div className="flex flex-wrap justify-center items-center gap-8 opacity-60">
              <div className="px-4 py-2 bg-white/10 rounded-lg text-sm font-medium">Epic Games</div>
              <div className="px-4 py-2 bg-white/10 rounded-lg text-sm font-medium">Ubisoft</div>
              <div className="px-4 py-2 bg-white/10 rounded-lg text-sm font-medium">Riot Games</div>
              <div className="px-4 py-2 bg-white/10 rounded-lg text-sm font-medium">Blizzard</div>
              <div className="px-4 py-2 bg-white/10 rounded-lg text-sm font-medium">Unity</div>
            </div>
          </div>
        </div>
      </div>

      {/* Decorative elements */}
      <div className="absolute top-0 left-0 w-full h-2 bg-gradient-to-r from-purple-500 via-pink-500 to-cyan-500" />
    </section>
  );
}
