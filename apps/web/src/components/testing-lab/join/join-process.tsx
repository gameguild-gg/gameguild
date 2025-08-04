'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { ArrowLeft, Briefcase, UserPlus } from 'lucide-react';
import Link from 'next/link';

interface JoinProcessProps {
  sessionSlug: string;
}

export function JoinProcess({ sessionSlug }: JoinProcessProps) {
  const [selectedRole, setSelectedRole] = useState<'tester' | 'developer' | null>(null);

  if (selectedRole) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 py-8">
        <div className="container mx-auto px-4 max-w-2xl">
          <div className="flex items-center gap-4 mb-8">
            <Button variant="ghost" onClick={() => setSelectedRole(null)} className="text-slate-400 hover:text-white">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back
            </Button>
            <h1 className="text-2xl font-bold text-white">Join as {selectedRole === 'tester' ? 'Tester' : 'Developer'}</h1>
          </div>

          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardContent className="p-8">
              <div className="text-center space-y-6">
                <h2 className="text-xl font-semibold text-white">Complete Your {selectedRole === 'tester' ? 'Tester' : 'Developer'} Registration</h2>
                <p className="text-slate-400">{selectedRole === 'tester' ? 'Fill out your profile to join testing sessions and start earning rewards' : 'Set up your developer profile to submit games for testing'}</p>

                {/* Registration form would go here */}
                <div className="bg-slate-800/50 border border-slate-600 rounded-lg p-6">
                  <p className="text-slate-400 text-sm">Registration form component will be implemented here</p>
                </div>

                <div className="flex gap-4">
                  <Button className="flex-1 bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0">Complete Registration</Button>
                  <Button variant="outline" className="border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50" onClick={() => setSelectedRole(null)}>
                    Cancel
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 py-8">
      <div className="container mx-auto px-4">
        <div className="flex items-center gap-4 mb-8">
          <Button asChild variant="ghost" className="text-slate-400 hover:text-white">
            <Link href={`/testing-lab/sessions/${sessionSlug}`}>
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Session
            </Link>
          </Button>
        </div>

        <section className="py-16">
          <div className="max-w-4xl mx-auto text-center space-y-8">
            <div>
              <h2 className="text-3xl md:text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-4">Ready to Get Started?</h2>
              <p className="text-xl text-slate-300 max-w-2xl mx-auto">Join our community of testers and developers to shape the future of gaming</p>
            </div>

            <div className="grid md:grid-cols-2 gap-8">
              {/* For Testers */}
              <Card className="bg-gradient-to-br from-blue-900/20 to-blue-800/20 border-blue-700/50 backdrop-blur-sm">
                <CardContent className="p-8 text-center space-y-6">
                  <div className="flex justify-center">
                    <div className="bg-gradient-to-r from-blue-600 to-blue-700 p-4 rounded-full">
                      <UserPlus className="h-8 w-8 text-white" />
                    </div>
                  </div>

                  <div>
                    <h3 className="text-2xl font-bold text-white mb-2">Join as Tester</h3>
                    <p className="text-slate-300 mb-6">Test games, provide feedback, and earn rewards while having fun</p>
                    <ul className="text-left text-slate-400 space-y-2 mb-6">
                      <li>• Play games before release</li>
                      <li>• Earn testing credits and rewards</li>
                      <li>• Connect with developers</li>
                      <li>• Flexible scheduling</li>
                    </ul>
                  </div>

                  <Button size="lg" onClick={() => setSelectedRole('tester')} className="w-full bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0">
                    Sign Up as Tester
                  </Button>
                </CardContent>
              </Card>

              {/* For Developers */}
              <Card className="bg-gradient-to-br from-purple-900/20 to-purple-800/20 border-purple-700/50 backdrop-blur-sm">
                <CardContent className="p-8 text-center space-y-6">
                  <div className="flex justify-center">
                    <div className="bg-gradient-to-r from-purple-600 to-purple-700 p-4 rounded-full">
                      <Briefcase className="h-8 w-8 text-white" />
                    </div>
                  </div>

                  <div>
                    <h3 className="text-2xl font-bold text-white mb-2">Submit Your Game</h3>
                    <p className="text-slate-300 mb-6">Get valuable feedback from real players to improve your game</p>
                    <ul className="text-left text-slate-400 space-y-2 mb-6">
                      <li>• Quality feedback from experienced testers</li>
                      <li>• Organized testing sessions</li>
                      <li>• Detailed reports and analytics</li>
                      <li>• Community-driven insights</li>
                    </ul>
                  </div>

                  <Button size="lg" onClick={() => setSelectedRole('developer')} variant="outline" className="w-full border-purple-600 bg-purple-800/20 text-purple-300 hover:bg-purple-700/30">
                    Submit Game for Testing
                  </Button>
                </CardContent>
              </Card>
            </div>

            {/* Additional Info */}
            <div className="bg-gradient-to-r from-slate-900/50 to-slate-800/50 border border-slate-700 rounded-lg p-8 backdrop-blur-sm">
              <h4 className="text-lg font-semibold text-white mb-4">How It Works</h4>
              <div className="grid md:grid-cols-3 gap-6 text-left">
                <div>
                  <div className="text-blue-400 font-semibold mb-2">1. Browse Sessions</div>
                  <p className="text-slate-400 text-sm">Find testing sessions that match your interests and availability</p>
                </div>
                <div>
                  <div className="text-purple-400 font-semibold mb-2">2. Join & Test</div>
                  <p className="text-slate-400 text-sm">Participate in structured testing sessions with clear guidelines</p>
                </div>
                <div>
                  <div className="text-green-400 font-semibold mb-2">3. Earn Rewards</div>
                  <p className="text-slate-400 text-sm">Get credits, certificates, or free games for your valuable feedback</p>
                </div>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
