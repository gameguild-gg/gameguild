export {};

declare let __webpack_public_path__: string;

declare global {
  interface Window {
    emception: typeof emception;
    Comlink: typeof Comlink;
  }
}

declare module '*.worker.ts' {
  const WorkerFactory: {
    new (): Worker;
  };
  export default WorkerFactory;
}
