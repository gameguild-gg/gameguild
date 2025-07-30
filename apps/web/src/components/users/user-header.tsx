'use client';

interface UserHeaderProps {
  userCount: number;
}

export function UserHeader({ userCount }: UserHeaderProps) {
  return (
    <div className="text-center mb-12">
      <div className="mb-6">
        {userCount > 0 && (
          <div className="flex justify-center mb-6">
            <div className="bg-gradient-to-r from-blue-600/20 to-purple-600/20 backdrop-blur-sm border border-blue-400/30 rounded-full px-4 py-2 flex items-center gap-2">
              <div className="w-2 h-2 bg-blue-400 rounded-full animate-pulse"></div>
              <span className="text-blue-300 font-semibold text-sm">
                {userCount} {userCount === 1 ? 'User' : 'Users'} • Manage Access
              </span>
            </div>
          </div>
        )}
        <h1
          className="text-5xl md:text-6xl font-bold text-white my-8"
          style={{ textShadow: '0 0 8px rgba(59, 130, 246, 0.25), 0 0 16px rgba(147, 51, 234, 0.2), 0 1px 3px rgba(0, 0, 0, 0.15)' }}
        >
          User Management
        </h1>
      </div>
      <p className="text-xl text-slate-300 max-w-3xl mx-auto leading-relaxed">
        Manage user accounts, permissions, and platform access. Monitor user activity, handle registrations, and ensure the community stays safe and engaged.
      </p>
    </div>
  );
}
