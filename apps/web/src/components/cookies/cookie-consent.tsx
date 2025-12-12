'use client';

import React, { useEffect, useMemo, useRef, useState } from 'react';
// Removed unused icons
// import { CookieIcon, Settings, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useCookies } from '@/hooks/use-cookies';

type CookieConsentProps = {
  className?: string;
  showPreferencesButton?: boolean;
  compactMode?: boolean;
};

export const CookieConsent = ({ className = '', showPreferencesButton = false, compactMode = false }: Readonly<CookieConsentProps>): React.JSX.Element | null => {
  const { hasConsented, hasDeclined, acceptAll, acceptEssential, decline, isLoading, consentState } = useCookies();
  const [visible, setVisible] = useState(false);

  // Typing animation state
  const terminalLines = useMemo(
    () => [
      'Our site uses cookies like power-ups: some are essential to keep the game running, others help us track stats and improve your experience.',
      'Choose your loadout below or accept all to start playing. You can change settings anytime.',
    ],
    [],
  );
  const [typedText, setTypedText] = useState<string>('');
  // Use refs to avoid stale closures in setInterval
  const lineIndexRef = useRef<number>(0);
  const charIndexRef = useRef<number>(0);
  const typingIntervalRef = useRef<number | null>(null);

  useEffect(() => {
    // Show modal only when tri-state is not answered
    if (!isLoading && consentState === 'not_answered') {
      setVisible(true);
    } else {
      setVisible(false);
    }
  }, [consentState, isLoading]);

  const handleAcceptAll = () => {
    acceptAll();
    setVisible(false);
  };

  const handleAcceptEssential = () => {
    acceptEssential();
    setVisible(false);
  };

  const handleDecline = () => {
    decline();
    setVisible(false);
  };

  // Keyboard shortcuts: Enter = Accept ALL, Esc = Deny
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (!visible) return;
      if (e.key === 'Enter') {
        e.preventDefault();
        handleAcceptAll();
      } else if (e.key === 'Escape') {
        e.preventDefault();
        handleDecline();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [visible]);

  // Fast typing animation (4x speed)
  useEffect(() => {
    if (!visible) return;

    // Reset typing state when modal becomes visible
    setTypedText('');
    lineIndexRef.current = 0;
    charIndexRef.current = 0;

    const CHUNK = 4; // 4x characters per tick

    const startTyping = () => {
      if (typingIntervalRef.current) {
        window.clearInterval(typingIntervalRef.current);
      }
      typingIntervalRef.current = window.setInterval(() => {
        const line = terminalLines[lineIndexRef.current] || '';
        if (charIndexRef.current < line.length) {
          const start = charIndexRef.current;
          const end = Math.min(start + CHUNK, line.length);
          setTypedText((prev) => prev + line.slice(start, end));
          charIndexRef.current = end;
        } else {
          // Move to next line
          const nextIndex = lineIndexRef.current + 1;
          if (nextIndex < terminalLines.length) {
            setTypedText((prev) => prev + '\n');
            lineIndexRef.current = nextIndex;
            charIndexRef.current = 0;
          } else {
            // Completed typing all lines
            if (typingIntervalRef.current) {
              window.clearInterval(typingIntervalRef.current);
              typingIntervalRef.current = null;
            }
          }
        }
      }, 15); // base interval, chunking makes it ~4x faster
    };

    startTyping();

    return () => {
      if (typingIntervalRef.current) {
        window.clearInterval(typingIntervalRef.current);
        typingIntervalRef.current = null;
      }
    };
    // We intentionally omit dependencies to restart only on visibility change
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [visible]);

  // Don't render anything while loading or if consent/decline already given
  if (isLoading || !visible) {
    return null;
  }

  // Bottom-right anchored terminal-style modal (non-blocking)
  return (
    <div className={`fixed bottom-4 right-4 z-50 font-mono ${className}`}>
      <div className="relative w-full max-w-xl rounded-lg border-2 border-emerald-500 bg-emerald-950 text-emerald-100 shadow-[0_0_30px_rgba(16,185,129,0.6)]">
        <div className="absolute -top-4 left-6 rounded bg-emerald-500 px-3 py-1 text-xs font-bold text-emerald-950 z-10">Cookie Settings</div>
        <div className="flex items-start gap-3 p-6 pt-10">
          <div className="flex-1">
            <div className="h-32 overflow-y-auto scrollbar-thin scrollbar-track-emerald-900 scrollbar-thumb-emerald-600">
              <pre className="whitespace-pre-wrap break-all text-sm leading-relaxed min-h-full">
                {typedText}
                <span className="inline-block w-2 align-baseline bg-[rgb(110,231,183)] animate-[blink_1s_step-start_infinite]"></span>
              </pre>
            </div>
            <div className="mt-5 flex flex-col gap-2 sm:flex-row">
              <Button onClick={handleAcceptAll} className="bg-emerald-400 text-emerald-950 hover:bg-emerald-300 font-bold">Accept ALL [ENTER]</Button>
              <Button onClick={handleAcceptEssential} variant="outline" className="border-emerald-400 text-emerald-100 bg-emerald-900/40 hover:bg-emerald-800/60 hover:text-emerald-50">Essential Only</Button>
              <Button onClick={handleDecline} variant="ghost" className="text-emerald-300 hover:text-emerald-100 hover:bg-emerald-800/40">Deny [ESC]</Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
