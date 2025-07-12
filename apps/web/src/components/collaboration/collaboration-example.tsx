'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { 
  InviteCollaboratorsModal, 
  InviteCollaboratorsExact,
  InviteCollaboratorsTheme 
} from '@/components/collaboration';

export function CollaborationExample() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isExactOpen, setIsExactOpen] = useState(false);
  const [isThemeOpen, setIsThemeOpen] = useState(false);

  const handleInvite = (email: string, role: string) => {
    console.log(`Inviting ${email} as ${role}`);
  };

  return (
    <div className="p-8 space-y-6 max-w-4xl mx-auto">
      <div className="space-y-2">
        <h1 className="text-3xl font-bold">Invite Collaborators Components</h1>
        <p className="text-muted-foreground">
          Different variations of the invite collaborators component with theme support.
        </p>
      </div>
      
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Theme-Aware Version */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Theme-Aware</CardTitle>
            <CardDescription>
              Automatically adapts to light/dark themes using semantic tokens
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button onClick={() => setIsModalOpen(true)} className="w-full">
              Open Modal Version
            </Button>
          </CardContent>
        </Card>

        {/* Exact Match Version */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Exact Match</CardTitle>
            <CardDescription>
              Pixel-perfect replica of the original image design
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button onClick={() => setIsExactOpen(true)} className="w-full">
              Open Exact Version
            </Button>
          </CardContent>
        </Card>

        {/* Enhanced Theme Version */}
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Enhanced Theme</CardTitle>
            <CardDescription>
              Premium theme-aware version with enhanced styling
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button onClick={() => setIsThemeOpen(true)} className="w-full">
              Open Enhanced
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Features List */}
      <Card>
        <CardHeader>
          <CardTitle>Features</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            <div>
              <h4 className="font-medium mb-2">✨ Visual Features</h4>
              <ul className="space-y-1 text-muted-foreground">
                <li>• Pixel-perfect image match</li>
                <li>• Custom avatar colors</li>
                <li>• Smooth animations</li>
                <li>• Responsive design</li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-2">🎨 Theme Support</h4>
              <ul className="space-y-1 text-muted-foreground">
                <li>• Light/dark mode compatible</li>
                <li>• Semantic color tokens</li>
                <li>• Consistent with design system</li>
                <li>• Auto-adapting borders & shadows</li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-2">⚡ Functionality</h4>
              <ul className="space-y-1 text-muted-foreground">
                <li>• Real-time search filtering</li>
                <li>• Role management</li>
                <li>• Email invitation flow</li>
                <li>• Team/Organization tabs</li>
              </ul>
            </div>
            <div>
              <h4 className="font-medium mb-2">🔧 Technical</h4>
              <ul className="space-y-1 text-muted-foreground">
                <li>• TypeScript support</li>
                <li>• Accessible components</li>
                <li>• shadcn/ui integration</li>
                <li>• Customizable props</li>
              </ul>
            </div>
          </div>
        </CardContent>
      </Card>
      
      {/* Modals */}
      <InviteCollaboratorsModal
        open={isModalOpen}
        onOpenChange={setIsModalOpen}
        onInvite={handleInvite}
      />
      
      <InviteCollaboratorsExact
        open={isExactOpen}
        onOpenChange={setIsExactOpen}
        onInvite={handleInvite}
      />
      
      <InviteCollaboratorsTheme
        open={isThemeOpen}
        onOpenChange={setIsThemeOpen}
        onInvite={handleInvite}
      />
    </div>
  );
}
