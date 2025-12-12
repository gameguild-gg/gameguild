export const formatChainId = (chainId: string | number | bigint): string => {
  if (typeof chainId === 'bigint') return `0x${chainId.toString(16)}`;

  if (typeof chainId === 'number') return `0x${chainId.toString(16)}`;

  return chainId.startsWith('0x') ? chainId : `0x${parseInt(chainId, 10).toString(16)}`;
};
