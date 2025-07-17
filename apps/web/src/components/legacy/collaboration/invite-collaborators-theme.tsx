'use client';

import { useState } from 'react';
import { Search, Users, Building, UserPlus, ChevronDown } from 'lucide-react';
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

interface InviteCollaboratorsThemeProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onInvite?: (email: string, role: string) => void;
  teamMembers?: TeamMember[];
}

export function InviteCollaboratorsTheme({ 
  open, 
  onOpenChange, 
  onInvite,
  teamMembers = []
}: InviteCollaboratorsThemeProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedRole, setSelectedRole] = useState<string>('Editor');

  const defaultTeamMembers: TeamMember[] = [
    {
      id: '1',
      name: 'Lena Hartfield',
      email: 'lena@hartfield.com',
      avatar: undefined,
      role: 'Admin'
    },
    {
      id: '2',
      name: 'Elliot Grayson',
      email: 'elliot@grayson.com',
      avatar: undefined,
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

  const getAvatarColors = (name: string) => {
    const colorPairs = [
      { bg: 'bg-blue-500', text: 'text-white' },
      { bg: 'bg-emerald-500', text: 'text-white' },
      { bg: 'bg-purple-500', text: 'text-white' },
      { bg: 'bg-orange-500', text: 'text-white' },
      { bg: 'bg-pink-500', text: 'text-white' },
      { bg: 'bg-indigo-500', text: 'text-white' }
    ];
    const index = name.charCodeAt(0) % colorPairs.length;
    return colorPairs[index];
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[480px] bg-background border-border text-foreground p-0 rounded-2xl overflow-hidden shadow-2xl">
        {/* Header */}
        <DialogHeader className="px-6 pt-6 pb-4">
          <div className="flex items-center gap-3">
            <div className="bg-muted p-2.5 rounded-lg border border-border/50">
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

        {/* Content */}
        <div className="px-6 pb-6">
          <Tabs defaultValue="team-members" className="w-full">
            {/* Tab Navigation */}
            <TabsList className="grid w-full grid-cols-2 bg-muted/50 p-1 rounded-lg h-auto border border-border/30">
              <TabsTrigger 
                value="team-members" 
                className="flex items-center gap-2 text-sm font-medium px-4 py-2.5 rounded-md data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm data-[state=active]:border data-[state=active]:border-border/40 text-muted-foreground transition-all"
              >
                <Users className="h-4 w-4" />
                Team members
              </TabsTrigger>
              <TabsTrigger 
                value="organisations"
                className="flex items-center gap-2 text-sm font-medium px-4 py-2.5 rounded-md data-[state=active]:bg-background data-[state=active]:text-foreground data-[state=active]:shadow-sm data-[state=active]:border data-[state=active]:border-border/40 text-muted-foreground transition-all"
              >
                <Building className="h-4 w-4" />
                Organisations
              </TabsTrigger>
            </TabsList>

            {/* Team Members Tab */}
            <TabsContent value="team-members" className="space-y-4 mt-4">
              {/* Search Input */}
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search by Name, Email or Username..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-10 bg-muted/50 border-border text-foreground placeholder:text-muted-foreground h-11 rounded-lg focus:border-ring focus:ring-1 focus:ring-ring"
                />
              </div>

              {/* Team Members Section */}
              <div className="space-y-3">
                <h3 className="text-sm font-medium text-muted-foreground">Team members</h3>
                
                <div className="space-y-3">
                  {filteredMembers.map((member) => {
                    const avatarColors = getAvatarColors(member.name);
                    return (
                      <div key={member.id} className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                          <Avatar className="h-10 w-10">
                            <AvatarImage src={member.avatar} alt={member.name} />
                            <AvatarFallback className={`${avatarColors.bg} ${avatarColors.text} text-sm font-medium border-0`}>
                              {getInitials(member.name)}
                            </AvatarFallback>
                          </Avatar>
                          <div>
                            <p className="text-sm font-medium text-foreground">{member.name}</p>
                            <p className="text-xs text-muted-foreground">{member.email}</p>
                          </div>
                        </div>
                        
                        {/* Role Selector */}
                        <Select 
                          defaultValue={member.role}
                          onValueChange={(value) => handleRoleChange(member.id, value)}
                        >
                          <SelectTrigger className="w-auto min-w-[80px] h-8 bg-muted/50 border-border text-foreground text-sm rounded-md hover:bg-muted transition-colors">
                            <SelectValue />
                            <ChevronDown className="h-4 w-4 ml-1 text-muted-foreground" />
                          </SelectTrigger>
                          <SelectContent className="bg-popover border-border rounded-lg shadow-lg">
                            <SelectItem value="Admin" className="text-popover-foreground hover:bg-accent focus:bg-accent text-sm">
                              Admin
                            </SelectItem>
                            <SelectItem value="Editor" className="text-popover-foreground hover:bg-accent focus:bg-accent text-sm">
                              Editor
                            </SelectItem>
                            <SelectItem value="Viewer" className="text-popover-foreground hover:bg-accent focus:bg-accent text-sm">
                              Viewer
                            </SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Invite New User Section */}
              {searchQuery && !filteredMembers.some(m => m.email === searchQuery) && (
                <div className="flex items-center justify-between p-3 bg-muted/30 rounded-lg border border-border/40">
                  <div className="flex items-center gap-3">
                    <div className="h-10 w-10 bg-muted rounded-full flex items-center justify-center border border-border/40">
                      <UserPlus className="h-5 w-5 text-muted-foreground" />
                    </div>
                    <div>
                      <p className="text-sm font-medium text-foreground">Invite "{searchQuery}"</p>
                      <p className="text-xs text-muted-foreground">Send invitation email</p>
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-2">
                    <Select value={selectedRole} onValueChange={setSelectedRole}>
                      <SelectTrigger className="w-auto min-w-[80px] h-8 bg-background border-border text-foreground text-sm rounded-md">
                        <SelectValue />
                        <ChevronDown className="h-4 w-4 ml-1 text-muted-foreground" />
                      </SelectTrigger>
                      <SelectContent className="bg-popover border-border rounded-lg shadow-lg">
                        <SelectItem value="Admin" className="text-popover-foreground hover:bg-accent text-sm">
                          Admin
                        </SelectItem>
                        <SelectItem value="Editor" className="text-popover-foreground hover:bg-accent text-sm">
                          Editor
                        </SelectItem>
                        <SelectItem value="Viewer" className="text-popover-foreground hover:bg-accent text-sm">
                          Viewer
                        </SelectItem>
                      </SelectContent>
                    </Select>
                    <Button
                      size="sm"
                      onClick={handleInvite}
                      className="bg-primary hover:bg-primary/90 text-primary-foreground text-sm px-3 h-8"
                    >
                      Invite
                    </Button>
                  </div>
                </div>
              )}
            </TabsContent>

            {/* Organisations Tab */}
            <TabsContent value="organisations" className="mt-4">
              <div className="text-center py-12 text-muted-foreground">
                <Building className="h-12 w-12 mx-auto mb-3 opacity-40" />
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
