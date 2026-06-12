import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import type { ProjectReadable } from '@/lib/api/generated';

type Props = {
  game: ProjectReadable;
};

export function GameCard({ game }: Readonly<Props>) {
  const { title, shortDescription } = game;

  return (
    <Card className="rounded-lg overflow-hidden shadow-lg max-w-[320px] mx-auto hover:shadow-xl transition-all duration-200">
      <div className="relative">
        <div className="flex aspect-square w-full items-center justify-center bg-gradient-to-br from-slate-900 via-indigo-900 to-slate-800 px-6 text-center">
          <span className="text-sm font-semibold uppercase tracking-wide text-white/80">{title}</span>
        </div>
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
