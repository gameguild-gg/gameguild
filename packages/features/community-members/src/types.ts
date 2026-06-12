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
  id?: string;
  name: string;
  title?: string;
  description?: string;
  tech: string;
  rating: number;
  featured?: boolean;
  url?: string;
  imageUrl?: string;
  isPinned?: boolean;
  slug?: string;
}

export interface MemberProfileStats {
  followers?: number;
  following?: number;
  posts?: number;
  projects?: number;
}
