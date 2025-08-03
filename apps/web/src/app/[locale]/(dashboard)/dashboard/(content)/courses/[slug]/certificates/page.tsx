'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Award, GripVertical, Plus, Search, Target, Trash2, TrendingUp, X } from 'lucide-react';
import type { CourseCertificate, CourseSkill } from '@/components/courses/editor/context/course-editor-provider';
import { useCourseEditor } from '@/components/courses/editor/context/course-editor-provider';
import { useState } from 'react';

// Mock data for available skills (in a real app, this would come from an API)
const AVAILABLE_SKILLS = [
  {
    id: 'js-basics',
    name: 'JavaScript Fundamentals',
    category: 'Programming',
    level: 'beginner' as const,
    description: 'Core JavaScript concepts and syntax',
  },
  {
    id: 'react-basics',
    name: 'React Fundamentals',
    category: 'Frontend',
    level: 'intermediate' as const,
    description: 'Component-based UI development',
  },
  {
    id: 'node-basics',
    name: 'Node.js Basics',
    category: 'Backend',
    level: 'intermediate' as const,
    description: 'Server-side JavaScript development',
  },
  {
    id: 'db-design',
    name: 'Database Design',
    category: 'Data',
    level: 'intermediate' as const,
    description: 'Relational database modeling',
  },
  {
    id: 'git-version',
    name: 'Git Version Control',
    category: 'Tools',
    level: 'beginner' as const,
    description: 'Version control with Git',
  },
  {
    id: 'api-design',
    name: 'API Design',
    category: 'Backend',
    level: 'advanced' as const,
    description: 'RESTful API development',
  },
  {
    id: 'testing',
    name: 'Software Testing',
    category: 'Quality',
    level: 'intermediate' as const,
    description: 'Unit and integration testing',
  },
  {
    id: 'deployment',
    name: 'Application Deployment',
    category: 'DevOps',
    level: 'advanced' as const,
    description: 'Production deployment strategies',
  },
];

const AVAILABLE_CERTIFICATES = [
  {
    id: 'web-dev-cert',
    name: 'Web Development Certificate',
    description: 'Comprehensive web development skills certification',
    imageUrl: '/certificates/web-dev.png',
    requirements: { completionPercentage: 80, passingGrade: 70, requiredSkills: ['js-basics', 'react-basics'] },
    template: 'professional',
  },
  {
    id: 'fullstack-cert',
    name: 'Full-Stack Developer Certificate',
    description: 'Complete full-stack development certification',
    imageUrl: '/certificates/fullstack.png',
    requirements: {
      completionPercentage: 90,
      passingGrade: 75,
      requiredSkills: ['js-basics', 'react-basics', 'node-basics', 'db-design'],
    },
    template: 'premium',
  },
];

const SKILL_LEVELS = [
  { value: 'beginner', label: 'Beginner', color: 'bg-green-100 text-green-800' },
  { value: 'intermediate', label: 'Intermediate', color: 'bg-blue-100 text-blue-800' },
  { value: 'advanced', label: 'Advanced', color: 'bg-orange-100 text-orange-800' },
  { value: 'expert', label: 'Expert', color: 'bg-red-100 text-red-800' },
];

const SKILL_CATEGORIES = ['Programming', 'Frontend', 'Backend', 'Data', 'Tools', 'Quality', 'DevOps', 'Design', 'Business'];

