'use client';

import { motion } from 'framer-motion';
import { useRouter } from 'next/navigation';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Book, Code, Paintbrush } from 'lucide-react';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import Image from 'next/image';
import { Track, TRACK_LEVEL_COLORS, TRACK_LEVELS } from '@/components/legacy/types/tracks';

const areaIcons = {
  programming: Code,
  art: Paintbrush,
  design: Book,
};

const areaColors = {
  programming: 'text-blue-600 dark:text-blue-400 border-blue-600 dark:border-blue-400',
  art: 'text-pink-600 dark:text-pink-400 border-pink-600 dark:border-pink-400',
  design: 'text-green-600 dark:text-green-400 border-green-600 dark:border-green-400',
  default: 'text-gray-600 dark:text-gray-400 border-gray-600 dark:border-gray-400',
};

interface TrackCardProps {
  track: Track;
  onClick?: () => void;
}

const getBannerStyle = (obtained?: string) => {
  switch (obtained) {
    case '1':
      return { bg: 'bg-gray-400', border: 'border-gray-400', text: 'text-white' };
    case '2':
      return { bg: 'bg-yellow-500', border: 'border-yellow-500', text: 'text-black' };
    default:
      return { bg: '', border: '', text: '' };
  }
};

const getBannerText = (obtained?: string) => {
  switch (obtained) {
    case '1':
      return 'Obtained';
    case '2':
      return 'Studied';
    default:
      return '';
  }
};

const getLevelBadge = (level: string) => {
  const levelName = TRACK_LEVELS[level as keyof typeof TRACK_LEVELS] || 'Unknown';
  const badgeColor = TRACK_LEVEL_COLORS[level as keyof typeof TRACK_LEVEL_COLORS] || 'bg-gray-500';
  return <Badge className={`${badgeColor} text-white`}>{levelName}</Badge>;
};

export function TrackCard({ track, onClick }: TrackCardProps) {
  const router = useRouter();

  const handleClick = () => {
    if (onClick) {
      onClick();
    } else {
      router.push(`/tracks/${track.slug}`);
    }
  };

  const AreaIcon = areaIcons[track.area as keyof typeof areaIcons] || Book;
  const areaColorClass = areaColors[track.area as keyof typeof areaColors] || areaColors.default;
  const bannerStyle = getBannerStyle(track.obtained);

  return (
    <Card
      className={`cursor-pointer hover:shadow-lg transition-shadow bg-white dark:bg-gray-950 ${areaColorClass} ${
        track.obtained && track.obtained !== '0' ? `border-2 ${bannerStyle.border} dark:${bannerStyle.border}` : ''
      } transition-all duration-300 ease-in-out hover:scale-105 hover:shadow-xl relative overflow-hidden`}
      onClick={handleClick}
    >
      {track.obtained && track.obtained !== '0' && (
        <div className={`absolute top-0 left-0 right-0 text-center text-xs py-0.5 ${bannerStyle.bg} ${bannerStyle.text}`} style={{ marginTop: '-1px' }}>
          {getBannerText(track.obtained)}
        </div>
      )}
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
        <CardHeader className="pb-2">
          <div className="flex justify-between items-center mb-2">
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger>
                  <div className="p-2 rounded-full transition-all duration-300 hover:bg-gray-800">
                    <AreaIcon className={`w-6 h-6 ${areaColorClass} transition-all duration-300 hover:scale-110`} />
                  </div>
                </TooltipTrigger>
                <TooltipContent>
                  <p className="capitalize">{track.area}</p>
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
            {getLevelBadge(track.level)}
          </div>
          <div className="relative w-full h-48 mb-4">
            <Image src={track.image || '/placeholder.svg'} alt={track.title} fill className="object-cover rounded-lg" />
          </div>
          <CardTitle className={`text-xl font-semibold ${areaColorClass}`}>{track.title}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-gray-600 dark:text-gray-300 mb-4">{track.description}</p>
          <div className="flex flex-wrap gap-2 mb-4">
            {track.tools.map((tool, index) => (
              <Badge key={`${track.id}-${tool}-${index}`} variant="secondary" className="text-xs bg-gray-200 dark:bg-gray-800 text-gray-700 dark:text-gray-200">
                {tool}
              </Badge>
            ))}
          </div>
          <div className="mt-4">
            <span className="text-sm font-medium text-gray-300">Knowledges: {track.knowledges.length}</span>
          </div>
          {typeof track.progress === 'number' && (
            <div className="mt-4">
              <div className="flex justify-between items-center mb-1">
                <span className="text-sm font-medium text-gray-300">Progress</span>
                <span className="text-sm font-medium text-gray-300">{track.progress}%</span>
              </div>
              <Progress value={track.progress} className="w-full" />
            </div>
          )}
        </CardContent>
      </motion.div>
    </Card>
  );
}
