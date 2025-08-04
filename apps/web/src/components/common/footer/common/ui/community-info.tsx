import React from 'react';
import { Gamepad2, Heart, Users } from 'lucide-react';

export function CommunityInfo() {
  return (
    <div className="lg:col-span-2">
      <div className="flex items-center gap-2 mb-6">
        <div className="w-10 h-10 bg-gradient-to-br from-blue-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
          <Gamepad2 className="w-5 h-5 text-white" />
        </div>
        <span className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Game Guild</span>
      </div>

      <p className="text-slate-400 mb-6 max-w-sm leading-relaxed">A thriving gaming community dedicated to education, collaboration, and innovation. Join us as we grow together and shape the future of gaming.</p>

      <div className="space-y-3 text-sm">
        <div className="flex items-center gap-3 text-slate-400 hover:text-blue-400 transition-colors group">
          <div className="p-2 bg-slate-800/50 rounded-lg group-hover:bg-blue-500/10 transition-colors">
            <Users className="w-4 h-4" />
          </div>
          <div>
            <span className="font-medium">Community-driven</span>
            <span className="ml-2">learning and development</span>
          </div>
        </div>

        <div className="flex items-center gap-3 text-slate-400 hover:text-purple-400 transition-colors group">
          <div className="p-2 bg-slate-800/50 rounded-lg group-hover:bg-purple-500/10 transition-colors">
            <Heart className="w-4 h-4" />
          </div>
          <div>
            <span className="font-medium">Open source</span>
            <span className="ml-2">and collaborative</span>
          </div>
        </div>
      </div>
    </div>
  );
}
