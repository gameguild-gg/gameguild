import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { ExternalLink, Star } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';

interface ProjectCardProps {
  name: string;
  description?: string;
  tech: string;
  rating?: number;
  featured?: boolean;
  imageHeight?: 'h-32' | 'h-48';
  colSpan?: string;
  imageUrl?: string;
  url?: string;
}

export function ProjectCard({
  name,
  description,
  tech,
  rating,
  featured = false,
  imageHeight = 'h-32',
  colSpan = '',
  imageUrl,
  url,
}: ProjectCardProps) {
  return (
    <Card className={`bg-slate-800/50 border-purple-500/20 overflow-hidden ${colSpan}`}>
      <div
        className={`relative ${imageHeight} ${featured ? 'bg-gradient-to-br from-blue-600 to-purple-600' : 'bg-gradient-to-br from-slate-700 to-slate-600'}`}
      >
        {imageUrl ? (
          <Image
            src={imageUrl}
            alt={name}
            className="w-full h-full object-cover"
            width={featured ? 400 : 300}
            height={imageHeight === 'h-48' ? 192 : 128}
            unoptimized
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center px-6 text-center">
            <span className="text-sm font-semibold uppercase tracking-wide text-white/80">{name}</span>
          </div>
        )}
        {featured && (
          <Badge className="absolute top-4 left-4 bg-yellow-600 text-yellow-100">Featured</Badge>
        )}
      </div>
      <CardContent className={featured ? 'p-6' : 'p-4'}>
        <h3 className={`font-bold text-white mb-2 ${featured ? 'text-xl' : 'text-base'}`}>{name}</h3>
        {description && <p className="text-gray-300 mb-4">{description}</p>}
        {featured && (
          <div className="flex flex-wrap gap-2 mb-4">
            <Badge className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
              Community
            </Badge>
            <Badge className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
              Collaborative
            </Badge>
            <Badge className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 backdrop-blur-sm shadow-md">
              Open Source
            </Badge>
          </div>
        )}
        {!featured && (
          <Badge className="rounded-full bg-gradient-to-r from-purple-500/20 to-blue-500/20 border border-purple-400/30 text-white px-3 py-1 mb-3 backdrop-blur-sm shadow-md">
            {tech}
          </Badge>
        )}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4 text-sm text-gray-400">
            {rating && rating > 0 ? (
              <span className="flex items-center gap-1">
                <Star className={featured ? 'w-4 h-4' : 'w-3 h-3'} />
                {rating}
              </span>
            ) : null}
            <span>{featured ? 'Community project' : 'Community'}</span>
          </div>
          {featured && url ? (
            <Button
              asChild
              size="sm"
              className="px-4 py-2 bg-gradient-to-r from-blue-600/80 to-purple-600/80 text-white rounded-lg hover:from-blue-700/80 hover:to-purple-700/80 transition-all duration-300 font-medium shadow-lg border border-white/10 backdrop-blur-sm"
            >
              <Link href={url}>
                <ExternalLink className="w-4 h-4 mr-2" />
                View Project
              </Link>
            </Button>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}
