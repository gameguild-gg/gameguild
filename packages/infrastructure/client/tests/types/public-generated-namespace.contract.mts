import type { GeneratedApi } from '../../dist/index.js';

type Assert<T extends true> = T;
type IsAny<T> = 0 extends (1 & T) ? true : false;

// Compile a public ESM consumer: enrollment imports must not narrow GeneratedApi
// to the raw schema module and drop the DTO/endpoint compatibility exports.
export type SessionUserIdRemainsTyped = Assert<
  IsAny<GeneratedApi.IdentityAuthenticationAuthSessionStateDto['userId']> extends true ? false : true
>;
export type AccessTokenRemainsTyped = Assert<
  IsAny<GeneratedApi.IdentityAuthenticationSignInOutput['accessToken']> extends true ? false : true
>;
