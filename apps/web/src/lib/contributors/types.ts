// Git stats interface
export interface GitStats {
  username: string;
  additions: number;
  deletions: number;
}

export interface Contributor {
  login: string;
  id: number;
  avatar_url: string;
  html_url: string;
  contributions: number;
  name?: string;
  bio?: string;
  company?: string;
  location?: string;
  email?: string;
  blog?: string;
  twitter_username?: string;
  public_repos?: number;
  public_gists?: number;
  followers?: number;
  following?: number;
  created_at?: string;
  // Additional stats from commits API
  additions?: number;
  deletions?: number;
  total_commits?: number;
}

export interface GitHubCommitStats {
  author: {
    login: string;
  };
  total: number;
  weeks: Array<{
    w: number;
    a: number;
    d: number;
    c: number;
  }>;
}
