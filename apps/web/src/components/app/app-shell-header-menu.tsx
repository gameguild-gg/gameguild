'use client';

import { Link, usePathname } from '@/i18n/navigation';
import type { DashboardNotificationSummary } from '@/lib/dashboard-notifications';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuGroup,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import { Bell, CheckCheck, Mail, Rss } from 'lucide-react';
import * as React from 'react';
import { toast } from 'sonner';
import type { DashboardNotificationItem } from '@/lib/dashboard-notifications';
import {
  markAllNotificationsReadAction,
  setNotificationReadAction,
  type NotificationReadActionResult,
} from '@/lib/notifications/mark-read-action';
import { WorkspaceUserMenu, type WorkspaceUser } from '@/components/console/workspace-user-menu';

interface NotificationItemProps {
  item: DashboardNotificationItem;
  onSetRead: (item: DashboardNotificationItem, isRead: boolean) => void;
}

function NotificationMenuItem({ item, onSetRead }: NotificationItemProps) {
  const toggleOnActivate = () => {
    if (item.isRead && item.actionUrl?.startsWith('/')) return;
    onSetRead(item, !item.isRead);
  };

  const content = (
    <div className="flex w-full flex-col gap-1">
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm font-medium">{item.title}</p>
        {item.isRead ? (
          <button
            type="button"
            aria-label={`Mark ${item.title} unread`}
            title="Mark unread"
            className="mt-0.5 shrink-0 rounded-sm p-0.5 text-muted-foreground opacity-0 transition-opacity hover:text-foreground focus-visible:opacity-100 group-hover:opacity-100"
            onClick={(event) => {
              event.preventDefault();
              event.stopPropagation();
              onSetRead(item, false);
            }}
          >
            <Mail className="size-3.5" />
          </button>
        ) : (
          <span className="mt-1 size-2 rounded-full bg-primary" aria-label="Unread" />
        )}
      </div>
      <p className="line-clamp-2 text-xs text-muted-foreground">{item.message}</p>
      <p className="text-xs text-muted-foreground">{item.createdLabel}</p>
      {item.actionText && <p className="text-xs font-medium text-primary">{item.actionText}</p>}
    </div>
  );

  if (item.actionUrl?.startsWith('/')) {
    return (
      <DropdownMenuItem className="group" render={<Link href={item.actionUrl} onClick={toggleOnActivate} />}>
        {content}
      </DropdownMenuItem>
    );
  }

  return (
    <DropdownMenuItem className="group" onClick={toggleOnActivate}>
      {content}
    </DropdownMenuItem>
  );
}

function ContextToggle({ isWorkspace }: { isWorkspace: boolean }) {
  return (
    <Button
      nativeButton={false}
      variant="ghost"
      size="icon"
      className="relative"
      render={
        isWorkspace ? (
          <Link href="/" aria-label="Open Community feed" title="Open Community feed" />
        ) : (
          <Link href="/workspace" aria-label="Open Workspace" title="Open Workspace" />
        )
      }
    >
      <Rss className="size-5" aria-hidden="true" />
    </Button>
  );
}

function NotificationsMenu({ notifications }: { notifications?: DashboardNotificationSummary }) {
  const notificationSummary = notifications ?? { items: [], unreadCount: 0 };
  const [readOverrides, setReadOverrides] = React.useState<Record<string, boolean>>({});
  const [hiddenUnreadCount, setHiddenUnreadCount] = React.useState<number | null>(null);

  const items = notificationSummary.items.map((item) =>
    readOverrides[item.id] === undefined ? item : { ...item, isRead: readOverrides[item.id] },
  );
  const shownUnreadCount = items.filter((item) => !item.isRead).length;
  const serverShownUnreadCount = notificationSummary.items.filter((item) => !item.isRead).length;
  const derivedHiddenUnreadCount = Math.max(0, notificationSummary.unreadCount - serverShownUnreadCount);
  const unreadCount = shownUnreadCount + (hiddenUnreadCount ?? derivedHiddenUnreadCount);
  const unreadLabel = unreadCount > 99 ? '99+' : String(unreadCount);

  const applyReadChanges = async (
    changes: Array<{ id: string; isRead: boolean }>,
    invoke: () => Promise<NotificationReadActionResult>,
    markAll: boolean,
  ) => {
    const previousOverrides = readOverrides;
    const previousHiddenUnreadCount = hiddenUnreadCount;
    setReadOverrides((current) => {
      const next = { ...current };
      for (const change of changes) {
        next[change.id] = change.isRead;
      }
      return next;
    });
    if (markAll) {
      setHiddenUnreadCount(0);
    }

    const result = await invoke();
    if (!result.success) {
      setReadOverrides(previousOverrides);
      setHiddenUnreadCount(previousHiddenUnreadCount);
      toast.error('Failed to update notifications. Please try again.');
    }
  };

  const handleSetRead = (item: DashboardNotificationItem, isRead: boolean) => {
    if (item.isRead === isRead) return;
    void applyReadChanges([{ id: item.id, isRead }], () => setNotificationReadAction(item.id, isRead), false);
  };

  const handleMarkAllRead = () => {
    if (shownUnreadCount === 0) return;
    const changes = items.filter((item) => !item.isRead).map((item) => ({ id: item.id, isRead: true }));
    void applyReadChanges(changes, markAllNotificationsReadAction, true);
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button variant="ghost" size="icon" className="relative" />}>
        <Bell className="size-5" />
        {unreadCount > 0 && (
          <Badge variant="destructive" className="absolute -right-1 -top-1 h-5 min-w-5 rounded-full px-1 text-xs">
            {unreadLabel}
          </Badge>
        )}
        <span className="sr-only">Notifications</span>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-80">
        <DropdownMenuGroup>
          <DropdownMenuLabel>Notifications</DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <div className="max-h-[300px] overflow-y-auto">
          {notificationSummary.items.length > 0 ? (
            items.map((item) => (
              <NotificationMenuItem key={item.id} item={item} onSetRead={handleSetRead} />
            ))
          ) : (
            <div className="px-3 py-6 text-center">
              <p className="text-sm font-medium">No notifications</p>
              <p className="mt-1 text-xs text-muted-foreground">New account updates will appear here.</p>
            </div>
          )}
        </div>
        <DropdownMenuSeparator />
        <div className="flex items-center justify-between gap-2 px-3 py-2">
          <p className="text-xs font-normal text-muted-foreground">Showing latest account notifications</p>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="h-7 gap-1 px-2 text-xs"
            disabled={shownUnreadCount === 0}
            onClick={handleMarkAllRead}
          >
            <CheckCheck className="size-3.5" />
            Mark all read
          </Button>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

interface AppShellHeaderMenuProps {
  notifications?: DashboardNotificationSummary;
  user: WorkspaceUser;
}

export function AppShellHeaderMenu({ notifications, user }: AppShellHeaderMenuProps) {
  const pathname = usePathname();
  const isWorkspace = pathname?.startsWith('/workspace') ?? false;

  return (
    <div
      role="group"
      aria-label="App menu actions"
      className="flex shrink-0 items-center justify-end gap-1 sm:gap-2"
    >
      <ContextToggle isWorkspace={isWorkspace} />
      <NotificationsMenu notifications={notifications} />
      <div className="ml-1 sm:ml-2">
        <WorkspaceUserMenu user={user} />
      </div>
    </div>
  );
}
