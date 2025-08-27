import {Card, CardContent, CardHeader} from "@/components/ui/card"
import {Calendar, Trophy, Users} from "lucide-react"

interface CommunityStatsProps {
  displayName: string
  joinDate: Date
}

export function CommunityStats({displayName, joinDate}: CommunityStatsProps) {
  return (
    <Card className="bg-slate-800/50 border-purple-500/20">
      <CardHeader>
        <h3 className="text-lg font-semibold text-white">Community Stats</h3>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-center gap-3 p-2 rounded-lg">
          <Calendar className="w-5 h-5 text-purple-400"/>
          <div>
            <div className="text-sm text-gray-400">Member Since</div>
            <div className="text-white">{joinDate.toLocaleDateString('en-US', {
              year: 'numeric',
              month: 'long',
              day: 'numeric'
            })}</div>
          </div>
        </div>
        <div className="flex items-center gap-3 p-2 rounded-lg">
          <Users className="w-5 h-5 text-green-400"/>
          <div>
            <div className="text-sm text-gray-400">Status</div>
            <div className="text-white">Active Member</div>
          </div>
        </div>
        <div className="flex items-center gap-3 p-2 rounded-lg">
          <Trophy className="w-5 h-5 text-yellow-400"/>
          <div>
            <div className="text-sm text-gray-400">Community Level</div>
            <div className="text-white">Contributor</div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
