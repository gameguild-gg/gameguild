import React from 'react';
import Link from 'next/link';
import { Button } from '@game-guild/ui/components';

const ExploreMoreSection: React.FC = () => {
  return (
    <section className="w-full bg-black text-white py-20 px-4">
      <div className="container mx-auto max-w-4xl text-center">
        {/* Sleeping character illustration */}
        <div className="mb-8 flex justify-center">
          <div className="relative">
            {/* Simple CSS illustration of a sleeping character */}
            <div className="w-32 h-20 relative">
              {/* Character body */}
              <div className="absolute bottom-0 left-1/2 transform -translate-x-1/2">
                <div className="w-20 h-12 bg-gradient-to-br from-amber-200 to-amber-300 rounded-full"></div>
                {/* Character head/ears */}
                <div className="absolute -top-2 left-2">
                  <div className="w-6 h-8 bg-gradient-to-br from-amber-600 to-amber-700 rounded-full transform -rotate-12"></div>
                </div>
                <div className="absolute -top-2 right-2">
                  <div className="w-6 h-8 bg-gradient-to-br from-amber-600 to-amber-700 rounded-full transform rotate-12"></div>
                </div>
                {/* Character face */}
                <div className="absolute top-1 left-1/2 transform -translate-x-1/2 w-12 h-8 bg-gradient-to-br from-amber-500 to-amber-600 rounded-full">
                  {/* Sleeping eyes */}
                  <div className="absolute top-2 left-2 w-1 h-1 bg-black rounded-full"></div>
                  <div className="absolute top-2 right-2 w-1 h-1 bg-black rounded-full"></div>
                  {/* Small nose */}
                  <div className="absolute top-3 left-1/2 transform -translate-x-1/2 w-0.5 h-0.5 bg-black rounded-full"></div>
                </div>
                {/* Character tail */}
                <div className="absolute top-0 -right-4">
                  <div className="w-8 h-3 bg-gradient-to-r from-amber-600 to-amber-700 rounded-full transform rotate-45"></div>
                </div>
              </div>
              {/* Blanket */}
              <div className="absolute bottom-0 left-1/2 transform -translate-x-1/2 w-24 h-8 bg-gradient-to-br from-rose-200 to-rose-300 rounded-t-full opacity-90"></div>
            </div>
          </div>
        </div>

        {/* Text content */}
        <div className="mb-8">
          <h2 className="text-4xl md:text-5xl font-bold mb-4 leading-tight">Still looking for something to watch?</h2>
          <p className="text-xl md:text-2xl text-gray-300">Check out our full library</p>
        </div>

        {/* CTA Button */}
        <Button
          asChild
          className="bg-transparent border-2 border-orange-500 text-orange-500 hover:bg-orange-500 hover:text-white transition-all duration-300 px-8 py-3 text-lg font-semibold rounded-lg"
        >
          <Link href="/courses">VIEW ALL</Link>
        </Button>
      </div>
    </section>
  );
};

export default ExploreMoreSection;
