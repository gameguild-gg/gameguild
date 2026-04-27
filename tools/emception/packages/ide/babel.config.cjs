module.exports = {
    presets: [
        ['@babel/preset-typescript', { allExtensions: true }],
        ['@babel/preset-env', { targets: { node: 'current' }, modules: 'commonjs' }],
    ],
};