export default function Page() {
  const { state, addRequiredSkill, removeRequiredSkill, addProvidedSkill, removeProvidedSkill, addCertificate, removeCertificate } = useCourseEditor();

  const [showSkillSearch, setShowSkillSearch] = useState(false);
  const [skillSearchQuery, setSkillSearchQuery] = useState('');
  const [skillSearchMode, setSkillSearchMode] = useState<'required' | 'provided'>('required');
  const [showCertificateSearch, setShowCertificateSearch] = useState(false);
  const [certificateSearchQuery, setCertificateSearchQuery] = useState('');
  const [newSkill, setNewSkill] = useState({
    name: '',
    category: 'Programming',
    level: 'beginner' as const,
    description: '',
  });
  const [showNewSkillForm, setShowNewSkillForm] = useState(false);

  const filteredSkills = AVAILABLE_SKILLS.filter((skill) => skill.name.toLowerCase().includes(skillSearchQuery.toLowerCase()) || skill.category.toLowerCase().includes(skillSearchQuery.toLowerCase()));

  const filteredCertificates = AVAILABLE_CERTIFICATES.filter((cert) => cert.name.toLowerCase().includes(certificateSearchQuery.toLowerCase()) || cert.description.toLowerCase().includes(certificateSearchQuery.toLowerCase()));

  const handleAddSkill = (skill: CourseSkill, mode: 'required' | 'provided') => {
    if (mode === 'required') {
      addRequiredSkill(skill);
    } else {
      addProvidedSkill(skill);
    }
    setSkillSearchQuery('');
    setShowSkillSearch(false);
  };

  const handleCreateNewSkill = () => {
    if (!newSkill.name.trim()) return;

    const skill: CourseSkill = {
      id: `custom_${Date.now()}`,
      name: newSkill.name,
      category: newSkill.category,
      level: newSkill.level,
      description: newSkill.description,
    };

    if (skillSearchMode === 'required') {
      addRequiredSkill(skill);
    } else {
      addProvidedSkill(skill);
    }

    setNewSkill({ name: '', category: 'Programming', level: 'beginner', description: '' });
    setShowNewSkillForm(false);
    setShowSkillSearch(false);
  };

  const handleAddCertificate = (cert: CourseCertificate) => {
    addCertificate(cert);
    setCertificateSearchQuery('');
    setShowCertificateSearch(false);
  };

  const getSkillLevelStyle = (level: string) => {
    return SKILL_LEVELS.find((l) => l.value === level)?.color || 'bg-gray-100 text-gray-800';
  };

  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Sticky Header */}
      <div className="sticky top-0 z-10 border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">Certificates & Skills</h1>
              <p className="text-sm text-muted-foreground">Configure required skills, learning outcomes, and certificate awards</p>
            </div>
            <div className="flex items-center gap-2">
              <Badge variant="outline" className="gap-1">
                <Award className="h-3 w-3" />
                Skills & Certs
              </Badge>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto space-y-6">
          {/* Required Skills Section */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">🎯 Prerequisites & Required Skills</CardTitle>
              <p className="text-sm text-muted-foreground">Skills students need before taking this course</p>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <Label>Required Skills</Label>
                <Button
                  onClick={() => {
                    setSkillSearchMode('required');
                    setShowSkillSearch(true);
                  }}
                  size="sm"
                  className="gap-2"
                >
                  <Plus className="h-4 w-4" />
                  Add Skill
                </Button>
              </div>

              {state.certificates.skillsRequired.length > 0 ? (
                <div className="space-y-2">
                  {state.certificates.skillsRequired.map((skill) => (
                    <div key={skill.id} className="flex items-center gap-3 p-3 bg-muted/30 rounded-lg border">
                      <GripVertical className="h-4 w-4 text-muted-foreground cursor-move" />
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{skill.name}</span>
                          <Badge variant="outline" className={getSkillLevelStyle(skill.level)}>
                            {skill.level}
                          </Badge>
                          <Badge variant="secondary">{skill.category}</Badge>
                        </div>
                        {skill.description && <p className="text-sm text-muted-foreground mt-1">{skill.description}</p>}
                      </div>
                      <Button variant="ghost" size="sm" onClick={() => removeRequiredSkill(skill.id)} className="text-destructive hover:text-destructive">
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-8 text-muted-foreground">
                  <Target className="h-12 w-12 mx-auto mb-4 opacity-50" />
                  <p>No required skills set.</p>
                  <p className="text-sm">Add prerequisites for students to complete this course.</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Provided Skills Section */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">🚀 Learning Outcomes & Skills Provided</CardTitle>
              <p className="text-sm text-muted-foreground">Skills students will gain by completing this course</p>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <Label>Skills Provided</Label>
                <Button
                  onClick={() => {
                    setSkillSearchMode('provided');
                    setShowSkillSearch(true);
                  }}
                  size="sm"
                  className="gap-2"
                >
                  <Plus className="h-4 w-4" />
                  Add Skill
                </Button>
              </div>

              {state.certificates.skillsProvided.length > 0 ? (
                <div className="space-y-2">
                  {state.certificates.skillsProvided.map((skill) => (
                    <div key={skill.id} className="flex items-center gap-3 p-3 bg-muted/30 rounded-lg border">
                      <GripVertical className="h-4 w-4 text-muted-foreground cursor-move" />
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{skill.name}</span>
                          <Badge variant="outline" className={getSkillLevelStyle(skill.level)}>
                            {skill.level}
                          </Badge>
                          <Badge variant="secondary">{skill.category}</Badge>
                        </div>
                        {skill.description && <p className="text-sm text-muted-foreground mt-1">{skill.description}</p>}
                      </div>
                      <Button variant="ghost" size="sm" onClick={() => removeProvidedSkill(skill.id)} className="text-destructive hover:text-destructive">
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-8 text-muted-foreground">
                  <TrendingUp className="h-12 w-12 mx-auto mb-4 opacity-50" />
                  <p>No learning outcomes defined.</p>
                  <p className="text-sm">Add skills students will gain from this course.</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Certificates Section */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">🏆 Certificate Awards</CardTitle>
              <p className="text-sm text-muted-foreground">Certificates students earn upon course completion</p>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <Label>Attached Certificates</Label>
                <Button onClick={() => setShowCertificateSearch(true)} size="sm" className="gap-2">
                  <Plus className="h-4 w-4" />
                  Add Certificate
                </Button>
              </div>

              {state.certificates.certificates.length > 0 ? (
                <div className="space-y-4">
                  {state.certificates.certificates.map((certificate) => (
                    <div key={certificate.id} className="p-4 bg-muted/30 rounded-lg border">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-2">
                            <Award className="h-5 w-5 text-yellow-600" />
                            <h4 className="font-semibold">{certificate.name}</h4>
                          </div>
                          <p className="text-sm text-muted-foreground mb-3">{certificate.description}</p>

                          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
                            <div>
                              <Label className="text-xs">Completion Required</Label>
                              <p className="font-medium">{certificate.requirements.completionPercentage}%</p>
                            </div>
                            {certificate.requirements.passingGrade && (
                              <div>
                                <Label className="text-xs">Passing Grade</Label>
                                <p className="font-medium">{certificate.requirements.passingGrade}%</p>
                              </div>
                            )}
                            <div>
                              <Label className="text-xs">Required Skills</Label>
                              <p className="font-medium">{certificate.requirements.requiredSkills.length} skills</p>
                            </div>
                          </div>
                        </div>
                        <Button variant="ghost" size="sm" onClick={() => removeCertificate(certificate.id)} className="text-destructive hover:text-destructive">
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-8 text-muted-foreground">
                  <Award className="h-12 w-12 mx-auto mb-4 opacity-50" />
                  <p>No certificates attached.</p>
                  <p className="text-sm">Add certificates students can earn by completing this course.</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Student Preview */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">👁️ Student Preview</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="p-6 bg-muted/30 rounded-lg border-2 border-dashed">
                <h3 className="font-semibold mb-2">How students will see this course:</h3>
                <div className="space-y-3 text-sm">
                  <div>
                    <strong>Prerequisites:</strong> {state.certificates.skillsRequired.length > 0 ? state.certificates.skillsRequired.map((s) => s.name).join(', ') : 'None required'}
                  </div>
                  <div>
                    <strong>You&apos;ll Learn:</strong> {state.certificates.skillsProvided.length > 0 ? state.certificates.skillsProvided.map((s) => s.name).join(', ') : 'Learning outcomes not specified'}
                  </div>
                  <div>
                    <strong>Certificates:</strong> {state.certificates.certificates.length > 0 ? `${state.certificates.certificates.length} certificate(s) available` : 'No certificates offered'}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Skill Search Modal */}
      {showSkillSearch && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-background border rounded-lg shadow-lg w-full max-w-2xl max-h-[80vh] overflow-hidden">
            <div className="p-4 border-b">
              <div className="flex items-center justify-between">
                <h3 className="font-semibold">Add {skillSearchMode === 'required' ? 'Required' : 'Provided'} Skill</h3>
                <Button variant="ghost" size="sm" onClick={() => setShowSkillSearch(false)}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
              <div className="flex gap-2 mt-3">
                <div className="flex-1 relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input placeholder="Search skills..." value={skillSearchQuery} onChange={(e) => setSkillSearchQuery(e.target.value)} className="pl-10" />
                </div>
                <Button variant="outline" onClick={() => setShowNewSkillForm(true)} className="gap-2">
                  <Plus className="h-4 w-4" />
                  New
                </Button>
              </div>
            </div>

            {showNewSkillForm && (
              <div className="p-4 border-b bg-muted/30">
                <h4 className="font-medium mb-3">Create New Skill</h4>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <Label className="text-xs">Name</Label>
                    <Input value={newSkill.name} onChange={(e) => setNewSkill({ ...newSkill, name: e.target.value })} placeholder="Skill name" />
                  </div>
                  <div>
                    <Label className="text-xs">Category</Label>
                    <Select value={newSkill.category} onValueChange={(value) => setNewSkill({ ...newSkill, category: value })}>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {SKILL_CATEGORIES.map((cat) => (
                          <SelectItem key={cat} value={cat}>
                            {cat}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <Label className="text-xs">Level</Label>
                    <Select value={newSkill.level} onValueChange={(value: any) => setNewSkill({ ...newSkill, level: value })}>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {SKILL_LEVELS.map((level) => (
                          <SelectItem key={level.value} value={level.value}>
                            {level.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <Label className="text-xs">Description</Label>
                    <Input value={newSkill.description} onChange={(e) => setNewSkill({ ...newSkill, description: e.target.value })} placeholder="Optional description" />
                  </div>
                </div>
                <div className="flex gap-2 mt-3">
                  <Button onClick={handleCreateNewSkill} size="sm">
                    Create Skill
                  </Button>
                  <Button variant="outline" onClick={() => setShowNewSkillForm(false)} size="sm">
                    Cancel
                  </Button>
                </div>
              </div>
            )}

            <div className="p-4 max-h-96 overflow-y-auto">
              <div className="space-y-2">
                {filteredSkills.map((skill) => (
                  <div key={skill.id} className="flex items-center justify-between p-3 border rounded-lg hover:bg-muted/50 cursor-pointer" onClick={() => handleAddSkill(skill, skillSearchMode)}>
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{skill.name}</span>
                        <Badge variant="outline" className={getSkillLevelStyle(skill.level)}>
                          {skill.level}
                        </Badge>
                        <Badge variant="secondary">{skill.category}</Badge>
                      </div>
                      <p className="text-sm text-muted-foreground">{skill.description}</p>
                    </div>
                    <Plus className="h-4 w-4" />
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Certificate Search Modal */}
      {showCertificateSearch && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-background border rounded-lg shadow-lg w-full max-w-2xl max-h-[80vh] overflow-hidden">
            <div className="p-4 border-b">
              <div className="flex items-center justify-between">
                <h3 className="font-semibold">Add Certificate</h3>
                <Button variant="ghost" size="sm" onClick={() => setShowCertificateSearch(false)}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
              <div className="relative mt-3">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input placeholder="Search certificates..." value={certificateSearchQuery} onChange={(e) => setCertificateSearchQuery(e.target.value)} className="pl-10" />
              </div>
            </div>

            <div className="p-4 max-h-96 overflow-y-auto">
              <div className="space-y-3">
                {filteredCertificates.map((cert) => (
                  <div key={cert.id} className="flex items-start justify-between p-4 border rounded-lg hover:bg-muted/50 cursor-pointer" onClick={() => handleAddCertificate(cert)}>
                    <div className="flex gap-3">
                      <Award className="h-8 w-8 text-yellow-600 mt-1" />
                      <div>
                        <h4 className="font-medium">{cert.name}</h4>
                        <p className="text-sm text-muted-foreground">{cert.description}</p>
                        <div className="flex gap-4 mt-2 text-xs text-muted-foreground">
                          <span>Completion: {cert.requirements.completionPercentage}%</span>
                          {cert.requirements.passingGrade && <span>Grade: {cert.requirements.passingGrade}%</span>}
                          <span>Skills: {cert.requirements.requiredSkills.length}</span>
                        </div>
                      </div>
                    </div>
                    <Plus className="h-4 w-4 mt-1" />
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
