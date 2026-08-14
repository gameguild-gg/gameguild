import { Link } from '@/i18n/navigation';
import { cn } from '@game-guild/ui/lib/utils';

export function ContextWorkspaceNav({ base, active, items }: { base: string; active: string; items: string[] }) {
  return <nav aria-label="Workspace sections" className="flex gap-1 overflow-x-auto border-b">
    {items.map((item) => {
      const slug = item.toLowerCase().replaceAll('/', '-').replaceAll(' ', '-');
      const href = slug === 'overview' ? base : `${base}/${slug}`;
      return <Link key={item} href={href} className={cn('whitespace-nowrap border-b-2 px-3 py-3 text-sm', active === slug ? 'border-primary font-medium' : 'border-transparent text-muted-foreground')}>{item}</Link>;
    })}
  </nav>;
}
