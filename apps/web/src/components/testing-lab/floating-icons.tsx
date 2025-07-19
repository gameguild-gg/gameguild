'use client';

import { useEffect, useState } from 'react';
import {
  Aperture,
  Atom,
  Beaker,
  Bug,
  Calculator,
  Camera,
  Code,
  Cog,
  Compass,
  Cpu,
  Database,
  Dice1,
  Dna,
  FileCode,
  Film,
  FlaskConical,
  Focus,
  FolderKanban,
  Gamepad2,
  GitBranch,
  Globe,
  Hammer,
  Headphones,
  Heart,
  Image,
  Joystick,
  Leaf,
  Medal,
  MessageSquare,
  Microscope,
  Monitor,
  Music,
  Network,
  Palette,
  Play,
  Recycle,
  Ruler,
  Settings,
  Share,
  Sparkles,
  Star,
  Sun,
  Sword,
  Target,
  Telescope,
  Terminal,
  TestTube,
  ThumbsUp,
  TreePine,
  Trophy,
  Users,
  Video,
  Volume2,
  Wind,
  Wrench,
  Zap,
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
      const minDistance = 15; // Increased minimum distance between icons (in percentage)
      const centerExclusionZone = { x: 25, y: 25, width: 50, height: 50 }; // Larger central area to avoid

      // Define predefined zones with equal distribution - increased icon count
      const zones = [
        { name: 'top', x: [8, 92], y: [3, 15], maxIcons: 8 },
        { name: 'right', x: [85, 97], y: [8, 75], maxIcons: 8 },
        { name: 'bottom', x: [8, 92], y: [85, 95], maxIcons: 8 },
        { name: 'left', x: [3, 15], y: [8, 75], maxIcons: 8 },
        { name: 'top-left', x: [8, 30], y: [15, 35], maxIcons: 4 },
        { name: 'top-right', x: [70, 92], y: [15, 35], maxIcons: 4 },
        { name: 'bottom-left', x: [8, 30], y: [65, 85], maxIcons: 4 },
        { name: 'bottom-right', x: [70, 92], y: [65, 85], maxIcons: 4 },
      ];

      // Optimized distance check using squared distance (avoid sqrt for performance)
      const isTooClose = (newX: number, newY: number, existingIcons: FloatingIcon[]): boolean => {
        const minDistanceSquared = minDistance * minDistance;
        return existingIcons.some((icon) => {
          const dx = newX - icon.x;
          const dy = newY - icon.y;
          return dx * dx + dy * dy < minDistanceSquared;
        });
      };

      // Helper function to check if position is in center exclusion zone
      const isInCenterZone = (x: number, y: number): boolean => {
        return (
          x >= centerExclusionZone.x &&
          x <= centerExclusionZone.x + centerExclusionZone.width &&
          y >= centerExclusionZone.y &&
          y <= centerExclusionZone.y + centerExclusionZone.height
        );
      };

      // Generate icons with guaranteed distribution across zones
      let iconId = 0;

      zones.forEach((zone) => {
        const iconsForThisZone = zone.maxIcons;
        let placedInZone = 0;

        while (placedInZone < iconsForThisZone && iconId < 48) {
          const randomIcon = icons[Math.floor(Math.random() * icons.length)];
          const randomDirection = directions[Math.floor(Math.random() * directions.length)];
          const randomColorScheme = colorSchemes[Math.floor(Math.random() * colorSchemes.length)];

          // Use grid-based placement within the zone for better distribution
          const gridX = placedInZone % 3; // 0, 1, or 2 (3 columns for better distribution)
          const gridY = Math.floor(placedInZone / 3); // Row calculation

          const zoneWidth = zone.x[1] - zone.x[0];
          const zoneHeight = zone.y[1] - zone.y[0];

          // Calculate position with some randomness within grid cell
          const cellWidth = zoneWidth / 3; // 3 columns
          const cellHeight = zoneHeight / Math.ceil(zone.maxIcons / 3); // Dynamic rows based on maxIcons

          const x = zone.x[0] + gridX * cellWidth + Math.random() * cellWidth * 0.8 + cellWidth * 0.1;
          const y = zone.y[0] + gridY * cellHeight + Math.random() * cellHeight * 0.8 + cellHeight * 0.1;

          // Verify position is valid
          if (!isInCenterZone(x, y) && !isTooClose(x, y, newIcons)) {
            newIcons.push({
              id: iconId,
              Icon: randomIcon,
              x,
              y,
              duration: 15 + Math.random() * 25, // 15-40 seconds
              delay: Math.random() * 10, // 0-10 seconds delay
              direction: randomDirection,
              color: randomColorScheme.color,
              glowColor: randomColorScheme.glowColor,
              size: 4 + Math.random() * 4, // Icon range: 4-8 (16px to 32px actual size)
            });

            placedInZone++;
            iconId++;
          } else {
            // If grid position doesn't work, try a few random positions in the zone
            let attempts = 0;
            let foundPosition = false;

            while (attempts < 10 && !foundPosition) {
              const randomX = zone.x[0] + Math.random() * zoneWidth;
              const randomY = zone.y[0] + Math.random() * zoneHeight;

              if (!isInCenterZone(randomX, randomY) && !isTooClose(randomX, randomY, newIcons)) {
                newIcons.push({
                  id: iconId,
                  Icon: randomIcon,
                  x: randomX,
                  y: randomY,
                  duration: 15 + Math.random() * 25,
                  delay: Math.random() * 10,
                  direction: randomDirection,
                  color: randomColorScheme.color,
                  glowColor: randomColorScheme.glowColor,
                  size: 4 + Math.random() * 4, // Icon range: 4-8 (16px to 32px actual size)
                });

                foundPosition = true;
                placedInZone++;
                iconId++;
              }
              attempts++;
            }

            // If we still can't place it, skip and move on
            if (!foundPosition) {
              placedInZone++;
              iconId++;
            }
          }
        }
      });

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
