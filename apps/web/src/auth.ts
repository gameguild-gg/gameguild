import {
  GameGuildAuth,
  CredentialsProvider,
} from '@game-guild/client';

export const { handlers, auth, signIn, signOut, signUp, update } =
  GameGuildAuth({
    providers: [CredentialsProvider()],
    debug: process.env.NODE_ENV === 'development',
  });
