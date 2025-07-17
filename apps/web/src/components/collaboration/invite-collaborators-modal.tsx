'use client';

import { useState } from 'react';
import { Search, Users, Building, UserPlus } from 'lucide-react';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@game-guild/ui/components/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';

interface TeamMember {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  role: 'Admin' | 'Editor' | 'Viewer';
}

interface InviteCollaboratorsModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onInvite?: (email: string, role: string) => void;
  teamMembers?: TeamMember[];
}

export function InviteCollaboratorsModal({ 
  open, 
  onOpenChange, 
  onInvite,
  teamMembers = []
}: InviteCollaboratorsModalProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRole, setSelectedRole] = useState<string>('Editor');

  const defaultTeamMembers: TeamMember[] = [
    {
      id: '1',
      name: 'Lena Hartfield',
      email: 'lena@hartfield.com',
      avatar: '/avatars/lena.jpg',
      role: 'Admin'
    },
    {
      id: '2',
      name: 'Elliot Grayson',
      email: 'elliot@grayson.com',
      avatar: '/avatars/elliot.jpg',
      role: 'Editor'
    }
  ];

  const displayMembers = teamMembers.length > 0 ? teamMembers : defaultTeamMembers;
  
  const filteredMembers = displayMembers.filter(member =>
    member.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    member.email.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleInvite = () => {
    if (searchQuery && onInvite) {
      onInvite(searchQuery, selectedRole);
      setSearchQuery('');
    }
  };

  const handleRoleChange = (memberId: string, newRole: string) => {
    // This would typically update the member's role via an API call
    console.log(`Updating ${memberId} role to ${newRole}`);
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(word => word.charAt(0))
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg bg-background border border-border text-foreground p-6 rounded-xl shadow-lg">
        <DialogHeader className="space-y-4">
          <div className="flex items-center gap-3">
            <div className="bg-muted p-2.5 rounded-lg border border-border">
              <UserPlus className="h-5 w-5 text-muted-foreground" />
            </div>
            <div>
              <DialogTitle className="text-lg font-medium text-foreground">
                Invite collaborators
              </DialogTitle>
              <p className="text-sm text-muted-foreground mt-0.5">
                Invite users to work on projects together
              </p>
            </div>
          </div>
        </DialogHeader>

        <div className="space-y-4 mt-4">
          <Tabs defaultValue="team-members" className="w-full">
            <TabsList className="grid w-full grid-cols-2 bg-muted p-0.5 rounded-lg">
              <TabsTrigger 
                value="team-members" 
                className="flex items-center gap-2 text-sm font-medium px-3 py-2 rounded-md data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm text-muted-foreground"
              >
                <Users className="h-4 w-4" />
                Team members
              </TabsTrigger>
              <TabsTrigger 
                value="organisations"
                className="flex items-center gap-2 text-sm font-medium px-3 py-2 rounded-md data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm text-muted-foreground"
              >
                <Building className="h-4 w-4" />
                Organisations
              </TabsTrigger>
            </TabsList>

            <TabsContent value="team-members" className="space-y-4 mt-4">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search by Name, Email or Username..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10 bg-muted border border-border text-foreground placeholder:text-muted-foreground h-10 rounded-lg focus:border-ring focus:ring-1 focus:ring-ring"
                />
              </div>

              <div className="space-y-3">
                <h3 className="text-sm font-medium text-muted-foreground mb-2">Team members</h3>
                
                {filteredMembers.map((member) => (
                  <div key={member.id} className="flex items-center justify-between py-3">
                    <div className="flex items-center gap-3">
                      <Avatar className="h-9 w-9">
                        <AvatarImage src={member.avatar} alt={member.name} />
                        <AvatarFallback className="bg-muted text-foreground text-sm font-medium">
                          {getInitials(member.name)}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <p className="text-sm font-medium text-foreground">{member.name}</p>
                        <p className="text-xs text-muted-foreground">{member.email}</p>
                      </div>
                    </div>
                    
                    <Select 
                      defaultValue={member.role}
                      onValueChange={(value) => handleRoleChange(member.id, value)}
                    >
                      <SelectTrigger className="w-20 h-8 bg-muted border border-border text-foreground text-xs rounded-md hover:bg-accent">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent className="bg-popover border border-border rounded-md shadow-md">
                        <SelectItem value="Admin" className="text-popover-foreground hover:bg-accent text-xs">
                          Admin
                        </SelectItem>
                        <SelectItem value="Editor" className="text-popover-foreground hover:bg-accent text-xs">
                          Editor
                        </SelectItem>
                        <SelectItem value="Viewer" className="text-popover-foreground hover:bg-accent text-xs">
                          Viewer
                        </SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                ))}
              </div>

              {searchQuery && !filteredMembers.some(m => m.email === searchQuery) && (
                <div className="flex items-center justify-between p-3 bg-muted rounded-lg border border-border">
                  <div className="flex items-center gap-3">
                    <div className="h-9 w-9 bg-accent rounded-full flex items-center justify-center">
                      <UserPlus className="h-4 w-4 text-muted-foreground" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-foreground">Invite "{searchQuery}"</p>
                      <p className="text-xs text-muted-foreground">Send invitation email</p>
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-2">
                    <Select value={selectedRole} onValueChange={setSelectedRole}>
                      <SelectTrigger className="w-20 h-8 bg-background border border-border text-foreground text-xs rounded-md">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent className="bg-popover border border-border rounded-md shadow-md">
                        <SelectItem value="Admin" className="text-popover-foreground hover:bg-accent text-xs">
                          Admin
                        </SelectItem>
                        <SelectItem value="Editor" className="text-popover-foreground hover:bg-accent text-xs">
                          Editor
                        </SelectItem>
                        <SelectItem value="Viewer" className="text-popover-foreground hover:bg-accent text-xs">
                          Viewer
                        </SelectItem>
                      </SelectContent>
                    </Select>
                    <Button
                      size="sm"
                      onClick={handleInvite}
                      className="bg-primary hover:bg-primary/90 text-primary-foreground text-xs"
                    >
                      Invite
                    </Button>
                  </div>
                </div>
              )}
            </TabsContent>

            <TabsContent value="organisations" className="mt-4">
              <div className="text-center py-12 text-muted-foreground">
                <Building className="h-10 w-10 mx-auto mb-3 opacity-40" />
                <p className="text-sm">No organisations available</p>
                <p className="text-xs mt-1 opacity-75">You can invite organisations to collaborate on projects</p>
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </DialogContent>
    </Dialog>
  );
}
