'use client';

import React, { useState, useTransition } from 'react';
import { Image, MapPin, Smile, Type, Users } from 'lucide-react';
import { createPostAction } from '@/lib/feed/server-actions';
import { useRouter } from '@/i18n/navigation';

export function CreatePostForm() {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [content, setContent] = useState('');
  const [postType, setPostType] = useState<string>('general');
  const [isExpanded, setIsExpanded] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!content.trim()) return;

    const formData = new FormData();
    formData.append('content', content.trim());
    formData.append('postType', postType);

    startTransition(async () => {
      const result = await createPostAction(formData);

      if (result.success) {
        setContent('');
        setPostType('general');
        setIsExpanded(false);
        // Optionally refresh the page or update the feed
        router.refresh();
      } else {
        console.error('Failed to create post:', result.error);
        // TODO: Add proper error handling/toast notification
      }
    });
  };

  const postTypes = [
    { value: 'general', label: 'General', icon: Type },
    { value: 'announcement', label: 'Announcement', icon: Users },
    { value: 'discussion', label: 'Discussion', icon: Users },
    { value: 'question', label: 'Question', icon: Type },
    { value: 'showcase', label: 'Showcase', icon: Image },
  ];

  return (
    <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-xl p-6 border border-slate-700/50 shadow-xl">
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Textarea */}
        <div className="relative">
          <textarea
            value={content}
            onChange={(e) => setContent(e.target.value)}
            onFocus={() => setIsExpanded(true)}
            placeholder="What's on your mind? Share with the community..."
            className="w-full bg-slate-800/50 border border-slate-600/50 rounded-lg px-4 py-3 text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500/50 resize-none transition-all duration-200"
            rows={isExpanded ? 4 : 2}
            maxLength={2000}
          />
          <div className="absolute bottom-2 right-2 text-xs text-slate-400">{content.length}/2000</div>
        </div>

        {/* Expanded Options */}
        {isExpanded && (
          <div className="space-y-4 animate-in slide-in-from-top-1 duration-200">
            {/* Post Type Selector */}
            <div className="flex flex-wrap gap-2">
              {postTypes.map((type) => {
                const Icon = type.icon;
                return (
                  <button
                    key={type.value}
                    type="button"
                    onClick={() => setPostType(type.value)}
                    className={`flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200 ${
                      postType === type.value ? 'bg-blue-600 text-white shadow-lg' : 'bg-slate-700/50 text-slate-300 hover:bg-slate-600/50 hover:text-white'
                    }`}
                  >
                    <Icon size={16} />
                    {type.label}
                  </button>
                );
              })}
            </div>

            {/* Action Buttons */}
            <div className="flex items-center justify-between pt-2 border-t border-slate-600/30">
              <div className="flex gap-2">
                <button
                  type="button"
                  className="p-2 text-slate-400 hover:text-white hover:bg-slate-700/50 rounded-lg transition-all duration-200"
                  title="Add image"
                >
                  <Image size={20} aria-label="Add image" />
                </button>
                <button
                  type="button"
                  className="p-2 text-slate-400 hover:text-white hover:bg-slate-700/50 rounded-lg transition-all duration-200"
                  title="Add location"
                >
                  <MapPin size={20} />
                </button>
                <button
                  type="button"
                  className="p-2 text-slate-400 hover:text-white hover:bg-slate-700/50 rounded-lg transition-all duration-200"
                  title="Add emoji"
                >
                  <Smile size={20} />
                </button>
              </div>

              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setIsExpanded(false);
                    setContent('');
                    setPostType('general');
                  }}
                  className="px-4 py-2 text-slate-300 hover:text-white transition-colors duration-200"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isPending || !content.trim()}
                  className="px-6 py-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-medium rounded-lg hover:from-blue-700 hover:to-purple-700 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg"
                >
                  {isPending ? 'Posting...' : 'Post'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Collapsed Submit Button */}
        {!isExpanded && content.trim() && (
          <div className="flex justify-end">
            <button
              type="submit"
              disabled={isPending}
              className="px-6 py-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white font-medium rounded-lg hover:from-blue-700 hover:to-purple-700 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 shadow-lg"
            >
              {isPending ? 'Posting...' : 'Post'}
            </button>
          </div>
        )}
      </form>
    </div>
  );
}
