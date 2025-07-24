import React from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Mail, Send } from 'lucide-react';

export function NewsletterSection() {
  return (
    <div className="border-b border-slate-700/50">
      <div className="container mx-auto max-w-6xl px-4 py-12">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-center">
          {/* Left Column - Content */}
          <div>
            <div className="inline-flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-blue-500/10 to-purple-500/10 border border-blue-500/20 rounded-full mb-4">
              <Mail className="w-4 h-4 text-blue-400" />
              <span className="text-blue-400 text-sm font-medium">Stay Updated</span>
            </div>

            <h2 className="text-2xl lg:text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-3">
              Join our newsletter
            </h2>
            <p className="text-slate-400 text-base lg:text-lg mb-4">
              Get the latest updates on courses, game jams, and community events delivered straight to your inbox.
            </p>
          </div>

          {/* Right Column - Form */}
          <div>
            <div className="flex flex-col sm:flex-row gap-3">
              <Input
                type="email"
                placeholder="Enter your email"
                className="flex-1 bg-white/5 backdrop-blur-sm text-white border-slate-600/50 placeholder:text-slate-500 h-12 focus:border-blue-400/50 focus:bg-white/10 transition-all"
              />
              <Button className="bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 text-white px-6 h-12 shadow-lg hover:shadow-blue-500/20 transition-all duration-300 group whitespace-nowrap">
                <Send className="w-4 h-4 mr-2 group-hover:translate-x-0.5 transition-transform" />
                Subscribe
              </Button>
            </div>
            <p className="text-xs text-slate-500 mt-3">No spam, unsubscribe at any time. We respect your privacy.</p>
          </div>
        </div>
      </div>
    </div>
  );
}
