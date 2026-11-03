import type { GeneratedApi } from '../../dist/index.cjs';

type Assert<T extends true> = T;
type IsAny<T> = 0 extends (1 & T) ? true : false;

// Cover the independently emitted CommonJS declarations as well as ESM.
export type SessionUserIdRemainsTyped = Assert<
  IsAny<GeneratedApi.IdentityAuthenticationAuthSessionStateDto['userId']> extends true ? false : true
>;
export type AccessTokenRemainsTyped = Assert<
  IsAny<GeneratedApi.IdentityAuthenticationSignInOutput['accessToken']> extends true ? false : true
>;
