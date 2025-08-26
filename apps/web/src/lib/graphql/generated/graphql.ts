/* eslint-disable */
import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
};

export enum DacPermissionLevel {
  ContentType = 'CONTENT_TYPE',
  Resource = 'RESOURCE',
  Tenant = 'TENANT'
}

export type Mutation = {
  __typename?: 'Mutation';
  healthMutation: Scalars['String']['output'];
};

/** The node interface is implemented by entities that have a global unique identifier. */
export type Node = {
  id: Scalars['ID']['output'];
};

export enum PermissionType {
  Accessibility = 'ACCESSIBILITY',
  Advertisement = 'ADVERTISEMENT',
  Affiliate = 'AFFILIATE',
  Analytics = 'ANALYTICS',
  Api = 'API',
  Approve = 'APPROVE',
  Archive = 'ARCHIVE',
  Audit = 'AUDIT',
  Backup = 'BACKUP',
  Ban = 'BAN',
  Banner = 'BANNER',
  Benchmark = 'BENCHMARK',
  Bookmark = 'BOOKMARK',
  Brand = 'BRAND',
  Carousel = 'CAROUSEL',
  Categorize = 'CATEGORIZE',
  Clone = 'CLONE',
  Collection = 'COLLECTION',
  Comment = 'COMMENT',
  Commission = 'COMMISSION',
  Create = 'CREATE',
  CrossReference = 'CROSS_REFERENCE',
  Delete = 'DELETE',
  Distribute = 'DISTRIBUTE',
  Draft = 'DRAFT',
  Edit = 'EDIT',
  Email = 'EMAIL',
  Escalate = 'ESCALATE',
  FactCheck = 'FACT_CHECK',
  Feature = 'FEATURE',
  Feedback = 'FEEDBACK',
  Flag = 'FLAG',
  Follow = 'FOLLOW',
  Guidelines = 'GUIDELINES',
  HardDelete = 'HARD_DELETE',
  Hide = 'HIDE',
  Improvement = 'IMPROVEMENT',
  Legal = 'LEGAL',
  License = 'LICENSE',
  Mention = 'MENTION',
  Metrics = 'METRICS',
  Migrate = 'MIGRATE',
  Monetize = 'MONETIZE',
  Newsletter = 'NEWSLETTER',
  Paywall = 'PAYWALL',
  Performance = 'PERFORMANCE',
  Pin = 'PIN',
  Plagiarism = 'PLAGIARISM',
  Pricing = 'PRICING',
  Proofread = 'PROOFREAD',
  Publish = 'PUBLISH',
  Push = 'PUSH',
  Quarantine = 'QUARANTINE',
  Rate = 'RATE',
  React = 'REACT',
  Read = 'READ',
  Recommend = 'RECOMMEND',
  Reject = 'REJECT',
  Reply = 'REPLY',
  Report = 'REPORT',
  Reschedule = 'RESCHEDULE',
  Restore = 'RESTORE',
  Revenue = 'REVENUE',
  Review = 'REVIEW',
  Rss = 'RSS',
  Schedule = 'SCHEDULE',
  Score = 'SCORE',
  Seo = 'SEO',
  Series = 'SERIES',
  Share = 'SHARE',
  Sms = 'SMS',
  SocialMedia = 'SOCIAL_MEDIA',
  Sponsorship = 'SPONSORSHIP',
  Spotlight = 'SPOTLIGHT',
  Standards = 'STANDARDS',
  StyleGuide = 'STYLE_GUIDE',
  Submit = 'SUBMIT',
  Subscribe = 'SUBSCRIBE',
  Subscription = 'SUBSCRIPTION',
  Suspend = 'SUSPEND',
  Syndicate = 'SYNDICATE',
  Tag = 'TAG',
  Template = 'TEMPLATE',
  Translate = 'TRANSLATE',
  Trending = 'TRENDING',
  Unpublish = 'UNPUBLISH',
  Version = 'VERSION',
  Vote = 'VOTE',
  Warning = 'WARNING',
  Widget = 'WIDGET',
  Withdraw = 'WITHDRAW'
}

export type Query = {
  __typename?: 'Query';
  /** Health check query to ensure GraphQL is working */
  health: Scalars['String']['output'];
  /** Fetches an object given its ID. */
  node?: Maybe<Node>;
  /** Lookup nodes by a list of IDs. */
  nodes: Array<Maybe<Node>>;
};


export type QueryNodeArgs = {
  id: Scalars['ID']['input'];
};


export type QueryNodesArgs = {
  ids: Array<Scalars['ID']['input']>;
};

export type HealthQueryVariables = Exact<{ [key: string]: never; }>;


export type HealthQuery = { __typename?: 'Query', health: string };


export const HealthDocument = {"kind":"Document","definitions":[{"kind":"OperationDefinition","operation":"query","name":{"kind":"Name","value":"Health"},"selectionSet":{"kind":"SelectionSet","selections":[{"kind":"Field","name":{"kind":"Name","value":"health"}}]}}]} as unknown as DocumentNode<HealthQuery, HealthQueryVariables>;