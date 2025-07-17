import { getTrackBySlug } from '@/lib/tracks/actions';
import { getCourseData } from '@/lib/courses/actions';
import Image from 'next/image';
import Link from 'next/link';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Progress } from '@game-guild/ui/components';
import {
  ArrowLeft,
  BookOpen,
  Code,
  Palette,
  Gamepad2,
  Clock,
  Users,
  Trophy,
  Star,
  CheckCircle,
  Play,
  Award,
  Target,
  Lightbulb,
  ExternalLink,
} from 'lucide-react';
import { TRACK_LEVELS, TRACK_LEVEL_COLORS, Track } from '@/types/tracks';

// Type definitions for real course data
interface RealCourse {
  id: string;
  title: string;
  description: string;
  slug: string;
  thumbnail: string;
  estimatedHours: number;
  difficulty: number;
  category: number;
}

interface Course {
  id: number;
  name: string;
  title: string;
  duration: string;
  description: string;
  projects: string[];
  image: string;
  level: string;
  features?: string[];
  slug?: string;
  isReal?: boolean;
}

interface TrackBlock {
  id: number;
  title: string;
  description: string;
  duration: string;
  courses: Course[];
  features: string[];
}

interface Instructor {
  name: string;
  title: string;
  image: string;
  avatar: string;
  bio: string;
  experience: string;
  students: string;
  credentials: string[];
}

interface Testimonial {
  name: string;
  role: string;
  company: string;
  content: string;
  comment: string;
  image: string;
  avatar: string;
  rating: number;
}

interface MockTrackData extends Track {
  duration: string;
  students: number;
  rating: number;
  reviewCount: number;
  industryRole: {
    title: string;
    description: string;
    responsibilities: string[];
    skills: string[];
  };
  industryContext: {
    overview: string;
    roleDescription: string;
    careerPaths: string[];
    industryStats: string[];
  };
  learningObjectives: string[];
  trackBlocks: TrackBlock[];
  tools: string[];
  prerequisites: string[];
  instructor: Instructor;
  testimonials: Testimonial[];
}

const areaIcons = {
  programming: Code,
  art: Palette,
  design: Gamepad2,
};

const areaBackgrounds = {
  programming: 'bg-gradient-to-br from-blue-900 via-blue-800 to-blue-950',
  art: 'bg-gradient-to-br from-purple-900 via-purple-800 to-purple-950',
  design: 'bg-gradient-to-br from-green-900 via-green-800 to-green-950',
  default: 'bg-gradient-to-br from-gray-900 via-gray-800 to-gray-950',
};

const areaAccents = {
  programming: 'border-blue-500 bg-blue-500/10',
  art: 'border-purple-500 bg-purple-500/10',
  design: 'border-green-500 bg-green-500/10',
  default: 'border-gray-500 bg-gray-500/10',
};

// Helper function to transform real courses to curriculum format
function transformRealCoursesToCurriculum(realCourses: RealCourse[]): TrackBlock[] {
  // Group courses by difficulty level
  const beginnerCourses = realCourses.filter((course) => course.difficulty === 0);
  const intermediateCourses = realCourses.filter((course) => course.difficulty === 1);
  const advancedCourses = realCourses.filter((course) => course.difficulty === 2 || course.difficulty === 3);

  const getDifficultyName = (difficulty: number) => {
    switch (difficulty) {
      case 0:
        return 'Beginner';
      case 1:
        return 'Intermediate';
      case 2:
        return 'Advanced';
      case 3:
        return 'Expert';
      default:
        return 'Beginner';
    }
  };

  const transformCourse = (course: RealCourse): Course => ({
    id: parseInt(course.id.slice(-8), 16), // Convert part of UUID to number for compatibility
    name: course.slug,
    title: course.title,
    description: course.description,
    duration: `${course.estimatedHours} hours`,
    level: getDifficultyName(course.difficulty),
    image: course.thumbnail.startsWith('/images/') ? course.thumbnail : '/placeholder.svg',
    projects: [], // Projects would need to come from course content
    features: [], // Features would need to come from course content
    slug: course.slug,
    isReal: true,
  });

  const blocks: TrackBlock[] = [];

  if (beginnerCourses.length > 0) {
    blocks.push({
      id: 0,
      title: 'Foundation Block',
      description: 'Build your fundamental skills with beginner-friendly courses',
      duration: `${beginnerCourses.reduce((sum, course) => sum + course.estimatedHours, 0)} hours`,
      features: ['Interactive Learning', 'Hands-on Projects', 'Expert Mentorship'],
      courses: beginnerCourses.slice(0, 6).map(transformCourse), // Limit to 6 courses per block
    });
  }

  if (intermediateCourses.length > 0) {
    blocks.push({
      id: 1,
      title: 'Core Development',
      description: 'Dive deeper into technical implementation and specialized skills',
      duration: `${intermediateCourses.reduce((sum, course) => sum + course.estimatedHours, 0)} hours`,
      features: ['Advanced Programming', 'Specialized Tools', 'Real Projects'],
      courses: intermediateCourses.slice(0, 6).map(transformCourse),
    });
  }

  if (advancedCourses.length > 0) {
    blocks.push({
      id: 2,
      title: 'Advanced Techniques',
      description: 'Master professional-level skills and cutting-edge technologies',
      duration: `${advancedCourses.reduce((sum, course) => sum + course.estimatedHours, 0)} hours`,
      features: ['Portfolio Development', 'Industry Preparation', 'Advanced Systems'],
      courses: advancedCourses.slice(0, 6).map(transformCourse),
    });
  }

  return blocks;
}

