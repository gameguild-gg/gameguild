import {Card, CardContent, CardHeader} from "@/components/ui/card"

interface AboutSectionProps {
  displayName: string
  joinDate: Date
}

export function AboutSection({displayName, joinDate}: AboutSectionProps) {
  return (
    <Card className="bg-slate-800/50 border-purple-500/20">
      <CardHeader>
        <h3 className="text-lg font-semibold text-white">About {displayName}</h3>
      </CardHeader>
      <CardContent className="space-y-4 text-gray-300">
        <p>
          Welcome to {displayName}&apos;s profile! This community member has been part of Game Guild
          since {joinDate.getFullYear()},
          contributing to various discussions and projects within our gaming community.
        </p>
        <p>
          As an active member of Game Guild, {displayName} participates in community events, shares
          knowledge,
          and collaborates on exciting gaming projects with fellow community members.
        </p>
        <p>
          Feel free to connect and explore the various projects and contributions this member has shared
          with the Game Guild community.
        </p>
      </CardContent>
    </Card>
  )
}
