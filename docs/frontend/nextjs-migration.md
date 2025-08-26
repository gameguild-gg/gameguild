# Next.js 15 Migration Notes

Consolidated summary.

## Params Promise

`params` is a Promise; unwrap with `React.use()`.

## Type Definition Update

Shared types updated to reflect promise-based params.

## Action Items

1. Use `const { slug } = React.use(params);`
2. Avoid synchronous property access on `params`.
