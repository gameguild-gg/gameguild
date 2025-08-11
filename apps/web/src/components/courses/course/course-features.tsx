import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Award, Clock, Play, Trophy, Users } from 'lucide-react';

const COURSE_FEATURES = [
  {
    icon: Play,
    title: 'Interactive Learning',
    description: 'Hands-on exercises and projects that reinforce theoretical concepts.',
    color: 'text-blue-400',
  },
  {
    icon: Users,
    title: 'Community Support',
    description: 'Join a community of learners and get help when you need it.',
    color: 'text-green-400',
  },
  {
    icon: Trophy,
    title: 'Certification',
    description: 'Earn a certificate of completion to showcase your achievements.',
    color: 'text-yellow-400',
  },
  {
    icon: Clock,
    title: 'Self-Paced',
    description: 'Learn at your own pace with lifetime access to course materials.',
    color: 'text-purple-400',
  },
] as const;

export function CourseFeatures() {
  return (
    <section>
      <h2 className="text-2xl font-bold mb-6 flex items-center">
        <Award className="mr-3 h-6 w-6 text-yellow-400" />
        Course Features
      </h2>
      <div className="grid md:grid-cols-2 gap-6">
        {COURSE_FEATURES.map(({ icon: Icon, title, description, color }) => (
          <Card key={title} className="bg-gray-800 border-gray-700">
            <CardHeader>
              <CardTitle className="text-lg flex items-center">
                <Icon className={`mr-2 h-5 w-5 ${color}`} />
                {title}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-gray-300">{description}</p>
            </CardContent>
          </Card>
        ))}
      </div>
    </section>
  );
}
