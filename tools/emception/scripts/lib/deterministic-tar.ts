export interface TarEntry {
  readonly path: string;
  readonly data: Uint8Array;
  readonly executable?: boolean;
}

const TAR_BLOCK_SIZE = 512;

export function createDeterministicTar(entries: readonly TarEntry[]): Buffer {
  const blocks: Buffer[] = [];
  for (const entry of entries) {
    const name = entry.path.replace(/^\//, '');
    const header = Buffer.alloc(TAR_BLOCK_SIZE);
    header.write(name.slice(0, 100), 0, 100, 'utf8');
    header.write(`${entry.executable ? '0000755' : '0000644'}\0`, 100, 8, 'utf8');
    header.write('0000000\0', 108, 8, 'utf8');
    header.write('0000000\0', 116, 8, 'utf8');
    header.write(`${entry.data.length.toString(8).padStart(11, '0')}\0`, 124, 12, 'utf8');
    header.write('00000000000\0', 136, 12, 'utf8');
    header[156] = 48;
    header.write('ustar\0', 257, 6, 'utf8');
    header.write('00', 263, 2, 'utf8');
    header.fill(0x20, 148, 156);
    let checksum = 0;
    for (const byte of header) checksum += byte;
    header.write(`${checksum.toString(8).padStart(6, '0')}\0 `, 148, 8, 'utf8');
    blocks.push(header);

    const dataBlock = Buffer.alloc(Math.ceil(entry.data.length / TAR_BLOCK_SIZE) * TAR_BLOCK_SIZE);
    dataBlock.set(entry.data);
    blocks.push(dataBlock);
  }
  blocks.push(Buffer.alloc(TAR_BLOCK_SIZE * 2));
  return Buffer.concat(blocks);
}
