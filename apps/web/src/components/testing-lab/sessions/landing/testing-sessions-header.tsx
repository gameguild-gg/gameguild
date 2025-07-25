'use client';

interface SessionHeaderProps {
  sessionCount: number;
}

export function TestingSessionsHeader({ sessionCount }: SessionHeaderProps) {
  return (
    <div className="text-center mb-12">
      <div className="mb-6">
        {sessionCount > 0 && (
          <div className="flex justify-center mb-6">
            <div className="bg-gradient-to-r from-blue-600/20 to-purple-600/20 backdrop-blur-sm border border-blue-400/30 rounded-full px-4 py-2 flex items-center gap-2">
              <div className="w-2 h-2 bg-blue-400 rounded-full animate-pulse"></div>
              <span className="text-blue-300 font-semibold text-sm">
                {sessionCount} Open {sessionCount === 1 ? 'Session' : 'Sessions'} • Join Now!
              </span>
            </div>
          </div>
        )}
        <h1
          className="text-5xl md:text-6xl font-bold text-white my-8"
          style={{ textShadow: '0 0 8px rgba(59, 130, 246, 0.25), 0 0 16px rgba(147, 51, 234, 0.2), 0 1px 3px rgba(0, 0, 0, 0.15)' }}
        >
          Test. Play. Earn.
        </h1>
      </div>
      <p className="text-xl text-slate-300 max-w-3xl mx-auto leading-relaxed">
        Join exclusive game testing sessions and shape the future of gaming. Get early access, provide valuable feedback, and earn amazing rewards while playing
        the latest titles before anyone else.
      </p>
    </div>
  );
}
