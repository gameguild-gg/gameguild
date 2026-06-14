declare module '*.wasm' {
  const wasmUrl: string;
  export default wasmUrl;
}

declare module '*.tar' {
  const tarUrl: string;
  export default tarUrl;
}
