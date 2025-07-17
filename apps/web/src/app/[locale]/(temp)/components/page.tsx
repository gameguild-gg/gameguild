import { ComponentShowcase } from '@/components/showcase';


export default function ComponentsPage() {
  return (
    <NotificationProvider>
      <ComponentShowcase />
    </NotificationProvider>
  );
}

export const metadata = {
  title: 'Components Showcase',
  description: 'Showcase of custom UI components including task dashboard, leaderboard, and appearance modal',
};
