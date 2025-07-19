'use client';

import { useEffect, useState } from 'react';
import {
  Users,
  Gamepad2,
  FolderKanban,
  FlaskConical,
  Bug,
  TestTube,
  Sparkles,
  MessageSquare,
  // Coding & Development
  Code,
  Terminal,
  FileCode,
  GitBranch,
  Database,
  Settings,
  Wrench,
  Cpu,
  // Sustainability
  Leaf,
  TreePine,
  Recycle,
  Wind,
  Sun,
  Zap,
  // Tools
  Hammer,
  Cog,
  Ruler,
  Compass,
  // Social
  Heart,
  Share,
  ThumbsUp,
  Star,
  Globe,
  Network,
  // Science
  Atom,
  Dna,
  Microscope,
  Beaker,
  Calculator,
  Telescope,
  // Photography
  Camera,
  Image,
  Focus,
  Aperture,
  Film,
  Palette,
  // Multimedia
  Video,
  Music,
  Headphones,
  Play,
  Volume2,
  Monitor,
  // Gaming
  Trophy,
  Target,
  Dice1,
  Joystick,
  Medal,
  Sword,
} from 'lucide-react';

interface FloatingIcon {
  id: number;
  Icon: React.ComponentType<{ className?: string }>;
  x: number;
  y: number;
  duration: number;
  delay: number;
  direction: 'up' | 'down' | 'left' | 'right' | 'diagonal-up' | 'diagonal-down';
  color: string;
  glowColor: string;
  size: number;
}

const icons = [
  // Community & Testing
  Users,
  MessageSquare,
  FlaskConical,
  TestTube,
  Bug,
  Sparkles,
  // Gaming & Projects
  Gamepad2,
  FolderKanban,
  Trophy,
  Target,
  Dice1,
  Joystick,
  Medal,
  Sword,
  // Coding & Development
  Code,
  Terminal,
  FileCode,
  GitBranch,
  Database,
  Settings,
  Wrench,
  Cpu,
  // Sustainability
  Leaf,
  TreePine,
  Recycle,
  Wind,
  Sun,
  Zap,
  // Tools
  Hammer,
  Cog,
  Ruler,
  Compass,
  // Social
  Heart,
  Share,
  ThumbsUp,
  Star,
  Globe,
  Network,
  // Science
  Atom,
  Dna,
  Microscope,
  Beaker,
  Calculator,
  Telescope,
  // Photography
  Camera,
  Image,
  Focus,
  Aperture,
  Film,
  Palette,
  // Multimedia
  Video,
  Music,
  Headphones,
  Play,
  Volume2,
  Monitor,
];

const directions = ['up', 'down', 'left', 'right', 'diagonal-up', 'diagonal-down'] as const;

// Define color schemes for different icons
const colorSchemes = [
  { color: 'text-blue-400', glowColor: '#3b82f6' }, // Blue
  { color: 'text-green-400', glowColor: '#10b981' }, // Green
  { color: 'text-purple-400', glowColor: '#8b5cf6' }, // Purple
  { color: 'text-orange-400', glowColor: '#f97316' }, // Orange
  { color: 'text-pink-400', glowColor: '#ec4899' }, // Pink
  { color: 'text-yellow-400', glowColor: '#eab308' }, // Yellow
  { color: 'text-indigo-400', glowColor: '#6366f1' }, // Indigo
  { color: 'text-red-400', glowColor: '#ef4444' }, // Red
];

export function FloatingIcons() {
  const [floatingIcons, setFloatingIcons] = useState<FloatingIcon[]>([]);

  useEffect(() => {
    const generateIcons = () => {
      const newIcons: FloatingIcon[] = [];

      // Generate 20 floating icons for better coverage
      for (let i = 0; i < 32; i++) {
        const randomIcon = icons[Math.floor(Math.random() * icons.length)];
        const randomDirection = directions[Math.floor(Math.random() * directions.length)];
        const randomColorScheme = colorSchemes[Math.floor(Math.random() * colorSchemes.length)];

        // Keep icons away from the center (container area)
        // Split into edge zones: top/bottom strips and left/right strips
        let x, y;
        const zone = Math.floor(Math.random() * 4); // 0: top, 1: right, 2: bottom, 3: left

        switch (zone) {
          case 0: // Top strip
            x = Math.random() * 100;
            y = Math.random() * 20; // Top 20%
            break;
          case 1: // Right strip
            x = 80 + Math.random() * 20; // Right 20%
            y = Math.random() * 80; // Avoid bottom 20%
            break;
          case 2: // Bottom strip (not fully down)
            x = Math.random() * 100;
            y = 70 + Math.random() * 15; // Bottom 15% but not at very bottom
            break;
          case 3: // Left strip
            x = Math.random() * 20; // Left 20%
            y = Math.random() * 80; // Avoid bottom 20%
            break;
          default:
            x = Math.random() * 20;
            y = Math.random() * 20;
        }

        newIcons.push({
          id: i,
          Icon: randomIcon,
          x,
          y,
          duration: 15 + Math.random() * 25, // 15-40 seconds
          delay: Math.random() * 10, // 0-10 seconds delay
          direction: randomDirection,
          color: randomColorScheme.color,
          glowColor: randomColorScheme.glowColor,
          size: 4 + Math.random() * 4, // Random size between 4 and 8 (16px to 32px actual size)
        });
      }

      setFloatingIcons(newIcons);
    };

    generateIcons();
  }, []);

  return (
    <div className="absolute inset-0 bg-gradient-to-br from-background/20 via-background/10 to-background/20 backdrop-blur-[0.5px] overflow-hidden">
      {floatingIcons.map((icon) => (
        <div
          key={icon.id}
          className="absolute pointer-events-none"
          style={{
            left: `${icon.x}%`,
            top: `${icon.y}%`,
            transform: `rotate(${Math.random() * 360}deg)`,
          }}
        >
          <div
            style={{
              filter: `drop-shadow(0 0 12px ${icon.glowColor}) drop-shadow(0 0 24px ${icon.glowColor}80) drop-shadow(0 0 36px ${icon.glowColor}40)`,
              width: `${icon.size * 4}px`,
              height: `${icon.size * 4}px`,
            }}
          >
            <icon.Icon className={`w-full h-full ${icon.color}`} />
          </div>
        </div>
      ))}
    </div>
  );
}
