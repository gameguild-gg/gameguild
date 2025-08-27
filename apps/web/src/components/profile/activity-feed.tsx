import {Card, CardContent} from "@/components/ui/card"
import {Avatar, AvatarFallback, AvatarImage} from "@/components/ui/avatar"

interface Activity {
  action: string
  item: string
  time: string
  type: string
}

interface ActivityFeedProps {
  activities: Activity[]
  username: string
  displayName: string
  initials: string
}

export function ActivityFeed({activities, username, displayName, initials}: ActivityFeedProps) {
  return (
    <div className="space-y-4">
      {activities.map((activity, index) => (
        <Card key={index} className="bg-slate-800/50 border-purple-500/20">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <Avatar className="w-8 h-8">
                <AvatarImage src={`/avatars/${username}.png`}/>
                <AvatarFallback className="bg-purple-600 text-white text-xs">{initials}</AvatarFallback>
              </Avatar>
              <div className="flex-1">
                <p className="text-gray-300">
                  <span className="text-white font-medium">{displayName}</span> {activity.action}{" "}
                  <span className="text-purple-400">{activity.item}</span>
                </p>
                <p className="text-sm text-gray-500">{activity.time}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
