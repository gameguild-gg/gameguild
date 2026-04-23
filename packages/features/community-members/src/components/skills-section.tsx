import { Card, CardContent, CardHeader } from '@game-guild/ui/components/card';
import { Progress } from '@game-guild/ui/components/progress';
import type { LucideIcon } from 'lucide-react';
import type { MemberSkill } from '../types';

interface SkillsSectionProps {
  title: string;
  icon: LucideIcon;
  iconColor: string;
  skills: MemberSkill[];
  levelColor: string;
}

export function SkillsSection({ title, icon: Icon, iconColor, skills, levelColor }: SkillsSectionProps) {
  return (
    <Card className="bg-slate-800/50 border-purple-500/20">
      <CardHeader>
        <h3 className="text-lg font-semibold text-white flex items-center gap-2">
          <Icon className={`w-5 h-5 ${iconColor}`} />
          {title}
        </h3>
      </CardHeader>
      <CardContent className="space-y-4">
        {skills.map((skill, index) => (
          <div key={index}>
            <div className="flex justify-between text-sm mb-1">
              <span className="text-gray-300">{skill.name}</span>
              <span className={levelColor}>{skill.level}%</span>
            </div>
            <Progress value={skill.level} className="h-2" />
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
