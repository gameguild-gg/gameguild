export interface MemberProfileSummary {
  username: string;
  displayName: string;
  initials: string;
  joinDate: Date;
  bio?: string;
}

export interface MemberActivity {
  action: string;
  item: string;
  time: string;
  type: string;
}

export interface MemberSkill {
  name: string;
  level: number;
}

export interface MemberProjectSummary {
  name: string;
  description?: string;
  tech: string;
  rating: number;
  featured?: boolean;
}
