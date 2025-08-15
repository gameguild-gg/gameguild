"use client"

import * as React from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Users, Plus, MoreHorizontal, Mail, Clock, Shield, Eye, Edit, Settings } from "lucide-react"
import { useCourse } from "@/components/course-context"
import { updateCourse } from "@/lib/local-db"
import { useToast } from "@/hooks/use-toast"
import type { TeamMember, TeamInvite, TeamRole } from "@/lib/types"
import { cn } from "@/lib/utils"

export default function CourseTeamPage() {
  const course = useCourse()
  const { toast } = useToast()
  const [isInviteDialogOpen, setIsInviteDialogOpen] = React.useState(false)

  const roleColors = {
    owner: "bg-purple-500/10 text-purple-400 border-purple-500/20",
    admin: "bg-blue-500/10 text-blue-400 border-blue-500/20",
    editor: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
    viewer: "bg-slate-500/10 text-slate-400 border-slate-500/20",
  }

  const roleIcons = {
    owner: Shield,
    admin: Settings,
    editor: Edit,
    viewer: Eye,
  }

  const handleInviteTeamMember = (memberData: Omit<TeamInvite, "id" | "invitedAt" | "status" | "invitedBy" | "expiresAt">) => {
    const newInvite: TeamInvite = {
      id: crypto.randomUUID(),
      ...memberData,
      invitedBy: "current-user", // TODO: Get from auth context
      invitedAt: Date.now(),
      expiresAt: Date.now() + (7 * 24 * 60 * 60 * 1000), // 7 days
      status: "pending",
    }

    const updatedInvites = [...(course.teamInvites || []), newInvite]
    const updatedCourse = { ...course, teamInvites: updatedInvites }
    
    updateCourse(course.slug, updatedCourse)
    setIsInviteDialogOpen(false)
    
    toast({
      title: "Team member invited",
      description: `Invitation sent to ${memberData.email}`,
    })
  }

  const handleUpdateMemberRole = (memberId: string, newRole: TeamRole) => {
    const updatedMembers = course.team.map((member: TeamMember) =>
      member.id === memberId ? { ...member, role: newRole } : member
    )
    const updatedCourse = { ...course, team: updatedMembers }
    
    updateCourse(course.slug, updatedCourse)
    
    toast({
      title: "Role updated",
      description: "Team member role has been updated successfully",
    })
  }

  const handleRemoveMember = (memberId: string) => {
    const updatedMembers = course.team.filter((member: TeamMember) => member.id !== memberId)
    const updatedCourse = { ...course, team: updatedMembers }
    
    updateCourse(course.slug, updatedCourse)
    
    toast({
      title: "Member removed",
      description: "Team member has been removed from the course",
    })
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Team Management</h1>
          <p className="text-muted-foreground">Manage course collaborators and permissions</p>
        </div>
        
        <Dialog open={isInviteDialogOpen} onOpenChange={setIsInviteDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="h-4 w-4 mr-2" />
              Invite Member
            </Button>
          </DialogTrigger>
          <DialogContent>
            <InviteTeamMemberDialog
              onInvite={handleInviteTeamMember}
              onClose={() => setIsInviteDialogOpen(false)}
            />
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid gap-6">
        {/* Current Team Members */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="h-5 w-5" />
              Team Members ({course.team.length})
            </CardTitle>
            <CardDescription>
              People who have access to collaborate on this course
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {course.team.map((member: TeamMember) => {
                const RoleIcon = roleIcons[member.role]
                return (
                  <div key={member.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex items-center gap-3">
                      <Avatar>
                        <AvatarFallback>
                          {member.name.split(" ").map((n: string) => n[0]).join("").slice(0, 2)}
                        </AvatarFallback>
                      </Avatar>
                      <div>
                        <div className="font-medium">{member.name}</div>
                        <div className="text-sm text-muted-foreground">{member.email}</div>
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className={cn("capitalize", roleColors[member.role])}>
                        <RoleIcon className="h-3 w-3 mr-1" />
                        {member.role}
                      </Badge>
                      
                      {member.role !== "owner" && (
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => handleUpdateMemberRole(member.id, "admin")}>
                              Change to Admin
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={() => handleUpdateMemberRole(member.id, "editor")}>
                              Change to Editor
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={() => handleUpdateMemberRole(member.id, "viewer")}>
                              Change to Viewer
                            </DropdownMenuItem>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem 
                              onClick={() => handleRemoveMember(member.id)}
                              className="text-destructive"
                            >
                              Remove from course
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      )}
                    </div>
                  </div>
                )
              })}
              
              {course.team.length === 0 && (
                <div className="text-center py-8 text-muted-foreground">
                  No team members yet. Invite collaborators to work on this course.
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Pending Invites */}
        {course.teamInvites && course.teamInvites.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Mail className="h-5 w-5" />
                Pending Invites ({course.teamInvites.length})
              </CardTitle>
              <CardDescription>
                Invitations that have been sent but not yet accepted
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {course.teamInvites.map((invite) => (
                  <div key={invite.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex items-center gap-3">
                      <div className="h-10 w-10 rounded-full bg-muted flex items-center justify-center">
                        <Mail className="h-4 w-4" />
                      </div>
                      <div>
                        <div className="font-medium">{invite.email}</div>
                        <div className="text-sm text-muted-foreground flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          Invited {new Date(invite.invitedAt).toLocaleDateString()}
                        </div>
                      </div>
                    </div>
                    
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className={cn("capitalize", roleColors[invite.role])}>
                        {invite.role}
                      </Badge>
                      <Badge variant="outline" className="bg-amber-500/10 text-amber-400 border-amber-500/20">
                        Pending
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}

function InviteTeamMemberDialog({ onInvite, onClose }: {
  onInvite: (data: Omit<TeamInvite, "id" | "invitedAt" | "status" | "invitedBy" | "expiresAt">) => void
  onClose: () => void
}) {
  const [email, setEmail] = React.useState("")
  const [role, setRole] = React.useState<TeamRole>("viewer")

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (email.trim()) {
      onInvite({ email: email.trim(), role })
      setEmail("")
      setRole("viewer")
    }
  }

  return (
    <>
      <DialogHeader>
        <DialogTitle>Invite Team Member</DialogTitle>
        <DialogDescription>
          Send an invitation to collaborate on this course
        </DialogDescription>
      </DialogHeader>
      
      <form onSubmit={handleSubmit}>
        <div className="space-y-4">
          <div>
            <Label htmlFor="email">Email Address</Label>
            <Input
              id="email"
              type="email"
              placeholder="colleague@company.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>
          
          <div>
            <Label htmlFor="role">Role</Label>
            <Select value={role} onValueChange={(value: TeamRole) => setRole(value)}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="viewer">Viewer - Can view course content</SelectItem>
                <SelectItem value="editor">Editor - Can edit course content</SelectItem>
                <SelectItem value="admin">Admin - Can manage team and settings</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
        
        <DialogFooter className="mt-6">
          <Button type="button" variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={!email.trim()}>
            Send Invitation
          </Button>
        </DialogFooter>
      </form>
    </>
  )
}
