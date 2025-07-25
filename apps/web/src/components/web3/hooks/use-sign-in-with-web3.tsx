'use client';

import { useCallback, useEffect } from 'react';
import { useWeb3 } from '@/components/web3/hooks/use-web3';
import { useConnectToWallet } from '@/components/web3/hooks/use-connect-to-wallet';

export enum Web3ProviderChoice {
  METAMASK = 'METAMASK',
  TORUS = 'TORUS',
}

export function useSignInWithWeb3(choice: Web3ProviderChoice) {
  const { state } = useWeb3();
  const [connectToWallet] = useConnectToWallet();

  const signIn = useCallback(async () => {
    if (!state.provider) {
      await connectToWallet();
    }
  }, [state.provider, connectToWallet]);

  useEffect(() => {
    const tryToSignInTorus = async () => {};

    const tryToSignInMetamask = async () => {
      if (state.provider && state.accountAddress) {
        try {
          // TODO: validate the chain id.
          const chain = await state.provider.getNetwork();

          const response = await postAuthWeb3Challenge({
            body: {
              walletAddress: state.accountAddress,
              chainId: chain.chainId.toString(),
            },
          });

          if (!response.data) {
            console.error('Error while trying to sign in with web3: no response data');
            return;
          }

          const message = response.data.challenge;
          if (!message) {
            console.error('Error while trying to sign in with web3: no challenge message');
            return;
          }

          // Eip1193Provider.
          const signature = await state.provider.send('personal_sign', [message, state.accountAddress]);

          await signInWithWeb3(signature, state.accountAddress);

          // todo: move this elsewhere!!
          // the await on the signInAndRedirectIfSucceed on metamask-sign-in-button.tsx is not working
          const session = await getSession();
          if (session) {
            window.location.href = '/chess';
          }
        } catch (error) {
          console.error('Error while trying to sign in with web3:', error);
        }
      }
    };
    if (choice === Web3ProviderChoice.METAMASK) tryToSignInMetamask();
    else tryToSignInTorus();
  }, [state, choice]);

  return [signIn];
}
