import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components';
import type { ProjectReadable } from '@/lib/api/generated';

type Props = {
  game: ProjectReadable;
};

export function GameCard({ game }: Readonly<Props>) {
  const { title, shortDescription } = game;

  return (
    <Card className="rounded-lg overflow-hidden shadow-lg max-w-[320px] mx-auto hover:shadow-xl transition-all duration-200">
      <div className="relative">
        <img
          alt="Profile picture"
          className="object-cover w-full"
          height="320"
          src="/assets/images/placeholder.svg"
          style={{ aspectRatio: '320/320', objectFit: 'cover' }}
          width="320"
        />
        <div className="flex justify-end">
          <Badge>{'draft'}</Badge>
        </div>
      </div>
      <CardContent className="p-4">
        <CardHeader>
          <CardTitle>{title}</CardTitle>
          <CardDescription>{shortDescription}</CardDescription>
        </CardHeader>
      </CardContent>
    </Card>
  );
}