// Mock data for demonstration
const getMockTrackData = async (track: Track): Promise<MockTrackData> => {
  // Fetch real course data
  const courseData = await getCourseData();
  const realCourses: RealCourse[] = courseData.courses.map((course) => ({
    id: course.id,
    title: course.title,
    description: course.description,
    slug: course.slug,
    thumbnail: course.image,
    estimatedHours: 40, // Default hours since not provided in Course type
    difficulty: course.level - 1, // Convert level (1-4) to difficulty (0-3)
    category: 0, // Default category
  }));

  const trackBlocks =
    realCourses.length > 0
      ? transformRealCoursesToCurriculum(realCourses)
      : [
          // Fallback mock data if no real courses available
          {
            id: 0,
            title: 'Foundation Block',
            description: 'Build your fundamental skills in game development',
            duration: '14 weeks',
            features: ['Interactive Learning', 'Hands-on Projects', 'Expert Mentorship'],
            courses: [
              {
                id: 1,
                name: 'Course 101',
                title: 'Course 101: Game Development Basics',
                description: 'Learn the core concepts and principles that drive successful game creation.',
                duration: '8 weeks',
                level: 'Beginner',
                image: '/placeholder.svg',
                projects: ['Simple 2D Game', 'Game Design Document'],
                features: ['Game Design Fundamentals', 'Development Lifecycle'],
              },
            ],
          },
        ];

  return {
    ...track,
    duration: '24+ weeks',
    students: 2547,
    rating: 4.8,
    reviewCount: 324,
    trackBlocks,
    prerequisites: [
      'Basic understanding of computer fundamentals',
      'Passion for game development',
      'Willingness to learn and practice regularly',
      'Access to a computer capable of running development tools',
    ],
    learningObjectives: [
      'Master the fundamentals of game development and design principles',
      'Build complete games from concept to deployment',
      'Understand industry-standard development workflows and pipelines',
      'Develop proficiency in essential game development tools and technologies',
      'Create a professional portfolio showcasing your game development skills',
      'Learn collaborative development practices used in professional studios',
      'Understand game monetization strategies and business aspects',
      'Master debugging and optimization techniques for game performance',
    ],
    industryRole: {
      title: 'Game Developer',
      description: 'Create interactive entertainment experiences that engage and inspire players worldwide.',
      responsibilities: [
        'Design and implement game mechanics and systems',
        'Collaborate with artists, designers, and other developers',
        'Optimize game performance and user experience',
        'Debug and troubleshoot technical issues',
        'Participate in code reviews and maintain code quality',
      ],
      skills: [
        'Programming in C# or C++',
        'Game engine proficiency (Unity/Unreal)',
        'Problem-solving and debugging',
        'Team collaboration and communication',
        'Understanding of game design principles',
      ],
    },
    tools: [
      'Unity 3D',
      'Visual Studio / VS Code',
      'Git Version Control',
      'Blender (3D Modeling)',
      'Photoshop / GIMP',
      'Perforce',
      'Jira / Trello',
      'Discord / Slack',
    ],
    instructor: {
      name: 'Alex Richardson',
      title: 'Senior Game Developer',
      image: '/placeholder.svg',
      avatar: '/placeholder.svg',
      bio: 'Former Lead Developer at Epic Games with 12+ years of industry experience. Alex has shipped multiple AAA titles and indie games, bringing real-world expertise to guide students through their game development journey.',
      experience: '12+ years',
      students: '2,500+',
      credentials: [
        'Lead Developer - Fortnite (Epic Games)',
        'Senior Programmer - Gears of War series',
        '15+ shipped game titles',
        'Industry mentor and speaker',
      ],
    },
    testimonials: [
      {
        name: 'Sarah Chen',
        role: 'Game Developer',
        company: 'Ubisoft',
        content:
          'This track completely transformed my understanding of game development. The structured approach and hands-on projects gave me the confidence to land my dream job.',
        comment:
          'This track completely transformed my understanding of game development. The structured approach and hands-on projects gave me the confidence to land my dream job.',
        rating: 5,
        image: '/placeholder.svg',
        avatar: '/placeholder.svg',
      },
      {
        name: 'Marcus Thompson',
        role: 'Indie Game Developer',
        company: 'Independent',
        content: 'The progression from basics to advanced topics was perfect. I went from knowing nothing to shipping my first commercial game within a year.',
        comment: 'The progression from basics to advanced topics was perfect. I went from knowing nothing to shipping my first commercial game within a year.',
        rating: 5,
        image: '/placeholder.svg',
        avatar: '/placeholder.svg',
      },
      {
        name: 'Elena Rodriguez',
        role: 'Technical Artist',
        company: 'Activision',
        content: 'The hands-on approach and real-world projects prepared me for the challenges I face daily in a professional studio environment.',
        comment: 'The hands-on approach and real-world projects prepared me for the challenges I face daily in a professional studio environment.',
        rating: 5,
        image: '/placeholder.svg',
        avatar: '/placeholder.svg',
      },
    ],
    industryContext: {
      overview:
        'Game development is one of the most dynamic and creative fields in technology today. As a game developer, you&apos;ll be responsible for bringing interactive entertainment experiences to life, combining technical expertise with creative vision to create games that engage and inspire players worldwide.',
      roleDescription:
        'Game developers work in collaborative teams to design, develop, and ship interactive entertainment products. This multidisciplinary field requires both technical programming skills and creative problem-solving abilities, making it one of the most rewarding career paths in the entertainment industry.',
      careerPaths: ['Gameplay Programmer', 'Engine Programmer', 'Technical Artist', 'Game Designer', 'Level Designer', 'Independent Game Developer'],
      industryStats: [
        'Global games market worth $321 billion (2024)',
        '3.3 billion gamers worldwide',
        'Average salary: $85,000 - $160,000',
        '250,000+ jobs in game development globally',
      ],
    },
  };
};

