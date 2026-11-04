import type { ContentPagesMarketingLead, GeneratedApi, IdentityTenantsUserMembership } from '../../dist/index.js';

type Assert<T extends true> = T;
type IsAny<T> = 0 extends 1 & T ? true : false;

// Compile a public ESM consumer: enrollment imports must not narrow GeneratedApi
// to the raw schema module and drop the DTO/endpoint compatibility exports.
export type SessionUserIdRemainsTyped = Assert<IsAny<GeneratedApi.IdentityAuthenticationAuthSessionStateDto['userId']> extends true ? false : true>;
export type AccessTokenRemainsTyped = Assert<IsAny<GeneratedApi.IdentityAuthenticationSignInOutput['accessToken']> extends true ? false : true>;

// DTO compatibility names must remain usable through both public surfaces.
export type MarketingAliasRemainsTyped = Assert<IsAny<ContentPagesMarketingLead['email']> extends true ? false : true>;
export type MembershipAliasRemainsTyped = Assert<IsAny<IdentityTenantsUserMembership['invitedAt']> extends true ? false : true>;
export type NamespacedAliasRemainsTyped = Assert<IsAny<GeneratedApi.ContentPagesMarketingLead['email']> extends true ? false : true>;
export type RatingBatchRemainsTyped = Assert<IsAny<GeneratedApi.PostApiRatingsMyBatchOutput[string]['userId']> extends true ? false : true>;
export type RatingSummaryBatchRemainsTyped = Assert<
  IsAny<GeneratedApi.PostApiRatingsSummariesBatchOutput[string]['averageRating']> extends true ? false : true
>;
