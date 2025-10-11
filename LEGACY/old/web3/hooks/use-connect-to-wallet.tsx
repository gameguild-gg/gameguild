import { useWeb3 } from '@/components/web3/hooks/use-web3';

export function useConnectToWallet() {
  const { connect } = useWeb3();

  const connectToWallet = async () => {
    await connect();
  };

  return [connectToWallet];
}