export default async function TrackDetailPage({ params }: { params: { slug: string } }) {
  const { slug } = await params;
  const track = await getTrackBySlug(slug);

  if (!track) {
    return (
      <div className="min-h-screen bg-gray-950 text-white">
        <div className="container mx-auto px-4 py-16">
          <div className="text-center">
            <h1 className="text-3xl font-bold text-red-400 mb-4">Track Not Found</h1>
            <p className="text-gray-300 mb-8">The learning track you&apos;re looking for doesn&apos;t exist.</p>
            <Button asChild className="bg-blue-600 hover:bg-blue-700">
              <Link href="/tracks">
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Learning Tracks
              </Link>
            </Button>
          </div>
        </div>
      </div>
    );
  }

  const trackData = await getMockTrackData(track);
  const AreaIcon = areaIcons[track.area as keyof typeof areaIcons] || BookOpen;
  const backgroundClass = areaBackgrounds[track.area as keyof typeof areaBackgrounds] || areaBackgrounds.default;
  const accentClass = areaAccents[track.area as keyof typeof areaAccents] || areaAccents.default;
  const levelName = TRACK_LEVELS[track.level as keyof typeof TRACK_LEVELS] || 'Unknown';
  const levelColor = TRACK_LEVEL_COLORS[track.level as keyof typeof TRACK_LEVEL_COLORS] || 'bg-gray-500';

  return (
    <div className={`min-h-screen text-white ${backgroundClass}`}>
      {/* Navigation */}
      <div className="container mx-auto px-4 pt-8">
        <Button asChild variant="ghost" className="text-white hover:bg-white/10 mb-6">
          <Link href="/tracks">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Learning Tracks
          </Link>
        </Button>
      </div>

      {/* Hero Section */}
      <div className="container mx-auto px-4 pb-16">
        <div className="grid lg:grid-cols-2 gap-12 items-center">
          {/* Left Side - Content */}
          <div className="space-y-6">
            <div className="flex items-center gap-3 mb-4">
              <div className={`p-3 rounded-full border-2 ${accentClass}`}>
                <AreaIcon className="h-8 w-8 text-white" />
              </div>
              <div>
                <Badge className={`${levelColor} text-white mb-2 px-3 py-1`}>{levelName}</Badge>
                <p className="text-gray-300 capitalize font-medium">{track.area} Learning Track</p>
              </div>
            </div>

            <h1 className="text-5xl lg:text-6xl font-bold leading-tight">{track.title}</h1>
            <p className="text-xl text-gray-300 leading-relaxed">{track.description}</p>

            {/* Track Stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-8">
              <div className="text-center">
                <div className="flex items-center justify-center mb-2">
                  <Clock className="h-5 w-5 text-blue-400" />
                </div>
                <p className="text-sm text-gray-400">Duration</p>
                <p className="font-semibold">{trackData.duration}</p>
              </div>
              <div className="text-center">
                <div className="flex items-center justify-center mb-2">
                  <BookOpen className="h-5 w-5 text-green-400" />
                </div>
                <p className="text-sm text-gray-400">Courses</p>
                <p className="font-semibold">{trackData.trackBlocks.reduce((total: number, block: TrackBlock) => total + block.courses.length, 0)} courses</p>
              </div>
              <div className="text-center">
                <div className="flex items-center justify-center mb-2">
                  <Users className="h-5 w-5 text-purple-400" />
                </div>
                <p className="text-sm text-gray-400">Students</p>
                <p className="font-semibold">{trackData.students.toLocaleString()}+</p>
              </div>
              <div className="text-center">
                <div className="flex items-center justify-center mb-2">
                  <Trophy className="h-5 w-5 text-yellow-400" />
                </div>
                <p className="text-sm text-gray-400">Certificate</p>
                <p className="font-semibold">Included</p>
              </div>
            </div>

            {/* CTA Buttons */}
            <div className="flex flex-col sm:flex-row gap-4 mt-8">
              <Button size="lg" className="bg-blue-600 hover:bg-blue-700 text-lg px-8 py-3">
                Start This Track
                <Play className="ml-2 h-5 w-5" />
              </Button>
              <Button size="lg" variant="outline" className="border-white/20 hover:bg-white/10 text-lg px-8 py-3">
                View Sample Course
              </Button>
            </div>
          </div>

          {/* Right Side - Hero Image */}
          <div className="relative">
            <div className="relative w-full h-96 rounded-2xl overflow-hidden shadow-2xl">
              <Image src={track.image || '/placeholder.svg'} alt={track.title} fill className="object-cover" />
              <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
            </div>

            {/* Floating stats card */}
            <Card className="absolute bottom-4 right-4 bg-black/80 border-white/20 backdrop-blur">
              <CardContent className="p-4">
                <div className="flex items-center gap-2 text-yellow-400">
                  <Star className="h-4 w-4 fill-current" />
                  <span className="font-semibold">{trackData.rating}</span>
                  <span className="text-gray-400 text-sm">({trackData.reviewCount} reviews)</span>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="bg-gray-900/50 backdrop-blur">
        <div className="container mx-auto px-4 py-16">
          <div className="grid lg:grid-cols-3 gap-12">
            {/* Left Content - Main Information */}
            <div className="lg:col-span-2 space-y-16">
              {/* Industry Context */}
              <section>
                <h2 className="text-3xl font-bold mb-6 flex items-center gap-3">
                  <Target className="h-8 w-8 text-blue-400" />
                  Understand the Role
                </h2>
                <div className="space-y-6">
                  <p className="text-lg text-gray-300 leading-relaxed">{trackData.industryContext.overview}</p>
                  <p className="text-gray-400 leading-relaxed">{trackData.industryContext.roleDescription}</p>

                  <div className="grid md:grid-cols-2 gap-6 mt-8">
                    <div>
                      <h3 className="text-xl font-semibold mb-4 text-blue-400">Career Paths</h3>
                      <ul className="space-y-2">
                        {trackData.industryContext.careerPaths.map((path: string, index: number) => (
                          <li key={index} className="flex items-center gap-2 text-gray-300">
                            <CheckCircle className="h-4 w-4 text-green-400 flex-shrink-0" />
                            {path}
                          </li>
                        ))}
                      </ul>
                    </div>
                    <div>
                      <h3 className="text-xl font-semibold mb-4 text-green-400">Industry Stats</h3>
                      <ul className="space-y-2">
                        {trackData.industryContext.industryStats.map((stat: string, index: number) => (
                          <li key={index} className="flex items-center gap-2 text-gray-300">
                            <Award className="h-4 w-4 text-yellow-400 flex-shrink-0" />
                            {stat}
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                </div>
              </section>

              {/* Learning Objectives */}
              <section>
                <h2 className="text-3xl font-bold mb-6 flex items-center gap-3">
                  <Lightbulb className="h-8 w-8 text-yellow-400" />
                  What You&apos;ll Learn
                </h2>
                <div className="grid md:grid-cols-2 gap-4">
                  {trackData.learningObjectives.map((objective: string, index: number) => (
                    <div key={index} className="flex items-start gap-3">
                      <CheckCircle className="h-5 w-5 text-green-400 mt-0.5 flex-shrink-0" />
                      <span className="text-gray-300">{objective}</span>
                    </div>
                  ))}
                </div>
              </section>

              {/* Track Curriculum */}
              <section>
                <h2 className="text-3xl font-bold mb-8">Track Curriculum</h2>
                <div className="space-y-8">
                  {trackData.trackBlocks.map((block: TrackBlock, blockIndex: number) => (
                    <div key={block.id} className="space-y-4">
                      <div className="flex items-center gap-4 mb-6">
                        <div className="bg-blue-600 text-white px-4 py-2 rounded-lg font-bold">Block {blockIndex}</div>
                        <div>
                          <h3 className="text-2xl font-bold">{block.title}</h3>
                          <p className="text-gray-400">{block.description}</p>
                        </div>
                      </div>

                      <div className="grid gap-6">
                        {block.courses.map((course: Course) => (
                          <Card
                            key={course.id}
                            className={`bg-gray-800/50 border-gray-700 hover:bg-gray-800 transition-all duration-200 group ${
                              course.isReal ? 'cursor-pointer' : 'cursor-default'
                            }`}
                          >
                            <CardContent className="p-6">
                              {course.isReal && course.slug ? (
                                <Link href={`/courses/${course.slug}`} className="block">
                                  <div className="flex items-start gap-4">
                                    <div className="relative w-24 h-24 rounded-lg overflow-hidden bg-gray-700 flex-shrink-0">
                                      <Image src={course.image} alt={course.title} fill className="object-cover" />
                                    </div>
                                    <div className="flex-grow">
                                      <div className="flex items-center gap-3 mb-2">
                                        <Badge variant="outline" className="border-blue-500 text-blue-400">
                                          {course.level}
                                        </Badge>
                                        <Badge variant="secondary" className="bg-gray-700 text-gray-300">
                                          {course.duration}
                                        </Badge>
                                      </div>
                                      <div className="flex items-center justify-between">
                                        <h4 className="text-xl font-semibold mb-2 group-hover:text-blue-400 transition-colors">{course.title}</h4>
                                        <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-blue-400 transition-colors" />
                                      </div>
                                      <p className="text-gray-400 mb-3">{course.description}</p>
                                      {course.features && course.features.length > 0 && (
                                        <div className="flex flex-wrap gap-2">
                                          {course.features.map((feature: string, index: number) => (
                                            <Badge key={index} variant="secondary" className="bg-blue-900/20 text-blue-300 text-xs">
                                              {feature}
                                            </Badge>
                                          ))}
                                        </div>
                                      )}
                                    </div>
                                  </div>
                                </Link>
                              ) : (
                                <div className="flex items-start gap-4">
                                  <div className="relative w-24 h-24 rounded-lg overflow-hidden bg-gray-700 flex-shrink-0">
                                    <Image src={course.image} alt={course.title} fill className="object-cover" />
                                  </div>
                                  <div className="flex-grow">
                                    <div className="flex items-center gap-3 mb-2">
                                      <Badge variant="outline" className="border-blue-500 text-blue-400">
                                        {course.level}
                                      </Badge>
                                      <Badge variant="secondary" className="bg-gray-700 text-gray-300">
                                        {course.duration}
                                      </Badge>
                                    </div>
                                    <div className="flex items-center justify-between">
                                      <h4 className="text-xl font-semibold mb-2">{course.title}</h4>
                                      <Badge variant="outline" className="border-yellow-500 text-yellow-400 opacity-60">
                                        Coming Soon
                                      </Badge>
                                    </div>
                                    <p className="text-gray-400 mb-3">{course.description}</p>
                                    {course.features && course.features.length > 0 && (
                                      <div className="flex flex-wrap gap-2">
                                        {course.features.map((feature: string, index: number) => (
                                          <Badge key={index} variant="secondary" className="bg-blue-900/20 text-blue-300 text-xs">
                                            {feature}
                                          </Badge>
                                        ))}
                                      </div>
                                    )}
                                  </div>
                                </div>
                              )}
                            </CardContent>
                          </Card>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </section>

              {/* Track Features */}
              <section>
                <h2 className="text-3xl font-bold mb-6">Track Features</h2>
                <div className="grid md:grid-cols-2 gap-6">
                  <Card className="bg-gray-800 border-gray-700">
                    <CardContent className="p-6">
                      <div className="flex items-center gap-3 mb-4">
                        <Award className="h-6 w-6 text-yellow-400" />
                        <h3 className="text-xl font-semibold">Industry Recognition</h3>
                      </div>
                      <p className="text-gray-300">Receive a verified certificate upon completion, recognized by leading game development studios worldwide.</p>
                    </CardContent>
                  </Card>

                  <Card className="bg-gray-800 border-gray-700">
                    <CardContent className="p-6">
                      <div className="flex items-center gap-3 mb-4">
                        <Users className="h-6 w-6 text-blue-400" />
                        <h3 className="text-xl font-semibold">Community Access</h3>
                      </div>
                      <p className="text-gray-300">Join our exclusive community of developers, participate in game jams, and collaborate on projects.</p>
                    </CardContent>
                  </Card>

                  <Card className="bg-gray-800 border-gray-700">
                    <CardContent className="p-6">
                      <div className="flex items-center gap-3 mb-4">
                        <Clock className="h-6 w-6 text-green-400" />
                        <h3 className="text-xl font-semibold">Flexible Schedule</h3>
                      </div>
                      <p className="text-gray-300">Learn at your own pace with self-paced modules and optional live sessions with instructors.</p>
                    </CardContent>
                  </Card>

                  <Card className="bg-gray-800 border-gray-700">
                    <CardContent className="p-6">
                      <div className="flex items-center gap-3 mb-4">
                        <BookOpen className="h-6 w-6 text-purple-400" />
                        <h3 className="text-xl font-semibold">Lifetime Access</h3>
                      </div>
                      <p className="text-gray-300">Get lifetime access to all course materials, updates, and new content additions.</p>
                    </CardContent>
                  </Card>
                </div>
              </section>

              {/* Tools & Technologies */}
              <section>
                <h2 className="text-3xl font-bold mb-6">Tools & Technologies</h2>
                <div className="flex flex-wrap gap-3">
                  {track.tools.map((tool, index) => (
                    <Badge key={`${track.id}-tool-${tool}-${index}`} variant="secondary" className="bg-gray-800 text-white border-gray-600 px-4 py-2 text-sm">
                      {tool.replace('-', ' ')}
                    </Badge>
                  ))}
                </div>
              </section>

              {/* Prerequisites */}
              <section>
                <h2 className="text-3xl font-bold mb-6">Prerequisites</h2>
                <div className="space-y-3">
                  {trackData.prerequisites.map((req: string, index: number) => (
                    <div key={index} className="flex items-start gap-3">
                      <CheckCircle className="h-5 w-5 text-blue-400 mt-0.5 flex-shrink-0" />
                      <span className="text-gray-300">{req}</span>
                    </div>
                  ))}
                </div>
              </section>

              {/* Instructor */}
              <section>
                <h2 className="text-3xl font-bold mb-6">Meet Your Instructor</h2>
                <Card className="bg-gray-800 border-gray-700">
                  <CardContent className="p-6">
                    <div className="flex items-start gap-6">
                      <div className="relative w-24 h-24 rounded-full overflow-hidden bg-gray-700 flex-shrink-0">
                        <Image src={trackData.instructor.avatar} alt={trackData.instructor.name} fill className="object-cover" />
                      </div>
                      <div className="flex-grow">
                        <h3 className="text-2xl font-bold mb-1">{trackData.instructor.name}</h3>
                        <p className="text-blue-400 font-medium mb-3">{trackData.instructor.title}</p>
                        <p className="text-gray-300 mb-4">{trackData.instructor.bio}</p>
                        <div className="space-y-2">
                          {trackData.instructor.credentials.map((cred: string, index: number) => (
                            <div key={index} className="flex items-center gap-2">
                              <Award className="h-4 w-4 text-yellow-400" />
                              <span className="text-sm text-gray-400">{cred}</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </section>

              {/* Student Testimonials */}
              <section>
                <h2 className="text-3xl font-bold mb-6">Student Success Stories</h2>
                <div className="grid gap-6">
                  {trackData.testimonials.map((testimonial: Testimonial, index: number) => (
                    <Card key={index} className="bg-gray-800 border-gray-700">
                      <CardContent className="p-6">
                        <div className="flex items-start gap-4">
                          <div className="relative w-16 h-16 rounded-full overflow-hidden bg-gray-700 flex-shrink-0">
                            <Image src={testimonial.avatar} alt={testimonial.name} fill className="object-cover" />
                          </div>
                          <div className="flex-grow">
                            <div className="flex items-center gap-2 mb-2">
                              {[...Array(testimonial.rating)].map((_, i) => (
                                <Star key={i} className="h-4 w-4 text-yellow-400 fill-current" />
                              ))}
                            </div>
                            <p className="text-gray-300 mb-3 italic">&quot;{testimonial.comment}&quot;</p>
                            <div>
                              <p className="font-semibold">{testimonial.name}</p>
                              <p className="text-sm text-gray-400">{testimonial.role}</p>
                            </div>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              </section>

              {/* Track Schedule */}
              <section>
                <h2 className="text-3xl font-bold mb-6">Track Schedule</h2>
                <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6">
                  <Card className="bg-gray-800 border-gray-700">
                    <CardHeader>
                      <CardTitle className="text-green-400">Spring 2025</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2 text-sm">
                        <p>
                          <span className="text-gray-400">Starts:</span> March 15
                        </p>
                        <p>
                          <span className="text-gray-400">Ends:</span> June 8
                        </p>
                        <p>
                          <span className="text-gray-400">Registration:</span> Open
                        </p>
                      </div>
                      <Button className="w-full mt-4 bg-green-600 hover:bg-green-700">Enroll Now</Button>
                    </CardContent>
                  </Card>

                  <Card className="bg-gray-800 border-gray-700">
                    <CardHeader>
                      <CardTitle className="text-blue-400">Summer 2025</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2 text-sm">
                        <p>
                          <span className="text-gray-400">Starts:</span> June 15
                        </p>
                        <p>
                          <span className="text-gray-400">Ends:</span> September 7
                        </p>
                        <p>
                          <span className="text-gray-400">Registration:</span> May 1
                        </p>
                      </div>
                      <Button variant="outline" className="w-full mt-4 border-gray-600">
                        Notify Me
                      </Button>
                    </CardContent>
                  </Card>

                  <Card className="bg-gray-800 border-gray-700">
                    <CardHeader>
                      <CardTitle className="text-orange-400">Fall 2025</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2 text-sm">
                        <p>
                          <span className="text-gray-400">Starts:</span> September 14
                        </p>
                        <p>
                          <span className="text-gray-400">Ends:</span> December 7
                        </p>
                        <p>
                          <span className="text-gray-400">Registration:</span> August 1
                        </p>
                      </div>
                      <Button variant="outline" className="w-full mt-4 border-gray-600">
                        Notify Me
                      </Button>
                    </CardContent>
                  </Card>

                  <Card className="bg-gray-800 border-gray-700">
                    <CardHeader>
                      <CardTitle className="text-purple-400">Winter 2026</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2 text-sm">
                        <p>
                          <span className="text-gray-400">Starts:</span> January 11
                        </p>
                        <p>
                          <span className="text-gray-400">Ends:</span> April 5
                        </p>
                        <p>
                          <span className="text-gray-400">Registration:</span> December 1
                        </p>
                      </div>
                      <Button variant="outline" className="w-full mt-4 border-gray-600">
                        Notify Me
                      </Button>
                    </CardContent>
                  </Card>
                </div>
              </section>
            </div>

            {/* Right Sidebar */}
            <div className="lg:col-span-1">
              <div className="sticky top-8 space-y-6">
                {/* Enrollment Card */}
                <Card className="bg-gray-800 border-gray-700">
                  <CardHeader>
                    <CardTitle className="text-xl text-center">Start Your Journey</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    {typeof track.progress === 'number' && (
                      <div>
                        <div className="flex justify-between items-center mb-2">
                          <span className="text-sm text-gray-400">Your Progress</span>
                          <span className="text-sm font-medium">{track.progress}%</span>
                        </div>
                        <Progress value={track.progress} className="w-full" />
                      </div>
                    )}

                    <div className="space-y-3 text-sm">
                      <div className="flex justify-between">
                        <span className="text-gray-400">Total Duration:</span>
                        <span className="font-medium">{trackData.duration}</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-400">Skill Level:</span>
                        <Badge className={`${levelColor} text-white text-xs`}>{levelName}</Badge>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-400">Certificate:</span>
                        <span className="font-medium">Yes</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-400">Students:</span>
                        <span className="font-medium">{trackData.students.toLocaleString()}+</span>
                      </div>
                      <div className="flex justify-between">
                        <span className="text-gray-400">Rating:</span>
                        <div className="flex items-center gap-1">
                          <Star className="h-3 w-3 text-yellow-400 fill-current" />
                          <span className="font-medium">{trackData.rating}</span>
                        </div>
                      </div>
                    </div>

                    <Button className="w-full bg-blue-600 hover:bg-blue-700 mt-6">Enroll in Track</Button>
                    <Button variant="outline" className="w-full border-gray-600 hover:bg-gray-700">
                      Preview First Course
                    </Button>
                  </CardContent>
                </Card>

                {/* Status Card (if user is enrolled) */}
                {track.obtained && track.obtained !== '0' && (
                  <Card className="bg-green-900/20 border-green-500/30">
                    <CardContent className="p-4">
                      <div className="flex items-center gap-3">
                        <div className="w-3 h-3 bg-green-400 rounded-full animate-pulse"></div>
                        <span className="font-medium">{track.obtained === '1' ? 'Track Completed' : 'Currently Enrolled'}</span>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Related Tracks */}
                <Card className="bg-gray-800 border-gray-700">
                  <CardHeader>
                    <CardTitle className="text-lg">Explore Related Tracks</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <Button asChild variant="ghost" className="w-full justify-start hover:bg-gray-700">
                      <Link href="/tracks">
                        <Code className="mr-3 h-4 w-4" />
                        Advanced Programming
                      </Link>
                    </Button>
                    <Button asChild variant="ghost" className="w-full justify-start hover:bg-gray-700">
                      <Link href="/tracks">
                        <Palette className="mr-3 h-4 w-4" />
                        Creative Arts & Design
                      </Link>
                    </Button>
                    <Button asChild variant="ghost" className="w-full justify-start hover:bg-gray-700">
                      <Link href="/tracks">
                        <Gamepad2 className="mr-3 h-4 w-4" />
                        Game Design Mastery
                      </Link>
                    </Button>
                  </CardContent>
                </Card>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
