# [3.11.0](https://github.com/gameguild-gg/gameguild/compare/v3.10.0...v3.11.0) (2026-07-09)

### BREAKING CHANGES

* **emception/core:** unify EmceptionAPI surfaces — add EmbedderEmceptionAPI ([7276626](https://github.com/gameguild-gg/gameguild/commit/72766261980ab51b0246c63216ca9f9dcd0a65c7))
* **emception/core:** discriminated WorkspaceBuildConfig union (semver-major) ([15a1438](https://github.com/gameguild-gg/gameguild/commit/15a143819e8a6cc8eb98ba5a28ffd965fc7807a9))

### Features

* **emception:** add language-flavor preset suffixes, latest C standards, C terminal preset ([abb5f0b](https://github.com/gameguild-gg/gameguild/commit/abb5f0b48ce17d2e98f7c6b38af336ce9e45e829))
* **emception:** replace EmbedderEmceptionAPI with full EmceptionAPI surface ([36740ab](https://github.com/gameguild-gg/gameguild/commit/36740ab0c7ae54ed3da466477266a159c060e9d8))
* **emception/core:** unify EmceptionAPI surfaces — add EmbedderEmceptionAPI ([7276626](https://github.com/gameguild-gg/gameguild/commit/72766261980ab51b0246c63216ca9f9dcd0a65c7))
* **emception/core:** discriminated WorkspaceBuildConfig union (semver-major) ([15a1438](https://github.com/gameguild-gg/gameguild/commit/15a143819e8a6cc8eb98ba5a28ffd965fc7807a9))

### Bug Fixes

* **emception/core:** make layout optional, harden parseWorkspaceBundle, remove full preset ([1a29cc8](https://github.com/gameguild-gg/gameguild/commit/1a29cc8d6a0327bcc0ea4009753b1f5beab37f93))

# [3.10.0](https://github.com/gameguild-gg/gameguild/compare/v3.9.0...v3.10.0) (2026-05-25)

### Features

* **block-content-editor:** math-live with latex for quiz with formula ([018f36c](https://github.com/gameguild-gg/gameguild/commit/018f36c787d7a238c17a751e41155bfcc03d82b0))
* **block-content-editor:** lexical top menu always visible ([1cd496f](https://github.com/gameguild-gg/gameguild/commit/1cd496f70ae834d29604498a411fcce3db0d4471))
* **block-content-editor:** monaco preferences for vega node ([7c71835](https://github.com/gameguild-gg/gameguild/commit/7c71835048b7cc59324a7424159dfcf7d91e9fc8))
* **block-content-editor:** monaco preferences for studio node ([cd52a61](https://github.com/gameguild-gg/gameguild/commit/cd52a610ab71f49860b54a5e3c329b74d152fdd5))
* **block-content-editor:** monaco preferences for html node ([bc0f711](https://github.com/gameguild-gg/gameguild/commit/bc0f711024923265fe7d1147f699206cfd8f1eed))
* **block-content-editor:** monaco preferences for markdown node ([7b29bbd](https://github.com/gameguild-gg/gameguild/commit/7b29bbd5854a70fda9c2e7e92d66423f9729318c))
* **block-content-editor:** monaco preferences for mermaid node ([e8b259e](https://github.com/gameguild-gg/gameguild/commit/e8b259e949a50a10e0646a54dd7d4a75b8cf96ee))
* **block-content-editor:** monaco global and preview preferences ([1751f10](https://github.com/gameguild-gg/gameguild/commit/1751f10fbf9ce09e5ee557ca257d35b72bfcb6da))
* **block-content-editor:** rectangle style for monaco ([4235806](https://github.com/gameguild-gg/gameguild/commit/423580620cffbceb67a12a14f1e5df0076285210))
* **block-content-editor:** shiki and settings menu for mermaid node ([83c5326](https://github.com/gameguild-gg/gameguild/commit/83c5326586f2725f93557d98301c833a4590acd7))
* **block-content-editor:** shiki and settings menu for code-studio node ([282c023](https://github.com/gameguild-gg/gameguild/commit/282c0233ac37a9c0862d7f56d91fc27cca291092))
* **block-content-editor:** shiki and settings menu for markdown node ([46da15d](https://github.com/gameguild-gg/gameguild/commit/46da15d5de987769bde2c472ac8a17b4513b8f3a))
* **block-content-editor:** shiki and settings menu for rich, html node ([bb4ff55](https://github.com/gameguild-gg/gameguild/commit/bb4ff550794ce2e4358c224fd537231144978489))
* **block-content-editor:** shiki and settings menu for vega node ([5b4220c](https://github.com/gameguild-gg/gameguild/commit/5b4220cfd5d584c3b621a351c06978ea0f89a4f2))
* **block-content-editor:** settings menu for quiz, media and othernodes ([072e196](https://github.com/gameguild-gg/gameguild/commit/072e196767d8aca6ea38bd95df51b20d0cf6715a))
* **block-content-editor:** block editor unified for all nodes ([b57ef5f](https://github.com/gameguild-gg/gameguild/commit/b57ef5fbe0120b44ac6aa8c05267e6d5d5de6f99))
* **block-content-editor:** resible editor in preview mode ([bb16fcb](https://github.com/gameguild-gg/gameguild/commit/bb16fcb7214697971bdea17ed97ef5a28dc14ec4))
* **block-content-editor:** code studio node editor redefined experience ([14b251f](https://github.com/gameguild-gg/gameguild/commit/14b251f092d0876e742f354065aa8b95e071b120))

### Bug Fixes

* **block-content-editor:** lexical lists ([995923b](https://github.com/gameguild-gg/gameguild/commit/995923be5dafa74f9749759db99783fc55589d73))
* **block-content-editor:** quiz preview button equal height ([dbb79fc](https://github.com/gameguild-gg/gameguild/commit/dbb79fccd874f82c5e8e4236f0caefd5282e3c6c))
* **block-content-editor:** code-studio does not open on its own. ([fc122f5](https://github.com/gameguild-gg/gameguild/commit/fc122f5249a011593b46c54a54c2e089844dda0f))
* **block-content-editor:** race condition over blocks ([ba6bde8](https://github.com/gameguild-gg/gameguild/commit/ba6bde8d9cd3cd3acdf945f7b6c21435171671bd))
* **block-content-editor:** can't access property "domNode" ([4546262](https://github.com/gameguild-gg/gameguild/commit/4546262cc2da7024b0f771cc043d2d0292189870))
* **block-content-editor:** monaco overflowingContentWidgets ([66291cf](https://github.com/gameguild-gg/gameguild/commit/66291cfb0719089f2c26c3c8b2b750d5e395d923))
* **block-content-editor:** tailwind classes fix ([8afdfd3](https://github.com/gameguild-gg/gameguild/commit/8afdfd36f28fc2d4a6ac7ca47a0f29b7f8b2af1a))
* **block-content-editor:** focus folder only opens in focus editor ([8a5f74a](https://github.com/gameguild-gg/gameguild/commit/8a5f74a507bc200a03c8414b3a43ab5261f5e024))
* **block-content-editor:** focus editor only multiple form ([a46e748](https://github.com/gameguild-gg/gameguild/commit/a46e7482bf0a4cd0b516c7d417e27387921e9fef))
* **block-content-editor:** only one full and focus editor for display ([c2e8054](https://github.com/gameguild-gg/gameguild/commit/c2e8054b3e79f79544e4ecdc9c0a725ef1753b13))
* **block-content-editor:** focus and full editor opens files ([f7f033d](https://github.com/gameguild-gg/gameguild/commit/f7f033d3b7a73813dd424e9b48221708e3b2df73))
* **block-content-editor:** delete dialog for remove displays in studio ([43e92be](https://github.com/gameguild-gg/gameguild/commit/43e92bed8d62523bb64fc6e4e120944497d51b67))

# [3.9.0](https://github.com/gameguild-gg/gameguild/compare/v3.8.0...v3.9.0) (2026-05-20)

### Features

* **block-content-editor:** static-viewer Direct Folder and file Section ([f98aa1e](https://github.com/gameguild-gg/gameguild/commit/f98aa1e4c8dc7e0dda955c5563c96f63ebbd5d63))

### Bug Fixes

* **block-content-editor:** scroll bar involuntary ([b2bfba8](https://github.com/gameguild-gg/gameguild/commit/b2bfba834282bd74c4dbefb35fcd22a67a2c5c10))
* **block-content-editor:** import project from folder ([9b8932e](https://github.com/gameguild-gg/gameguild/commit/9b8932ef326705fa64e10f0e8eab4364ff80ffc4))

# [3.8.0](https://github.com/gameguild-gg/gameguild/compare/v3.7.3...v3.8.0) (2026-05-19)

### Features

* courses-v2 ([52fd948](https://github.com/gameguild-gg/gameguild/commit/52fd948e9a0073ce23ca4f78e76ef9b1de9f9d79))

# [3.7.3](https://github.com/gameguild-gg/gameguild/compare/v3.7.2...v3.7.3) (2026-05-18)

### Bug Fixes

* **emception:** enforce pinned python version in manifest ([6b0121d](https://github.com/gameguild-gg/gameguild/commit/6b0121db426d352b1e9023b82713820d30c6e162))
* **emception:** presets file organization ([1fa26a0](https://github.com/gameguild-gg/gameguild/commit/1fa26a095122fffe3a8f3a71249c996abaecda66))

# [3.7.2](https://github.com/gameguild-gg/gameguild/compare/v3.7.1...v3.7.2) (2026-05-17)

### Bug Fixes

* abort ([5c1541a](https://github.com/gameguild-gg/gameguild/commit/5c1541a5b4b3f9eb2ba631ab20b103efe72673d0))

# [3.7.1](https://github.com/gameguild-gg/gameguild/compare/v3.7.0...v3.7.1) (2026-05-17)

### Bug Fixes

* **emception:** now opengl demo works with sdl ([36d520b](https://github.com/gameguild-gg/gameguild/commit/36d520b4a44f68303c1c167205d11e029e153e44))

# [3.7.0](https://github.com/gameguild-gg/gameguild/compare/v3.6.0...v3.7.0) (2026-05-17)

### Features

* **emception:** add SDL3+OpenGL ES 3 preset, demo, and E2E test ([8e978ac](https://github.com/gameguild-gg/gameguild/commit/8e978acde504b36e8acd52d4b841f22a36824faa))

# [3.6.0](https://github.com/gameguild-gg/gameguild/compare/v3.5.2...v3.6.0) (2026-05-16)

### Features

* **emception:** Add Dear ImGui support with SDL3 demo ([fe437f7](https://github.com/gameguild-gg/gameguild/commit/fe437f7f9f4149d9eb7cc99b975331039b6c55c3))
* **emception:** add Allegro 5 build, preset, and e2e test ([b41f43f](https://github.com/gameguild-gg/gameguild/commit/b41f43fc395a66fdf958b1b310d4eb9b7d50c67a))

### Bug Fixes

* **emception:** make all graphis libs interactible ([deb3c7f](https://github.com/gameguild-gg/gameguild/commit/deb3c7fa92a578effd7a1afe9c878938ece7fe84))
* make sdl canvas interactible ([513f08a](https://github.com/gameguild-gg/gameguild/commit/513f08a953ec304a7f02df8baa638a82b9425cca))
* **emception:** pre-warm manifest-symlink .a paths for lld ([2fc5621](https://github.com/gameguild-gg/gameguild/commit/2fc5621efee60839ff8b9864f8f1ba96061dd756))
* **emception:** stabilize canvas presets and app dev sync ([a497175](https://github.com/gameguild-gg/gameguild/commit/a497175f47c5ec5f0fe85cbe04629f891283dbc6))
* **emception:** remove redundant SDL2 headers from allegro bundle and sysroot ([9771ff3](https://github.com/gameguild-gg/gameguild/commit/9771ff349af71c1bb1d6f2e94d965b55bad1d3c9))

# [3.5.2](https://github.com/gameguild-gg/gameguild/compare/v3.5.1...v3.5.2) (2026-05-13)

### Bug Fixes

* **ci:** skip prepack on npm publish — dist+cdn already staged by build job ([b17da38](https://github.com/gameguild-gg/gameguild/commit/b17da384c6d650abfba1f8d84054d1347fa116e5))

# [3.5.1](https://github.com/gameguild-gg/gameguild/compare/v3.5.0...v3.5.1) (2026-05-13)

### Bug Fixes

* **emception:** P0'+P4+P5 — kill stale CDN fallback, LGWIN=24, sort tar entries ([87b5230](https://github.com/gameguild-gg/gameguild/commit/87b5230ee3fec513fe66c04ce502fb01e7da92be))

# [3.5.0](https://github.com/gameguild-gg/gameguild/compare/v3.4.0...v3.5.0) (2026-05-12)

### Features

* **emception:** shrink npm tarball below 200 MB ([0d1f6cd](https://github.com/gameguild-gg/gameguild/commit/0d1f6cd1222052f013442bd5781928ba1062a37c))

# [3.4.0](https://github.com/gameguild-gg/gameguild/compare/v3.3.8...v3.4.0) (2026-05-12)

### Features

* gglexical to block-content-editor ([ca9878c](https://github.com/gameguild-gg/gameguild/commit/ca9878c09c82d248d7e180a0635a393ac07b436d))
* **block-editor:** block-editor page ([a0793dc](https://github.com/gameguild-gg/gameguild/commit/a0793dc76129bab70c2e35a7d0a0201583915c4d))

### Bug Fixes

* metadata ([d6e18c8](https://github.com/gameguild-gg/gameguild/commit/d6e18c89b48f62d8feacb03e99c2433a13c6fdc0))
* editor to block-content-editor ([95a0ead](https://github.com/gameguild-gg/gameguild/commit/95a0ead72360d8a52f1a2fe5c08de25ddb6c36de))

# [3.3.8](https://github.com/gameguild-gg/gameguild/compare/v3.3.7...v3.3.8) (2026-05-12)

### Bug Fixes

* ci ([950cac4](https://github.com/gameguild-gg/gameguild/commit/950cac46c3db00a988ebe1ef742330b0c2ee6295))

# [3.3.7](https://github.com/gameguild-gg/gameguild/compare/v3.3.6...v3.3.7) (2026-05-11)

### Bug Fixes

* **emception:** fall back core CDN staging ([295fd36](https://github.com/gameguild-gg/gameguild/commit/295fd363738fba384a6fd1da14da5dd1e95dacea))

# [3.3.6](https://github.com/gameguild-gg/gameguild/compare/v3.3.5...v3.3.6) (2026-05-10)

### Bug Fixes

* **emception:** publish runtime assets from packages ([d2e8446](https://github.com/gameguild-gg/gameguild/commit/d2e8446be25e8d4fa2d80ab6bc8ffd6a87099d54))

# [3.3.5](https://github.com/gameguild-gg/gameguild/compare/v3.3.4...v3.3.5) (2026-05-10)

### Bug Fixes

* ci ([d96f331](https://github.com/gameguild-gg/gameguild/commit/d96f331c02ab5779787907724708918486c2410e))

# [3.3.4](https://github.com/gameguild-gg/gameguild/compare/v3.3.3...v3.3.4) (2026-05-09)

### Bug Fixes

* ci ([bf8ee7c](https://github.com/gameguild-gg/gameguild/commit/bf8ee7c150524ca53056aed73c394eefda8e25d5))

# [3.3.3](https://github.com/gameguild-gg/gameguild/compare/v3.3.2...v3.3.3) (2026-05-09)

### Bug Fixes

* ci ([e9cf1fd](https://github.com/gameguild-gg/gameguild/commit/e9cf1fd2d240dccfdbcb09d786eb71f446a66ed6))

# [3.3.2](https://github.com/gameguild-gg/gameguild/compare/v3.3.1...v3.3.2) (2026-05-09)

### Bug Fixes

* ci ([a53916f](https://github.com/gameguild-gg/gameguild/commit/a53916f7e8d61c7b5edc0b934fc8e0c42ffa5fb1))

# [3.3.1](https://github.com/gameguild-gg/gameguild/compare/v3.3.0...v3.3.1) (2026-05-09)

### Bug Fixes

* ci ([d44aa0c](https://github.com/gameguild-gg/gameguild/commit/d44aa0c1adf849e11e6ab4377a82369fba6183ab))

# [3.3.0](https://github.com/gameguild-gg/gameguild/compare/v3.2.1...v3.3.0) (2026-05-09)

### Features

* **ci:** skip unchanged api/web/emception builds ([1179df5](https://github.com/gameguild-gg/gameguild/commit/1179df5fa8176bccc439059fd2cb3b3f8b5f91b5))
* **emception:** pin all tool versions + fix emsdk parallel race ([3468fde](https://github.com/gameguild-gg/gameguild/commit/3468fde08b8020f1626697d0c5475d03fbb99d44))
* **emception:** Build Raylib with WebGL2 support ([fcd18e8](https://github.com/gameguild-gg/gameguild/commit/fcd18e85fb26e6ee39fe81abfe00b3a265dca24f))
* **emception:** add raylib canvas support with GLFW/WebGL patching ([56ad56c](https://github.com/gameguild-gg/gameguild/commit/56ad56c328847871767a9f52337a42e8c910586d))

### Bug Fixes

* ci ([aaaf683](https://github.com/gameguild-gg/gameguild/commit/aaaf68331cf3ff11fb37cf08d7a5972fc7242ea1))
* ci ([17b57b3](https://github.com/gameguild-gg/gameguild/commit/17b57b3c837f860d1c74a8fc2c910c95aa4abd0b))
* **emception:** raylib ([9d631c6](https://github.com/gameguild-gg/gameguild/commit/9d631c628fecfb0ff5bc427066c79fe4ac1361ba))
* reame packages ([f103d39](https://github.com/gameguild-gg/gameguild/commit/f103d394738529ca7a8a385cc2daab80f6fec1a0))
* ci ([2b6194a](https://github.com/gameguild-gg/gameguild/commit/2b6194af38c73706433c7bcd988ca68178cbf0c1))
* **release:** exclude .next build artifacts from release assets ([7f17e50](https://github.com/gameguild-gg/gameguild/commit/7f17e50663d1d0cd53c5451b88c7d957f0c03b9b))
* **ci:** enable npm trusted publishing without token ([e8bb888](https://github.com/gameguild-gg/gameguild/commit/e8bb888bea021fd6c1ee2fee4311a51298844474))
* **ci:** narrow emception artifact cache hash inputs ([15dbb28](https://github.com/gameguild-gg/gameguild/commit/15dbb28000dd9fb06d55f3dddcdf1b8911998b94))
* **ci:** build ide deps when full emception build is skipped ([71ed226](https://github.com/gameguild-gg/gameguild/commit/71ed22623a5161206330b7031a1f1c64a7b33aa0))
* **ci:** restore emception artifact from flattened layout ([2539ac4](https://github.com/gameguild-gg/gameguild/commit/2539ac432c4bde47160f72b5d68131088694adee))
* **emception:** fix cmake 3.31.x WASM build ([6aed533](https://github.com/gameguild-gg/gameguild/commit/6aed533179106977df4104967258217cf37fd3fb))
* ci ([5ca969d](https://github.com/gameguild-gg/gameguild/commit/5ca969dfc4fc024fda1d793fecf23ab92e9b3564))
* ci ([7772998](https://github.com/gameguild-gg/gameguild/commit/7772998045cdce2fdd9bc112d62fc4fdd8d81b18))
* ci ([1b74a04](https://github.com/gameguild-gg/gameguild/commit/1b74a049982d6c3fbbc4f83c25b6dd9e64f80157))
* ci ([e1ab55c](https://github.com/gameguild-gg/gameguild/commit/e1ab55ceaee7b728e4388a78eea0192044908a5c))
* **dotnet-wasm:** upgrade from .NET 8 to .NET 9 ([3d1a109](https://github.com/gameguild-gg/gameguild/commit/3d1a1098b20d55dec9106292e99b0581d3de35e2))
* **emception:** hoist @vitejs/plugin-react for ide-react build ([d1f664e](https://github.com/gameguild-gg/gameguild/commit/d1f664e7b73b918d1e1172aeda8e9ef66fcedc13))
* **ci:** build @emception/ide before ide-react ([522778f](https://github.com/gameguild-gg/gameguild/commit/522778f2d60a8a46474f42bb83a7ce4cbb027d71))
* **emception:** prebuild package deps before typecheck ([e96573e](https://github.com/gameguild-gg/gameguild/commit/e96573e929615159c1b5cb040d807da94a15d2fd))
* **emception:** resolve workspace package types in CI ([88746da](https://github.com/gameguild-gg/gameguild/commit/88746dab6ef6bc0cffadc94862e46e3834fb25b4))
* **dotnet-wasm:** auto-fallback to local sdk for wasm-tools ([967fe9c](https://github.com/gameguild-gg/gameguild/commit/967fe9cbd2932ae5f047e2ff934e511353b830ad))
* keep dotnet available after cleanup ([fe68287](https://github.com/gameguild-gg/gameguild/commit/fe68287726024bbfcfbd45c942cd4753f02844c9))
* add GITHUB_TOKEN job env and dotnet PATH for CI ([54d70d5](https://github.com/gameguild-gg/gameguild/commit/54d70d5a666aa3cb00bf7215c900eb9ae6045b3b))
* ci ([de8f814](https://github.com/gameguild-gg/gameguild/commit/de8f8146759d89153c11df6eeb6069997f2b2654))
* ci ([d34a1d4](https://github.com/gameguild-gg/gameguild/commit/d34a1d429f7c2dee106b4af0a40fbd0341a77805))
* ci ([1277894](https://github.com/gameguild-gg/gameguild/commit/1277894185579d2a38a2b7f9595bc0296c490cab))
* ci ([af09193](https://github.com/gameguild-gg/gameguild/commit/af0919317e0e09e7baeaa80e1f05d045eb46f74c))
* ci ([a58f99d](https://github.com/gameguild-gg/gameguild/commit/a58f99d62499d210cfa230323dbb529c2e181cdd))
* ci ([bc458cd](https://github.com/gameguild-gg/gameguild/commit/bc458cde2ae7f3e936ff46903c0bd8ad09a66391))
* ci ([2b3396c](https://github.com/gameguild-gg/gameguild/commit/2b3396cfcac82a53d93d4c2116fe442d5c2dbb84))
* ci ([c994694](https://github.com/gameguild-gg/gameguild/commit/c9946940a0b47f56131c55ebfce089e4a4e3bd0d))
* ci ([8d5bae3](https://github.com/gameguild-gg/gameguild/commit/8d5bae38c11eb0a7096ef5fc092d5f0228872291))
* ci update the node version ([bd22554](https://github.com/gameguild-gg/gameguild/commit/bd225544be4ce5b8856f83957cf29fa9177ce496))
* ci ([3388acc](https://github.com/gameguild-gg/gameguild/commit/3388accb5dd8251fe891acff9361784cef7a32f4))
* ci on act ([babc160](https://github.com/gameguild-gg/gameguild/commit/babc1609e089c40afa9f6217f02da4f83b4fae35))
* ci again ([fe90271](https://github.com/gameguild-gg/gameguild/commit/fe902712b148065fe5b37f8233acb51c17479f10))
* **emception:** trying to fix ci/cd ([d6edd53](https://github.com/gameguild-gg/gameguild/commit/d6edd53f93e2e72af47de03dc8381ca6da79d8c1))
* **emception:** add raylib to the ci/cd ([5b93f58](https://github.com/gameguild-gg/gameguild/commit/5b93f58630d2cf4ed803b520c444436a334a2a2f))
* **emception:** clean ([410af09](https://github.com/gameguild-gg/gameguild/commit/410af09571108c942fd478e4496531d0312983fb))
* **emception:** raylib workspace almost done ([212d11c](https://github.com/gameguild-gg/gameguild/commit/212d11c09aa9c3887517963e695205288e77f597))
* **emception/raylib:** remove undeclared emscripten_notify_memory_growth from stub ([2e40855](https://github.com/gameguild-gg/gameguild/commit/2e40855a72d15eab6610cce1c655f4c07dbc702b))
* **emception:** clean ninja/cmake/imgui/brotli userland dirs on npm run clean ([284485b](https://github.com/gameguild-gg/gameguild/commit/284485b83da0777b59f6467c3aa01f25f8d2d089))
* **emception/raylib:** generate raylib-runtime.mjs in build script + use VFS ([dcf6086](https://github.com/gameguild-gg/gameguild/commit/dcf608657d5ada2b6bb9629b68f392aed3dd7a48))
* **emception:** remove cli ([beea559](https://github.com/gameguild-gg/gameguild/commit/beea5592c562093d6b02f857ee9b64e6968d9ea3))
* **emception:** presets minor improvement ([737a424](https://github.com/gameguild-gg/gameguild/commit/737a4246e6fa0f0de7292ac679f9d2a4c6e9a32b))

# [3.2.1](https://github.com/gameguild-gg/gameguild/compare/v3.2.0...v3.2.1) (2026-04-28)

### Bug Fixes

* **emception:** use * for internal workspace deps ([288df31](https://github.com/gameguild-gg/gameguild/commit/288df31731761283df60f670a45428dc24502340))

# [3.2.0](https://github.com/gameguild-gg/gameguild/compare/v3.1.1...v3.2.0) (2026-04-28)

### Features

* **meta:** Phase 9.2 + 9.5 — bin shim + cookbook README  Phase 9.2 — npx emception doctor via meta-package bin: - Add bin/emception.mjs: pass-through shim that resolves and imports   @emception/cli/dist/bin/emception.js. Graceful error when @emception/cli   is not installed (optional peer dep). - Add 'bin': { 'emception': './bin/emception.mjs' } to package.json. - Add 'bin/' to the files array. - Add '@emception/cli' as an optional peerDependency so consumers can   opt into CLI tools without pulling the dep unconditionally.  Phase 9.5 — meta-package README cookbook section: - New 'Cookbook' section between 'Which package' and 'Project Goals'. - Recipes: grade assignment (browser), grade assignment (Node CI),   SDL canvas demo, reactive IDE in tutorial site,   diagnose + mirror sysroot to CDN. ([b35e7d9](https://github.com/gameguild-gg/gameguild/commit/b35e7d96f2c7c3041cb85eabaf0bbca4eca2bffe))
* **ide:** Phase 8 — reactive IdeProps, fullscreen portal, custom element, Jest infra  ## What's new  ### ide-types.ts - Add `InjectedEmceptionAPI` interface (run / readFile / writeFile / listDir / resetVfs / dispose) - Add full `IdeProps` interface with panel toggles, fullscreen, visibility,   canvas path, headless I/O and style props - Add `deriveStorageKey(workspaceName?)` — falls back to legacy key so   existing localStorage data is not lost - Export `SDL_CANVAS_PATH` constant - Keep `WORKSPACE_STORAGE_KEY` as a deprecated legacy export  ### Ide.tsx - Destructure all IdeProps with sensible defaults - Derive localStorage key from `workspaceName` prop via `deriveStorageKey` - Gate file-explorer sidebar on `enableFileExplorer` prop - Gate terminal row on `enableTerminal` prop - Gate canvas panel on `enableCanvas` prop - Filter hidden / solution files via `showHiddenFiles` / `showSolutionFiles` - Use `canvasPath` prop instead of the hard-coded constant - Guard workspace storage effects on `enableWorkspace` prop - Import `bootInWorker` from `@emception/browser` - Wrap render output in a `createPortal(…, document.body)` when   `fullscreen` is true (SSR-safe: guard on `typeof document !== 'undefined'`)  ### emception-ide.ts (new) - `<emception-ide>` light-DOM custom element wrapping the React `<Ide>` - Maps HTML attributes → IdeProps (boolean defaults, string attrs) - JS-only props: workspaceConfig, api, onStdout, onStderr, stdin, onFullscreenChange - `update()` public method for programmatic prop changes without attributes - `registerEmceptionIde()` — safe multi-call registration helper  ### index.ts - Re-export `IdeProps`, `InjectedEmceptionAPI` types - Re-export `ELEMENT_NAME`, `EmceptionIdeElement`, `registerEmceptionIde` - Re-export `EmceptionAPI` from `@emception/core`  ### Jest infrastructure (new) - `packages/ide/jest.config.mjs` — Babel transform for .ts/.tsx, node env - `packages/ide/babel.config.cjs` — preset-typescript + preset-env - `packages/ide/src/__mocks__/style.cjs` — CSS import stub - `test:packages:ide` added to root `test:packages` run-s chain  ### Tests (new) - `ide-types.test.ts` — 9 tests covering WORKSPACE_STORAGE_KEY,   SDL_CANVAS_PATH and all deriveStorageKey branches - Existing `ide-utils.test.ts` (57 tests) now runs cleanly with the new   Babel config (previously failed with TS parse errors)  Total: 66 green tests in @emception/ide ([2767f47](https://github.com/gameguild-gg/gameguild/commit/2767f471e636efd5f09c4c5ffbf7e3be0d806b1f))
* **core/runtime:** WorkerOrchestrator + 20 tests (Phase 7.2 prep #3) ([ad77258](https://github.com/gameguild-gg/gameguild/commit/ad77258c3c3d35dabd349ba4a459e6c77585fe25))
* **emception/core:** extract RpcChannel + BootHandshake (Phase 7.2 prep) ([13dd6b4](https://github.com/gameguild-gg/gameguild/commit/13dd6b46fcb96538f892e1bcf7386eda41526289))
* **emception/core:** extract RequestCorrelator (Phase 7.2 prep) ([1167082](https://github.com/gameguild-gg/gameguild/commit/1167082847c2d00280582a568de991542d64d99e))
* **emception/core:** add cmake.targets multi-binary CMake support ([ffebaa6](https://github.com/gameguild-gg/gameguild/commit/ffebaa61f036a678cf0fdcc1ad7ea9e69181e707))
* **emception/react:** <EmceptionRun> + useEmception (Phase 6.2) ([f3a606b](https://github.com/gameguild-gg/gameguild/commit/f3a606bb4f3b9a5a882c9f0947d72d5d8f238b2d))
* **emception/webcomponent:** real <emception-run> element (Phase 6.1) ([ab3190e](https://github.com/gameguild-gg/gameguild/commit/ab3190e90bed5eae722a64935c5b82569817f05b))
* **emception/core:** Phase 7.7 — runtime feature guards (canvas / xterm) ([67627d3](https://github.com/gameguild-gg/gameguild/commit/67627d30391eff8dee5dd94d33b692a3ca269c22))
* **emception/core:** Phase 3.7 — workspace zip export/import ([191ff67](https://github.com/gameguild-gg/gameguild/commit/191ff6784b10863c716ee36b61ffa490e2292ab6))
* **emception/core:** Phase 6.1/6.2 prep — shared adapter helpers ([6bd3ad3](https://github.com/gameguild-gg/gameguild/commit/6bd3ad3c7a6b054b01bf760fb15348d6d9924424))
* **emception/node:** add NodeRuntimeAdapter (Phase 7.1/7.2 skeleton) ([edefd12](https://github.com/gameguild-gg/gameguild/commit/edefd12880dcafdd63dda3dbd4322a592c319281))
* **emception/core:** wire clang-query and doctest engine handlers ([006c2d3](https://github.com/gameguild-gg/gameguild/commit/006c2d3c5baa3cc88f7ded2dff052a0f406dd070))
* **emception/core:** doctest console-output parser (Phase 5.5 — pure half) ([f1b549a](https://github.com/gameguild-gg/gameguild/commit/f1b549acaf70e02da0f7e879535ec274e7c83051))
* **emception/core:** clang-query matcher engine (Phase 5.4 — pure half) ([c0b74ac](https://github.com/gameguild-gg/gameguild/commit/c0b74aceadf3f8c59f32f976232b12bd0d9842c4))
* **emception/core:** cancellation primitives — timeout + AbortSignal (Phase 2.2) ([13637bd](https://github.com/gameguild-gg/gameguild/commit/13637bdc66833c9851cf7b4015e7bef1be9a2a25))
* **emception/core:** preset-aware compile argv builder (Phase 4.3) ([b4f0f9a](https://github.com/gameguild-gg/gameguild/commit/b4f0f9aeaa9b1549a8f56362a0a9a0a8a1e0f19d))
* **emception/cli:** doctor verifies workspace-store writability (Phase 7.8) ([a44db9e](https://github.com/gameguild-gg/gameguild/commit/a44db9e0b8e3c5095d6e68e1c593784fdfa87cee))
* **emception:** stdio-file test kind + Node stream/manifest helpers ([61cd021](https://github.com/gameguild-gg/gameguild/commit/61cd021e17bdfe3c33ea24c62e34f3725b357ebc))
* **emception/core:** visibility-aware test report redaction (Phase 5.7) ([4777566](https://github.com/gameguild-gg/gameguild/commit/4777566fae9b9a815717722c4b3918e5cc6ea8d0))
* **emception:** view-config validator + fs WorkspaceManager + ESM hygiene ([e9775d3](https://github.com/gameguild-gg/gameguild/commit/e9775d395750be61512fe1cc072193d69cb32128))
* **emception/core:** seed hashing + in-memory workspace store ([50c4a94](https://github.com/gameguild-gg/gameguild/commit/50c4a94f98c6171e9e439c8f17b6d815c8aa501c))
* **emception/core:** test engine skeleton (Phase 5.1/5.2/5.6) ([9e5508b](https://github.com/gameguild-gg/gameguild/commit/9e5508bbaba155a45132ed99560c66335400b274))
* **emception/core:** typed tools surface + workspace manager interface ([4091f5d](https://github.com/gameguild-gg/gameguild/commit/4091f5d68fac1b047fed58d919af3329a0a7a475))
* **emception/core:** build config resolver (Phase 3.5 / Phase 4 prep) ([3c440a8](https://github.com/gameguild-gg/gameguild/commit/3c440a8c6a269731d4b6838ac7d3c0428fa03269))
* **emception/core:** stdin/stdout stream normalizers (Phase 2.1) ([313247c](https://github.com/gameguild-gg/gameguild/commit/313247c809cdc8573e2940122ec30c1a29c9fc47))
* **emception/cli:** implement `emception cdn-export <dir>` (Phase 1.3) ([38c305e](https://github.com/gameguild-gg/gameguild/commit/38c305eef0fb162fdf8fc092746e4ce0a557ef33))
* **emception/browser:** add `tty: 'none'` headless mode (Phase 1.1) ([5bb4fc5](https://github.com/gameguild-gg/gameguild/commit/5bb4fc50c75608ada24d4e159cab47fb604fc3da))
* **emception/core:** add HeadlessIOProvider (Phase 1.1 foundation) ([30661b7](https://github.com/gameguild-gg/gameguild/commit/30661b74421e14692de677385f1845f58b5e5ad0))
* **emception/browser:** zero-config manifest URL default (Phase 1.2) ([33224b0](https://github.com/gameguild-gg/gameguild/commit/33224b0fa5e35592bdfff59023b6acf1ca00ce64))
* **emception/core:** typed event API on EmceptionAPI (Phase 1.5) ([117892c](https://github.com/gameguild-gg/gameguild/commit/117892c37429b865f364bbbf8ba545a09f1c2aee))
* **emception/browser:** add COI preflight at @emception/browser/coi (Phase 1.4) ([07b7e00](https://github.com/gameguild-gg/gameguild/commit/07b7e00f23c37c3e29a212fa0508af1f477e3fe6))
* **emception/cli:** implement `emception doctor` (Phase 1 sketch / Phase 9.2 prep) ([79baa53](https://github.com/gameguild-gg/gameguild/commit/79baa53baea6914e9a7f69f5a9384159844861cc))
* **emception/core:** add RuntimeAdapter interface (Phase 1.8) ([347256b](https://github.com/gameguild-gg/gameguild/commit/347256b52f357e1d3a005d9a9dda41d581cc7371))
* **emception:** meta package becomes thin wrapper over scoped pkgs (Phase 0.3) ([e0e4ada](https://github.com/gameguild-gg/gameguild/commit/e0e4ada86d5d32785fb2490b28e30839d171034e))
* **emception/browser:** migrate full browser runtime — Phase 0.2 complete ([472b118](https://github.com/gameguild-gg/gameguild/commit/472b1180e996a0918198e578cb3d7e79524e17e2))
* **emception/browser:** migrate VFS layer (LazyFS, IDBFS, EmscriptenFS) — Phase 0.2 ([c129ce6](https://github.com/gameguild-gg/gameguild/commit/c129ce6675b66e0c655f0d9523051a0bfe0e5bb7))
* **emception:** bootstrap @emception/* monorepo (Phase 0.1, 0.5, 0.8) ([2bb0890](https://github.com/gameguild-gg/gameguild/commit/2bb08900254eb168a4131ffeb07b55c704a228d7))

### Bug Fixes

* **emception:** fix react issue ([3598b05](https://github.com/gameguild-gg/gameguild/commit/3598b0505b8a426c5cbced59be27c06329ead3e9))

# [3.1.1](https://github.com/gameguild-gg/gameguild/compare/v3.1.0...v3.1.1) (2026-04-24)

### Bug Fixes

* **ci:** force OIDC trusted publishing for emception npm publish ([a3261af](https://github.com/gameguild-gg/gameguild/commit/a3261af760d836082dde820a04944e2240c6a356))

# [3.1.0](https://github.com/gameguild-gg/gameguild/compare/v3.0.2...v3.1.0) (2026-04-24)

### Features

* design a better develeper experience for emception ([91e7774](https://github.com/gameguild-gg/gameguild/commit/91e7774846c933d86a8c4b5f98faad830cc3a6bd))
* GameGuild Readme ([a65e260](https://github.com/gameguild-gg/gameguild/commit/a65e2600c40c1a53ac4879bd5d7430b64e0f48e3))
* Security Policy ([fd7c03b](https://github.com/gameguild-gg/gameguild/commit/fd7c03be3cc1ac49f7de6bd7121e9d0c66c5933e))
* Development Setup ([c5d04ed](https://github.com/gameguild-gg/gameguild/commit/c5d04ed12408355333e76d6eea2292ae5a50f958))
* Legal and Dispute Process terms ([98221ec](https://github.com/gameguild-gg/gameguild/commit/98221ecd7cb47660c1dcead2df146891b4c1cadd))
* Contributor License Agreement (CLA) ([23b2ff9](https://github.com/gameguild-gg/gameguild/commit/23b2ff9d8f6f4b325870fd1e2a55ff87807ed6d1))

### Bug Fixes

* **docs:** fix readme ([cf69e02](https://github.com/gameguild-gg/gameguild/commit/cf69e02fff56ffc57e7ae0ab89e886ce507dfc34))
* **docs:** link for CLA ([7b155f6](https://github.com/gameguild-gg/gameguild/commit/7b155f6f1d549afe9189fd923f825831f4ad06cc))
* name ([5a2e0d6](https://github.com/gameguild-gg/gameguild/commit/5a2e0d64679890944c3e4d3d9f7c18ae305933c4))
* Code of Conduct ([5fb160e](https://github.com/gameguild-gg/gameguild/commit/5fb160ef48209e9d9c83b38a119eaf5242c4f535))
* Contributing to GameGuild ([c95a7a4](https://github.com/gameguild-gg/gameguild/commit/c95a7a4c67f3e4b11aaaffaf32b05f7cb15b281b))
* dual license MIT and commercial ([b9b5bf3](https://github.com/gameguild-gg/gameguild/commit/b9b5bf3977989175da2dbd032519389a672d9fb1))
* update license AGPL to MIT ([38d265a](https://github.com/gameguild-gg/gameguild/commit/38d265adbdef3c727cb33b311406214f5937e98a))

# [3.0.2](https://github.com/gameguild-gg/gameguild/compare/v3.0.1...v3.0.2) (2026-04-23)

### Bug Fixes

* **emception:** imgui build without gh_token ([3c559a0](https://github.com/gameguild-gg/gameguild/commit/3c559a00642c8dc989c603509abf036c302bc0d4))

# [3.0.1](https://github.com/gameguild-gg/gameguild/compare/v3.0.0...v3.0.1) (2026-04-23)

### Bug Fixes

* **emception:** pass GITHUB_TOKEN to imgui release detection to avoid 403 rate-limit ([a58c318](https://github.com/gameguild-gg/gameguild/commit/a58c3186b933f679d4e8fb8b97d6b0b22fd6890b))

# [3.0.0](https://github.com/gameguild-gg/gameguild/compare/v2.55.0...v3.0.0) (2026-04-23)

### BREAKING CHANGES

* **api:** Removes chapter entity and its related associations ([5204c51](https://github.com/gameguild-gg/gameguild/commit/5204c5148bc1778db4bfb0ccb9b0098984fb0f8e))

### Features

* **emception:** fix asyncfy ([d0c2b65](https://github.com/gameguild-gg/gameguild/commit/d0c2b6541adf2d428d18b8a36b8bd4aedf669c0d))
* **dotnet-wasm:** updating build ([5f081a3](https://github.com/gameguild-gg/gameguild/commit/5f081a3364c5e1a6acd2f14af6601f861664e3fd))
* rust wip ([7b2b448](https://github.com/gameguild-gg/gameguild/commit/7b2b4484c410bf5ae028b5a26add3fe2801fd5a8))
* **dotnet-wasm:** dotnet install system ([38745e5](https://github.com/gameguild-gg/gameguild/commit/38745e50e001537a8b53a500482c07e67b0583be))
* **dotnet-wasm:** updating scripts .sh to .ts ([33e067f](https://github.com/gameguild-gg/gameguild/commit/33e067f70cc9d97a8282634a44570ea5a9fef9a8))
* **editor:** static-viewer ([1e709ed](https://github.com/gameguild-gg/gameguild/commit/1e709ede9697771977add726013906f7754fd6f8))
* **editor:** drag drop for block engine ([6abb423](https://github.com/gameguild-gg/gameguild/commit/6abb4238285124d2663241fee144fa5c25b96fc0))
* **editor:** editor engine chooser for free editors ([23859b6](https://github.com/gameguild-gg/gameguild/commit/23859b6325a457639292f381a10841a05b592b4a))
* **editor:** Full Simplified imports and dev use for editor ([00d7a9e](https://github.com/gameguild-gg/gameguild/commit/00d7a9e8b42a795c58022f2a905b3ede8ea9b9b3))
* **courses:** add week14 of networking ([5ce915e](https://github.com/gameguild-gg/gameguild/commit/5ce915e20267af4e3952ef23ef91d854adc785f1))
* **emception:** direct clang+wasm-ld compile, dock UX, path refactor ([4e9775d](https://github.com/gameguild-gg/gameguild/commit/4e9775d19308839fd73e57b9aa91b247b0f5de86))
* **editor:** create project for home page ([35b058f](https://github.com/gameguild-gg/gameguild/commit/35b058f8938d2ed13d8143a0e98afc72474dff80))
* **editor:** block engine order ([3f71eaf](https://github.com/gameguild-gg/gameguild/commit/3f71eaf13863668e79209d3169b5f9c320e094b3))
* **ide:** now we dont use jspi anymore, we use asyncfy to increase compatibility ([3ff7b92](https://github.com/gameguild-gg/gameguild/commit/3ff7b92acd588faa05ed68c482f941fa00f95f44))
* caveman skill ([1d96fb6](https://github.com/gameguild-gg/gameguild/commit/1d96fb63b8d0b11d406ff6eaad5f79e63c209d83))
* **editor:** rich text node ([dabe065](https://github.com/gameguild-gg/gameguild/commit/dabe0653859d0808f93ec97da992814854fc3099))
* **editor:** scrollToIndex ([d145614](https://github.com/gameguild-gg/gameguild/commit/d145614979d526323ddbdb722929534ccbabbf3c))
* **editor:** table-editor for block engine ([6deb620](https://github.com/gameguild-gg/gameguild/commit/6deb6208498e891aef946f1032b0038caa01ad11))
* **editor:** html node ([8d134be](https://github.com/gameguild-gg/gameguild/commit/8d134beb01751875be50f4c1b376089a9f9c3369))
* **editor:** adapter code-studio to blocks engine ([5a267d5](https://github.com/gameguild-gg/gameguild/commit/5a267d5777fbeeb9311845831c5f99c47eebd16a))
* **editor:** blocks engine ([977789a](https://github.com/gameguild-gg/gameguild/commit/977789aa243d49a8ee91106a7b60efb6ba8cde60))
* blocks engine ([70ed82f](https://github.com/gameguild-gg/gameguild/commit/70ed82f822430f9015198e9a39a09e89869815fa))
* highlight quiz ([3f096f5](https://github.com/gameguild-gg/gameguild/commit/3f096f516c13ba43fb7632c0332a65dd5e772ebe))
* hotspot quiz ([9bb44e0](https://github.com/gameguild-gg/gameguild/commit/9bb44e0bad46ac074e0a0f935555940a8e28dfd8))
* numeric quiz ([9354c57](https://github.com/gameguild-gg/gameguild/commit/9354c574d6c48032e774992f269b56b6cb239aeb))
* formula quiz validate ([72fe024](https://github.com/gameguild-gg/gameguild/commit/72fe024d3bbc07dd71de033504846d55e48ba6df))
* formula quiz mode write formula ([318a402](https://github.com/gameguild-gg/gameguild/commit/318a40269328388fc3155da8eb4a7a5a19caa300))
* formula quiz ([5235a4a](https://github.com/gameguild-gg/gameguild/commit/5235a4aa1500ce49693ad8510a35cee2b4c25316))
* standard quizzes ([f36dfe0](https://github.com/gameguild-gg/gameguild/commit/f36dfe0741d6972e734dff87ab3bc5eb9c5f3000))
* quiz matching random positions ([590bd15](https://github.com/gameguild-gg/gameguild/commit/590bd152571b6799ad71cb1e2983e695dfbca3ca))
* quiz matching colors ([4e255f7](https://github.com/gameguild-gg/gameguild/commit/4e255f72594bd3a24a11234103cd16eee5fac090))
* quiz no feedback mode ([b1eec50](https://github.com/gameguild-gg/gameguild/commit/b1eec503bbaf920f6f01301fcac249d3536b88b5))
* verify answer for essay quiz ([03ffc95](https://github.com/gameguild-gg/gameguild/commit/03ffc95370e6f8147c2ce71c88f33c22045700b8))
* lexical toolbar for essay quiz ([2202815](https://github.com/gameguild-gg/gameguild/commit/2202815452fe127d964021e61f9d4c891b476027))
* fill blank number specs ([02f60d5](https://github.com/gameguild-gg/gameguild/commit/02f60d57533702f0f4842d1ede95c8e5032a6ced))
* fill blank number option ([fa04cee](https://github.com/gameguild-gg/gameguild/commit/fa04cee600ce3d50d7337dedef32d8c88a3bf22d))
* fill blank case sensitive ([a7641bb](https://github.com/gameguild-gg/gameguild/commit/a7641bbc60a617423e5a88739dce486e727d2186))
* **gglexical:** project slideshow use projects ([fe5d5ad](https://github.com/gameguild-gg/gameguild/commit/fe5d5ad1e8e73bdc8b936e411e5a0cbac01b8e41))
* **gglexical:** git history and snapshots ([a0575c4](https://github.com/gameguild-gg/gameguild/commit/a0575c4f6443d95d97aaf1521f760645eb6523c3))
* **gglexical:** fill blank carries over ([9302970](https://github.com/gameguild-gg/gameguild/commit/930297019666f200c88cd84e76161153ff051f89))
* **gglexical:** fill blank show words ([f24ab70](https://github.com/gameguild-gg/gameguild/commit/f24ab70aabc112591748d129157b1b97a8d60f48))
* **gglexical:** fill blank new modes ([3bf5dc3](https://github.com/gameguild-gg/gameguild/commit/3bf5dc3028c40d8206760dfd55f04d2857db1ea9))
* **gglexical:** quiz fill blank editor accepted answers ([add10f8](https://github.com/gameguild-gg/gameguild/commit/add10f8db72dbb6bcf47ebebbc527810107faed1))
* **gglexical:** quiz ([3a6448d](https://github.com/gameguild-gg/gameguild/commit/3a6448d94c28c1abfca659e51e984a53070afd0f))
* **gglexical:** cell and lexical format dual converter ([6a0cea5](https://github.com/gameguild-gg/gameguild/commit/6a0cea5bce161b1ae9306aa2cafc7f854dbdf9e8))
* **gglexical:** multi-block preview panel and block bar ([66778f4](https://github.com/gameguild-gg/gameguild/commit/66778f49a60c502e9b28e57e76b23c4012c1caff))
* **gglexical:** multi-block editor panel and block bar ([963c3f2](https://github.com/gameguild-gg/gameguild/commit/963c3f2887b0d668e232f9b037a8c0bf391a1997))
* **gglexical:** restrictions and modes ([4df6305](https://github.com/gameguild-gg/gameguild/commit/4df63058de516cc059bcb3e962afc31d577fa104))
* **gglexical:** union for single and multiple projects type2 ([aef6d84](https://github.com/gameguild-gg/gameguild/commit/aef6d849b2f3c990dfa2307314547e81ed74ae06))
* **gglexical:** delete block and panel dialog confirmation ([00da5ab](https://github.com/gameguild-gg/gameguild/commit/00da5ab199c49eb3b4f45a2c39791c797843749b))
* **gglexical:** multi-block for type2 project ([155c07c](https://github.com/gameguild-gg/gameguild/commit/155c07c2f73b060ef4e3555c9d4f6d5af92d0309))
* **gglexical:** type2 with 1 block ([887ca22](https://github.com/gameguild-gg/gameguild/commit/887ca22afad35e59b2ab7a5bc2c2e25fb5b907a3))
* **gglexical:** create project dialog new ui ([bac3461](https://github.com/gameguild-gg/gameguild/commit/bac3461367ee5920396679aefa363411d90dadba))
* **gglexical:** project-viewer for import projects for projects ([010b689](https://github.com/gameguild-gg/gameguild/commit/010b6898ceebd326f3433b09a7736a6b5d4e09e7))
* **gglexical:** project-node for import projects for projects ([719742a](https://github.com/gameguild-gg/gameguild/commit/719742a5e736bb4934df17d39c94fc430bdfb1c1))
* **gglexical:** project card type view ([79d5925](https://github.com/gameguild-gg/gameguild/commit/79d5925855f765ea92a217cb4dc5fbd3257e0a53))
* **gglexical:** project sequential panel select ([9d17c0d](https://github.com/gameguild-gg/gameguild/commit/9d17c0dddffb16ae046ccf1cfa98be101b4486c2))
* **gglexical:** preview renderer sequential integration for viewer page ([1bfd337](https://github.com/gameguild-gg/gameguild/commit/1bfd3378e5e8fdbb8559a2b1e88e66624a6aeac1))
* **gglexical:** preview renderer sequential slide ([c06d260](https://github.com/gameguild-gg/gameguild/commit/c06d26026f13a4936861c04a681236b6871016ee))
* **gglexical:** preview renderer sequential continuous ([19e7e33](https://github.com/gameguild-gg/gameguild/commit/19e7e33ef1ecbfce18d34f1b01a91f17a6b0ff3a))
* **gglexical:** definition project for sequential type ([a1c54b8](https://github.com/gameguild-gg/gameguild/commit/a1c54b8f7d6b86904fca0249199e48f658139055))
* **gglexical:** create project for layout sequential ([dc0c34e](https://github.com/gameguild-gg/gameguild/commit/dc0c34ec44b3acd09a34866210a9eb647735dbab))
* **gglexical:** studio page sequential layout ([1290136](https://github.com/gameguild-gg/gameguild/commit/12901365d835387999a00b85f27bd35e9450610f))
* **gglexical:** engine for layout sequential for studio page ([f2288d4](https://github.com/gameguild-gg/gameguild/commit/f2288d450fdbd33c8e77484efe35f65b0eb1180a))
* **gglexical:** base structure for new layout page sequential ([a55f870](https://github.com/gameguild-gg/gameguild/commit/a55f870ed8d34a7e4a4bbd30805f68d7c6c1977e))
* **gglexical:** code and quiz in type1 with restrictions ([eb01d5c](https://github.com/gameguild-gg/gameguild/commit/eb01d5c651b1decee0b55a28bfa7fad5429d9a00))
* **gglexical:** import project for new project ([9d2ca5e](https://github.com/gameguild-gg/gameguild/commit/9d2ca5e704b35f8db8488e45b8c4b8df0089ea91))
* **gglexical:** preview page access project.preferences ([b3c085b](https://github.com/gameguild-gg/gameguild/commit/b3c085bf826029ae252fd6b5b6fe9873f856eccd))
* **gglexical:** preview renderer type2 best experience ([1130bfd](https://github.com/gameguild-gg/gameguild/commit/1130bfd97f025c232d3e71d934d52ae6923cd122))
* **gglexical:** viewer page for type2 project ([1a4e4af](https://github.com/gameguild-gg/gameguild/commit/1a4e4af07a1f0545410c6bface93b6c6888c6b1d))
* **gglexical:** project modes (free - code - quiz) pages ([f526526](https://github.com/gameguild-gg/gameguild/commit/f526526989cf2b1990be1b4e072f418537ba8bc1))
* **gglexical:** create project for new type2 project ([d8e614a](https://github.com/gameguild-gg/gameguild/commit/d8e614acf45f1c479e776cffbbd67cba039b4489))
* **gglexical:** preview button in studio page ([d1b3559](https://github.com/gameguild-gg/gameguild/commit/d1b355969357485ab8e3de5408c375485c5e5c23))
* **gglexical:** manager page open for 2 type projects ([2e87af0](https://github.com/gameguild-gg/gameguild/commit/2e87af0590de50ee776c7a7fb33a2b190fab5d6a))
* **gglexical:** studio page for 2 type projects ([8b93d9d](https://github.com/gameguild-gg/gameguild/commit/8b93d9dbd2fda1776ec0190e31a645efa12c3115))
* **gglexical:** manager-page filter types for mimetype ([40b3df5](https://github.com/gameguild-gg/gameguild/commit/40b3df580b556b6b5ce98cffbb24ccb63bc01d4e))
* **gglexical:** code-studio theme preferences in project preferences ([9ad1974](https://github.com/gameguild-gg/gameguild/commit/9ad197415454c1e0faf05572c4d8c12cc71577d0))
* **gglexical:** project-preferences ([0066cee](https://github.com/gameguild-gg/gameguild/commit/0066cee9fcb5bb26b8ccf77244fd43662044469e))
* **gglexical:** system-settings modal-size focused in node ([164a2ba](https://github.com/gameguild-gg/gameguild/commit/164a2ba680bfe8fa4371ae28cc22a1f28b4d40ae))
* **gglexical:** settings-menu new ui ([f1b4aad](https://github.com/gameguild-gg/gameguild/commit/f1b4aad07ac9dbd393bbd8eaabf041354a748e73))
* **gglexical:** settings-menu global preferences ([5acced4](https://github.com/gameguild-gg/gameguild/commit/5acced4b131e9029875b78cc8da62a6e6e7b52d0))
* **gglexical:** settings-menu modal-size ([bb2f652](https://github.com/gameguild-gg/gameguild/commit/bb2f6529a5800d8f0c663d461eb6a7a5820c9f4e))
* **gglexical:** code-studio layout editor border ([a4d977f](https://github.com/gameguild-gg/gameguild/commit/a4d977fd6218892cddf8f5821b2f542bb6e59d7c))
* **gglexical:** code-studio aspect-ratio in preview ([a749cf8](https://github.com/gameguild-gg/gameguild/commit/a749cf82f29a6abfbadf3bd7ad0910e36481084c))
* **gglexical:** code-studio aspect-ratio ([ab06a62](https://github.com/gameguild-gg/gameguild/commit/ab06a62004ffd685d97d687ca96a3124517c3d4a))
* **gglexical:** code-studio without border container ([8ae55cc](https://github.com/gameguild-gg/gameguild/commit/8ae55cc90e5e59ffe682b1615a942b129bc4975f))
* **gglexical:** code-studio fullscreen display ([ecec3bf](https://github.com/gameguild-gg/gameguild/commit/ecec3bf918b4e33aa664449baec0adfd3b88d9b2))
* **gglexical:** editor-preferences ([901cf7e](https://github.com/gameguild-gg/gameguild/commit/901cf7e286ac6e2b04bf9b26bae3561cefd9236c))
* **gglexical:** code-studio full-width and height ([c077981](https://github.com/gameguild-gg/gameguild/commit/c0779814e530f4614d0d65188208949413377c10))
* **gglexical:** code-studio output butons ([65c7788](https://github.com/gameguild-gg/gameguild/commit/65c7788a0e5cd0427c45348431c4964da40bc95e))
* **gglexical:** code-studio focus-editor reset ([2dd4165](https://github.com/gameguild-gg/gameguild/commit/2dd41652551e1281bde5034ff4f230cea613add4))
* **gglexical:** focus-editor language-selector ([4e81e37](https://github.com/gameguild-gg/gameguild/commit/4e81e3703c68311113e42b9dc6f1babe9fcf0c62))
* **gglexical:** code-studio focus-editor ([01be060](https://github.com/gameguild-gg/gameguild/commit/01be06005db8f30ede55aae38f700dfd9c6eda69))
* **gglexical:** update manager-page for collection system ([14ffe0a](https://github.com/gameguild-gg/gameguild/commit/14ffe0a41e1115c1c605e36c36e72778da4f640c))
* **gglexical:** code-studio asset collections ([49c9aac](https://github.com/gameguild-gg/gameguild/commit/49c9aac76ca9be85360645a5be71232f0c995eb3))
* **gglexical:** code-studio standard display for execution and test ([fb7b004](https://github.com/gameguild-gg/gameguild/commit/fb7b0047232bbf5c60adbcaa7978d294e1e90197))
* **gglexical:** code-studio read-only and protected for all files ([55743fc](https://github.com/gameguild-gg/gameguild/commit/55743fc79ccb0dbcbd43851b33da6dc9f2669233))
* **gglexical:** code-studio read-only access ([9a224b9](https://github.com/gameguild-gg/gameguild/commit/9a224b9e8578ccd1a7235a48853e751d43c31d50))
* **gglexical:** code-studio protected access ([05a0310](https://github.com/gameguild-gg/gameguild/commit/05a03102c036b3f02f74e5bb92ad1d20fd370b7e))
* **gglexical:** code-studio execution modes (file main test) ([1bd5472](https://github.com/gameguild-gg/gameguild/commit/1bd54729f50b2d714e33d33747d1ddd501f6a2e8))
* **gglexical:** code-studio unified output ([1665358](https://github.com/gameguild-gg/gameguild/commit/1665358f7c63826b32826f7b19531f68f3562bd2))
* **gglexical:** add assets Text Storage save mode ([f4aa286](https://github.com/gameguild-gg/gameguild/commit/f4aa286ea2ad70d355fb288b063c26edc38f7e51))
* **gglexical:** code-studio use asset system for files ([e685ba0](https://github.com/gameguild-gg/gameguild/commit/e685ba069d8b36c96ca7c3882378c92ded780af7))
* **gglexical:** quiz-node categorization ([9441243](https://github.com/gameguild-gg/gameguild/commit/944124307a1c511d4edf674fa1aad198433ef206))
* **gglexical:** new pages ([f10133e](https://github.com/gameguild-gg/gameguild/commit/f10133e508444c24b07029f92026a7b2fd919170))
* **gglexical:** quiz-node reset-quiz new ux ([141d8b1](https://github.com/gameguild-gg/gameguild/commit/141d8b14a07169e7460d737a04e193036fef6549))
* **gglexical:** asset system manager ([3604413](https://github.com/gameguild-gg/gameguild/commit/36044138097f844ae68da1a903a524d6d3f963c9))
* **gglexical:** page studio size details ([74b112c](https://github.com/gameguild-gg/gameguild/commit/74b112ce4cdec996c699c17e8208919753def8e4))
* **gglexical:** page link navigation ([306441a](https://github.com/gameguild-gg/gameguild/commit/306441a1c0f418622fa06921146a4933a32f5c82))
* **gglexical:** asset filter with ranges ([b0a6f24](https://github.com/gameguild-gg/gameguild/commit/b0a6f243adebe97861e0919cc83dad698c8c2c6b))
* **gglexical:** asset delete project information alert ([12a45f6](https://github.com/gameguild-gg/gameguild/commit/12a45f613fdaeaa67334f9c8bb05533c4a0ed1b0))
* **gglexical:** manager page order list items ([8b3cb7e](https://github.com/gameguild-gg/gameguild/commit/8b3cb7ec52a526730ffc8142c0ea2fb350bbe2e8))
* **gglexical:** manager page with asset vision ([448d767](https://github.com/gameguild-gg/gameguild/commit/448d7678635ac0a7a7b586b9dc4040ae32d3e2c6))
* **gglexical:** media dialog with local options ([c84e65f](https://github.com/gameguild-gg/gameguild/commit/c84e65fd078c2bae9d4b1344f9b9dbae46e42d4e))
* **gglexical:** import/export update for new asset system ([8933e66](https://github.com/gameguild-gg/gameguild/commit/8933e663f2aec3c228a29bc91f5eb52b45dd3a80))
* **gglexical:** asset system ([adabf08](https://github.com/gameguild-gg/gameguild/commit/adabf08d8c61cd0914b1dd02e9e979c7e8d5b6b8))
* **gglexical:** code editor csharp runner ([23c4318](https://github.com/gameguild-gg/gameguild/commit/23c43181086dfd80f7835fea5f4e8f9635dda55e))
* **gglexical:** code editor update wasm best performance ([ba2e97e](https://github.com/gameguild-gg/gameguild/commit/ba2e97e5aac0033adf5c95501c99329b7ff7e415))
* **gglexical:** dotnet wasm multiple files support ([6c8d2f8](https://github.com/gameguild-gg/gameguild/commit/6c8d2f89e221d8d349a3d6673a2aca07899f8a3e))
* **gglexical:** dotnet wasm with csharp standard ([f0cee55](https://github.com/gameguild-gg/gameguild/commit/f0cee55f97b9746c84591660b6606d910a8bff38))
* **gglexical:** code editor wat runner env bindings ([3c9efde](https://github.com/gameguild-gg/gameguild/commit/3c9efde3f1377f651285ed00552d2a9fa6ca66be))
* **gglexical:** code editor wat (webassembly text) runner ([9127419](https://github.com/gameguild-gg/gameguild/commit/912741937d0bbd5f15e2c1696d53206b2ccacc1c))
* **gglexical:** code editor ruby runner ([09b5c70](https://github.com/gameguild-gg/gameguild/commit/09b5c704cc7414aa66405bc20f076215c4e61f16))
* **gglexical:** code editor python wasi runner ([0a82c8e](https://github.com/gameguild-gg/gameguild/commit/0a82c8e25db7c96b1a7e3361505a23bd58c04303))
* **gglexical:** code editor sql runner ([f16b69a](https://github.com/gameguild-gg/gameguild/commit/f16b69af09eb16d303731ccdb804596a26fc852a))
* **gglexical:** code editor php runner ([d30a610](https://github.com/gameguild-gg/gameguild/commit/d30a610d98f71b8e6face6e2e2a23ffef864714d))
* **gglexical:** code editor list more languages ([1bc4af9](https://github.com/gameguild-gg/gameguild/commit/1bc4af908075227ce46a43dbd768bcb6f22ff934))
* **gglexical:** code editor c runner ([78df6df](https://github.com/gameguild-gg/gameguild/commit/78df6df3099ec670a6c1bff579f30ae6afb514b8))
* **api:** Add program activity grades endpoints ([27b52ee](https://github.com/gameguild-gg/gameguild/commit/27b52eec2827c554f701ccc61b9d5c3d459bcb22))
* **gglexical:** code editor executing state progress ([9035555](https://github.com/gameguild-gg/gameguild/commit/9035555cee9871a5b75ad196d13917f67c4f5dec))
* **gglexical:** code editor cpp runner ([5230749](https://github.com/gameguild-gg/gameguild/commit/5230749f1cd8848337f421cb03310f5f5e71d0d6))
* **gglexical:** code editor link confirmation ([63704e7](https://github.com/gameguild-gg/gameguild/commit/63704e7c844146c519dd1f17877403d38b053f56))
* **gglexical:** xterm link confirm ([73d63cd](https://github.com/gameguild-gg/gameguild/commit/73d63cda0b322ad314570ab6b6e29736a66b7b8b))
* **gglexical:** link confirm dialog ([0facbd2](https://github.com/gameguild-gg/gameguild/commit/0facbd240261ab38cafd8a4cef5a573afbdd4d80))
* **gglexical:** code editor xterm addons ([cc0692e](https://github.com/gameguild-gg/gameguild/commit/cc0692eda7673b87da32cb81759672fb193c6a1f))
* **gglexical:** code editor xterm request input with Lua ([4d3e070](https://github.com/gameguild-gg/gameguild/commit/4d3e07036e332c7f7b0cf1d1969809301e976022))
* **gglexical:** code editor lua runner ([04683f2](https://github.com/gameguild-gg/gameguild/commit/04683f2f8779b91e6399882a8e0628d479ff0f0b))
* **gglexical:** code editor templates and support for more languages ([7dfaf2d](https://github.com/gameguild-gg/gameguild/commit/7dfaf2d2d78ac856785c7de47b2b12d8a0e1904d))
* **gglexical:** code editor python path support ([b62cad2](https://github.com/gameguild-gg/gameguild/commit/b62cad291cf43009df0f6da56365440585fa558e))
* **gglexical:** code editor virtual file system ([669eb5b](https://github.com/gameguild-gg/gameguild/commit/669eb5b0c310185ddcd3cb4823c781a0421152dd))
* **gglexical:** code editor python support ([45a31b3](https://github.com/gameguild-gg/gameguild/commit/45a31b34e8cb1a75a4e434177fad47451e0a0414))
* **gglexical:** script update wasm packages ([6bd2f7b](https://github.com/gameguild-gg/gameguild/commit/6bd2f7bb525bd5fbcd21e1ccaef27bdcc7318abf))
* **gglexical:** code editor reduce download wasm ([1ecd823](https://github.com/gameguild-gg/gameguild/commit/1ecd82342a8b6a07e689d7c4279434bd9d7e0e22))
* **gglexical:** code editor unified runner and JS/TS ([69fa793](https://github.com/gameguild-gg/gameguild/commit/69fa7935dbd42b3ce182c11932901943bbed912d))
* **gglexical:** code editor studio/viewer node from display-1 ([a9e3b04](https://github.com/gameguild-gg/gameguild/commit/a9e3b04d06fe3455c0a6738b561ec921ecf705a4))
* **gglexical:** code editor grid dimensions ([72e35ea](https://github.com/gameguild-gg/gameguild/commit/72e35ea2b50d29e0077c6afd23b7f026af08a57c))
* **gglexical:** code editor multiple and unique instancies ([8f198a2](https://github.com/gameguild-gg/gameguild/commit/8f198a22ad7600b863a889696e3342a52fe6a809))
* **gglexical:** code editor display grid layout ([1490fef](https://github.com/gameguild-gg/gameguild/commit/1490fefb139853fd7b8c8a5f985fd2b101d82228))
* **gglexical:** code editor file explorer drag drop ([60c1603](https://github.com/gameguild-gg/gameguild/commit/60c160343b44788998c2e143145a0d3a89dced93))
* **gglexical:** code editor duplicate name dialog ([ddcc0ef](https://github.com/gameguild-gg/gameguild/commit/ddcc0ef6595f17dfe44412e8458d7c4dc914d7a8))
* **gglexical:** code editor file explorer delete confirm ([d5ac97d](https://github.com/gameguild-gg/gameguild/commit/d5ac97d9f87a6e06056ca0337f13cf79880cfe39))
* **gglexical:** code editor file explorer vertical menu ([b523f42](https://github.com/gameguild-gg/gameguild/commit/b523f429d67facd34ef5f7342195c59b232971e5))
* **gglexical:** code editor file-tabs drag-drop ([a095b6a](https://github.com/gameguild-gg/gameguild/commit/a095b6ad9d6f3aebe5c4d32bb067802a9e9ae5fb))
* **gglexical:** code editor file-tabs better user experience ([586a6c0](https://github.com/gameguild-gg/gameguild/commit/586a6c0086a61eea7a35f0a193785a05de3d4ee7))
* **gglexical:** code editor no open file message ([63fd7f1](https://github.com/gameguild-gg/gameguild/commit/63fd7f1031e3d4dae8d9b3ff0f91c6fb3ebc1021))
* **gglexical:** code editor shiki Highlighter ([69d58f1](https://github.com/gameguild-gg/gameguild/commit/69d58f1c92288f264210f5d915f1de2ed78ada9b))
* **gglexical:** code editor file-tabs ([959a5d8](https://github.com/gameguild-gg/gameguild/commit/959a5d8c3197cc531aab7b9c8a48e4aa86a6805c))
* Add Apple Pay webhook handling and enhance PayPal webhook header validation ([93d4b9a](https://github.com/gameguild-gg/gameguild/commit/93d4b9a8d92d43500c6f739ace809c425c060b19))
* Add Feature Flags and Resources management pages with server-side data prefetching ([da6709d](https://github.com/gameguild-gg/gameguild/commit/da6709d604c14921726c14dc2606280b297db1fd))
* Implement ProcessPayPalWebhookCommandHandler and integrate CQRS in billing module ([bf99457](https://github.com/gameguild-gg/gameguild/commit/bf9945712ff577d2cd070e95f891fd74b6406e6b))
* Add resource quota management with RequiresQuota attribute ([8f18bd5](https://github.com/gameguild-gg/gameguild/commit/8f18bd54cc2b530f802cb840e19a018e219768a2))
* Remove development configuration files for GameGuild ([3e5c2bc](https://github.com/gameguild-gg/gameguild/commit/3e5c2bc5e90f41b99369e4e04ff0bcd9b9e2d288))
* Remove GameGuild project files and configuration settings ([4760366](https://github.com/gameguild-gg/gameguild/commit/4760366458f6bd2414958d75c3f18e63a7fb85bf))
* Update HealthController and HealthEndpoint for anonymous access; refactor namespaces and add integration tests ([96fad4c](https://github.com/gameguild-gg/gameguild/commit/96fad4c036c3a6d973c92172e818ca566cbc5acd))
* Add dotnet-tools configuration for EF Core ([923666d](https://github.com/gameguild-gg/gameguild/commit/923666d5c53926ab0b14a469ccacb82c06a1a74d))
* Integrate Audit module with necessary services and database context updates ([5d7a5aa](https://github.com/gameguild-gg/gameguild/commit/5d7a5aa28a11630001c433c3d62d0b7617a0dbfc))
* Add Game Jams and Localization modules with necessary models and services ([16874c2](https://github.com/gameguild-gg/gameguild/commit/16874c28f6d4f7953e561f0cc829cdc34c4e309b))
* **SLA Monitoring:** Implement SLO compliance and violation queries ([ad85657](https://github.com/gameguild-gg/gameguild/commit/ad856576e9933b5a65efb7665c443b549c542f6e))
* Implement Testing Lab module with services, validators, and settings management ([02bdc8d](https://github.com/gameguild-gg/gameguild/commit/02bdc8db10915da87ab8100774369d3cb4041cb6))
* Implement country-based targeting handler for feature flags ([5a2c135](https://github.com/gameguild-gg/gameguild/commit/5a2c135c3fc3f0fe56230c6bb60050a9a5f5f0b0))
* Implement billing webhook processing module ([5e27837](https://github.com/gameguild-gg/gameguild/commit/5e2783717c9314700856a13c08e2b4b387d6e4db))
* Implement SIEM integration service for security event logging ([6122609](https://github.com/gameguild-gg/gameguild/commit/61226094b5b31464aa52d41fb754a97cbc3520dc))
* **audit:** Implement cryptographic signing service for audit logs ([801a1a2](https://github.com/gameguild-gg/gameguild/commit/801a1a25dc98e3587a004b7e4c4bc86352c6bb5c))
* Implement user profile and user management queries with pagination and filtering ([895cc1f](https://github.com/gameguild-gg/gameguild/commit/895cc1f81fb9f96d27f745c6ad3afcfd4cf4d519))
* **web:** Add testing session content for Intro2GPro ([bc3ade5](https://github.com/gameguild-gg/gameguild/commit/bc3ade5b0bab75f930aa0fbb65fb439a8df61177))
* contact/links page ([9e48e60](https://github.com/gameguild-gg/gameguild/commit/9e48e60b9a60f8b833e649b0091f1b63a0e4b6f7))
* **web:** Add course data and structure ([4f4af08](https://github.com/gameguild-gg/gameguild/commit/4f4af0847a508616c3fa01174c41ad11c812876a))
* **courses:** Add game mechanics content to intro2gpro ([ca1c3ae](https://github.com/gameguild-gg/gameguild/commit/ca1c3ae6ff69a0b6ee2cb0015f359b02f92ec11f))
* **web:** Adds game dev assignment and production content ([a370b9b](https://github.com/gameguild-gg/gameguild/commit/a370b9bb7473857b3c14429b7ca60a8b740592d1))
* **web:** Adds game dev assignment and production content ([010a6d2](https://github.com/gameguild-gg/gameguild/commit/010a6d24918a73a7574181ac5bed61b6b3d57990))
* **web:** Implements cookie consent banner ([a0951b7](https://github.com/gameguild-gg/gameguild/commit/a0951b779fc9c550c0549617ada8c6283484d78c))
* Add tenant import/export commands and DTOs ([1e2e133](https://github.com/gameguild-gg/gameguild/commit/1e2e1339ea3d504e03b0757e798ac2d5a959d766))
* **subscriptions:** Complete SubscriptionRepository implementation (92→65 errors, 27 fixed) ([572b56b](https://github.com/gameguild-gg/gameguild/commit/572b56b611a709735554f36cdf3eee0561603fba))
* **subscriptions:** Add Subscriptions module with clean architecture ([4147055](https://github.com/gameguild-gg/gameguild/commit/4147055ce13033ed9c1eab8cce1e2d767255743a))
* **TestingLab:** Integrate TestingLab module with custom CQRS ([1df8f04](https://github.com/gameguild-gg/gameguild/commit/1df8f044746797a6529e3ee6265cc8d2b5261a7c))
* **TestingLab:** Complete TestingLab module integration ([8673463](https://github.com/gameguild-gg/gameguild/commit/86734638cbe171129e5bf160c9a7c215874da2b1))
* Implement Just-in-Time (JIT) permission elevation handlers and services ([ad4ea57](https://github.com/gameguild-gg/gameguild/commit/ad4ea57d4d263b1e6d711c89d1710cfbbec617da))
* Implement notification management system ([50c246d](https://github.com/gameguild-gg/gameguild/commit/50c246de0beb699b9edadcba4ca1bbbde61cf71f))
* Add TargetingRule model for feature flag targeting ([63080e0](https://github.com/gameguild-gg/gameguild/commit/63080e00e708b59dbcfc0af89ae48ad2853c669b))
* Add subscription plans domain events and models ([ffe8ce5](https://github.com/gameguild-gg/gameguild/commit/ffe8ce5553a23720bd34e7d43f918a6e7b980ef9))
* update CourseCard component to use a new CourseCardCourse type for better type safety ([80f92a9](https://github.com/gameguild-gg/gameguild/commit/80f92a9749d7d800878e7a91ff994c1df9fa1581))
* **web:** Adds route-based analytics tracking ([6a2813e](https://github.com/gameguild-gg/gameguild/commit/6a2813eea9170a28448cb8e42964ae6ed649ba3c))
* **core:** improve exception handler visibility and add cache get-or-set method ([7366f6d](https://github.com/gameguild-gg/gameguild/commit/7366f6dbc6f5fcda682d6ba19a9f92c8fa66ab8c))
* **permissions:** add comprehensive multi-layer permission module with caching and auditing ([90863be](https://github.com/gameguild-gg/gameguild/commit/90863bed9fd9e5558b496bd6fe60f20f2516327f))
* **core:** add comprehensive CQRS infrastructure with optimized mediator and pipeline behaviors ([db912e5](https://github.com/gameguild-gg/gameguild/commit/db912e575523ec47459e31d66fa5436767cb9717))
* **core:** implement Cloudflare Dynamic DNS service with support classes and configuration ([8a41122](https://github.com/gameguild-gg/gameguild/commit/8a41122c5dbd63515a68919cd0bacf2bc5ce4c71))
* **course:** Add 'Automation in Game Development' module to Intro to GPro ([413efd2](https://github.com/gameguild-gg/gameguild/commit/413efd2f6326c6b309811ee1574cbf284d39335e))
* **mermaid:** Standardize chart background and container styling ([6b1af26](https://github.com/gameguild-gg/gameguild/commit/6b1af26dffc29b13ef4807881acd295b157463ef))
* **authentication:** integrate permission service for tenant user management and token enhancements ([49f64f1](https://github.com/gameguild-gg/gameguild/commit/49f64f13a4831bd63af2da081f79302d9ee9d764))
* **authentication:** switch to permission service for tenant data and set current tenant context ([4f837c7](https://github.com/gameguild-gg/gameguild/commit/4f837c70d3d0dd5502c6568eea0a8eb62f5a716e))
* **authentication:** implement comprehensive user authentication and token management ([3bc9f93](https://github.com/gameguild-gg/gameguild/commit/3bc9f9358151d3655f6d4028d6d5d0ce1bdb40f0))
* **authentication:** add comprehensive user auth events and handlers with enhanced sign-in/out logging and token management ([6b57cf6](https://github.com/gameguild-gg/gameguild/commit/6b57cf61d1d70d12c9415e48ea7941267043ebe5))
* **auth:** add comprehensive authentication controller and clean up repositories ([170262e](https://github.com/gameguild-gg/gameguild/commit/170262efeb8456a5f4632305512b18dcc18ae56d))
* **authentication:** Enhance refresh token handling with anomaly detection and repository usage ([dc7c0fb](https://github.com/gameguild-gg/gameguild/commit/dc7c0fb7480d5f979965c62062a3f993eb49efa1))
* **authentication:** implement comprehensive auth service with anomaly detection and repositories ([cd6f966](https://github.com/gameguild-gg/gameguild/commit/cd6f966a172da2cb2f3bdf79179982e36b4224da))
* **auth:** add comprehensive authentication and audit services with MFA and session management ([187e577](https://github.com/gameguild-gg/gameguild/commit/187e57706981b0caa8abdf7e4034bf0871219a56))
* **user-profiles:** restructure module with CQRS, validation, and handlers ([bfa4083](https://github.com/gameguild-gg/gameguild/commit/bfa4083b6cca7fce9b53c2df89d6f2a295930e11))
* **user-profiles:** replace GraphQL with repository pattern and add user profile service and handlers; extend user entity with names ([af0af99](https://github.com/gameguild-gg/gameguild/commit/af0af99f0a25cd7a069ecc4d5cbee478d0341784))
* **tenants:** separate tenant domains and settings into dedicated modules and services ([72835d2](https://github.com/gameguild-gg/gameguild/commit/72835d22239a973bb0a748dadd616f7dfcb80967))
* **resources:** add resource quota and permission service abstractions and implementations ([917ea51](https://github.com/gameguild-gg/gameguild/commit/917ea51a888a1931aec0712e9264781b362744f0))
* **database:** introduce streamlined ApplicationDbContext and seeding with language defaults ([48c394a](https://github.com/gameguild-gg/gameguild/commit/48c394a88071ec01acf25c661ed7dc10714184b3))
* **credentials:** Add validator for creating user credentials ensuring data integrity ([dce6a2d](https://github.com/gameguild-gg/gameguild/commit/dce6a2dbc643e94a40337f1c92915baee2ca7baf))
* **tenants,users:** Add detailed validators, enhance handlers, and improve EF configurations ([792722e](https://github.com/gameguild-gg/gameguild/commit/792722e4aceed49f416bc0850f3179ad3a31ff2c))
* **users:** overhaul user module with CQRS, services, and API controller ([2e27066](https://github.com/gameguild-gg/gameguild/commit/2e270660d9dd9c9f9229f9c9842d63b1e18f5aee))
* **credentials:** add full CQRS commands, events, and validations for credential management including soft-deletes ([c659093](https://github.com/gameguild-gg/gameguild/commit/c65909372c63556ccbbc97fb917658525fa84bed))
* **credentials:** Add comprehensive credentials REST API controller using CQRS pattern ([dfa76d1](https://github.com/gameguild-gg/gameguild/commit/dfa76d18cb705bdd60efbc26a4982a555b8b6e75))
* **credentials:** refactor to hexagonal architecture with CQRS and repository pattern ([182ecb9](https://github.com/gameguild-gg/gameguild/commit/182ecb9a14a19e87d073f56fcd8c860d7ed2d396))
* **api:** add multi-tenant support with domain and tenant seeding ([e18d507](https://github.com/gameguild-gg/gameguild/commit/e18d50742dea039ae44007d7ab9c57709b4c85ef))
* **tenants:** add caching, context, and core services; remove legacy commands and controllers ([3c6eded](https://github.com/gameguild-gg/gameguild/commit/3c6ededf3e81dd00a3be29ec7eee1b5edc118b2b))
* **web/courses:** switch to myPrograms hook, add null-safe filtering/sorting, remove debug UI ([108978e](https://github.com/gameguild-gg/gameguild/commit/108978e967e6785bc235c39b7f7b8182b835da3a))
* **web/graphql:** refresh codegen and queries; align mutations with API changes ([98ae7ae](https://github.com/gameguild-gg/gameguild/commit/98ae7aef5a25aab5988ad10d0e96882d3fdd9134))
* **api/authz:** enhance context and authorization logging across REST and GraphQL; wire ContextMiddleware; add docs ([0b31a70](https://github.com/gameguild-gg/gameguild/commit/0b31a70516233721bcab0ec48491e4bd872e03bd))
* **web:** migrate course detail pages to server components with GraphQL-backed actions ([1e52b9c](https://github.com/gameguild-gg/gameguild/commit/1e52b9c8cef7f348f9495086d794f5984ae7b575))
* **web:** integrate GraphQL program & product operations into courses UI ([1b4af93](https://github.com/gameguild-gg/gameguild/commit/1b4af932469362806b843cbacaeeb4175d02187b))
* **web:** improve Apollo provider auth flow and provider composition ([1694d2f](https://github.com/gameguild-gg/gameguild/commit/1694d2f828664e7e61cbc7a44d98248af195a5ef))
* **graphql-schema:** regenerate schema with program CRUD & extended entities ([46371f6](https://github.com/gameguild-gg/gameguild/commit/46371f682191853ffc158dbde26ba76d4d71673e))
* **api:** refine Program GraphQL mutations and queries ([987fcc4](https://github.com/gameguild-gg/gameguild/commit/987fcc49ead889e71382fb0413f11e6f6f252024))
* **api-sdk:** regenerate REST SDK with path & enum changes and adapt web layer ([d7cfbe3](https://github.com/gameguild-gg/gameguild/commit/d7cfbe3bdfd04aae62b4c6504b4f959ab603671a))
* **products-graphql:** add authenticated product queries and diagnostic auth logging ([082a104](https://github.com/gameguild-gg/gameguild/commit/082a104ae2a04208777365f27963a5167c52f3be))
* **products-graphql:** integrate Program relationships into Product schema ([33ba038](https://github.com/gameguild-gg/gameguild/commit/33ba038db432f4a4abe462ea817da4ee0f5c2e97))
* **programs-graphql:** add Program GraphQL module with queries, mutations, type definitions, and DI registration ([a4bb9f0](https://github.com/gameguild-gg/gameguild/commit/a4bb9f0276716cd500ef98c4166fd27fcd70a2bd))
* **graphql-security:** support batched GraphQL operation introspection in security middleware ([d97cd0f](https://github.com/gameguild-gg/gameguild/commit/d97cd0f0847283ee6dad9876055444bb48b09d02))
* **observability:** bind OpenTelemetry options and adjust development exporter defaults ([8ab3d31](https://github.com/gameguild-gg/gameguild/commit/8ab3d31b49f50273063908c98acc0693520a0e9a))
* **cors:** enhance CORS policy logic and apply to GraphQL endpoint; add development CORS config ([0bba7a5](https://github.com/gameguild-gg/gameguild/commit/0bba7a5a2b7eb9e532e1987084f5b106147685e7))
* **json:** introduce centralized JsonSerializerConfiguration and apply across API, SignalR, OAuth, tests ([4bec959](https://github.com/gameguild-gg/gameguild/commit/4bec9595a56b475b968ddb120980924cf8e62b95))
* **config:** introduce unified security, auth, rate limiting, and validation settings per environment ([947f6c9](https://github.com/gameguild-gg/gameguild/commit/947f6c9cafa8237b8613601ae3db119f5800092f))
* **logging:** bootstrap Serilog early and enable structured request logging ([9e3a78a](https://github.com/gameguild-gg/gameguild/commit/9e3a78a01a908d6062aa2aba115abc8eb16b9b71))
* **auth/mfa:** align controller with new service API and safer user handling ([0220493](https://github.com/gameguild-gg/gameguild/commit/02204935c5ba166ab76a044da20492dc0cf39c40))
* **auth:** modernize authorization configuration ([9ae5cf3](https://github.com/gameguild-gg/gameguild/commit/9ae5cf3340f69ddc2ccce4ddc527ad5f0a2f8cd8))
* **telemetry:** refine OpenTelemetry resource config and align instrumentation with available APIs ([c18113d](https://github.com/gameguild-gg/gameguild/commit/c18113db2ad3cf4e0ab54447be151ceab48a694a))
* **auth:** tenant token enrichment respects expiration and emits permission claims ([ae77496](https://github.com/gameguild-gg/gameguild/commit/ae77496130bf961e024ddb6289ce1d0701ccf590))
* **features:** integrate OpenFeature DB provider and adjust feature flag schema ([2e74b97](https://github.com/gameguild-gg/gameguild/commit/2e74b97e033611dc8836301a4c70344a94dc6a51))
* register Resources, Tenants, and Projects modules in DI ([20895a6](https://github.com/gameguild-gg/gameguild/commit/20895a6ce36cd9fa4ace7230c45c15d9e383d0f8))
* **subscriptions:** enhance UserSubscription domain and service behaviors ([61d38e1](https://github.com/gameguild-gg/gameguild/commit/61d38e102d51d7c22db73c9ad8f99dd1a1125a65))
* **programs:** expand IProgramService contract with detailed operations and docs ([8566034](https://github.com/gameguild-gg/gameguild/commit/8566034776ff9c2e098b03c32e122deae3f6fdb5))
* **features:** add DatabaseFeatureFlagProvider backed by EF Core ([cc0c958](https://github.com/gameguild-gg/gameguild/commit/cc0c958a377716beadef7f8b8d76202a1364ad02))
* **users:** increase balance precision and align EF configuration; improve create handler ([997f255](https://github.com/gameguild-gg/gameguild/commit/997f255fb8e2c21998786616a3b5f00f8b421cef))
* **notifications:** consolidate module namespaces, split DTOs/enums, add service and controller, register DbSets ([1609fa9](https://github.com/gameguild-gg/gameguild/commit/1609fa9c9da84387354a41f8cd9f86b1a96d9100))
* **programs:** adopt GameGuild.Authorization namespace in GraphQL ([540f614](https://github.com/gameguild-gg/gameguild/commit/540f614c78287b2fc2cf4e190bc92c6f07689c32))
* **core:** move IValidator to Core.Behaviors and return Result with FluentValidation context ([a4fdc31](https://github.com/gameguild-gg/gameguild/commit/a4fdc31b54cedebe02854e2133a8d5fc3596daa9))
* **core,cqrs:** introduce legacy ValidationError types and deprecate ValidationResult in favor of unified Error/Result ([5d2f419](https://github.com/gameguild-gg/gameguild/commit/5d2f4198740e4affde611e0224fa992bd85d958b))
* **core:** add UnifiedExceptionHandler and relocate GlobalExceptionHandler to Core/Exceptions ([269ea9a](https://github.com/gameguild-gg/gameguild/commit/269ea9a935be66eb96af7881efecd10edf035258))
* **core:** add ASP.NET Core route parameter transformers (kebab/slug/snake) and refine ToKebabParameterTransformer ([80f8d5b](https://github.com/gameguild-gg/gameguild/commit/80f8d5b01a7b8a2448c042d46138f12b6e159b29))
* **core:** add ToUniqueSlugCase extension and update SlugCase docs ([ec4c8fa](https://github.com/gameguild-gg/gameguild/commit/ec4c8faa8058ddd6e3600a3e4ddb3910ea0b3c4c))
* **seeding:** add database seeder and contract ([606146d](https://github.com/gameguild-gg/gameguild/commit/606146df94ada3460b1d3204a52d477c2237d788))
* **permissions:** implement DAC resolver and permission services; update DbContext and seeders ([85c1811](https://github.com/gameguild-gg/gameguild/commit/85c1811aaeffed9cce265e35afd47a2e3a8187a6))
* **infra:** Cloudflare Dynamic DNS service and hosted worker ([b6cee86](https://github.com/gameguild-gg/gameguild/commit/b6cee869b15a2306c02ae7971f6bc66dce622226))
* add global exception handler ([8555164](https://github.com/gameguild-gg/gameguild/commit/85551644675ed8dc2425960a0ff09695135ea4d1))
* **ef:** add ModelBuilder extensions for base entities and soft delete filters ([17fedbb](https://github.com/gameguild-gg/gameguild/commit/17fedbbd71ba739e8aee2c715ee446fedc88ae3e))
* **identity:** add HTTP-based context implementations, adapters, and request logging middleware ([654eea9](https://github.com/gameguild-gg/gameguild/commit/654eea9d32a5abc9647cb1239fa10e4c32456358))
* **permissions-domain:** add DAC/Module permission contracts and permission entities ([426697a](https://github.com/gameguild-gg/gameguild/commit/426697a1592cce42aa533a72c72f53fe02dbc0b9))
* **identity:** introduce core identity context interfaces and remove legacy duplicates ([da32cfc](https://github.com/gameguild-gg/gameguild/commit/da32cfcb4a6a88c115280bb84458e99e777b0fec))
* **web:** Group game publishing content by platform in mock data ([2c6cd95](https://github.com/gameguild-gg/gameguild/commit/2c6cd95b044382f757456a17bd4fc40b830072d5))
* **web:** Add nested content for 'Gamedev Issues' in mock data ([c051067](https://github.com/gameguild-gg/gameguild/commit/c0510671c48775f78ce58e62eea05653bba055b7))
* **web:** Refactor course content sidebar for hierarchical display ([0898092](https://github.com/gameguild-gg/gameguild/commit/08980928f7c4826f150544a185409f8bd0d97297))
* **api:** Add TestingLab adapter services ([407fdde](https://github.com/gameguild-gg/gameguild/commit/407fddeebfbb8e4e27882aa8a5286a30e595e73d))
* **api:** Add TestingLab repositories ([e0bba37](https://github.com/gameguild-gg/gameguild/commit/e0bba376e2def4a79844c7212b0d3687c19985df))
* **auth:** add CQRS using to notification classes and standardize UserSignedUpNotification formatting ([cd7249f](https://github.com/gameguild-gg/gameguild/commit/cd7249f1ac608f4c0cb88188800f798b96bada15))
* **web:** Add game development issues content ([06466e6](https://github.com/gameguild-gg/gameguild/commit/06466e63f878cca5ba571f4f7450ecae73a96753))
* **tenants:** add tenant defaulting and settings integration ([e379af0](https://github.com/gameguild-gg/gameguild/commit/e379af0b9dcfbd82d63d62197cdc741996fce811))
* **common:** introduce request context services and wire up DI + middleware ([c40785f](https://github.com/gameguild-gg/gameguild/commit/c40785f2268530b2f3762619a62e43815dc819d4))
* **features:** integrate OpenFeature SDK and register feature flag services ([ac521ad](https://github.com/gameguild-gg/gameguild/commit/ac521ad7ab80a74022f3aab2345214c25748267a))
* **cqrs:** add PaginatedQuery<TResult> base class and SortDirection enum ([eeeebbb](https://github.com/gameguild-gg/gameguild/commit/eeeebbbd41e60c4f99db5ad2967c8c48a55ae3d5))
* **users:** extend User entity with phone, last seen, and domain helpers ([8d6ac59](https://github.com/gameguild-gg/gameguild/commit/8d6ac596d24a47a97d858763518e67a634aac7b9))
* **tenants:** enhance Tenant entity with slug index, fields, and helpers ([c99a7b1](https://github.com/gameguild-gg/gameguild/commit/c99a7b1c77745c03a1e4df66644d2075fc81bb20))
* **subscriptions:** add repository, CQRS commands/handlers, controller, events, and DI registration ([cac1bde](https://github.com/gameguild-gg/gameguild/commit/cac1bde720c37962485d1ce9b2bbf78745b205ce))
* **payments:** add advanced payment commands, result models, and service interface ([dcb1c5c](https://github.com/gameguild-gg/gameguild/commit/dcb1c5ca990f4a3553459bdebb19384d25b1ebce))
* **notifications:** add in-app notifications with preferences and bulk operations ([9005744](https://github.com/gameguild-gg/gameguild/commit/90057446c3dc4becb1906a3a033186e26b4d9803))
* **features:** add feature flags system with targeting, caching, and analytics ([dfac125](https://github.com/gameguild-gg/gameguild/commit/dfac125fb7cddfca86fd57a84e9dcd6a31b0e8a9))
* **resources:** introduce tenant resource quotas, usage tracking and admin APIs ([44c577d](https://github.com/gameguild-gg/gameguild/commit/44c577d2c91d34649bf0167046a7ca358f7dc428))
* **billing:** add webhook processing pipeline for Stripe and PayPal ([a97a29d](https://github.com/gameguild-gg/gameguild/commit/a97a29d842f3d0014fe88175741f0b8ec03539e1))
* **db:** add DbSets for billing, feature flags, and resource quotas; move FinancialTransaction DbSet; add required usings ([5fc5a18](https://github.com/gameguild-gg/gameguild/commit/5fc5a18eda2cbf81dbb2a06e068b050f9369fad7))
* **core:** introduce CustomResults for ProblemDetails mapping ([a52798d](https://github.com/gameguild-gg/gameguild/commit/a52798da569ef8d446314ad17df8527aad01a896))
* **core:** add domain error types (Error, ErrorType, ValidationError) ([2e2f7c3](https://github.com/gameguild-gg/gameguild/commit/2e2f7c3f9e2b8957a2b5403eb310101a7ab58952))
* **core:** register external module services in AddExternalServices ([6e34afa](https://github.com/gameguild-gg/gameguild/commit/6e34afa406902300c8584f25a0f1e3557339afc7))
* **subscriptions:** register repository and add required usings ([822d1e6](https://github.com/gameguild-gg/gameguild/commit/822d1e655548275c5cc815cd05f94b8bc50b9dea))
* **apps/web:** Add react-query client setup and project list page with updated dependencies ([a64b92c](https://github.com/gameguild-gg/gameguild/commit/a64b92ccfc307c352cbd6c1feac61f1a7eaafa80))
* **web:** Adds game publishing course content ([cf9ad40](https://github.com/gameguild-gg/gameguild/commit/cf9ad40642d41cd8ee614b8eed7b6ce3e346e8cd))
* **kyc:** add KYC provider and verification status enums ([74279eb](https://github.com/gameguild-gg/gameguild/commit/74279ebe0b0a8baaa3ade913b2a0b0001fc5b491))
* **subscriptions:** add subscription types/intervals/statuses, acquisition/access, and promos ([d1d391e](https://github.com/gameguild-gg/gameguild/commit/d1d391eff63c84da676c9fd11819ea489d91c45e))
* **payments:** add gateways, methods, transaction types/statuses, and wallet status ([ffaa365](https://github.com/gameguild-gg/gameguild/commit/ffaa36571eb43acd295e6cc91d00c8fc89a6893a))
* **feedback:** add FeedbackFormQuestionType enum ([7a0f111](https://github.com/gameguild-gg/gameguild/commit/7a0f11180edf4d8d69b47856645e8bf079e82031))
* **content:** add visibility, moderation, development, progress, and project enums ([91aa253](https://github.com/gameguild-gg/gameguild/commit/91aa253a5f85e87435b0c1fb1ae95e4ccf666d7d))
* **learning:** add program and product learning domain enums ([362f6fe](https://github.com/gameguild-gg/gameguild/commit/362f6feeea052678c28bb837390f6c079f954980))
* **certificates:** add certificate enums and verification method ([7db9736](https://github.com/gameguild-gg/gameguild/commit/7db9736695d0fabb9cc5ad3ff0e707add3be1feb))
* **tags:** add TagType, TagRelationshipType, and SkillProficiencyLevel enums ([f526503](https://github.com/gameguild-gg/gameguild/commit/f5265033c0fbb039911f1609659178ebfa3356f2))
* **shared:** add core value objects (Address, EmailAddress, PhoneNumber, Money, BillingCycle) ([576ca9f](https://github.com/gameguild-gg/gameguild/commit/576ca9f849605b69c55f9f81e8ed2a4ca6ee0a37))
* **graphql:** add permission queries and mutations ([a07741c](https://github.com/gameguild-gg/gameguild/commit/a07741cbb07fc23c577cf01fbf6fe32411ad2369))
* **graphql:** introduce 3-layer DAC authorization (attributes, middleware, directive, extensions) ([9299e55](https://github.com/gameguild-gg/gameguild/commit/9299e55da1535207ea4dc28d3b71aedd97c3a3c8))
* **graphql:** add base Query and Mutation root types ([8fd5c6a](https://github.com/gameguild-gg/gameguild/commit/8fd5c6a3fc79284f025029ed27eb1be542a0c5c5))
* **web:** Adds game development careers content ([4e89619](https://github.com/gameguild-gg/gameguild/commit/4e8961979352c25984a9ce6a31cbc85c0c79b888))
* **ai4games:** Integrate 'State Machines' content into course mock data ([dde83d5](https://github.com/gameguild-gg/gameguild/commit/dde83d539a6b97e965904c1a5046c073d20ed8d6))
* **ai4games:** Add 'State Machines' lesson content ([741c1d5](https://github.com/gameguild-gg/gameguild/commit/741c1d5ef503b81e1372cec2fedcf8442ef2a79a))
* **web:** Enhances testing lab UI and functionality ([cdcad06](https://github.com/gameguild-gg/gameguild/commit/cdcad0623ad0c2bf7dd3e7e13a4d0a85e7b2142d))
* **apps/web:** Add new API routes and remove deprecated session lookup action ([b0f20e6](https://github.com/gameguild-gg/gameguild/commit/b0f20e6313cb05ebd3526fa667cbb1580f28c6e8))
* **apps/web:** Add action to fetch testing session by slug ([8805052](https://github.com/gameguild-gg/gameguild/commit/88050528a829efc72d71b08142a9edc497c60541))
* **apps/web:** Improve project tags handling and enhance update debugging ([a50b72c](https://github.com/gameguild-gg/gameguild/commit/a50b72c9a54d0a58629276a2ee27ba915478bbfa))
* **apps/web:** Add full project management with API integration and detail enhancements ([5b00ea8](https://github.com/gameguild-gg/gameguild/commit/5b00ea836955847a01e53508f430c9ebaf1fe7be))
* **courses:** Adds intro to game dev tools content ([4a61039](https://github.com/gameguild-gg/gameguild/commit/4a61039887463ca8e78c55c9eaae1b8d60d8d135))
* **apps/web:** Add version management and enhanced testing session handling in project details ([46be10d](https://github.com/gameguild-gg/gameguild/commit/46be10dadcaf145d31f946ecc0583268060a18db))
* **apps/web/components/profile:** Add new SubmitForTestingSheet component with enhanced UI and filtering ([2261c89](https://github.com/gameguild-gg/gameguild/commit/2261c89e67ad85a3c69c59e18e895f7324690677))
* **apps/web:** Add comprehensive project management and testing lab features ([d513a09](https://github.com/gameguild-gg/gameguild/commit/d513a0985669c8a00e89da4221ed4b0b8abf822a))
* **apps/web:** Update testing lab sessions page to use new session fetch action ([a3214d7](https://github.com/gameguild-gg/gameguild/commit/a3214d722d8b39f405d9cb17f9ede5f9067446db))
* **apps/web:** Add tenant detail page with tabs and loading states; enhance user detail routing and lists ([e1c8a9a](https://github.com/gameguild-gg/gameguild/commit/e1c8a9ae1c520e5c391b6a71397c4bfb0035ecee))
* **apps/web/components/testing-lab/testing-sessions-list:** Add TestingSessionsList component with filtering, pagination, and UI ([8b0a818](https://github.com/gameguild-gg/gameguild/commit/8b0a818ee45d92db318616b6af0782f3e658108f))
* **apps/web/lib/admin/testing-lab/sessions:** Replace sample data with API calls for testing sessions ([9ad4c16](https://github.com/gameguild-gg/gameguild/commit/9ad4c16a045d21a949e54239832b3a231ade82cc))
* **apps/web:** Replace testing session API with server actions and add tenant tab placeholders ([c705795](https://github.com/gameguild-gg/gameguild/commit/c705795ae236e6a0894dec7dbabcbdd8b293a172))
* **apps/web/src/lib/admin/testing-lab:** Refactor testing session actions with mock data and simplified API usage ([43d0a63](https://github.com/gameguild-gg/gameguild/commit/43d0a63466760a677c2730698a840a04cb573c6c))
* **apps/web/src/app/[locale]/dashboard/testing-lab/sessions:** Replace TestingSessionList with new TestingSessionManagementContent and update data fetching ([2e55ce3](https://github.com/gameguild-gg/gameguild/commit/2e55ce3dcee73ca6759e15da630f64736355ccbd))
* **apps/web/components/testing-lab:** Export TestingSessionsList from index for easier imports ([dd5ada8](https://github.com/gameguild-gg/gameguild/commit/dd5ada82f1ff4ba331a1be4842e4519b8078dbb7))
* **apps/web/components/testing-lab:** Add TestingSessionManagementContent component as session management wrapper ([cf96b1e](https://github.com/gameguild-gg/gameguild/commit/cf96b1ea4d1b4111a4acd4b78af55dc2f8acd23e))
* **apps/web/components/testing-lab:** Add TestingSessionsList component with filtering, sorting, and pagination ([5aa4df1](https://github.com/gameguild-gg/gameguild/commit/5aa4df1b0ad83f46a153ca33834e35087c2830f6))
* **apps/web/lib/admin/testing-lab:** Add server actions for fetching and searching testing requests ([7d312bd](https://github.com/gameguild-gg/gameguild/commit/7d312bdb5f49d061d55f0344a73043bc4dd8448f))
* **apps/web/components/testing-lab:** Add TestingRequestsList component with search, sort, pagination, and table UI ([550e382](https://github.com/gameguild-gg/gameguild/commit/550e3824a14f419262f10753c508cb6d2c5ed31f))
* **apps/web:** Migrate achievements page to use server-side data fetching ([1c7d340](https://github.com/gameguild-gg/gameguild/commit/1c7d340294f361f8de2262eeeb473b1b5d0ed658))
* **apps/web:** Add achievements and user management components with tenant management update ([9127412](https://github.com/gameguild-gg/gameguild/commit/912741205d5fe090463b833c0872c6b390fd49e9))
* **apps/web:** Add TenantManagementContent component with tenant refresh capability ([9b5af1c](https://github.com/gameguild-gg/gameguild/commit/9b5af1cbb5896468e3712f1f99d76c5caa24b91a))
* **apps/web:** Enhance TenantsList component with filtering, sorting, and pagination ([0e5f911](https://github.com/gameguild-gg/gameguild/commit/0e5f911935b74eb90fd45b1e3da01c321518fb63))
* **apps/web/components/users:** Add new simplified user list component ([34420e3](https://github.com/gameguild-gg/gameguild/commit/34420e3c4468938762a74062976dd50212434a49))
* **apps/web:** Add user management and testing request approval workflow ([85319b0](https://github.com/gameguild-gg/gameguild/commit/85319b0cbf0cd00fc3d95257eaf101d79637ab22))
* **apps/web:** Add detailed logging and fallback handling in UsersPage ([2ce288f](https://github.com/gameguild-gg/gameguild/commit/2ce288f41fe490f6b290447de95a1f8ae7e2b973))
* **apps/web:** Add comprehensive testing lab settings and enhanced user management UI ([fbcab49](https://github.com/gameguild-gg/gameguild/commit/fbcab491010ee8dabc42aa90e6be07be2bed9b01))
* **mock-data:** Integrate DSA expectations and update sort orders ([45a7d58](https://github.com/gameguild-gg/gameguild/commit/45a7d588aa610ebde2c2e9db5b3745d14d2734bd))
* **mock-data:** Integrate AI for Games expectations into mock data ([99308cc](https://github.com/gameguild-gg/gameguild/commit/99308cce7a9dbb4142b6ada94d5f3bd04fbc5e2f))
* **dsa:** Add week 01 expectations content file ([c27a23a](https://github.com/gameguild-gg/gameguild/commit/c27a23ae0692811a59fd7a3b81e9b0ff381f78ee))
* **ai4games:** Add week 01 expectations content file ([346e24b](https://github.com/gameguild-gg/gameguild/commit/346e24b935af65a52ce7fe661e8ee45817f1166b))
* **apps/web:** unify testing session data and update session UI links and details component ([5da3bdb](https://github.com/gameguild-gg/gameguild/commit/5da3bdbcff70e75d9678cefeb71e64bb59129c36))
* **apps/api:** Add public testing sessions endpoint and improve TestingLab API consistency ([bdf97eb](https://github.com/gameguild-gg/gameguild/commit/bdf97ebbe2e1e92522a0e1bfa6b10958b49dd23f))
* **academic-honesty:** Introduce Academic Honesty Policy page ([3835957](https://github.com/gameguild-gg/gameguild/commit/3835957e7dfdd385515dbec20dd0dd5bab45244d))
* **apps/web:** Add dashboard layout with header, sidebar, and page components ([d66f9ff](https://github.com/gameguild-gg/gameguild/commit/d66f9ffe742c3dc37b19377fadf1066fac376dcf))
* **apps/web/components/auth:** Refactor auth forms and update exports structure ([14d2c5e](https://github.com/gameguild-gg/gameguild/commit/14d2c5ea51e5a31ec404011d93b4acd5d1d396aa))
* **apps/web:** Add global providers, zustand store, and query client integration ([1bd21e0](https://github.com/gameguild-gg/gameguild/commit/1bd21e05e4edb92eea02799e99f7b15b608bdee0))
* **apps/api:** Configure GraphQL options based on environment variables ([069d5ca](https://github.com/gameguild-gg/gameguild/commit/069d5ca5db09620a618d5300bf3ad247d329e186))
* **apps/api:** Add TestingLab services, repositories, and dependency registrations with improved service initialization order ([aab436c](https://github.com/gameguild-gg/gameguild/commit/aab436c1501f8c6255ac411d99ab52a3dac7802d))
* integrate DSA course into mock data system ([113cdfd](https://github.com/gameguild-gg/gameguild/commit/113cdfd36efea9b5461b13ecbd1e32ad11d2e1c4))
* add Data Structures and Algorithms course content ([64c3161](https://github.com/gameguild-gg/gameguild/commit/64c316179aacdc09df0d6fa01fbfd02eaaef1fb4))
* **apps/api:** Revamp TestingLab module with new entities, services, commands, queries, and permission management ([81dc6cc](https://github.com/gameguild-gg/gameguild/commit/81dc6ccab8ca4ca4c9feae93236c64996975b2a4))
* **apps/api:** Add queries for testing lab data retrieval ([4a4985a](https://github.com/gameguild-gg/gameguild/commit/4a4985ae9b39acfed2540e981dcd48e96a600daf))
* **apps/api:** Add handlers for testing requests and sessions with event publishing ([1a306cb](https://github.com/gameguild-gg/gameguild/commit/1a306cb0acefdb44355aca4f42c957484a75895d))
* **apps/api:** Add testing lab event notifications for feedback, requests, and sessions ([8e23d4c](https://github.com/gameguild-gg/gameguild/commit/8e23d4cbc08dcb41041ccb44a4593172aa47b1df))
* **apps/api:** Add comprehensive TestingLab module with entities, enums, and permissions ([bbc5b73](https://github.com/gameguild-gg/gameguild/commit/bbc5b7318da1e9888815d32bcdec9780be1199fc))
* **apps/api:** Add commands for managing testing requests and sessions ([3ddf1b2](https://github.com/gameguild-gg/gameguild/commit/3ddf1b267fdbd86f230577857cd80edcfa74e6e5))
* **apps/api:** Add comprehensive TestingLab module abstractions and service interfaces ([1b0828b](https://github.com/gameguild-gg/gameguild/commit/1b0828b3c6aec34ccdb58534156a245c6824cab2))
* **apps/api:** Add validators for TestingLab commands ([3798695](https://github.com/gameguild-gg/gameguild/commit/3798695ea4e41fb5a10d9351840b31dc0a7c3c83))
* Enable word wrap in VS Code settings ([b35a6ec](https://github.com/gameguild-gg/gameguild/commit/b35a6ecc2275c1b2880db5967f7ed68ecb3efdcc))
* **apps/web:** Add testing session sidebar and improve layout structure ([4f56e77](https://github.com/gameguild-gg/gameguild/commit/4f56e77d21bd646bbaa107b5be9c8b3eadab16c8))
* **apps/web:** Restructure testing-lab section with new layout and landing page components ([52654d1](https://github.com/gameguild-gg/gameguild/commit/52654d1e219f827bfd4dcaba06ccf7ec26a659f6))
* **apps/gglexical:** Dynamize feature lists in navigation cards ([be5d338](https://github.com/gameguild-gg/gameguild/commit/be5d33855e01d25cf4f94c568bfb0cdb27b8f9b5))
* **apps/web:** Centralize HTML, font, and ThemeProvider in root layout ([8f14596](https://github.com/gameguild-gg/gameguild/commit/8f145961f7564b877f64b782efdc972d22b2c1e8))
* **apps/web:** Make not-found page standalone with html structure ([d16b00a](https://github.com/gameguild-gg/gameguild/commit/d16b00aeaa6b3934ec92b7b933b809daa2f935be))
* **apps/web:** Make global-error page standalone with html structure ([fb71535](https://github.com/gameguild-gg/gameguild/commit/fb715356f87a74e62c58a421d93f16d9eeadbd09))
* add redirect from /p to /programs ([378fadc](https://github.com/gameguild-gg/gameguild/commit/378fadcd334c5c498683547caf59d30b3f8e3a03))
* restructure course routes and cleanup empty files ([ed7c4c4](https://github.com/gameguild-gg/gameguild/commit/ed7c4c42b9f24462193cf8abd6cf2594bc5f479f))
* add GitHub issue modals to auth links and remove sign-up div ([0a79d0f](https://github.com/gameguild-gg/gameguild/commit/0a79d0f83ddccecc146318fcb85b0e952bf5080e))
* integrate GitHub fork button into default header ([0e48290](https://github.com/gameguild-gg/gameguild/commit/0e48290520c309019d4b3c32f50025b915ad2031))
* add GitHub fork button component ([649af32](https://github.com/gameguild-gg/gameguild/commit/649af326636e6c1be94c1bafa41d8797fe67a099))
* enhance course content sidebar with theme toggle and header ([3d3234e](https://github.com/gameguild-gg/gameguild/commit/3d3234edc423562a29c21a01e3b8188ba031e97b))
* restructure course content routing with new layout ([1893d0e](https://github.com/gameguild-gg/gameguild/commit/1893d0e214111c38d73eb756d51dae34d704e395))
* add responsive sidebar infrastructure for course content ([0ec4fac](https://github.com/gameguild-gg/gameguild/commit/0ec4fac254768ccb8de4a631f0fed3eb576c1922))
* **courses:** add AI for Games course content and update mock data ([b54825a](https://github.com/gameguild-gg/gameguild/commit/b54825a748b009f20e0f8c49902fd340d547545b))
* add slug field to ProgramContent model ([e124989](https://github.com/gameguild-gg/gameguild/commit/e124989da8afdd60f4393f6475987c02fead65c5))
* add slug field to ProgramContent model ([b644747](https://github.com/gameguild-gg/gameguild/commit/b644747a50d153be0a1435cc26f5d5da82b6b2da))
* replace ForbidResult with custom PermissionDeniedResult for better error messaging ([bc4d002](https://github.com/gameguild-gg/gameguild/commit/bc4d002705c9717eeb9d28405e3eb04eccde9983))
* fix post-login UI state update and add password visibility toggle ([8e8c677](https://github.com/gameguild-gg/gameguild/commit/8e8c67776b36ad8e5fbc074710668af1d544b9ef))
* add GitHub issue modal to Terms of Service, Privacy, and Cookies links ([cc47342](https://github.com/gameguild-gg/gameguild/commit/cc473429cffb2d87599e0df0f1591fa3389e7236))
* update licenses page to fetch from GitHub API with caching and remove SPDX identifier ([7065633](https://github.com/gameguild-gg/gameguild/commit/706563356ab5bdb8171135a30f4157d0c8cfc251))
* restructure licensing with separate license files ([26467b6](https://github.com/gameguild-gg/gameguild/commit/26467b67d6cdb8bc066b4b29193a8f9e23a2bd57))
* implement fail-safe GraphQL codegen to prevent build failures ([3b19128](https://github.com/gameguild-gg/gameguild/commit/3b191282948898da60c3a2100a45a41972c11c3e))
* **github-issues-modal:** add a new modal to allow users to report issues directly to github ([fb1f976](https://github.com/gameguild-gg/gameguild/commit/fb1f976cd9b8139d79fb12e68311db10460d6941))
* **apps/web:** Add legacy route rewrites for projects ([c125822](https://github.com/gameguild-gg/gameguild/commit/c125822f8b4dc256559dafa2baf89120626490b4))
* **apps/web:** Add legacy redirects and update routing ([b1793de](https://github.com/gameguild-gg/gameguild/commit/b1793def0a55f69749947e06357e30a7ea157db9))
* **apps/web:** Add Versions page to manage game builds and show upload history ([ed8d846](https://github.com/gameguild-gg/gameguild/commit/ed8d8460f16c122ef450b18b9669e010c6721235))
* **apps/web:** Add Store Presence page for managing project store info and media ([a9b2d6a](https://github.com/gameguild-gg/gameguild/commit/a9b2d6a44fa1f34f280857d30e0ee52c5934e9ac))
* **apps/web:** Add Team page to manage and invite project members ([6d15edd](https://github.com/gameguild-gg/gameguild/commit/6d15eddfc363543b129de0ce332ce955e2f58d2f))
* **apps/web:** Implement detailed project overview page with stats and activities ([279299c](https://github.com/gameguild-gg/gameguild/commit/279299c05c1d9491a31df4c83ab491ed6817ed02))
* **apps/web:** Add project route layout with context provider and navigation ([3aa53cf](https://github.com/gameguild-gg/gameguild/commit/3aa53cf8555133d6470a70aad85f62113a83fc53))
* **apps/web:** Integrate content management API for projects ([723761f](https://github.com/gameguild-gg/gameguild/commit/723761f4dc2884831eae988a638f5a178f0e150b))
* **types:** Define Course and related types for course management ([a5861cb](https://github.com/gameguild-gg/gameguild/commit/a5861cb67d0a745f47ea49cbca964e2c846b653c))
* **local-db:** Implement local storage for game projects and courses ([20a3fd7](https://github.com/gameguild-gg/gameguild/commit/20a3fd7c6cad160270580912ec7a08897ff0ad41))
* **content-management/programs:** Add program-to-course transformation utility ([9176960](https://github.com/gameguild-gg/gameguild/commit/91769608cdb310f16d575ec37e1a52062a88a4db))
* **apps/web:** Implement CourseCard for program details in list and grid view ([d635b75](https://github.com/gameguild-gg/gameguild/commit/d635b753f2f01633a3a1a22a6dfe91e33764dda0))
* **apps/web:** Add CourseCard component with default and compact views ([8f06574](https://github.com/gameguild-gg/gameguild/commit/8f0657491cc476875e5c3455237d3d44cc5324ea))
* **apps/web:** Update courses and projects dashboard pages ([97ff75d](https://github.com/gameguild-gg/gameguild/commit/97ff75dd5cfe248402547ab8e978ea3f3eee3778))
* **projects:** Integrate new project management dialog in projects page ([881be20](https://github.com/gameguild-gg/gameguild/commit/881be209354da36187f75910a4e88de5e2c35dac))
* **courses:** Add course card and filtering mechanisms ([0ce270f](https://github.com/gameguild-gg/gameguild/commit/0ce270f17980a1f68cc2f7531809d31ca9811fa1))
* **courses:** Create initial course page with mock data ([5185c7e](https://github.com/gameguild-gg/gameguild/commit/5185c7ec35936fed60be0278283949ef744746b4))
* **apps/api:** Add CertificateDtoMappings for entity to DTO conversion ([c0da78d](https://github.com/gameguild-gg/gameguild/commit/c0da78d00f8422b2848cdeaa39da7980e238c3d9))
* **apps/api:** Add AddCertificateTagDto for managing certificate tags ([5f3f79d](https://github.com/gameguild-gg/gameguild/commit/5f3f79d30ca97b7e767ba918a2c096510cdc1227))
* **apps/api:** Add CreateCertificateDto and UpdateCertificateDto for certificate data transfer ([514a898](https://github.com/gameguild-gg/gameguild/commit/514a898a54629b9a4bc1c3af09183fc8130a2354))
* **apps/api:** Implement ProgramCertificatesController for managing certificates ([35961fa](https://github.com/gameguild-gg/gameguild/commit/35961fa2c060ac0aa522a137dbb34aa39bbbcff8))
* **apps/web/users:** Add top-right profile action buttons ([a445f66](https://github.com/gameguild-gg/gameguild/commit/a445f6606c5095c146d6b83cfc0266d3a5b773ba))
* **apps:** Update GraphQL resolvers, schema and client auth ([4a43e4f](https://github.com/gameguild-gg/gameguild/commit/4a43e4f7c2248bdc732a2dcf2db262516a109f80))
* **apps/web:** Enhance user lookup with auth fallback ([d4d5985](https://github.com/gameguild-gg/gameguild/commit/d4d59853663257bc271ec29a38b951ddeba43e0e))
* **users:** Improve search matching and profile redirection ([c6d42d0](https://github.com/gameguild-gg/gameguild/commit/c6d42d06c28bc8fcafd6b063406d902bcbaa2781))
* **apps/web:** Add user profile page with username lookup ([3729b58](https://github.com/gameguild-gg/gameguild/commit/3729b58047c160838fe544e62b312f14f07c383a))
* **apps/api:** Add username migration files ([4cf5c99](https://github.com/gameguild-gg/gameguild/commit/4cf5c998df85ebf2a25462bac45204bb549c216a))
* **apps/api:** Implement unique username migration and update handler ([6f61680](https://github.com/gameguild-gg/gameguild/commit/6f61680b4b308249291cbefbef034b830c8c7408))
* **apps/api:** Add unique username field with auto-generation support ([a9d9557](https://github.com/gameguild-gg/gameguild/commit/a9d95575f51768e4a6b7252ca4f1914fab0a9e83))
* **apps/web:** Normalize session status display in detail component ([c052c68](https://github.com/gameguild-gg/gameguild/commit/c052c683c707ec0ac393f2e1600a01e0df0f8a7c))
* **apps/web:** Add user form component ([5e1fc18](https://github.com/gameguild-gg/gameguild/commit/5e1fc186026ef5f7b880689c479ea7c0f2acb74b))
* **apps/web:** Add GraphQL, feed & tenant tabs ([cc80d95](https://github.com/gameguild-gg/gameguild/commit/cc80d959f0d357653c693ede27388360871510be))
* **apps:** Add project version endpoints and UI improvements ([9eee0cd](https://github.com/gameguild-gg/gameguild/commit/9eee0cdbfff523cc8a2763f59a141cb94134ab14))
* **apps/web:** Enhance role editing with aggregated permissions ([6ef7b7b](https://github.com/gameguild-gg/gameguild/commit/6ef7b7b5a14128d17b10002fd40bfecc862c8b68))
* **api/web:** Update role template endpoints for id/name support ([daac91f](https://github.com/gameguild-gg/gameguild/commit/daac91fc9ae920fb7f52f1b27115f529b2c7ff21))
* **auth:** Improve refresh token rotation, logging and DB schema ([0a7576c](https://github.com/gameguild-gg/gameguild/commit/0a7576ce21148648d7fd9aca49b51e36dba0d0c2))
* **apps/web:** Expand role permissions mapping conversion ([0e927c4](https://github.com/gameguild-gg/gameguild/commit/0e927c4b172b9f9a71730d5affea1902c334308e))
* **permissions:** Add SimplePermissionService and default permission template handling ([82c47f3](https://github.com/gameguild-gg/gameguild/commit/82c47f32f52bf478c430a0756b45db55b11beb04))
* **apps/web:** Add testing lab roles actions and new dashboard header ([206cfa1](https://github.com/gameguild-gg/gameguild/commit/206cfa1b9f0b412e41d911ad51fe10897b54d60c))
* **apps/web:** Add testing lab roles and user role endpoints ([65e294e](https://github.com/gameguild-gg/gameguild/commit/65e294e6a193b7d11744019fd750263cef172792))
* **apps/web:** Improve testing lab location management ([3459cb2](https://github.com/gameguild-gg/gameguild/commit/3459cb2501c757c407c6af58fc37d91acf00bc4c))
* **apps:** Enhance token expiry, tenant context, and UI ([10cd356](https://github.com/gameguild-gg/gameguild/commit/10cd3565e856bc6910e5e49511bd1fa26ce11fef))
* **apps:** Enhance logging, permissions and UI theme toggle ([d930ecd](https://github.com/gameguild-gg/gameguild/commit/d930ecdd115c11e01b5c9757ee22a879aea22e59))
* **testing-lab:** Add settings module with API endpoints and web integration ([1f95e3c](https://github.com/gameguild-gg/gameguild/commit/1f95e3c4163022944c7ddfa5b14e7eb4d0634eef))
* complete course content integration and fix image loading issues ([b127462](https://github.com/gameguild-gg/gameguild/commit/b1274629e1899ef853e8d055a693d259514a6411))
* **apps/api:** Implement module-based permissions with testing lab support ([1bbb823](https://github.com/gameguild-gg/gameguild/commit/1bbb823b3af538725190989c74946cc02030c91a))
* remove USE_IN_MEMORY_DB and clean up database configuration ([59413ae](https://github.com/gameguild-gg/gameguild/commit/59413ae509df79b45f41e5b0580a8c19556a731c))
* **courses:** add some old courses content ([95cc111](https://github.com/gameguild-gg/gameguild/commit/95cc111afc3c0f945bce5ffdc2c32c10f5e9ac84))
* **web:** New middleware for subdomain-aware i18n routing ([aa96982](https://github.com/gameguild-gg/gameguild/commit/aa969827c2e63d6f50eea72d1a3bd8e36e19d058))
* **apps/web:** Update auth redirects, i18n routing and middleware config ([5879a4e](https://github.com/gameguild-gg/gameguild/commit/5879a4ed6daeb1c46d158f49e4f0425b80db0440))
* **apps/web:** Enhance intl and tenant middleware ([a0c6506](https://github.com/gameguild-gg/gameguild/commit/a0c65062e9b35d2a2d1593a65aa1db0bb4aa9470))
* **markdown:** add markdown renderer ([d7d7679](https://github.com/gameguild-gg/gameguild/commit/d7d76791a43716c16b63878abb806d3ea9de44c6))
* **apps/web:** Add health check endpoint ([5d87723](https://github.com/gameguild-gg/gameguild/commit/5d8772388eafa2e17d2a7e6d4d873a2dd728a646))
* consolidate API generation and improve developer experience ([e445e21](https://github.com/gameguild-gg/gameguild/commit/e445e21fc810572db2ac78c46cca4af1ebf76bcd))
* **apps/web:** Add courses actions, context, and track hook ([48873b4](https://github.com/gameguild-gg/gameguild/commit/48873b4edbb9444daef699da23269859c99116c7))
* **apps/web:** Add posts actions endpoints ([e664479](https://github.com/gameguild-gg/gameguild/commit/e664479de2e244085a2121caf011bcdd91dcccef))
* **apps/web/feed:** Add recent activity and cache refresh actions ([9ec8474](https://github.com/gameguild-gg/gameguild/commit/9ec847499d17aa2342b8ade98a7f038c5420953e))
* **apps/web/testing-lab:** Update UI components and add session form ([4bfcd57](https://github.com/gameguild-gg/gameguild/commit/4bfcd57f1b5fc319291c09897db82e23b764fef2))
* **apps/web:** Add course detail, location selector and create form components ([3b41c50](https://github.com/gameguild-gg/gameguild/commit/3b41c505adfd05b354403704acd4451f75e64f3a))
* **testing-lab:** Add manage permissions button ([b726bdc](https://github.com/gameguild-gg/gameguild/commit/b726bdc1c5cfc4c8419ee367ecf7d483b1b0f50a))
* **tenant:** Add tenant detail view, update auth token and remove legacy managers ([55db845](https://github.com/gameguild-gg/gameguild/commit/55db845a1927f6f4b3ca2b569672798e7ca90a61))
* **admin:** Add user management, roles and analytics features ([6c85f40](https://github.com/gameguild-gg/gameguild/commit/6c85f40c6b3ea1986383438691ebfe5efe5de548))
* **apps/web:** Add authenticated client and generated API client ([e4655a4](https://github.com/gameguild-gg/gameguild/commit/e4655a42e7fca08433b5d22f99b0a00cce9994f7))
* **dashboard:** Revamp courses, achievements, tenants and user detail pages ([111916a](https://github.com/gameguild-gg/gameguild/commit/111916a1e104509689a259fb12bb0f1b7c5b6f7d))
* **apps/web:** Add enhanced permission and notification types ([d2382ca](https://github.com/gameguild-gg/gameguild/commit/d2382caeb8dd9ba7224ed94ef9e848f1710aea93))
* **apps/web:** Add comprehensive server actions across modules ([fa02cba](https://github.com/gameguild-gg/gameguild/commit/fa02cbaf28b5b89d8cba9f86c92aef0b5d01d8d2))
* **apps/web:** Enhance auth token refresh and add achievements actions ([cea92ef](https://github.com/gameguild-gg/gameguild/commit/cea92efa4f4a2cbaee4cd4154890c73bf56c3bae))
* **apps/web/cookies:** Add enhanced consent and preferences UI ([72d78a0](https://github.com/gameguild-gg/gameguild/commit/72d78a0ed824ca11eb92a39d1ffc5d066d3ad747))
* **apps/web/users:** Add dynamic user detail and permissions pages ([ca76f8b](https://github.com/gameguild-gg/gameguild/commit/ca76f8bbc52e8a7c93504ae6dee6651bf954d061))
* **apps/api:** Enhance DAC and module permission management ([4003284](https://github.com/gameguild-gg/gameguild/commit/4003284ca3bac197662a42a102beb5486bf93f6f))
* **apps/web/dashboard:** Update courses page with new API and course list wrapper ([c3ecaf3](https://github.com/gameguild-gg/gameguild/commit/c3ecaf3386ee87c26d26d1eace3fb9547bad63ed))
* **apps/web:** Add cn utility for merging Tailwind classes ([9e1831e](https://github.com/gameguild-gg/gameguild/commit/9e1831eb37e44660e60ba8a6d061346b38c1397b))
* **prettier-config:** Update config settings ([0f4c6e3](https://github.com/gameguild-gg/gameguild/commit/0f4c6e3b2c9c0126de15d75b0fa344c5e8792a6f))
* **apps/web:** Add testing lab sessions UI components ([b87ab09](https://github.com/gameguild-gg/gameguild/commit/b87ab0997ad7400c031fd4baec0a161e4df98b3b))
* **apps/testing-lab/management:** Add UI components for feedback, requests, and sessions ([965296a](https://github.com/gameguild-gg/gameguild/commit/965296a530f3857cc8d227711e03927954a4724d))
* **server-actions:** Add comprehensive modules for activity tracking, commerce, communication and content management ([cf003d3](https://github.com/gameguild-gg/gameguild/commit/cf003d398a7eb27c36280930bf5b7b4ac62a6846))
* **apps/web:** Add tenant domains actions and update token usage ([44c5e65](https://github.com/gameguild-gg/gameguild/commit/44c5e659122efe9a525ae5b94d80d3146833ff55))
* **apps/web:** Add testing feedback SDK actions ([686df2e](https://github.com/gameguild-gg/gameguild/commit/686df2e9a0dbe28244658f88bcbcf2438d8157c7))
* **apps/web:** Add system and database health actions ([7219cd7](https://github.com/gameguild-gg/gameguild/commit/7219cd7a169418c8ecc3daef9a051a029406a424))
* **apps/web-users:** Add SDK actions for users ([3f80b9b](https://github.com/gameguild-gg/gameguild/commit/3f80b9b4011839b90aff3b0a8f37d7a49bc08c48))
* **apps/web:** Refactor project actions with centralized auth client ([67f3a35](https://github.com/gameguild-gg/gameguild/commit/67f3a35160ee44e646c058189a08842a606db801))
* **apps/web:** Add content interaction SDK actions ([5c07ae9](https://github.com/gameguild-gg/gameguild/commit/5c07ae97e757005bb9fea59690be411ebe3cb48c))
* **apps/web:** Add programs SDK actions ([d892b2c](https://github.com/gameguild-gg/gameguild/commit/d892b2cdfcf4e30b1a4f536e3c02d4393221ef52))
* **apps/web:** Add payments SDK action endpoints ([657ce51](https://github.com/gameguild-gg/gameguild/commit/657ce51c3ebf64f68a6b26931cda88c518a729e0))
* **apps/web/posts:** Replace legacy actions with authenticated API calls ([54d9e32](https://github.com/gameguild-gg/gameguild/commit/54d9e32668f0ed42c329747a57b98a8e199e2571))
* **apps/web/activity-grades:** Add API actions for activity grades ([d8939c2](https://github.com/gameguild-gg/gameguild/commit/d8939c2940f5e63ea1183a02fbcb78b23c81739c))
* **apps/web:** Implement achievements API actions ([e37aa3c](https://github.com/gameguild-gg/gameguild/commit/e37aa3c8bfef267e337d0c6a4b6fafdbb92a4549))
* **apps/web:** Add credentials actions for CRUD operations ([6411f65](https://github.com/gameguild-gg/gameguild/commit/6411f659933583516932155c44a0ca3746c0ae1c))
* **apps/web:** Add new subscription actions with API integration ([22b2791](https://github.com/gameguild-gg/gameguild/commit/22b2791de4b6fc80b0a668f7b7fc57cb56853a77))
* **apps/web:** Add health and database status endpoints ([aa64591](https://github.com/gameguild-gg/gameguild/commit/aa645917317c05f936583c66b2191a7580148924))
* **payments:** Add payments actions API endpoints ([e992a93](https://github.com/gameguild-gg/gameguild/commit/e992a93f7b3ab8f4e5313ff6a46ebf90316bb2d3))
* **testing-lab/users:** Add comprehensive user testing actions ([bbf1e65](https://github.com/gameguild-gg/gameguild/commit/bbf1e65e51e9aba92a546ef2e0fd614b40793f57))
* **user-management:** Add comprehensive and enhanced user actions ([c37fe1d](https://github.com/gameguild-gg/gameguild/commit/c37fe1d36ce558f6365dd7352fc808e69b1d921c))
* **apps/web:** Add GitHub license content retrieval function ([60b78d6](https://github.com/gameguild-gg/gameguild/commit/60b78d6c09130deab0ebb444cc4d8b1a83646e22))
* **apps/web/dashboard:** Add modular dashboard overview components ([cc332d6](https://github.com/gameguild-gg/gameguild/commit/cc332d60ddcd93e44016f5a17ca80c6044843733))
* **apps/dashboard:** Add dashboard header and sidebar content components ([93746e2](https://github.com/gameguild-gg/gameguild/commit/93746e22a97379f0adc831b83fd10ca633da18e4))
* **apps/web/legal/licenses:** Implement dynamic license page ([a229aa8](https://github.com/gameguild-gg/gameguild/commit/a229aa82dfc47ce17e49aebc96fa154b10d95bd5))
* **apps/web:** Add not-found page and update dashboard imports ([d6635bb](https://github.com/gameguild-gg/gameguild/commit/d6635bb0a16039b9b96f282d062772aff2bb1c2d))
* **dashboard:** Update analytics and overview pages ([bb95dac](https://github.com/gameguild-gg/gameguild/commit/bb95dac94b0b3224e10bc3478cb19a1556583e80))
* **apps/web:** Integrate GitHub data in stats and update navigation links ([17a2e69](https://github.com/gameguild-gg/gameguild/commit/17a2e6985a738352f118c1c6967f60e63e246a6d))
* **apps/web:** Add user profile and not found pages ([5e31334](https://github.com/gameguild-gg/gameguild/commit/5e313348821713da073dd47da95548bd3ed9143d))
* **analytics/web-vitals:** Enable sendBeacon and fetch reporting ([c1100f1](https://github.com/gameguild-gg/gameguild/commit/c1100f14366a6d878213d2d57bf1e140c6be5b75))
* **apps/web:** Add comprehensive content interaction endpoints ([7a0b82f](https://github.com/gameguild-gg/gameguild/commit/7a0b82f63a4153a55981cb2d292e2c3b3d69d03b))
* **apps/web:** Add activity grading, achievements, profiles, and users modules ([658b18f](https://github.com/gameguild-gg/gameguild/commit/658b18f1b8db68a11ece57f8480c28d1f1a85dfe))
* **payment-commerce:** Add payments, subscriptions and analytics modules ([2aa86f1](https://github.com/gameguild-gg/gameguild/commit/2aa86f1fa93d61e5f7e2ed74c3544421d3696092))
* **testing-lab:** Add comprehensive server actions and reorganize structure ([7b408d4](https://github.com/gameguild-gg/gameguild/commit/7b408d48052828204f6faf5deb7930fedc5a1a8c))
* **apps/web:** Add attendance tracker component ([36056e4](https://github.com/gameguild-gg/gameguild/commit/36056e4910ed97e3b40b76d01035e084e44764bd))
* **apps/web:** Update testing lab pages with placeholders ([ef4ddcb](https://github.com/gameguild-gg/gameguild/commit/ef4ddcb9f771f2e361c8e0f3219f9a77117f3185))
* **apps/web:** Add metadata debug log and update layout imports ([f8dd6fc](https://github.com/gameguild-gg/gameguild/commit/f8dd6fc6dfcc65a8d7e9bae88feaaabf20a778d7))
* **apps/web/components/content/markdown:** Add markdown content component ([3527431](https://github.com/gameguild-gg/gameguild/commit/35274318e994fb1a89781555ba04950c43a476dd))
* **apps/web:** Add MarkdownContent and refactor feedback UI ([6db2d98](https://github.com/gameguild-gg/gameguild/commit/6db2d98521aba5a3ae906e416e4bf30c92684e74))
* **apps/web:** Add legal and social media links components ([cbc0156](https://github.com/gameguild-gg/gameguild/commit/cbc01568e59f68a75602bb0593dad14f87bbbf6b))
* **apps/dashboard:** Remove redundant analytics pages and add refresh button ([85922ee](https://github.com/gameguild-gg/gameguild/commit/85922eec16c03fcca6ffd9bb8c217f8383fb42b7))
* **apps/web:** Update user profile to use session and add tenant switcher ([3a16d9e](https://github.com/gameguild-gg/gameguild/commit/3a16d9eb661697775e0a04375c4e950d36045623))
* **common/header:** Add user profile dropdown and sign-in flow ([2c51ae2](https://github.com/gameguild-gg/gameguild/commit/2c51ae213b45d7ff3bcb5e670c45856f7a546b69))
* **apps/web:** Enhance auth callbacks for profile and tenant updates ([09bab5f](https://github.com/gameguild-gg/gameguild/commit/09bab5f98be0bfa34f8fa211997c7e672763c094))
* **apps/web:** Update token refresh logic for update events ([58839d2](https://github.com/gameguild-gg/gameguild/commit/58839d26f9888e946b54b62ff8d7f55d55d5b5f8))
* **apps/web:** Enhance authentication with tenant and session management ([6d91d87](https://github.com/gameguild-gg/gameguild/commit/6d91d875a6864ef1131f8f8f693b9f384b874394))
* **apps/web:** Add local auth support and update endpoints ([364b45c](https://github.com/gameguild-gg/gameguild/commit/364b45c489792ac31227d1d48064f36d4bf64b22))
* **apps/web/auth:** Revamp auth config and token management ([ccabe92](https://github.com/gameguild-gg/gameguild/commit/ccabe92f073e520f176b581876c96b04d08be172))
* **apps/web:** Refactor GitHub integration and project stats enhancements ([d7932cf](https://github.com/gameguild-gg/gameguild/commit/d7932cfdb50db0704d30f7a8476c12f527675c32))
* **apps/web:** Add i18n module with en-US messages ([629b7a0](https://github.com/gameguild-gg/gameguild/commit/629b7a0847c9f73b52e57d00b9ce44099fbe0f26))
* **apps/web/courses:** Add course list UI and filtering components ([7cb2f57](https://github.com/gameguild-gg/gameguild/commit/7cb2f576659e2cf667ee64bc6373988017e465bd))
* **apps/web:** Add course catalog page and filtering components ([ced1c0a](https://github.com/gameguild-gg/gameguild/commit/ced1c0a2661490205b78a01be6bae46b7a607231))
* **apps/course-editor:** Add context, reducer, and server actions ([89fad5e](https://github.com/gameguild-gg/gameguild/commit/89fad5e4d2d41ab46655911a42f36d10c0ced899))
* **apps/web:** Add testing lab settings page ([4fa8ceb](https://github.com/gameguild-gg/gameguild/commit/4fa8ceb4f92e2cde3a8c568e7188eeb9ed179521))
* **apps/web:** Add Toaster to layout ([26cd534](https://github.com/gameguild-gg/gameguild/commit/26cd534c2cf9a1e20551b0d493998d3d74da97ab))
* **dashboard:** Revamp tenant and user pages and remove legacy achievements ([92f0e8d](https://github.com/gameguild-gg/gameguild/commit/92f0e8dd5b5da7726637f6a02ec3e84b76476f8e))
* **apps/web:** Wrap testing pages in dashboard layout ([5bd3ccc](https://github.com/gameguild-gg/gameguild/commit/5bd3ccc4d6842fdaa07c91ce5dfa41ec06ffc91b))
* **apps/web:** Integrate dashboard layout in feedback page ([8462a55](https://github.com/gameguild-gg/gameguild/commit/8462a5548cedcacb146af36f87d47897b6d185b2))
* **apps/web:** Refactor testing lab pages to use dashboard layout ([2a45966](https://github.com/gameguild-gg/gameguild/commit/2a45966e4dd77a6b4110b6fe90eba32f02be9c8c))
* **apps/web:** Update testing lab feedback page with dashboard layout ([da4336e](https://github.com/gameguild-gg/gameguild/commit/da4336e2b570c03147b6268dbff8fd836a1061a3))
* **apps/web:** Create testing requests dashboard ([1b95f70](https://github.com/gameguild-gg/gameguild/commit/1b95f706f0c7e052f2a1e838de2bef0fd09f25af))
* **apps/web:** Enable active link highlighting using current pathname ([7fcb6a9](https://github.com/gameguild-gg/gameguild/commit/7fcb6a9ffec02a70d7874d1f40c3332f13da682f))
* **tenants:** Add tenant actions and update import paths ([5c3a8e8](https://github.com/gameguild-gg/gameguild/commit/5c3a8e8a85af90943d7010d2918565b59f6573e1))
* **dashboard/tenant:** Reorganize UI components and add tenant utilities ([d578f74](https://github.com/gameguild-gg/gameguild/commit/d578f74257a0493b8daae3e1bd103fda3c3aeaa9))
* **apps/web:** Add create project form and move version submission form ([0eb3d4e](https://github.com/gameguild-gg/gameguild/commit/0eb3d4e7c30d173a11806d16d8b3e17c90045419))
* **apps/web:** Add join session form component ([0ed37db](https://github.com/gameguild-gg/gameguild/commit/0ed37dbe7beb35d80317c3d255fdb052dac8b284))
* **testing-lab:** Add attendance endpoints and tracker props ([876bd17](https://github.com/gameguild-gg/gameguild/commit/876bd1733c9e6dfbacb2f161c56170feefb009f2))
* **apps/web:** Implement session-based testing lab pages ([2d69106](https://github.com/gameguild-gg/gameguild/commit/2d691066da1455b16c63844ffb247557ddc8c86f))
* **apps/web:** Add testing feedback list UI and update dependencies ([b515fb6](https://github.com/gameguild-gg/gameguild/commit/b515fb60c3158f369cee0bab0c9070567793ce0d))
* **apps/web/testing-lab:** Add testing feedback list component ([e93a01e](https://github.com/gameguild-gg/gameguild/commit/e93a01e314b5500775b707ff2c66ded1742cf10b))
* **apps/web:** Enhance testing feedback list with filtering and UI components ([6eaacda](https://github.com/gameguild-gg/gameguild/commit/6eaacda59b7851960702dd0169346d15ffa66ffe))
* **apps/web/testing-lab:** Refactor components to use server data ([48652c1](https://github.com/gameguild-gg/gameguild/commit/48652c18265092b0a18b799082029fd709cd8e46))
* **testing-lab:** Replace role-based fetch with server actions ([3a47f71](https://github.com/gameguild-gg/gameguild/commit/3a47f7137701cdff34e066b4e58114c795a29739))
* **testing-lab:** Add detail components for feedback and session ([a59e25e](https://github.com/gameguild-gg/gameguild/commit/a59e25e1d3ba65828658418bf3025a5dce8f1921))
* **testing-lab:** Replace todos with API-driven data fetching ([d2c9a92](https://github.com/gameguild-gg/gameguild/commit/d2c9a92fac79c3fa3a445f057f6da2e855534afb))
* **apps/web:** Implement testing session details page ([19f9e14](https://github.com/gameguild-gg/gameguild/commit/19f9e14714bb6d8e66a732e6acac81540cca304e))
* **dashboard/testing-lab:** Pass testing requests data to list ([d96bceb](https://github.com/gameguild-gg/gameguild/commit/d96bceb3ed68996debc01d4786eb44b36e696954))
* **dashboard/testing-lab:** Add reports feedback page and remove submit page ([0001a9c](https://github.com/gameguild-gg/gameguild/commit/0001a9cfc7b617b473e142754cf03007c9df0e75))
* **apps/web:** Revamp testing lab pages and types ([2c4b943](https://github.com/gameguild-gg/gameguild/commit/2c4b94365b0b4f55d82a78c97ae265f97de652bc))
* **content/coding:** Integrate clang/pyodide and restructure editor ([b12c867](https://github.com/gameguild-gg/gameguild/commit/b12c86744b0d0940278a022de8bfba457fb29a66))
* **apps/web:** Revamp testing lab pages and filters ([9295f7a](https://github.com/gameguild-gg/gameguild/commit/9295f7ae5f4009ec4410ff6ca9b010a3d8d50ce1))
* **apps/web:** Implement testing lab dashboards ([3ab421a](https://github.com/gameguild-gg/gameguild/commit/3ab421ae0d6aa379790fc4df5672e9e61e2665e7))
* **apps/web:** Add testing lab requests management UI ([49c1aaf](https://github.com/gameguild-gg/gameguild/commit/49c1aaff13b3817cebd64399b6ca29a7b7828fdf))
* **apps/web:** Add new editor and course sync ([8012576](https://github.com/gameguild-gg/gameguild/commit/8012576d7c8d6b281bc29e28558ed09f508c69cc))
* **lib/sync:** Implement multi-adapter sync system ([b3c727d](https://github.com/gameguild-gg/gameguild/commit/b3c727d2bf595147fd17e79a3da32627e6583b25))
* **apps/web:** Add program management suite ([d23b6ac](https://github.com/gameguild-gg/gameguild/commit/d23b6acf862e7671854553d41f81b0449e0f4758))
* **users:** Add enhanced filtering and management UI ([ddda13f](https://github.com/gameguild-gg/gameguild/commit/ddda13fce6258060ef43d6885da6af7288f55df3))
* **dashboard/sidebar:** Update icon containers and labels ([b478559](https://github.com/gameguild-gg/gameguild/commit/b478559d851fa14a4f2aac90ca491b355e1f176f))
* **dashboard:** Revamp testing lab layouts and sidebar ([019cca7](https://github.com/gameguild-gg/gameguild/commit/019cca7e709db7257c4275a8fc94582c3ac0673a))
* **apps/web/editor:** Implement enhanced editor page with autosave and IndexedDB support ([6f46cc9](https://github.com/gameguild-gg/gameguild/commit/6f46cc91796cbe1524dd6a1e10d1c0af9cc159c7))
* **apps/web:** Add default modal and support error children ([6d9a1b4](https://github.com/gameguild-gg/gameguild/commit/6d9a1b4301e551cf721ad6aa659161a43354bf4b))
* **apps/web/dashboard:** Add fallback pages for subroutes and remove test page ([5375e01](https://github.com/gameguild-gg/gameguild/commit/5375e012a29d5e0d5c021d15d9a9ed338fafa2b0))
* **editor:** API client and new GUI for editor ([3c44204](https://github.com/gameguild-gg/gameguild/commit/3c442047833a5727161b5bcf7a3a34ac0a010d80))
* **api:** Add NameIdentifier claim and improve session tests ([29b46d8](https://github.com/gameguild-gg/gameguild/commit/29b46d85c143d0ad080bcfd2f7c3a1b04e31b39e))
* **apps/api:** Toggle mock seeding via env var ([76dcac6](https://github.com/gameguild-gg/gameguild/commit/76dcac6f2a87123cde45abf6f9bccf97499e0314))
* **apps/api:** Add default CORS and conditional sample seeding ([2327a6d](https://github.com/gameguild-gg/gameguild/commit/2327a6dc2ef28395305c85efcd5ee428f9214717))
* **docker:** Add multi-stage Dockerfile and update docker-compose configuration ([ba2f7f2](https://github.com/gameguild-gg/gameguild/commit/ba2f7f2aae1a8575449abde202816ad3f0e3125a))
* **api:** Add TestingSession permissions and seeder ([8ccbfeb](https://github.com/gameguild-gg/gameguild/commit/8ccbfeb18e851180f84c8dba3775f375cd6bf5eb))
* **auth:** Convert auth actions and JWT utilities to async ([ecf32e3](https://github.com/gameguild-gg/gameguild/commit/ecf32e3c0a0b2202f348b53df1d7d25bf93c8af9))
* **apps/web:** Enhance session UI and logging ([ab2cf79](https://github.com/gameguild-gg/gameguild/commit/ab2cf79765868d5372b00821cdbd341b6ef57da2))
* **apps/web:** Improve testing sessions mapping and dynamic refresh ([a296fdb](https://github.com/gameguild-gg/gameguild/commit/a296fdbab5be0f297eef85eb916e85292c40b27b))
* **apps/web:** Add community feed and improve sessions handling ([e02aff0](https://github.com/gameguild-gg/gameguild/commit/e02aff05a80323f13e2b9e41faa5ab8b347c3b44))
* **apps/web:** Add testing lab pages and feed server actions ([f8d4476](https://github.com/gameguild-gg/gameguild/commit/f8d44767dfa7498989334d48b3a96379388b01f6))
* **dashboard:** Integrate API endpoints and SSR actions ([a6a3664](https://github.com/gameguild-gg/gameguild/commit/a6a366459b4cb5260b0bb965cba71f4a01be9a4e))
* **apps/web:** Add project and testing lab pages with server actions ([80a83c7](https://github.com/gameguild-gg/gameguild/commit/80a83c7b72a0144ce1c1b4fa6f30b99c3e1cd57e))
* **apps/web:** Always fetch all tenants and add default tenant fallback ([4deb02b](https://github.com/gameguild-gg/gameguild/commit/4deb02b4c1fce0d2fed6b446c5ad62b31fcbf6d1))
* **apps/web:** Migrate tenant management to client auth actions ([dd36412](https://github.com/gameguild-gg/gameguild/commit/dd36412b69253a4f3ca7e5dc648ed399b590419f))
* **apps/web:** Revamp tenant management and switching ([86629d1](https://github.com/gameguild-gg/gameguild/commit/86629d10b2bafc6213f735f47801a2cd8491d377))
* **apps/web:** Integrate auth for user endpoints ([1449152](https://github.com/gameguild-gg/gameguild/commit/14491521f0e48d59665b43c73ae67278d603aeba))
* **apps/web:** Integrate backend notifications and reorganize dashboard actions ([d3e9d58](https://github.com/gameguild-gg/gameguild/commit/d3e9d58b050861bcb3c8506f455c3183e37adcad))
* **apps/web:** Integrate session authentication ([3e537e1](https://github.com/gameguild-gg/gameguild/commit/3e537e184962a717bdcfaf7d6e10a3e1e6c716a6))
* **apps/api:** Add ManagerId field to sessions ([14e8a67](https://github.com/gameguild-gg/gameguild/commit/14e8a674d6423ed5bb71645b83aee50134efadbf))
* **course-editor:** Add certificates, delivery, help, publish, SEO and content structure pages ([83d0b97](https://github.com/gameguild-gg/gameguild/commit/83d0b97fc588641fff11144c1e6f08c806aaf718))
* **apps/web/dashboard:** Revamp course editor pages ([f13fabc](https://github.com/gameguild-gg/gameguild/commit/f13fabce5dbc881543a6ce855b16c02b240e2782))
* **apps/web:** Add sticky headers and collapsible sections to course pages ([07a51df](https://github.com/gameguild-gg/gameguild/commit/07a51dff75af9f0642dffde8d94ab4118e1378bb))
* **apps/web:** Add modular course editor pages ([f86fed2](https://github.com/gameguild-gg/gameguild/commit/f86fed2c5056e8091e695220743651e374ef913a))
* **apps/web:** Add dashboard course editor pages ([35d01ce](https://github.com/gameguild-gg/gameguild/commit/35d01ce656c4a9fb7d6094653110505c619f6f40))
* **apps/web:** Implement unified course editor and enhanced listings ([0654ec6](https://github.com/gameguild-gg/gameguild/commit/0654ec64e4e26352c5ab0f2c3935943332a090ea))
* **apps/web:** Migrate editor page and update UI imports ([4a659aa](https://github.com/gameguild-gg/gameguild/commit/4a659aa18fa5988a3fe2b53cd3ee9f0e41dac92b))
* **dashboard/layout:** Remove sidebar trigger and add sidebar component ([a818506](https://github.com/gameguild-gg/gameguild/commit/a8185069609379ccb9f86c3debdf1c54fe577d00))
* **apps/web:** Update course actions and add course editor context ([d6e12e4](https://github.com/gameguild-gg/gameguild/commit/d6e12e4c3a4425556872c5f9faa4e65d88076e98))
* **apps/web:** Add Lexical dependencies and update Monaco editor ([021c838](https://github.com/gameguild-gg/gameguild/commit/021c838cf67e61f879897a3e3ddb54ae085eb2eb))
* **sync:** Add storage adapters, caching & sync provider ([bb48a19](https://github.com/gameguild-gg/gameguild/commit/bb48a19579423eff90892720059e7f991fc37772))
* **apps/api:** Add testing lab URL and achievements schema ([0a2471f](https://github.com/gameguild-gg/gameguild/commit/0a2471fe2adc82d994a839a45f71f3fa473ecdd0))
* **components:** Add generic data views and enhanced filter system ([287940d](https://github.com/gameguild-gg/gameguild/commit/287940d322c971a0b668b6a0d55a92c347f96445))
* **apps/web:** Enforce key existence and add value extractor ([7e04131](https://github.com/gameguild-gg/gameguild/commit/7e0413103a5e1618582b1473c89865009b116a98))
* **common/filters:** Improve period selectors with type safety ([810c1ec](https://github.com/gameguild-gg/gameguild/commit/810c1ec902d178e7c2479e63529e42664b707eaf))
* **components:** Add reusable data display and filter components ([9c167a9](https://github.com/gameguild-gg/gameguild/commit/9c167a90963a09c46bee9f432188cd3f6e883556))
* **apps/api:** Add comprehensive mock data seeding ([a56cfc9](https://github.com/gameguild-gg/gameguild/commit/a56cfc984f14a3f460267474c1d61ce0f498128d))
* **apps/web:** Refine Testing Lab UI Components ([342b272](https://github.com/gameguild-gg/gameguild/commit/342b2722c620f4fb079a084f8a6740ff0f250a79))
* **apps/web:** Improve testing lab UI and session sorting ([7f6e615](https://github.com/gameguild-gg/gameguild/commit/7f6e61595457444b4ecd83a48f47fbdbc3008847))
* **apps/web:** Enhance UI styling with gradients and tooltips ([9728df0](https://github.com/gameguild-gg/gameguild/commit/9728df0ff3094499b684f71218ce299cd465f6ea))
* **apps/web:** Add refresh token debug info ([05cf649](https://github.com/gameguild-gg/gameguild/commit/05cf64999ed4935c91d4dd741457addf80e40071))
* **components/filters:** Enhance period selector with tooltips and dynamic quarters ([bb0e7f8](https://github.com/gameguild-gg/gameguild/commit/bb0e7f84c39afd0ea6598eaaf55daa1b1fc83f2a))
* **apps/web:** Enhance period selector and sessions UI design ([1defe6e](https://github.com/gameguild-gg/gameguild/commit/1defe6e9588ea0ecb2f020a67eae482233a684f2))
* **testing-lab:** Update UI styling and layout ([6e755c5](https://github.com/gameguild-gg/gameguild/commit/6e755c59f6bf4a366cb37fb055e6bade7abfd11e))
* **apps/web:** Enhance testing lab sessions with filters and view modes ([d7963f3](https://github.com/gameguild-gg/gameguild/commit/d7963f3b9ab6cbe8b7f783b09f8a2783a7e3aa4d))
* **apps/web/testing-lab:** Add session grid, row, and table views ([261d736](https://github.com/gameguild-gg/gameguild/commit/261d73628cfe0651008ac8bea6fe389ee7ea51ae))
* **auth:** Enhance token refresh logging and debugging ([164d562](https://github.com/gameguild-gg/gameguild/commit/164d562bbec8d34e6aa32c8639b4f5c9959aea90))
* **testing-lab:** Update layout and UI styling ([60d5ba7](https://github.com/gameguild-gg/gameguild/commit/60d5ba758d13839281a0142508dbccfd3be8b7ae))
* **apps/web:** Add animated floating icons to testing lab landing ([ff5c224](https://github.com/gameguild-gg/gameguild/commit/ff5c224cd38087a80d003c5fa781ce7e82f4fd17))
* **auth:** Improve authentication and token refresh integration ([eda7e4e](https://github.com/gameguild-gg/gameguild/commit/eda7e4e72fd0d5d8236bb58fe642300684156e25))
* **apps/web/testing-lab:** Revamp session pages and UI components ([ba7f2b4](https://github.com/gameguild-gg/gameguild/commit/ba7f2b42f64eb9aba719885e5ee555a00a3d3e94))
* **apps/web:** Use slugs for test session routes ([756d7e9](https://github.com/gameguild-gg/gameguild/commit/756d7e90d51dbf77b8bbe65a2d58c8ed52da160e))
* **apps/web:** Add Testing Lab functionality ([bb7a8e2](https://github.com/gameguild-gg/gameguild/commit/bb7a8e22fea342dae48afe92656d1493de0d28a7))
* **apps/web:** Add testing lab auth layout and error/loading components ([e533028](https://github.com/gameguild-gg/gameguild/commit/e5330280ab54369890977c3ea8c0b026fa6ccf0b))
* **projects:** Add onProjectCreated callback and update project UI styles ([c7a1153](https://github.com/gameguild-gg/gameguild/commit/c7a11530b50f21ce3f31a7e079e3afe1a86ed409))
* **apps/web:** Replace legacy project list with enhanced overview ([ed2bdce](https://github.com/gameguild-gg/gameguild/commit/ed2bdce2f342ba450bca14d19cbd68ecb2e8f9dc))
* **auth:** Use server actions for backend authentication ([d8243fe](https://github.com/gameguild-gg/gameguild/commit/d8243fe2f9d9a5e2162bc017d028e837bfdcc809))
* **apps/web:** Add auth debug and refine session configuration ([15d0b58](https://github.com/gameguild-gg/gameguild/commit/15d0b588c751e2b61ded51c179b6d7c6f70c8268))
* **apps/web:** Revamp auth and dashboard layouts ([09354dc](https://github.com/gameguild-gg/gameguild/commit/09354dc3ce504cadb19a6c1d79fd436d0a0afa0f))
* **apps/dashboard/testing-lab:** Add sidebar with calendar and quick stats ([145784f](https://github.com/gameguild-gg/gameguild/commit/145784fa801702adf58c7f22abb55510493722b2))
* **dashboard/testing-lab:** Enhance sessions UI with detailed list and table ([7eb8907](https://github.com/gameguild-gg/gameguild/commit/7eb8907fbafad6b232259158407ec6d35aa7f3ad))
* **apps/web:** Integrate API for testing lab components ([0034c37](https://github.com/gameguild-gg/gameguild/commit/0034c3777f6e0a560dcec11f2c143b848efe9f25))
* **apps/web:** Refactor testing lab overview and improve auth config ([0ff755c](https://github.com/gameguild-gg/gameguild/commit/0ff755c95de88f8234d10f41ecaca961668d2c60))
* **auth:** Improve token refresh and auth hooks integration ([63bd7eb](https://github.com/gameguild-gg/gameguild/commit/63bd7eb7faa0589449a6742d5981efff7e783d03))
* **apps/web:** Add testing lab page with overview ([9033f43](https://github.com/gameguild-gg/gameguild/commit/9033f43f3f4b5783f659e743f9d9dc37a134b354))
* **apps/web:** Update dashboard layout with auth and styling ([89c1d1f](https://github.com/gameguild-gg/gameguild/commit/89c1d1fbacf3a0b64c0ebea26f0b32ea4a8f5c35))
* **apps/web:** Add comprehensive community feed system ([2def27d](https://github.com/gameguild-gg/gameguild/commit/2def27da3928aed5fdd72bb3207057865657b7c4))
* **apps/web:** Enhance header and notifications UI styles ([32d4d11](https://github.com/gameguild-gg/gameguild/commit/32d4d11a449a345fdce50d2881a26f4a5d1df744))
* **apps/web:** Refactor footer into modular components ([0401f7a](https://github.com/gameguild-gg/gameguild/commit/0401f7a39f5d20bc7fcd10c2ad74b10243b293b8))
* **apps/api:** Add achievement configurations and default DataLoader options ([50f2784](https://github.com/gameguild-gg/gameguild/commit/50f27847e4dbf5be466777f31eec71742fcb03c5))
* **modules/user-achievements:** Add user achievements module ([c094cd0](https://github.com/gameguild-gg/gameguild/commit/c094cd003178a789408161b91672a1fbb880b14b))
* **apps/web:** Enhance contributors UI and stats layout ([afcf2b9](https://github.com/gameguild-gg/gameguild/commit/afcf2b9b4c5cbe8639b62ad49898539c4463224c))
* **apps/web:** Revamp contributors UI layout and styling ([8fc0ae4](https://github.com/gameguild-gg/gameguild/commit/8fc0ae4ec0d4855e8a678657551a4536b0c2268c))
* **apps/web:** Add roadmap, stats, and updated contribution UI ([4cac003](https://github.com/gameguild-gg/gameguild/commit/4cac003cbb3a29dd80b9b37193d113ad6433375d))
* **apps/web:** Update contributors header and add contribution guide ([c65d14c](https://github.com/gameguild-gg/gameguild/commit/c65d14cf810753d97faa8eb02d6a46a93fd6177e))
* **apps/web:** Enhance contributors UI with GitHub integration ([6a7e5c7](https://github.com/gameguild-gg/gameguild/commit/6a7e5c76663eaf024492dcc1ebfa8d7d40b321a3))
* **apps/web:** Update auth errors, branding and community pages ([077da0b](https://github.com/gameguild-gg/gameguild/commit/077da0b115df765fc1ca20f40c766a9f0466c53b))
* **apps/web:** Add instrumentation client for monitoring ([57f8ec3](https://github.com/gameguild-gg/gameguild/commit/57f8ec36f67da5709874fc9495865082a05ae39a))
* **apps/web:** Add onRequestError logging function ([8349362](https://github.com/gameguild-gg/gameguild/commit/83493620dae72e5e84679c3d324a0923ed105152))
* **apps/web:** Add manifest, robots, and sitemap endpoints ([507d7b2](https://github.com/gameguild-gg/gameguild/commit/507d7b291d0a6596d33fd05ba3d06fbb79055433))
* **apps/web/dashboard:** Revise error, loading and not found pages ([b9ba45e](https://github.com/gameguild-gg/gameguild/commit/b9ba45e2a7005ead80acccb23e3b179ac87b1f1f))
* **apps/api:** Replace Error with ErrorMessage for localized error handling ([a6a65df](https://github.com/gameguild-gg/gameguild/commit/a6a65df9a68626286215e4fd1e09c941d411d55b))
* **apps/web:** Add testing lab, courses, and peer review features ([f3c187d](https://github.com/gameguild-gg/gameguild/commit/f3c187d3ceeaf3fcd2c94d1c8019aa25678bbd8f))
* **apps/api:** Add feedback reporting and rating endpoints ([830d6e6](https://github.com/gameguild-gg/gameguild/commit/830d6e627af353fe221e0ece7020558ef00bef83))
* **api/web:** Add UpdateAttendance DTO and explicit score type annotation ([ff858cd](https://github.com/gameguild-gg/gameguild/commit/ff858cd541eb79eec0b19912bbbd4546325344d5))
* **api/web:** Add attendance endpoints and reporting features ([2a46f5e](https://github.com/gameguild-gg/gameguild/commit/2a46f5e0e3ab5a7381c30cdb1e43a00750caaf09))
* **apps/api,apps/web:** Update testing workflow and sidebar integration ([f8d408a](https://github.com/gameguild-gg/gameguild/commit/f8d408a2f64489db31babcb244eb913c95396853))
* **testing-lab:** Add simplified testing workflow endpoints and UI ([c5a3b14](https://github.com/gameguild-gg/gameguild/commit/c5a3b1472aefdfde80a95d68dfaa4e5e1fdc5c1c))
* **apps/web:** Add testing lab pages and progress tracker ([8be6661](https://github.com/gameguild-gg/gameguild/commit/8be66618434ac9cce56372c63b44ad4dee07103f))
* **apps/web:** Implement course content viewer and learning modules ([1fc56f5](https://github.com/gameguild-gg/gameguild/commit/1fc56f5ef88a597cc6f031026cd9bd0d4e7bd52b))
* **apps/web:** Add slug generation and update course links ([aae436b](https://github.com/gameguild-gg/gameguild/commit/aae436b1bdb8d418890b2f6511436d30c6553dcf))
* **apps/web:** Replace form action with custom submit handler ([ca9cbef](https://github.com/gameguild-gg/gameguild/commit/ca9cbef5c6c60688854ea384b0ff133bc497792a))
* **apps/web:** Import sidebar components ([a8a9329](https://github.com/gameguild-gg/gameguild/commit/a8a932969788a64d955f366b8a6adeebeb879402))
* **apps/api:** Update ratings schema and add new tables ([eda8402](https://github.com/gameguild-gg/gameguild/commit/eda8402ccb18ac48b1899c1c848753dfaa1abd33))
* **apps/api:** Exclude soft-deleted products from global statistics ([ec0e585](https://github.com/gameguild-gg/gameguild/commit/ec0e585084715c7a8acbe215587cd50253439893))
* **apps/api:** Add cancel payment command handler ([23ecab5](https://github.com/gameguild-gg/gameguild/commit/23ecab5dad1e8fecf845a01e7be7303cfce21de0))
* **apps/api:** Add product stats query and update deletion test threshold ([86eea2d](https://github.com/gameguild-gg/gameguild/commit/86eea2d570c62669ea0d2a5318f571db47470483))
* **api:** Enhance product queries with filters ([56bfc70](https://github.com/gameguild-gg/gameguild/commit/56bfc703f9371df1e65a2258b2aebb3ec4aa4efc))
* **api/products:** Add publish and unpublish commands with role validation ([054a78c](https://github.com/gameguild-gg/gameguild/commit/054a78c7017873acf452c1e2f509c9a1365fcf51))
* **ui:** Add comprehensive UI component library ([f79305b](https://github.com/gameguild-gg/gameguild/commit/f79305b8e51a2d0f3a798fc2e1ccc77a9f123f37))
* **system:** Integrate payment gateway and course completion UI ([d5618ad](https://github.com/gameguild-gg/gameguild/commit/d5618ad4b2e9b4ef0ffddc0f71a1499799ed9673))
* **apps/web:** Add certificate generation and progress tracker ([9eb03aa](https://github.com/gameguild-gg/gameguild/commit/9eb03aa61db10fac3a4181b11ee68845d1bb18b5))
* **api/payments:** Add comprehensive payment tests ([b52af26](https://github.com/gameguild-gg/gameguild/commit/b52af26472a7111578ddf2c6785a4826fab2c154))
* **apps/web:** Add activity submission, reporting and peer review features ([4882207](https://github.com/gameguild-gg/gameguild/commit/4882207f9807aeb2b9938f552861f9d72c3539fd))
* **apps/api:** Update payment flow and API endpoints ([12a9109](https://github.com/gameguild-gg/gameguild/commit/12a9109642a1ef0981eea241b9cc84f0df433306))
* **apps/api/tests:** Update GraphQL introspection config ([e1cd163](https://github.com/gameguild-gg/gameguild/commit/e1cd16387e702e3ee32aafae5c1e73e9db395a02))
* **apps/api:** Pass logger to GraphQL config ([a6fbf47](https://github.com/gameguild-gg/gameguild/commit/a6fbf47792a08387a6813c97e31a1adaf90c2fc2))
* **api:** Enhance logging and update GraphQL tests ([67df9fd](https://github.com/gameguild-gg/gameguild/commit/67df9fd8a435be131482ac8f411117912d6f9fad))
* **api:** Add optimized projects query & enforce DataLoader defaults ([6c859b9](https://github.com/gameguild-gg/gameguild/commit/6c859b9811ced06aa05aaaebafb86a54451cc315))
* **apps/api:** Add DbContextFactory support for GraphQL DataLoaders ([73cc000](https://github.com/gameguild-gg/gameguild/commit/73cc000cba16755046acd161a55ebdc0006b2ff8))
* **apps/api/programs:** Add rating model and update enrollment status ([146ef7e](https://github.com/gameguild-gg/gameguild/commit/146ef7e1138afd54fb8dfdaeaf1fd8e437666759))
* **apps/api:** Add auto-enrollment and program CQRS endpoints ([cc86f22](https://github.com/gameguild-gg/gameguild/commit/cc86f22eabb0d3bde7d8752df948926f3b945ead))
* **apps/api:** Integrate MediatR CQRS and add GraphQL data loaders ([b7a09dd](https://github.com/gameguild-gg/gameguild/commit/b7a09dd752a5e0a2d6b69dac514f521fcd1701b8))
* **apps/api:** Enhance payments module and add user context ([78a67b3](https://github.com/gameguild-gg/gameguild/commit/78a67b39322b8b9a44dd0de1c58f11adf16e1612))
* **apps/api:** Add user and tenant context middleware and docs ([74e5005](https://github.com/gameguild-gg/gameguild/commit/74e50052f0cbde0ce8d6caa24dc7523b2f5438bf))
* **apps/api:** Add product handlers and update status enum ([b39078b](https://github.com/gameguild-gg/gameguild/commit/b39078b4e014e0422b66b3edf49c9935a16b680f))
* **apps/api:** Add payments, products, and programs modules ([14f40c1](https://github.com/gameguild-gg/gameguild/commit/14f40c18b698c55b07f5f6b3a285e77fa4624d10))
* **mods/programs:** Add progress and enrollment ([9c25f08](https://github.com/gameguild-gg/gameguild/commit/9c25f08464ad44db55743c3fe1ef19d2755ccafb))
* **posts:** Add posts module with event-driven social features ([b67e041](https://github.com/gameguild-gg/gameguild/commit/b67e041d5a64179e80f60ccf71882deff23983fc))
* **apps/api,apps/web:** Integrate Google OAuth sign-in and profile creation ([ce7f427](https://github.com/gameguild-gg/gameguild/commit/ce7f427d76c87a002cd152e6648b57e42bb28d0b))
* **api/web:** Add Google token auth and course enhancements ([4ec7e3f](https://github.com/gameguild-gg/gameguild/commit/4ec7e3f7d23fb5d939400358c03b64e5e77f754e))
* **apps/api:** Add TestingLab GraphQL types and test helper ([581b8b6](https://github.com/gameguild-gg/gameguild/commit/581b8b6ffb0cad2da1d84c979aca913fb1c79bd2))
* **api:** Refine tenant permissions and auth tests ([89bb021](https://github.com/gameguild-gg/gameguild/commit/89bb0212701e06c23a9f175ee2053cddc01e6f56))
* **apps:** Add DAC auth and tenant mutations with enhanced error handling ([97f19a8](https://github.com/gameguild-gg/gameguild/commit/97f19a89cbc4babe4eeaead409ac0b7aabea8963))
* **apps/web:** Add auth error page and track context exports ([23482ed](https://github.com/gameguild-gg/gameguild/commit/23482ed8c7c4d87fc1b5f90e1545b6cc2820c1cb))
* **web:** Add auth page, improvements docs, and track data ([19654cc](https://github.com/gameguild-gg/gameguild/commit/19654cc80500df62b585f2f43af6bb2bc829d244))
* **apps/web:** Add comprehensive component showcase and examples ([ade2720](https://github.com/gameguild-gg/gameguild/commit/ade272075cce2a37f6ad42ea489e15acb19fd2e0))
* **apps/api:** Adopt CQRS pattern and add test module support ([1e536ce](https://github.com/gameguild-gg/gameguild/commit/1e536ce21b80e757659f8ec4b8d9ffd33f1509af))
* **apps/web:** Add courses and tracks layouts, error and loading states ([d198d91](https://github.com/gameguild-gg/gameguild/commit/d198d9147969e6fdd0e38533535a126182799fcd))
* **apps/api:** Add user profiles, auth logging, program access checks and balance normalization ([9f722fe](https://github.com/gameguild-gg/gameguild/commit/9f722fe0ef951acc54ff5fdc7dbc63904d55727c))
* **apps/web:** Modularize code structure and update imports ([69ffa43](https://github.com/gameguild-gg/gameguild/commit/69ffa43cfd08a71b4c2c75a1d6f701e0afcf9b8d))
* **apps/api:** Modernize authentication with CQRS & JWT support ([14cc316](https://github.com/gameguild-gg/gameguild/commit/14cc316bfd896a99eed721bb2085dc8fa66933b4))
* **tests:** Add integration tests & style config ([55a0ae6](https://github.com/gameguild-gg/gameguild/commit/55a0ae608d8cc71c93cc5243ac816f1d52ab00c0))
* **apps/api:** Add domain events, resource config and user profiles handlers ([92e0f17](https://github.com/gameguild-gg/gameguild/commit/92e0f177687f1ecde690292cc58ab28ce196d9a3))
* **apps/api/tenants:** Implement CQRS endpoints and validators ([5cb192c](https://github.com/gameguild-gg/gameguild/commit/5cb192c82caebc6a644db609cb9aae29e1414fb9))
* **apps/api:** Add tenant module commands, handlers, queries, and validators ([e345395](https://github.com/gameguild-gg/gameguild/commit/e3453958c66d71649ca0e291680ad90a0f9d6623))
* **apps/api:** Enhance user profiles with bulk ops and validators ([871fb0f](https://github.com/gameguild-gg/gameguild/commit/871fb0f641cabdcfcae639d0f733defb7973b50e))
* **apps/api:** Add bulk user profile commands and update migration names ([6c9dabe](https://github.com/gameguild-gg/gameguild/commit/6c9dabe1d1ef57acc34e12dbbfefa9d72ae7e6c9))
* **apps/api:** Refactor user GraphQL inputs and bulk handlers ([4356420](https://github.com/gameguild-gg/gameguild/commit/4356420ee237082c1290934b2dc40b9587594706))
* **modules/users:** Revamp user commands, events, and GraphQL endpoints ([f9d8fce](https://github.com/gameguild-gg/gameguild/commit/f9d8fced3f3d97873d86a742593e4ae4437d3a10))
* **apps/api:** Add auth, payment, and subscription modules ([160a8ee](https://github.com/gameguild-gg/gameguild/commit/160a8ee4364197ea9acd7c8bcf0398069574d935))
* **tests/api:** Add API tests project and reorganize solution structure ([b857d90](https://github.com/gameguild-gg/gameguild/commit/b857d90ba559aa3f31d35428cd8cac90ec36db99))
* **apps/web:** Add enhanced E2E tests for frontend-API integration ([bc4a9c8](https://github.com/gameguild-gg/gameguild/commit/bc4a9c8e86b3a9553c21e540fdea838ba9dfeacf))
* **apps/web:** Add enrollment and course detail pages ([f98187c](https://github.com/gameguild-gg/gameguild/commit/f98187c2fae870adb9db15e6e0b9ba2940f0a4da))
* **apps:** Add learning tracks and course dashboards ([57ef341](https://github.com/gameguild-gg/gameguild/commit/57ef3415a579bdc347574ed79b3948d6a8de41d6))
* **apps/web:** Restructure course catalog and add landing pages ([51598bd](https://github.com/gameguild-gg/gameguild/commit/51598bde414a2cacd2a0fd5d133c049bdb075a1d))
* **apps/api,apps/web:** Add courses pages, notifications, header, and migration updates ([46841fa](https://github.com/gameguild-gg/gameguild/commit/46841fa3bd3af5437b72b3a23e1ce5f73dd758d7))
* **apps/api:** Add program flow documentation ([60af355](https://github.com/gameguild-gg/gameguild/commit/60af3553943679b5f74741cb41d37f2731ae17a3))
* **apps/api:** Update test fixture and endpoint routes ([ea860d2](https://github.com/gameguild-gg/gameguild/commit/ea860d2a68276bb45e1ab3441cd8fc1ae169e939))
* **apps/api:** Add email lookup and duplicate check in user service ([6c2e82b](https://github.com/gameguild-gg/gameguild/commit/6c2e82be7be7c6e9993330b55294a232068db6df))
* **program:** Add verification, enrollment status and wishlist features ([cf272d9](https://github.com/gameguild-gg/gameguild/commit/cf272d90b979e9a89114a9cf3f3ae2f9416a08bb))
* **api/web:** Enhance tenant management with global admin support ([172f721](https://github.com/gameguild-gg/gameguild/commit/172f7216b4514f77a7ee913571ca3b3db59f57de))
* **apps/api:** Add GET user memberships endpoint and update tests ([faaf36a](https://github.com/gameguild-gg/gameguild/commit/faaf36a43f891e42270cf699936e3021233a5e88))
* **apps:** Add tenant management, admin login and super admin seeding ([7950ee3](https://github.com/gameguild-gg/gameguild/commit/7950ee3c9840b173a50139fbd6cc0419f1313f25))
* **api/swagger:** Add JWT authentication support to Swagger UI ([d7be8cd](https://github.com/gameguild-gg/gameguild/commit/d7be8cdbe4ca48cd65cb8a5d7e065850652e3845))
* **api/web:** Add GET /users/me endpoint and integrate with web app ([eace8e6](https://github.com/gameguild-gg/gameguild/commit/eace8e6d1ba3e0592a30e509f303a28b0831aaa6))
* **web:** Add API type generation and client integration ([2c36823](https://github.com/gameguild-gg/gameguild/commit/2c368239be368a290a9f1a5cfdeb7715b13a5cab))
* **apps/api:** Add ActivityGrade and ContentInteraction modules with permission inheritance ([b1815b1](https://github.com/gameguild-gg/gameguild/commit/b1815b10d7c60a9241e6384cd7e7f2d1b55876c3))
* **apps/api:** Add ActivityGrade and ContentInteraction modules with permission inheritance ([1ced743](https://github.com/gameguild-gg/gameguild/commit/1ced7439e9bc5278bd9dcf25525178dd86b7ac19))
* **apps/api:** Update to global project default permissions ([5b09eb2](https://github.com/gameguild-gg/gameguild/commit/5b09eb2f749a0f4acd4fc8764738d682043d4b2c))
* **graphql:** Add 3-layer DAC authorization for GraphQL APIs ([0fd472d](https://github.com/gameguild-gg/gameguild/commit/0fd472d40287a594aeb09a5a2a231927f3badcd6))
* **program:** Add ProgramContent module with permissions inheritance ([38baace](https://github.com/gameguild-gg/gameguild/commit/38baace804f03abd87a9e16b9f2a3115f969ce46))
* **cms/tenant:** Enhance auth, DTO mapping, and auto-assign endpoints ([737502a](https://github.com/gameguild-gg/gameguild/commit/737502a2e5d46a8410285a44dae06dd83f6c6c91))
* **modules/tenant:** Add tenant domain and auto-assignment functionality ([bf1afc2](https://github.com/gameguild-gg/gameguild/commit/bf1afc24ed20a12da956b75589da73338cb26dec))
* **apps/cms:** Integrate TestingLab GraphQL API, DTOs, and tests ([90b4667](https://github.com/gameguild-gg/gameguild/commit/90b4667e596f72ee1f546d5e65dbf7fdcb58551b))
* **program:** Add Program module with DAC permissions ([f0c650c](https://github.com/gameguild-gg/gameguild/commit/f0c650c4831bd8336cada31b9ce962a148735f21))
* **apps/cms:** Add TestingLab module with controllers, models, services and tests ([41cc19d](https://github.com/gameguild-gg/gameguild/commit/41cc19d1abfac3a0e1eb5f996a18167466ee8e56))
* **apps/web:** Replace image banner with CSS gradient and drop shadow ([d7411a4](https://github.com/gameguild-gg/gameguild/commit/d7411a4f30b7f909d390a637deb81c1339c22dec))
* **auth:** Set 5 min JWT clock skew and add JWT utils ([c31815e](https://github.com/gameguild-gg/gameguild/commit/c31815ee7bba4c9a2c4dd72f6b66288892584f61))
* **apps:** Add DB seeding, refine auth and Next.js migration ([cd35a37](https://github.com/gameguild-gg/gameguild/commit/cd35a37c488d77f18a12b16c7f33e45468428f8f))
* **apps/cms:** Update project schema with slug validation and new tables ([0ae0bc5](https://github.com/gameguild-gg/gameguild/commit/0ae0bc5c0500e63c25d32a1057abc82e14c270af))
* **apps/cms:** Enhance project models, permissions, and tests ([bacabd7](https://github.com/gameguild-gg/gameguild/commit/bacabd7e8071a31a8a4f2a5ef364471907a41941))
* **apps/cms:** Integrate project permissions and auto-generate slugs ([1cb6ed1](https://github.com/gameguild-gg/gameguild/commit/1cb6ed1b192d5f80c768917dfe634739e69a4ac5))

_Release notes truncated to fit GitHub's 125 KB body limit. See the full commit list: https://github.com/gameguild-gg/gameguild/compare/v2.55.0...v3.0.0._

# [2.55.0](https://github.com/gameguild-gg/gameguild/compare/v2.54.3...v2.55.0) (2026-04-03)


### Features

* **networking:** Add Week 12 performance and reliability quiz ([7f26276](https://github.com/gameguild-gg/gameguild/commit/7f2627669c0f3ac9bbaed0cae13c5734a9353a1b))

## [2.54.3](https://github.com/gameguild-gg/gameguild/compare/v2.54.2...v2.54.3) (2026-04-02)


### Bug Fixes

* **ai4games2:** fix some readings and contents ([98b6865](https://github.com/gameguild-gg/gameguild/commit/98b6865981140f76931a4f93ec2a9ddcdaf0a451))

## [2.54.2](https://github.com/gameguild-gg/gameguild/compare/v2.54.1...v2.54.2) (2026-04-01)


### Bug Fixes

* **reveal:** do not recalculate font size on every slide change ([4087f59](https://github.com/gameguild-gg/gameguild/commit/4087f596008a8a6794a970cffdff228df56982be))

## [2.54.1](https://github.com/gameguild-gg/gameguild/compare/v2.54.0...v2.54.1) (2026-03-31)


### Bug Fixes

* add npmrc to install dependencies nested ([4bc86ea](https://github.com/gameguild-gg/gameguild/commit/4bc86ea006176bf6c623e6a0c6bc983ef4f7ce3a))
* **ci:** apps/web ([2b31bca](https://github.com/gameguild-gg/gameguild/commit/2b31bcad59a43f57c2db50167d403229afde64e4))
* **neo4j:** fix reveal presentation ([5b5a543](https://github.com/gameguild-gg/gameguild/commit/5b5a543a580d75f6f19e3bf89158af2ce53bdf1b))

# [2.54.0](https://github.com/gameguild-gg/gameguild/compare/v2.53.1...v2.54.0) (2026-03-31)


### Bug Fixes

* **courses:** broken links ([aef66b7](https://github.com/gameguild-gg/gameguild/commit/aef66b7d948bfc0130afdc5380f10679e3a6d97d))


### Features

* **courses/networking:** Add Week 12 content on performance, reliability, and packet budgets ([933277e](https://github.com/gameguild-gg/gameguild/commit/933277ed9e5e455feed88fcbfacbe795d171551d))

## [2.53.1](https://github.com/gameguild-gg/gameguild/compare/v2.53.0...v2.53.1) (2026-03-30)


### Bug Fixes

* **emception/ide:** Restore editor input after SDL3 canvas rendering ([d6f346f](https://github.com/gameguild-gg/gameguild/commit/d6f346fa8357229989b4b209d2727e2afa3c933c))

# [2.53.0](https://github.com/gameguild-gg/gameguild/compare/v2.52.0...v2.53.0) (2026-03-30)


### Features

* **courses/ai4games2:** Integrate Week 12 assignment and refine course content ([3a86b2c](https://github.com/gameguild-gg/gameguild/commit/3a86b2c4c7b08945bd2056c9d966346846e0f053))

# [2.52.0](https://github.com/gameguild-gg/gameguild/compare/v2.51.0...v2.52.0) (2026-03-30)


### Bug Fixes

* **ci:** hopefully deploy npm ([a3180d9](https://github.com/gameguild-gg/gameguild/commit/a3180d90f3d5dd225c24c1b3bd8046e18fe11567))
* **emception:** Correct SDL3 cache path and enhance package publishing ([a576427](https://github.com/gameguild-gg/gameguild/commit/a576427938d91e36f642d19997d99e9e7e3929f4))


### Features

* **courses:** Add multi-agent AI week and update graph databases content ([1ef830d](https://github.com/gameguild-gg/gameguild/commit/1ef830d8becb7081bfe4609e7dbc289c21c6bbec))

# [2.51.0](https://github.com/gameguild-gg/gameguild/compare/v2.50.14...v2.51.0) (2026-03-30)


### Bug Fixes

* descriptions of the final assignment checkpoint 2 ([4d392a8](https://github.com/gameguild-gg/gameguild/commit/4d392a89921feea2fdf3fded4f9720ac65300008))
* **emception:** backspace issue on shell ([d3e8887](https://github.com/gameguild-gg/gameguild/commit/d3e8887da147c1245896c5e461cf5fbf12da84bf))
* **emception:** Disable SDL_CAMERA and SDL_SENSOR in build script ([2473400](https://github.com/gameguild-gg/gameguild/commit/2473400fcd6dcf4b054d1958ae75d776e5efd199))
* **emception:** fix curl_lite ([157e96c](https://github.com/gameguild-gg/gameguild/commit/157e96ce27e0eaaf6519e282ff39905bec523140))
* **emception:** fix sdl build process ([31e86c0](https://github.com/gameguild-gg/gameguild/commit/31e86c0020dc246442c2b4d103380312a1c515f9))


### Features

* **course/networking/week11:** Introduce non-blocking I/O, parallelism, and concurrency module ([911d019](https://github.com/gameguild-gg/gameguild/commit/911d0199e3040fa261a0878a4523238da6657f6d))
* **emception:** Add E2E tests for SDL3 compilation and rendering ([c53bb05](https://github.com/gameguild-gg/gameguild/commit/c53bb05a5624eb8f37162d520495c86f33f75181))
* **emception:** add sdl ([6d5ade8](https://github.com/gameguild-gg/gameguild/commit/6d5ade83ef7e147e022ec8b33d4f8b7b007628b0))
* **emception:** Enhance SDL3 integration with robust compilation and app lifecycle ([ef5da4b](https://github.com/gameguild-gg/gameguild/commit/ef5da4baaaaedf78a5692095f619b50c1bf730cd))
* **emception:** Implement main IDE layout and functionality ([dfd0ad2](https://github.com/gameguild-gg/gameguild/commit/dfd0ad28387574b679f782bbb69285f168fbc741))
* **emception:** Introduce core IDE components and utilities ([fa688e6](https://github.com/gameguild-gg/gameguild/commit/fa688e62eeae70d873f2bdb2479aeb1f4290a0d9))

## [2.50.14](https://github.com/gameguild-gg/gameguild/compare/v2.50.13...v2.50.14) (2026-03-23)


### Bug Fixes

* **courses:** fixed reveal markdown orientation ([f699196](https://github.com/gameguild-gg/gameguild/commit/f6991963aa094ec57e5e6917ca7c78530a846d79))
* **courses:** kv storage presentation ([fc65d27](https://github.com/gameguild-gg/gameguild/commit/fc65d278eec9bf138956c5d007ceeaa3ef2b35f0))

## [2.50.13](https://github.com/gameguild-gg/gameguild/compare/v2.50.12...v2.50.13) (2026-03-23)


### Bug Fixes

* **courses:** contents ([72f27ee](https://github.com/gameguild-gg/gameguild/commit/72f27ee99f8ea31becac2436b7c7b84d060964d4))

## [2.50.12](https://github.com/gameguild-gg/gameguild/compare/v2.50.11...v2.50.12) (2026-03-23)


### Bug Fixes

* **ai-course:** add goap content to ai ([996d1ff](https://github.com/gameguild-gg/gameguild/commit/996d1ff1c9ffd645e04e17c0c3550bec30e38cbe))

## [2.50.11](https://github.com/gameguild-gg/gameguild/compare/v2.50.10...v2.50.11) (2026-03-20)


### Bug Fixes

* **networking:** Add Week 10 HTTP quiz ([f1d3afa](https://github.com/gameguild-gg/gameguild/commit/f1d3afa9bdee3ebc69ceb35007bdce9373efe1e5))

## [2.50.10](https://github.com/gameguild-gg/gameguild/compare/v2.50.9...v2.50.10) (2026-03-20)


### Bug Fixes

* **networking:** add http topics ([46da8c6](https://github.com/gameguild-gg/gameguild/commit/46da8c6c81f2dacf13a5afe6a8540fef1e9488b7))

## [2.50.9](https://github.com/gameguild-gg/gameguild/compare/v2.50.8...v2.50.9) (2026-03-20)


### Bug Fixes

* **emception:** print the reason a bundle is downloaded ([490c903](https://github.com/gameguild-gg/gameguild/commit/490c903aee04a31462192cfe39996e9a25b03324))

## [2.50.8](https://github.com/gameguild-gg/gameguild/compare/v2.50.7...v2.50.8) (2026-03-19)


### Bug Fixes

* improve the timings for the database final project ([0e91ebd](https://github.com/gameguild-gg/gameguild/commit/0e91ebdf24c67a3ece35d945322ce096ce13157f))

## [2.50.7](https://github.com/gameguild-gg/gameguild/compare/v2.50.6...v2.50.7) (2026-03-19)


### Bug Fixes

* database grading ([f5c9931](https://github.com/gameguild-gg/gameguild/commit/f5c99318e2f9e914e2ebf732390a87c558e44356))

## [2.50.6](https://github.com/gameguild-gg/gameguild/compare/v2.50.5...v2.50.6) (2026-03-19)


### Bug Fixes

* **database-course:** mongodb and final project ([225844d](https://github.com/gameguild-gg/gameguild/commit/225844d6da12a95378188fff84e6864a56681a43))

## [2.50.5](https://github.com/gameguild-gg/gameguild/compare/v2.50.4...v2.50.5) (2026-03-19)


### Bug Fixes

* reveal and minor typos ([77aa7cf](https://github.com/gameguild-gg/gameguild/commit/77aa7cf7dc4d2e3e70e450f63e8580d888ef4d58))

## [2.50.4](https://github.com/gameguild-gg/gameguild/compare/v2.50.3...v2.50.4) (2026-03-17)


### Bug Fixes

* networking final project ([e244b3a](https://github.com/gameguild-gg/gameguild/commit/e244b3a60688c412cf6a9f3921c7b0f04c902660))

## [2.50.3](https://github.com/gameguild-gg/gameguild/compare/v2.50.2...v2.50.3) (2026-03-17)


### Bug Fixes

* emception ci ([3dcfb4d](https://github.com/gameguild-gg/gameguild/commit/3dcfb4d37bd7d16a3c531020497fb283f1a0e8ec))
* emception file path ([ca6fff4](https://github.com/gameguild-gg/gameguild/commit/ca6fff47c15632e2b35b9d5e4a12b80e3b66c47c))

## [2.50.2](https://github.com/gameguild-gg/gameguild/compare/v2.50.1...v2.50.2) (2026-03-16)


### Bug Fixes

* reveal js ([c20758e](https://github.com/gameguild-gg/gameguild/commit/c20758e0f1e3a20c1c7e4d127468fa2a7917c833))

## [2.50.1](https://github.com/gameguild-gg/gameguild/compare/v2.50.0...v2.50.1) (2026-03-16)


### Bug Fixes

* **apps/web:** Fix underscore interpretation in math and improve Mermaid rendering in RevealJS ([8e4233d](https://github.com/gameguild-gg/gameguild/commit/8e4233d711461b1dc5fb1096b8259b4a6f3737c6))
* mongodb graph ([079dd85](https://github.com/gameguild-gg/gameguild/commit/079dd852eb09a0ae69839a6ae7760e97b57b9668))
* revealjs compilation issue ([ff7eb50](https://github.com/gameguild-gg/gameguild/commit/ff7eb50dd962c1bab0c974834e2c7f1729d5f590))

# [2.50.0](https://github.com/gameguild-gg/gameguild/compare/v2.49.6...v2.50.0) (2026-03-16)


### Bug Fixes

* **apps/web:** Fix Langium module resolution and update editor dependencies ([e8d672c](https://github.com/gameguild-gg/gameguild/commit/e8d672cd4f1acec9fbc0c7f743962f69102afb17))
* **apps/web:** Improve Langium module resolution for diverse environments ([645cd5a](https://github.com/gameguild-gg/gameguild/commit/645cd5ab53bb2088afc24b2fd47196ea97686883))


### Features

* **apps/web/ai4games2:** Add comprehensive final project content and assignments ([66511cc](https://github.com/gameguild-gg/gameguild/commit/66511ccd04324802134128b3c3d28d1eec78c18f))
* **apps/web/courses:** Add Wave Function Collapse and update MongoDB fundamentals for Week 10 ([0313c46](https://github.com/gameguild-gg/gameguild/commit/0313c46a725acf23bbfdbc1cdd5a0cee73e4ef37))

## [2.49.6](https://github.com/gameguild-gg/gameguild/compare/v2.49.5...v2.49.6) (2026-03-15)


### Bug Fixes

* ci again ([b18b48a](https://github.com/gameguild-gg/gameguild/commit/b18b48a5d32c0eece9788f35c4789fc20f3675fd))

## [2.49.5](https://github.com/gameguild-gg/gameguild/compare/v2.49.4...v2.49.5) (2026-03-15)


### Bug Fixes

* run step if files have changed. fix emsdk folder path ([5284a7e](https://github.com/gameguild-gg/gameguild/commit/5284a7e8f0e2c9f5b7a1af12d2d28a05e580589f))

## [2.49.4](https://github.com/gameguild-gg/gameguild/compare/v2.49.3...v2.49.4) (2026-03-15)


### Bug Fixes

* organize emception demos ([f873a84](https://github.com/gameguild-gg/gameguild/commit/f873a84ecd3684e5b70d593d389503c86758e585))

## [2.49.3](https://github.com/gameguild-gg/gameguild/compare/v2.49.2...v2.49.3) (2026-03-09)


### Bug Fixes

* detect python ([6ca828a](https://github.com/gameguild-gg/gameguild/commit/6ca828a83b9d8ba10cda83ab38c9d38e09d73660))

## [2.49.2](https://github.com/gameguild-gg/gameguild/compare/v2.49.1...v2.49.2) (2026-03-09)


### Bug Fixes

* ci... again ([d467402](https://github.com/gameguild-gg/gameguild/commit/d467402f919e4c4bdf617d6b4412f834b20ea704))

## [2.49.1](https://github.com/gameguild-gg/gameguild/compare/v2.49.0...v2.49.1) (2026-03-09)


### Bug Fixes

* ci ([8ed65f3](https://github.com/gameguild-gg/gameguild/commit/8ed65f355a4dcd875305592bbb91c150b965388c))

# [2.49.0](https://github.com/gameguild-gg/gameguild/compare/v2.48.3...v2.49.0) (2026-03-09)


### Bug Fixes

* ci ([e3baebb](https://github.com/gameguild-gg/gameguild/commit/e3baebb376c09e10b2580ac8f27fd054ca2194d2))
* ci ([3421884](https://github.com/gameguild-gg/gameguild/commit/3421884afd20a0da75cae6dea7f1d97909946dda))
* ci ([a93c587](https://github.com/gameguild-gg/gameguild/commit/a93c587bef07f2d7f11e29cbba5d06ff5dc06854))


### Features

* break emception into 2 projects: app and tool ([16d69c6](https://github.com/gameguild-gg/gameguild/commit/16d69c69a256903581191e2134f0ece0a7e26d14))

## [2.48.3](https://github.com/gameguild-gg/gameguild/compare/v2.48.2...v2.48.3) (2026-03-06)


### Bug Fixes

* ci/cd ([caa0e1c](https://github.com/gameguild-gg/gameguild/commit/caa0e1c436a19e3d34859549fcee46dd2e476cc0))

## [2.48.2](https://github.com/gameguild-gg/gameguild/compare/v2.48.1...v2.48.2) (2026-03-06)


### Bug Fixes

* stdin on emception ([92c4e33](https://github.com/gameguild-gg/gameguild/commit/92c4e3366a7375fe818fbb85d58848d3145a2bf4))

## [2.48.1](https://github.com/gameguild-gg/gameguild/compare/v2.48.0...v2.48.1) (2026-03-06)


### Bug Fixes

* **emception:** Improve LLVM source acquisition and suppress filelock warning ([bf2488e](https://github.com/gameguild-gg/gameguild/commit/bf2488e56f37a1acf3229319fc0fc23e047ea7eb))

# [2.48.0](https://github.com/gameguild-gg/gameguild/compare/v2.47.0...v2.48.0) (2026-03-06)


### Features

* **emception:** Enhance JSPI with WASM exceptions and dynamic versioning ([c6b1387](https://github.com/gameguild-gg/gameguild/commit/c6b13877f071600dbb20937003d3fd5f96491ffb))

# [2.47.0](https://github.com/gameguild-gg/gameguild/compare/v2.46.0...v2.47.0) (2026-03-05)


### Features

* **orchestrator:** Enhance tool execution with Python error capture and VFS caching ([9eeaf7e](https://github.com/gameguild-gg/gameguild/commit/9eeaf7e9027635acb814e1552fd51301a926cce3))

# [2.46.0](https://github.com/gameguild-gg/gameguild/compare/v2.45.0...v2.46.0) (2026-03-04)


### Features

* **emception:** Add VFS asset bundling for faster loading ([5216ac6](https://github.com/gameguild-gg/gameguild/commit/5216ac66f826e9182e0f200a8df0edea80349139))

# [2.45.0](https://github.com/gameguild-gg/gameguild/compare/v2.44.0...v2.45.0) (2026-03-04)


### Features

* **tools:** add Emception browser-based C/C++ toolchain ([b9d4e29](https://github.com/gameguild-gg/gameguild/commit/b9d4e2988b8dbcf686150bf9177c1867a45003c1))

# [2.44.0](https://github.com/gameguild-gg/gameguild/compare/v2.43.0...v2.44.0) (2026-03-02)


### Features

* **ai4games2:** Add midterm chess engine competition ([dc6bffa](https://github.com/gameguild-gg/gameguild/commit/dc6bffa46424c4293e72eb7b7836cd927353d116))

# [2.43.0](https://github.com/gameguild-gg/gameguild/compare/v2.42.2...v2.43.0) (2026-03-02)


### Features

* **courses/ai4games2:** Add Week 07: Advanced Chess Techniques module ([8cdef58](https://github.com/gameguild-gg/gameguild/commit/8cdef58d66f2c1dca355c598da30193ec274818e))

## [2.42.2](https://github.com/gameguild-gg/gameguild/compare/v2.42.1...v2.42.2) (2026-02-27)


### Bug Fixes

* **data-analysis:** add workbook for data vis ([99b50ea](https://github.com/gameguild-gg/gameguild/commit/99b50ea8b4c499ad340f2d0463d7b334c87a193d))

## [2.42.1](https://github.com/gameguild-gg/gameguild/compare/v2.42.0...v2.42.1) (2026-02-26)


### Bug Fixes

* add quiz for week 07 networking ([ac1e58e](https://github.com/gameguild-gg/gameguild/commit/ac1e58e7635649a94c3705d1b76d2d432df096b5))

# [2.42.0](https://github.com/gameguild-gg/gameguild/compare/v2.41.7...v2.42.0) (2026-02-24)


### Features

* **courses/networking:** add Week 07 lecture and readings on distributed state synchronization ([a24558e](https://github.com/gameguild-gg/gameguild/commit/a24558ea18eeb92291c578d5d5c617ff0db414fb))
* **courses/networking:** distributed states ([43de5e2](https://github.com/gameguild-gg/gameguild/commit/43de5e2d9bbffa428b56e3392604d7ce951aa17b))

## [2.41.7](https://github.com/gameguild-gg/gameguild/compare/v2.41.6...v2.41.7) (2026-02-23)


### Bug Fixes

* **courses/databases:** mermaid renderer ([a58aa59](https://github.com/gameguild-gg/gameguild/commit/a58aa59fe143c7098dfd2c3846f7cb2ee69f0a7b))

## [2.41.6](https://github.com/gameguild-gg/gameguild/compare/v2.41.5...v2.41.6) (2026-02-23)


### Bug Fixes

* **courses/databases:** add week07 content ([07f4f24](https://github.com/gameguild-gg/gameguild/commit/07f4f246aaa4eddb7d3a07a840e7930638052a93))
* quiz week 07 ([28defdb](https://github.com/gameguild-gg/gameguild/commit/28defdb8762ae9d69150bc01e710e06eae45204b))

## [2.41.5](https://github.com/gameguild-gg/gameguild/compare/v2.41.4...v2.41.5) (2026-02-23)


### Bug Fixes

* varint ([f516590](https://github.com/gameguild-gg/gameguild/commit/f5165901f0d8c850d79e8f9583a108b93ca1e282))

## [2.41.4](https://github.com/gameguild-gg/gameguild/compare/v2.41.3...v2.41.4) (2026-02-23)


### Bug Fixes

* add more varint comments ([65a0c7b](https://github.com/gameguild-gg/gameguild/commit/65a0c7be2101a2d9250c0cdf4d74c367025fbad3))

## [2.41.3](https://github.com/gameguild-gg/gameguild/compare/v2.41.2...v2.41.3) (2026-02-23)


### Bug Fixes

* add comments to the varint code ([3edeb3d](https://github.com/gameguild-gg/gameguild/commit/3edeb3dbb10099fc5649f5be5031dfbf17ac3d09))
* binary formats varint ([c4df6d0](https://github.com/gameguild-gg/gameguild/commit/c4df6d0e970d485832440308e0023b54829b1b0a))
* varints ([d9cea88](https://github.com/gameguild-gg/gameguild/commit/d9cea884328713bdcf8ca24bec83787369734e56))

## [2.41.2](https://github.com/gameguild-gg/gameguild/compare/v2.41.1...v2.41.2) (2026-02-22)


### Bug Fixes

* not null logic ([e5829d9](https://github.com/gameguild-gg/gameguild/commit/e5829d9704483c1fb2826c3d43df0d7ec1a4cc85))

## [2.41.1](https://github.com/gameguild-gg/gameguild/compare/v2.41.0...v2.41.1) (2026-02-22)


### Bug Fixes

* marp plugin ([e268a25](https://github.com/gameguild-gg/gameguild/commit/e268a25ff9fa327658ec64731d576ed920a4d7b8))

# [2.40.0](https://github.com/gameguild-gg/gameguild/compare/v2.39.0...v2.40.0) (2026-01-20)


### Features

* **networking/week02:** Add detailed lecture slides and new quiz format ([686be05](https://github.com/gameguild-gg/gameguild/commit/686be0545f5175b423ac66f90076dd884aaf8cf8))

# [2.39.0](https://github.com/gameguild-gg/gameguild/compare/v2.38.0...v2.39.0) (2026-01-15)


### Features

* **ai4games2:** Add Week 3 Utility AI module ([6ca9031](https://github.com/gameguild-gg/gameguild/commit/6ca9031ff23d2a1bc6e51a26724b33042eddba68))

# [2.38.0](https://github.com/gameguild-gg/gameguild/compare/v2.37.0...v2.38.0) (2026-01-12)


### Features

* **apps/web:** Add Marp and RemarkJS presentation support ([a1beb8f](https://github.com/gameguild-gg/gameguild/commit/a1beb8f9d1ce3ba403c84cdfbb5cd71cc64c62bf))
* **apps/web:** Integrate Marp Core for presentation authoring ([b2fa44c](https://github.com/gameguild-gg/gameguild/commit/b2fa44c8598fbf3046ed2746bad234a87f1a0dd0))
* **markdown-renderer:** Enable Reveal.js presentations with optimized settings ([066c00f](https://github.com/gameguild-gg/gameguild/commit/066c00f14714ea48b394dda3bc7fbc9d6eddc732))

# [2.37.0](https://github.com/gameguild-gg/gameguild/compare/v2.36.0...v2.37.0) (2026-01-12)


### Features

* **content:** Add Reveal content type and enhance presentation rendering ([6b8070d](https://github.com/gameguild-gg/gameguild/commit/6b8070d975b359f651c6240d4c93a2d1ba901ab7))
* **program-content:** Introduce Marp presentation content type ([66d467b](https://github.com/gameguild-gg/gameguild/commit/66d467b822fdf05ce6e68f68f5cf8e23051f0372))

# [2.36.0](https://github.com/gameguild-gg/gameguild/compare/v2.35.0...v2.36.0) (2026-01-12)


### Bug Fixes

* **apps/web/course-content:** Prevent null access in frontmatter renderer detection ([baaa2b2](https://github.com/gameguild-gg/gameguild/commit/baaa2b2af94ab3d936d3e725044836496a105e16))


### Features

* **apps/web/course-content:** Enable presentation-style markdown via frontmatter ([37784c7](https://github.com/gameguild-gg/gameguild/commit/37784c72e0a41de9538b17f78b3cb8fcf29a8843))

# [2.35.0](https://github.com/gameguild-gg/gameguild/compare/v2.34.0...v2.35.0) (2026-01-11)


### Bug Fixes

* **apps/web/markdown-renderer:** Improve word wrapping for code blocks and inline code ([690c034](https://github.com/gameguild-gg/gameguild/commit/690c034ac298c3acc030a33fb22abfcebf44958a))
* **dashes:** replace all strange dash behavior ([a7c3448](https://github.com/gameguild-gg/gameguild/commit/a7c34482a9f7bb83edd499cdd2ec7996dc22fe95))
* **web:** fix TypeScript errors in weeks 11-14 database content files ([acba836](https://github.com/gameguild-gg/gameguild/commit/acba8369256cade7aba632069c53505bccfb7ef0))


### Features

* add Drizzle ORM MongoDB integration guide ([789f335](https://github.com/gameguild-gg/gameguild/commit/789f335c492e6962672caf104a1e9e1c74872413))
* add MongoDB aggregation pipeline guide ([735d889](https://github.com/gameguild-gg/gameguild/commit/735d8899b9355b3ba4a666384c95bac98e7c60a7))
* add MongoDB CRUD operations documentation ([9a267ff](https://github.com/gameguild-gg/gameguild/commit/9a267ff69451fe8febac9da7cbbb812aa3167c39))
* add MongoDB resources and quiz content ([741db1b](https://github.com/gameguild-gg/gameguild/commit/741db1ba42932eb17e02ad5730e600ca69677ca3))
* add MongoDB schema design patterns guide ([826709c](https://github.com/gameguild-gg/gameguild/commit/826709cf62ff89d9dbe2d9ddfe9e2227e124ac26))
* add Week 10 MongoDB content structure and exports ([767c64d](https://github.com/gameguild-gg/gameguild/commit/767c64d60a00603c3925fb0b7ee0832a094f0bb3))
* add Week 10 MongoDB fundamentals content ([6523eaa](https://github.com/gameguild-gg/gameguild/commit/6523eaa7c8497d1efe81f62a4b006734e87e3bb6))
* **courses/ai4games2:** Introduce Behavior Trees module ([ce84638](https://github.com/gameguild-gg/gameguild/commit/ce8463872533bf73843b9dad6cf4f24f9fcd2215))
* **sql:** add week 02 to databases course ([e727526](https://github.com/gameguild-gg/gameguild/commit/e727526b496c7c37bbbfdfcb791196998ce77d46))

# [2.34.0](https://github.com/gameguild-gg/gameguild/compare/v2.33.0...v2.34.0) (2026-01-07)


### Features

* **courses/networking/week02:** Add Network Addressing module with C++ assignment ([8b95938](https://github.com/gameguild-gg/gameguild/commit/8b95938c8ed9225aa54a749c06e0aabb956417ce))

# [2.33.0](https://github.com/gameguild-gg/gameguild/compare/v2.32.0...v2.33.0) (2026-01-06)


### Features

* **courses/ai4games2/fsm:** Add Finite State Machine assignment ([176531f](https://github.com/gameguild-gg/gameguild/commit/176531f405377d407e03e6c4c4242f4c2823be01))

# [2.32.0](https://github.com/gameguild-gg/gameguild/compare/v2.31.0...v2.32.0) (2026-01-06)


### Features

* **courses/ai4games2/fsm:** Refine FSM example with state registration and improved transition mapping ([a9099cd](https://github.com/gameguild-gg/gameguild/commit/a9099cd877b4728d3d9f96fb353332bd6fb860ce))

# [2.31.0](https://github.com/gameguild-gg/gameguild/compare/v2.30.0...v2.31.0) (2026-01-06)


### Features

* **apps/web/ai4games2:** Add FSM & Decision Architectures module, reorganize course content ([f3b7551](https://github.com/gameguild-gg/gameguild/commit/f3b755194099de35f60f69d49e280fa199243b70))

# [2.30.0](https://github.com/gameguild-gg/gameguild/compare/v2.29.0...v2.30.0) (2026-01-05)


### Features

* **courses/ai4games2:** Add slug for syllabus content ([967838a](https://github.com/gameguild-gg/gameguild/commit/967838a0440ce742734cedddea07588967cea572))

# [2.29.0](https://github.com/gameguild-gg/gameguild/compare/v2.28.0...v2.29.0) (2026-01-05)


### Features

* **apps/web/markdown-renderer:** Implement SQL support in code activity component ([9e54d41](https://github.com/gameguild-gg/gameguild/commit/9e54d41a848fb4c4341e5965d2e2c690c38b1b71))

# [2.28.0](https://github.com/gameguild-gg/gameguild/compare/v2.27.0...v2.28.0) (2026-01-04)


### Features

* **databases/week1:** Add Quiz 01 and refine Week 1 course content ([a8d4ede](https://github.com/gameguild-gg/gameguild/commit/a8d4ede56dfe15b6474184669e75dadfec14e364))

# [2.27.0](https://github.com/gameguild-gg/gameguild/compare/v2.26.0...v2.27.0) (2026-01-04)


### Features

* **courses/databases:** Add comprehensive database zoo lesson and refine decision guide ([dde45ff](https://github.com/gameguild-gg/gameguild/commit/dde45ff823eca1b25c92813a968e2c86672166cb))

# [2.26.0](https://github.com/gameguild-gg/gameguild/compare/v2.25.0...v2.26.0) (2026-01-02)


### Features

* **apps/web/courses/databases:** Define course structure and Week 01 module ([d089dd0](https://github.com/gameguild-gg/gameguild/commit/d089dd04c2d63d15b63013ab1faecbf54236ad71))
* **course-sidebar:** Display cumulative duration for content items ([c38b421](https://github.com/gameguild-gg/gameguild/commit/c38b4218b))
* **courses/networking:** Add Week 01 quiz and update syllabus ([af4883c](https://github.com/gameguild-gg/gameguild/commit/af4883c73))
* **courses/networking/intro:** Add comprehensive networking fundamentals lecture and quiz ([ac5a365](https://github.com/gameguild-gg/gameguild/commit/ac5a3651d))
* **apps/web:** Add Networking Week 01 and content updates ([6027d17](https://github.com/gameguild-gg/gameguild/commit/6027d1719))
* **apps/web:** Add image proxy and networking course intro ([67d1ba2](https://github.com/gameguild-gg/gameguild/commit/67d1ba280))
* **courses:** Add Game Networking course and enhance image fields ([3017369](https://github.com/gameguild-gg/gameguild/commit/30173695c))
* **courses/databases:** Update syllabus to include DDL, DQL, and DML operations; enhance normalization and schema design content ([440304b](https://github.com/gameguild-gg/gameguild/commit/440304b98))
* **courses:** Add comprehensive networking syllabus and update database details ([86f1a28](https://github.com/gameguild-gg/gameguild/commit/86f1a2848))
* **courses/databases:** Add initial module content and expand syllabus ([04faf8c](https://github.com/gameguild-gg/gameguild/commit/04faf8cfa))
* **data/courses:** Add Databases course ([938b4e9](https://github.com/gameguild-gg/gameguild/commit/938b4e96b))


### Bug Fixes

* networking ([7be38d8](https://github.com/gameguild-gg/gameguild/commit/7be38d871))


### Documentation

* **courses/databases:** Refine instructor contact links and add meeting option ([91b345d](https://github.com/gameguild-gg/gameguild/commit/91b345d34))
* **web/courses/databases:** Update course thumbnail and add to syllabus ([a8361f8](https://github.com/gameguild-gg/gameguild/commit/a8361f8fa))
* **courses/networking/intro:** Reorder answers for networking fundamentals quiz ([505384c](https://github.com/gameguild-gg/gameguild/commit/505384ce2))
* **networking-syllabus:** Clarify course assessment and grading methods ([82effcd](https://github.com/gameguild-gg/gameguild/commit/82effcd85))
* **courses:** Add instructor details and refine course content ([c05208c](https://github.com/gameguild-gg/gameguild/commit/c05208c73))
* add outer joins and advanced patterns lesson ([185861b](https://github.com/gameguild-gg/gameguild/commit/185861bc8))
* add join fundamentals lesson for database course ([360e644](https://github.com/gameguild-gg/gameguild/commit/360e64495))
* **ai4games2:** Add comprehensive course syllabus ([ca7ce60](https://github.com/gameguild-gg/gameguild/commit/ca7ce6076))
* **courses:** Improve syllabi content and refine mock data order ([d9453f8](https://github.com/gameguild-gg/gameguild/commit/d9453f8ef))



# [2.25.0](https://github.com/gameguild-gg/gameguild/compare/v2.24.0...v2.25.0) (2025-12-09)


### Bug Fixes

* Add 'new' keyword to resolve property hiding warnings across modules ([89019a3](https://github.com/gameguild-gg/gameguild/commit/89019a36739d5c529a877690dc6b250c0191c05a))
* add back gource video generation ([81fe005](https://github.com/gameguild-gg/gameguild/commit/81fe00564bb4145c9fc629b068f77bd863c323e9))
* Add CQRS and Database namespaces, remove invalid references ([118d784](https://github.com/gameguild-gg/gameguild/commit/118d78436bb15c4a868c83f9102850a0abb490fe))
* Add missing namespace references and CQRS imports ([dc233da](https://github.com/gameguild-gg/gameguild/commit/dc233da73c7c66acace88bd6fdd759b123813c38))
* add TODO comments and temporary types for missing API generation ([a9d2704](https://github.com/gameguild-gg/gameguild/commit/a9d2704f155237541d8128d6107b029f9eba512c))
* api gen to generate enums properly ([adb9442](https://github.com/gameguild-gg/gameguild/commit/adb94426b6074c61d523f6f6f1c19d23cfbfd723))
* **api:** Add GameGuild.Authorization import to TenantsController (36 errors fixed) ([374eb74](https://github.com/gameguild-gg/gameguild/commit/374eb74c69f35b0d0679df4818ca398938479782))
* **api:** Add missing enum imports to Programs and UserAchievements modules (202 errors fixed) ([3aa4f2c](https://github.com/gameguild-gg/gameguild/commit/3aa4f2c7144bb23af335c259351807fad630e292))
* **api:** Fix context import namespaces in Projects module (72 errors fixed) ([13611bf](https://github.com/gameguild-gg/gameguild/commit/13611bff197da4776a62839ed4997b9b7350cad8))
* **api:** Fix context imports, Product namespace, and ResourcePermission constraint (120 errors fixed) ([1027b35](https://github.com/gameguild-gg/gameguild/commit/1027b35eed84ac12032a8eb1870c21d088c75fcd))
* **api:** Fix CQRS interfaces, missing imports, and override modifiers (24 errors fixed) ([01e0836](https://github.com/gameguild-gg/gameguild/commit/01e0836510ca068516a33f672e8b182575c111b4))
* **api:** Fix enum ambiguity and Slug index in Programs (2 errors fixed) ([cd8c7a6](https://github.com/gameguild-gg/gameguild/commit/cd8c7a651a308004f6ed20ffee81dc45eaf18b5f))
* **api:** Fix Product namespace ambiguity in ProductController (26 errors fixed) ([d1ef749](https://github.com/gameguild-gg/gameguild/commit/d1ef749ed64d8e9c5505304c5d728c21a4eb5f21))
* **api:** Fix property hiding warnings and implement missing interface methods (86 errors fixed) ([1fa4add](https://github.com/gameguild-gg/gameguild/commit/1fa4add458873d4c28d55252a5f441f994078fa4))
* **api:** Fix SLA queries, ActivityGrade attribute, Program indexes, and ProjectFeedback (34 errors fixed) ([bc4a8d5](https://github.com/gameguild-gg/gameguild/commit/bc4a8d588e4646302c3832cf29149b065e2d00e8))
* **api:** Replace Product with ProductEntity in ProductController attributes (52 errors fixed) ([4017792](https://github.com/gameguild-gg/gameguild/commit/4017792080650b9ca0bbe1fd90d385d56c0d8e01))
* **api:** Resolve duplicate usings, context interfaces, and missing enums (22 errors fixed) ([85673d9](https://github.com/gameguild-gg/gameguild/commit/85673d969282a5f2f706c4a7cc1ba3b0d7043724))
* **api:** Resolve namespace and interface errors (340 errors fixed) ([f68d1e2](https://github.com/gameguild-gg/gameguild/commit/f68d1e25252c7f4f7c1738f7e65bf20b89d44ad9))
* **apps/web/hooks:** Clean up import order and whitespace in useUserDetail hook ([c732bcd](https://github.com/gameguild-gg/gameguild/commit/c732bcddef496006bdbc9b6011fcc991ba728b64))
* **apps/web:** Correct achievements actions export path ([212db6d](https://github.com/gameguild-gg/gameguild/commit/212db6d4008e8896bbeadcee3ddfc01eacfbcdb8))
* **apps/web:** Correct prop name for TenantsList in tenant dashboard page ([bbaaa80](https://github.com/gameguild-gg/gameguild/commit/bbaaa80bb25be8785820cf52e3b52aa792920763))
* **auth/anomaly:** correct property initialization for unique counters ([f15f7d0](https://github.com/gameguild-gg/gameguild/commit/f15f7d01cf681d8dedf2efd1a8c84e18ba4edf52))
* **auth/mfa:** ensure Success result returns array for backup codes ([38d5de6](https://github.com/gameguild-gg/gameguild/commit/38d5de6d0ce453a977d4193bac5565274b899603))
* **auth/session:** use correct termination reason for user-initiated logout ([06242d9](https://github.com/gameguild-gg/gameguild/commit/06242d931b8d1989e5660e5b0ee9de7dc8784986))
* build by enforcing types ([934eed7](https://github.com/gameguild-gg/gameguild/commit/934eed73b9cd26f959a63cba8d912747d79571c5))
* **compat:** disable missing GraphQL extension and EF query splitting ([d4799ac](https://github.com/gameguild-gg/gameguild/commit/d4799ac4ca6f41f37710d1db50490d90ca5cd78c))
* complete build error resolution across workspaces ([fa7e788](https://github.com/gameguild-gg/gameguild/commit/fa7e788db4349a4e3140a885c063bf3cbfdcceb3))
* **core/exceptions:** normalize validation problem details payload ([c96ccd6](https://github.com/gameguild-gg/gameguild/commit/c96ccd642089a40c429bb75b23ef98a2a10992c0))
* **core:** add missing middleware namespace import ([c771a30](https://github.com/gameguild-gg/gameguild/commit/c771a30433c710eb7ef35f42bed5f680641caeb2))
* Create missing DTOs and enums, add DomainEvent base class ([db50d8e](https://github.com/gameguild-gg/gameguild/commit/db50d8e41604ed4c02a43e9b595d3754cedba378))
* **db:** correct model namespaces in ApplicationDbContext ([586431e](https://github.com/gameguild-gg/gameguild/commit/586431e8bcac34f3afd5a74ed959714edbdc2066))
* **events:** add EF Core and database usings to DomainEventProcessorService ([4c4f4ec](https://github.com/gameguild-gg/gameguild/commit/4c4f4ec7178a159563a09322fc762cf53585d3ef))
* **gglexical:** adapt new page names ([aa528a5](https://github.com/gameguild-gg/gameguild/commit/aa528a5ca1386b7def1951fa0080a3fe064ab4e6))
* **gglexical:** admonition styles ([43dc5b7](https://github.com/gameguild-gg/gameguild/commit/43dc5b74a1414a2cf5a926a238be6bbdd143ec99))
* **gglexical:** api tagData ([451c87e](https://github.com/gameguild-gg/gameguild/commit/451c87e8127b967907a2154e15556fcae7af8649))
* **gglexical:** button plugin fix style ([5999d24](https://github.com/gameguild-gg/gameguild/commit/5999d24305484ba4cb37f9de7cd03996b86efd0f))
* **gglexical:** code editor button editor ([8b17a29](https://github.com/gameguild-gg/gameguild/commit/8b17a29345c9329097f8f5eee701885b76a3eefb))
* **gglexical:** code editor cancel its save ([31e52cc](https://github.com/gameguild-gg/gameguild/commit/31e52ccff3be0caef8504dd5abce9b2f7244c8d6))
* **gglexical:** code editor command palette fix ([9d37f4f](https://github.com/gameguild-gg/gameguild/commit/9d37f4f1b878cdd4288523b77be379a69e905d19))
* **gglexical:** code editor drag tab on instance unique ([dbe52e9](https://github.com/gameguild-gg/gameguild/commit/dbe52e9e53553ef31c4cf1f2ad0844bccdc01153))
* **gglexical:** code editor execution and test modes ([aba04a0](https://github.com/gameguild-gg/gameguild/commit/aba04a0030cb6d42ac707869f27dd266d82821c5))
* **gglexical:** code editor file explorer select ([4e27ae6](https://github.com/gameguild-gg/gameguild/commit/4e27ae6f5ee62d1653e008900c379bdc91dae0f6))
* **gglexical:** code editor file explorer subfolders ([d7cb984](https://github.com/gameguild-gg/gameguild/commit/d7cb9840c541f24b0f62663a1f370936b011fdb0))
* **gglexical:** code editor file tab selected file inside a folder ([65e5a53](https://github.com/gameguild-gg/gameguild/commit/65e5a53bf90c434be8827fa5855b26f708c37717))
* **gglexical:** code editor file-explorer ([5ea3e5e](https://github.com/gameguild-gg/gameguild/commit/5ea3e5e5df8091a7d25ddeba7a777e9282638faa))
* **gglexical:** code editor file-explorer drag-drop style ([81e00d1](https://github.com/gameguild-gg/gameguild/commit/81e00d1d16e09067070b546353be2a58d8732cc4))
* **gglexical:** code editor file-explorer reorder files ([9ec42c2](https://github.com/gameguild-gg/gameguild/commit/9ec42c23c80883d8aa95d799a1f57d45457d386b))
* **gglexical:** code editor file-explorer selected file display ([f346478](https://github.com/gameguild-gg/gameguild/commit/f346478073f7110ca819435449624a3f9e43d274))
* **gglexical:** code editor fix display name ([1168c4e](https://github.com/gameguild-gg/gameguild/commit/1168c4ed2fb4940390c503f7ed3c86b402bedf80))
* **gglexical:** code editor hello-world files ([c14cca7](https://github.com/gameguild-gg/gameguild/commit/c14cca7fa8e93f3c4cd819d678114f7bfe6bce7a))
* **gglexical:** code editor instanceID ([92e6342](https://github.com/gameguild-gg/gameguild/commit/92e634260cdbe8e094156e686fd27aef0c56a85b))
* **gglexical:** code editor layout button fix ([c132c87](https://github.com/gameguild-gg/gameguild/commit/c132c879ebb9264c0b82dec5f3cf1ee2975554b7))
* **gglexical:** code editor major number of colums ([4b17748](https://github.com/gameguild-gg/gameguild/commit/4b17748a734e7a3d9b988ca7aefd25c15d8d011d))
* **gglexical:** code editor monaco background logo ([182ae80](https://github.com/gameguild-gg/gameguild/commit/182ae804a0e45a35fcb3823fbbed362a06997ea2))
* **gglexical:** code editor monaco link ([95ab268](https://github.com/gameguild-gg/gameguild/commit/95ab268df43c74dac93511e1772b77c228617111))
* **gglexical:** code editor monaco undo/redo ([4571ea9](https://github.com/gameguild-gg/gameguild/commit/4571ea9e90533c83bf23e84121865cdfe5fe6f73))
* **gglexical:** code editor node fix ([9d50077](https://github.com/gameguild-gg/gameguild/commit/9d5007769259cd0f02f6c022610910e8d0a4a3ff))
* **gglexical:** code editor path ([5f9dd16](https://github.com/gameguild-gg/gameguild/commit/5f9dd16edf35b2cd5351734d35f033f9faa32b8d))
* **gglexical:** code editor path error ([0bb3ec9](https://github.com/gameguild-gg/gameguild/commit/0bb3ec98e036ec30b510a4558ad5fc1560509983))
* **gglexical:** code editor preview/viewer ([ed57540](https://github.com/gameguild-gg/gameguild/commit/ed5754027b2ee4e36d7b71a346458c79ba26514c))
* **gglexical:** code editor quickjs race condition fix ([9cfd712](https://github.com/gameguild-gg/gameguild/commit/9cfd712d8534a12a13bbba098155b111c16d8c5f))
* **gglexical:** code editor remove button editor old ([cf159d0](https://github.com/gameguild-gg/gameguild/commit/cf159d075d08d79d3b09e756dbcb5d9ef9d99719))
* **gglexical:** code editor remove monaco message "loading" ([453dac7](https://github.com/gameguild-gg/gameguild/commit/453dac7e2900ba54dbad5f005cfc70a0754dce4d))
* **gglexical:** code editor resizable panel 4 directions ([317284b](https://github.com/gameguild-gg/gameguild/commit/317284b7e76938c8e711b799c5c668785ae980d9))
* **gglexical:** code editor resizable panel up to panel ([0b1b93c](https://github.com/gameguild-gg/gameguild/commit/0b1b93c29fac4fb0017d6cc93ed371dbac68df93))
* **gglexical:** code editor save outside editor ([ebb5a9b](https://github.com/gameguild-gg/gameguild/commit/ebb5a9b9db9c5824f7fc7fa1c75490a03884d1d6))
* **gglexical:** code editor save system ([3e385ed](https://github.com/gameguild-gg/gameguild/commit/3e385edd62d292a27f2c0006257b292017991050))
* **gglexical:** code editor tab and file-explorer not sync ([c9d9fc1](https://github.com/gameguild-gg/gameguild/commit/c9d9fc1e3a761e6a1136d50196699903e61729bb))
* **gglexical:** code editor tabs fix ([86050db](https://github.com/gameguild-gg/gameguild/commit/86050db8aeeafd42a85e1ea728bb79a36b17e5d8))
* **gglexical:** code editor tabs reorder ([5dbbe9f](https://github.com/gameguild-gg/gameguild/commit/5dbbe9f376593ace1f17a270d281b59eaa956e8a))
* **gglexical:** code editor terminal dont execute tab without selected ([dac37eb](https://github.com/gameguild-gg/gameguild/commit/dac37ebb1aff0f41b40502ebf9325a396727303b))
* **gglexical:** code editor terminal scroll ([c4f5d52](https://github.com/gameguild-gg/gameguild/commit/c4f5d526ff0ab66b18e3d7f20ff5b7b9899a159e))
* **gglexical:** code editor view mode ([73127dd](https://github.com/gameguild-gg/gameguild/commit/73127dd3e6cdb225edf5087e7b095e783af2d42f))
* **gglexical:** code studio node constructor ([4d8d332](https://github.com/gameguild-gg/gameguild/commit/4d8d3321c9d75c3bbb5568250f3d5b1e56ecaf9c))
* **gglexical:** create dialog inserts correct storageType ([ebc7f6d](https://github.com/gameguild-gg/gameguild/commit/ebc7f6d896640a44cc0f1a581cf51cfdbd062e27))
* **gglexical:** divider-styles ([116bd9c](https://github.com/gameguild-gg/gameguild/commit/116bd9cbc643a41567ce3b1ae313f112dac19441))
* **gglexical:** doc architecture ([074d5f5](https://github.com/gameguild-gg/gameguild/commit/074d5f56a762c44295c3627a19f7bd486e9ded48))
* **gglexical:** doc gglexical + storage architecture ([6dae552](https://github.com/gameguild-gg/gameguild/commit/6dae552ff07b3c90dcb6e8f14f9df539c2eef363))
* **gglexical:** docs architecture ([0045296](https://github.com/gameguild-gg/gameguild/commit/0045296307ae5a00dd1fcd18504ec0631262514f))
* **gglexical:** editor button fix ([d67ca1b](https://github.com/gameguild-gg/gameguild/commit/d67ca1b7b8b94b2a694cafe20619f1879f519708))
* **gglexical:** editor rounded layout fix ([d51062b](https://github.com/gameguild-gg/gameguild/commit/d51062b82cf442307df1fd84658dd01fe478f89d))
* **gglexical:** fix folder gglexical ([3d7c131](https://github.com/gameguild-gg/gameguild/commit/3d7c131a3babac98c055f8cb769d8389be13ade4))
* **gglexical:** fix storage ([a7f1382](https://github.com/gameguild-gg/gameguild/commit/a7f138281e5dea89c6a90f5f1a5f909afc08ba39))
* **gglexical:** fixed value for itemsPerPage ([fc8f60c](https://github.com/gameguild-gg/gameguild/commit/fc8f60cbd724bd44c6d320f11e8be19ca72f0101))
* **gglexical:** floating content dialog dont scroll ([08d4872](https://github.com/gameguild-gg/gameguild/commit/08d4872ce8a3eca18c179bdd5e5161435ddc5dfa))
* **gglexical:** floating content dialog scrolls studio page ([a570729](https://github.com/gameguild-gg/gameguild/commit/a570729d5290d33c3a98e0a65ce2bda66b3c77c3))
* **gglexical:** formatting fix ([d108d71](https://github.com/gameguild-gg/gameguild/commit/d108d711ba955e88d1092ccabd4e6b93be3a1485))
* **gglexical:** gallery grid ([7f908a4](https://github.com/gameguild-gg/gameguild/commit/7f908a4ebff267856312fbe9a82e122ed438fdc1))
* **gglexical:** gallery-node size control ([660b445](https://github.com/gameguild-gg/gameguild/commit/660b445540ca40abf8a3f421e62b62eeb8e1a9f8))
* **gglexical:** google-drive save document ([76c4d6b](https://github.com/gameguild-gg/gameguild/commit/76c4d6b790959543b9eff5cbe529e3a777f123c4))
* **gglexical:** google-drive sync ([a8b705f](https://github.com/gameguild-gg/gameguild/commit/a8b705f424ad8e2c0ccd3257117173c5c5ee7476))
* **gglexical:** hyperlink plugin ([44d1ff4](https://github.com/gameguild-gg/gameguild/commit/44d1ff41eb61d7114aa2a0c8f3c0cff296bae096))
* **gglexical:** hyperlink plugin and selector http/https/local ([da34efa](https://github.com/gameguild-gg/gameguild/commit/da34efa876ee0cdf5ffc689d73e898918e9d568a))
* **gglexical:** hyperlink plugin component ([7be8195](https://github.com/gameguild-gg/gameguild/commit/7be81955ba892c07438cf6f882e3828f65650846))
* **gglexical:** hyperlink plugin open past link ([689b3fe](https://github.com/gameguild-gg/gameguild/commit/689b3fee5a7d43cd80a6502955829b402e3e917f))
* **gglexical:** import project fixes ([9ff0800](https://github.com/gameguild-gg/gameguild/commit/9ff0800262494c2ac78cb8977d80a0da8aa10aed))
* **gglexical:** info-dialog tags ([2afcacd](https://github.com/gameguild-gg/gameguild/commit/2afcacd6b2bfe3832227f21e35706cfe8595e88a))
* **gglexical:** list color fix ([b89d160](https://github.com/gameguild-gg/gameguild/commit/b89d1609ff606e1151cd13191c7150a53d274209))
* **gglexical:** list color in editor ([c06cab6](https://github.com/gameguild-gg/gameguild/commit/c06cab622c7fa02063da3fc8612cb320bd866db7))
* **gglexical:** list color use color-palette ([bcb7519](https://github.com/gameguild-gg/gameguild/commit/bcb751974be0c2c6a3fe732d4e2166a580fa3bc6))
* **gglexical:** markdown dont open and plugin dialog more space ([9af5c85](https://github.com/gameguild-gg/gameguild/commit/9af5c85e0443ba04dc9e82f50bd2dbc2991b748d))
* **gglexical:** mode filter list ([e2629b8](https://github.com/gameguild-gg/gameguild/commit/e2629b8220060f26747b7ade4394e265e3b2f6d8))
* **gglexical:** modes and modal ([7e4911b](https://github.com/gameguild-gg/gameguild/commit/7e4911b347fe332224e9f9092b744bbea5783323))
* **gglexical:** new Google api GIS ([920fa96](https://github.com/gameguild-gg/gameguild/commit/920fa96afdb5da8521e52626ea2d2c7b3d5ecf3b))
* **gglexical:** next and app/layout ([c7c03e9](https://github.com/gameguild-gg/gameguild/commit/c7c03e98065dac62b28db794bfc5fd5790271aac))
* **gglexical:** open project auto sync googledrive ([ba45d34](https://github.com/gameguild-gg/gameguild/commit/ba45d34d4e2d134de3ec947278d5cae395fa19b9))
* **gglexical:** open project storage fix ([1fbdd7c](https://github.com/gameguild-gg/gameguild/commit/1fbdd7c1aeff139c5f5e29d70378b2d81f06b0b0))
* **gglexical:** open project sync google-drive analyzer ([85b39eb](https://github.com/gameguild-gg/gameguild/commit/85b39eb0ac0a6181e9c5fd865aa73fb83ad93904))
* **gglexical:** ordered list ([889c1ac](https://github.com/gameguild-gg/gameguild/commit/889c1accdc9b3765993dbc6d82f884623ee7acb9))
* **gglexical:** ordered list fix ([d7a422f](https://github.com/gameguild-gg/gameguild/commit/d7a422ff53bf066026c2a1618798120155348a2e))
* **gglexical:** presentation fixes ([175e6a5](https://github.com/gameguild-gg/gameguild/commit/175e6a5eebd2dcc865e78d30f08843d76da168e4))
* **gglexical:** preview width ([3afd0fa](https://github.com/gameguild-gg/gameguild/commit/3afd0fa5479ac798dce4ab56dd34112cf58a09d1))
* **gglexical:** project list line break ([141947d](https://github.com/gameguild-gg/gameguild/commit/141947d6747d92cf1e55d9d9390fb728a053b0e4))
* **gglexical:** quiz editor dark mode fix ([4c01d93](https://github.com/gameguild-gg/gameguild/commit/4c01d93d22a1dbc03a442e985847ba25b484e32d))
* **gglexical:** save and sync efficient ([7f45999](https://github.com/gameguild-gg/gameguild/commit/7f45999625ec9d6ffe419364538a77491205f5a1))
* **gglexical:** semantic fix ([3ca9021](https://github.com/gameguild-gg/gameguild/commit/3ca90211a2b733dd799150b38184e609d13f70bc))
* **gglexical:** side error ([54b80d6](https://github.com/gameguild-gg/gameguild/commit/54b80d677b41bc100f6632c9c3209aed23d12ebb))
* **gglexical:** storage selector fix ([0d92dc2](https://github.com/gameguild-gg/gameguild/commit/0d92dc26622014fc2a1393fecc7104adfe25d594))
* **gglexical:** studio page design fix ([6d8bb05](https://github.com/gameguild-gg/gameguild/commit/6d8bb05ab35cea57b499be99afa3ca3053bf5cc7))
* **gglexical:** superscript fix ([166a097](https://github.com/gameguild-gg/gameguild/commit/166a0978764c06e8915469309dfae16fb33bd8d7))
* **gglexical:** system api for local, gameguild-cloud and google-drive ([aa70b42](https://github.com/gameguild-gg/gameguild/commit/aa70b423c7fb9c345a572774259054010ef57769))
* **gglexical:** table edit dark mode fix ([526a512](https://github.com/gameguild-gg/gameguild/commit/526a512134879852d78f9bc1483dc724b30d95d2))
* **gglexical:** table editor edit button ([6123682](https://github.com/gameguild-gg/gameguild/commit/6123682c36408b6da74bf42d34a6b668e9a367ab))
* **gglexical:** table plugin fix ([791fb97](https://github.com/gameguild-gg/gameguild/commit/791fb971911569040dc9c9b3c0fb5cdb1d9cf751))
* **gglexical:** table style in studio ([96b8243](https://github.com/gameguild-gg/gameguild/commit/96b824360845809b8d83465066bcc8379342e19d))
* **gglexical:** table truncate fix ([dfc842e](https://github.com/gameguild-gg/gameguild/commit/dfc842e1f49da655500cd8ee414857316e4749b6))
* **gglexical:** table viewer fix ([6ccabf3](https://github.com/gameguild-gg/gameguild/commit/6ccabf3f999e93e319a781b98b4db19ce27d4154))
* **gglexical:** table-of-contents container ([6074527](https://github.com/gameguild-gg/gameguild/commit/6074527f5d281a14a5cbc8efa595f73ec7ff3aff))
* **gglexical:** text color apply color in text not marked ([cd40161](https://github.com/gameguild-gg/gameguild/commit/cd4016197ccea18637b3c70644e5f81cf917c2c1))
* **gglexical:** toolbar not open ([942a776](https://github.com/gameguild-gg/gameguild/commit/942a776abd0d4cc20ddc970e915b0ec81f44f466))
* **gglexical:** TopMenu ([ed4543a](https://github.com/gameguild-gg/gameguild/commit/ed4543a1ed14ae96b92ed35fd92fc9fac333f6c9))
* **gglexical:** unordered list fix ([d4491f1](https://github.com/gameguild-gg/gameguild/commit/d4491f1f214fea80425879df1a2423716ac10c4b))
* **gglexical:** view document list layout fix ([ac3adc9](https://github.com/gameguild-gg/gameguild/commit/ac3adc9e675318fb2be3b0472c595a39352b168b))
* **logging:** streamline request context creation and correct header value enumeration ([4e6d9c6](https://github.com/gameguild-gg/gameguild/commit/4e6d9c6453a5aae60f0c3c49e210011e14b002ae))
* **programs:** Change Program base class from Content to EntityBase<Guid> ([1a91d7a](https://github.com/gameguild-gg/gameguild/commit/1a91d7ae15dace95d7166f31f5cc5dd3964558ef))
* rendering issues related to cors ([2f88f7e](https://github.com/gameguild-gg/gameguild/commit/2f88f7e1811dd66f12736e5bbf11881328a4e421))
* Replace MediatR with GameGuild.CQRS and fix namespace references ([21bb3d7](https://github.com/gameguild-gg/gameguild/commit/21bb3d72d9a7cc57f2960d68594643e31b691599))
* Replace remaining MediatR references and add Content namespace ([ec6a43d](https://github.com/gameguild-gg/gameguild/commit/ec6a43ded83dc9e9867c19b0b57b6d1e364a6375))
* Replace remaining MediatR refs and fix Description attribute namespace ([830bcb5](https://github.com/gameguild-gg/gameguild/commit/830bcb5d2caf7e60e605c8aff9648b1b51b92906))
* resolve build errors across workspace packages ([3c31a87](https://github.com/gameguild-gg/gameguild/commit/3c31a8742e43058b7cd235ad39f69f0366e4342d))
* SLA queries, ActivityGrade attribute, Program indexes, ProjectFeedback (34 errors) ([f72f7ec](https://github.com/gameguild-gg/gameguild/commit/f72f7ec3b5913768edc1e5876e2fd14c2539cd28))
* **TestingLab,Projects:** Remove stub methods and fix property hiding (20→12 errors, 8 fixed) ([dc43104](https://github.com/gameguild-gg/gameguild/commit/dc431043e63c0447261905216ba4a9867063f809))
* update references from gameguild to game-guild across multiple files ([6a1b920](https://github.com/gameguild-gg/gameguild/commit/6a1b9204c33d049cbffa730e5e4ed5bededc7833))
* Update RemainingQuota logic to return zero instead of negative value when over limit ([e361263](https://github.com/gameguild-gg/gameguild/commit/e3612639e51267c262e737119430c708dfe00102))
* **web:** Fixes "self is not defined" error ([012b424](https://github.com/gameguild-gg/gameguild/commit/012b42430cb83e6fb0405e6f065bc263428bd50c))


### Features

* Add Apple Pay webhook handling and enhance PayPal webhook header validation ([93d4b9a](https://github.com/gameguild-gg/gameguild/commit/93d4b9a8d92d43500c6f739ace809c425c060b19))
* Add dotnet-tools configuration for EF Core ([923666d](https://github.com/gameguild-gg/gameguild/commit/923666d5c53926ab0b14a469ccacb82c06a1a74d))
* Add Feature Flags and Resources management pages with server-side data prefetching ([da6709d](https://github.com/gameguild-gg/gameguild/commit/da6709d604c14921726c14dc2606280b297db1fd))
* Add Game Jams and Localization modules with necessary models and services ([16874c2](https://github.com/gameguild-gg/gameguild/commit/16874c28f6d4f7953e561f0cc829cdc34c4e309b))
* add global exception handler ([8555164](https://github.com/gameguild-gg/gameguild/commit/85551644675ed8dc2425960a0ff09695135ea4d1))
* Add resource quota management with RequiresQuota attribute ([8f18bd5](https://github.com/gameguild-gg/gameguild/commit/8f18bd54cc2b530f802cb840e19a018e219768a2))
* Add subscription plans domain events and models ([ffe8ce5](https://github.com/gameguild-gg/gameguild/commit/ffe8ce5553a23720bd34e7d43f918a6e7b980ef9))
* Add TargetingRule model for feature flag targeting ([63080e0](https://github.com/gameguild-gg/gameguild/commit/63080e00e708b59dbcfc0af89ae48ad2853c669b))
* Add tenant import/export commands and DTOs ([1e2e133](https://github.com/gameguild-gg/gameguild/commit/1e2e1339ea3d504e03b0757e798ac2d5a959d766))
* **api-sdk:** regenerate REST SDK with path & enum changes and adapt web layer ([d7cfbe3](https://github.com/gameguild-gg/gameguild/commit/d7cfbe3bdfd04aae62b4c6504b4f959ab603671a))
* **api/authz:** enhance context and authorization logging across REST and GraphQL; wire ContextMiddleware; add docs ([0b31a70](https://github.com/gameguild-gg/gameguild/commit/0b31a70516233721bcab0ec48491e4bd872e03bd))
* **api:** add multi-tenant support with domain and tenant seeding ([e18d507](https://github.com/gameguild-gg/gameguild/commit/e18d50742dea039ae44007d7ab9c57709b4c85ef))
* **api:** Add program activity grades endpoints ([27b52ee](https://github.com/gameguild-gg/gameguild/commit/27b52eec2827c554f701ccc61b9d5c3d459bcb22))
* **api:** refine Program GraphQL mutations and queries ([987fcc4](https://github.com/gameguild-gg/gameguild/commit/987fcc49ead889e71382fb0413f11e6f6f252024))
* **apps/api:** Add public testing sessions endpoint and improve TestingLab API consistency ([bdf97eb](https://github.com/gameguild-gg/gameguild/commit/bdf97ebbe2e1e92522a0e1bfa6b10958b49dd23f))
* **apps/api:** Add TestingLab services, repositories, and dependency registrations with improved service initialization order ([aab436c](https://github.com/gameguild-gg/gameguild/commit/aab436c1501f8c6255ac411d99ab52a3dac7802d))
* **apps/api:** Configure GraphQL options based on environment variables ([069d5ca](https://github.com/gameguild-gg/gameguild/commit/069d5ca5db09620a618d5300bf3ad247d329e186))
* **apps/web/components/auth:** Refactor auth forms and update exports structure ([14d2c5e](https://github.com/gameguild-gg/gameguild/commit/14d2c5ea51e5a31ec404011d93b4acd5d1d396aa))
* **apps/web/components/profile:** Add new SubmitForTestingSheet component with enhanced UI and filtering ([2261c89](https://github.com/gameguild-gg/gameguild/commit/2261c89e67ad85a3c69c59e18e895f7324690677))
* **apps/web/components/testing-lab/testing-sessions-list:** Add TestingSessionsList component with filtering, pagination, and UI ([8b0a818](https://github.com/gameguild-gg/gameguild/commit/8b0a818ee45d92db318616b6af0782f3e658108f))
* **apps/web/components/testing-lab:** Add TestingRequestsList component with search, sort, pagination, and table UI ([550e382](https://github.com/gameguild-gg/gameguild/commit/550e3824a14f419262f10753c508cb6d2c5ed31f))
* **apps/web/components/testing-lab:** Add TestingSessionManagementContent component as session management wrapper ([cf96b1e](https://github.com/gameguild-gg/gameguild/commit/cf96b1ea4d1b4111a4acd4b78af55dc2f8acd23e))
* **apps/web/components/testing-lab:** Add TestingSessionsList component with filtering, sorting, and pagination ([5aa4df1](https://github.com/gameguild-gg/gameguild/commit/5aa4df1b0ad83f46a153ca33834e35087c2830f6))
* **apps/web/components/testing-lab:** Export TestingSessionsList from index for easier imports ([dd5ada8](https://github.com/gameguild-gg/gameguild/commit/dd5ada82f1ff4ba331a1be4842e4519b8078dbb7))
* **apps/web/components/users:** Add new simplified user list component ([34420e3](https://github.com/gameguild-gg/gameguild/commit/34420e3c4468938762a74062976dd50212434a49))
* **apps/web/lib/admin/testing-lab/sessions:** Replace sample data with API calls for testing sessions ([9ad4c16](https://github.com/gameguild-gg/gameguild/commit/9ad4c16a045d21a949e54239832b3a231ade82cc))
* **apps/web/lib/admin/testing-lab:** Add server actions for fetching and searching testing requests ([7d312bd](https://github.com/gameguild-gg/gameguild/commit/7d312bdb5f49d061d55f0344a73043bc4dd8448f))
* **apps/web/src/app/[locale]/dashboard/testing-lab/sessions:** Replace TestingSessionList with new TestingSessionManagementContent and update data fetching ([2e55ce3](https://github.com/gameguild-gg/gameguild/commit/2e55ce3dcee73ca6759e15da630f64736355ccbd))
* **apps/web/src/lib/admin/testing-lab:** Refactor testing session actions with mock data and simplified API usage ([43d0a63](https://github.com/gameguild-gg/gameguild/commit/43d0a63466760a677c2730698a840a04cb573c6c))
* **apps/web:** Add achievements and user management components with tenant management update ([9127412](https://github.com/gameguild-gg/gameguild/commit/912741205d5fe090463b833c0872c6b390fd49e9))
* **apps/web:** Add action to fetch testing session by slug ([8805052](https://github.com/gameguild-gg/gameguild/commit/88050528a829efc72d71b08142a9edc497c60541))
* **apps/web:** Add comprehensive project management and testing lab features ([d513a09](https://github.com/gameguild-gg/gameguild/commit/d513a0985669c8a00e89da4221ed4b0b8abf822a))
* **apps/web:** Add comprehensive testing lab settings and enhanced user management UI ([fbcab49](https://github.com/gameguild-gg/gameguild/commit/fbcab491010ee8dabc42aa90e6be07be2bed9b01))
* **apps/web:** Add dashboard layout with header, sidebar, and page components ([d66f9ff](https://github.com/gameguild-gg/gameguild/commit/d66f9ffe742c3dc37b19377fadf1066fac376dcf))
* **apps/web:** Add detailed logging and fallback handling in UsersPage ([2ce288f](https://github.com/gameguild-gg/gameguild/commit/2ce288f41fe490f6b290447de95a1f8ae7e2b973))
* **apps/web:** Add full project management with API integration and detail enhancements ([5b00ea8](https://github.com/gameguild-gg/gameguild/commit/5b00ea836955847a01e53508f430c9ebaf1fe7be))
* **apps/web:** Add global providers, zustand store, and query client integration ([1bd21e0](https://github.com/gameguild-gg/gameguild/commit/1bd21e05e4edb92eea02799e99f7b15b608bdee0))
* **apps/web:** Add new API routes and remove deprecated session lookup action ([b0f20e6](https://github.com/gameguild-gg/gameguild/commit/b0f20e6313cb05ebd3526fa667cbb1580f28c6e8))
* **apps/web:** Add react-query client setup and project list page with updated dependencies ([a64b92c](https://github.com/gameguild-gg/gameguild/commit/a64b92ccfc307c352cbd6c1feac61f1a7eaafa80))
* **apps/web:** Add tenant detail page with tabs and loading states; enhance user detail routing and lists ([e1c8a9a](https://github.com/gameguild-gg/gameguild/commit/e1c8a9ae1c520e5c391b6a71397c4bfb0035ecee))
* **apps/web:** Add TenantManagementContent component with tenant refresh capability ([9b5af1c](https://github.com/gameguild-gg/gameguild/commit/9b5af1cbb5896468e3712f1f99d76c5caa24b91a))
* **apps/web:** Add user management and testing request approval workflow ([85319b0](https://github.com/gameguild-gg/gameguild/commit/85319b0cbf0cd00fc3d95257eaf101d79637ab22))
* **apps/web:** Add version management and enhanced testing session handling in project details ([46be10d](https://github.com/gameguild-gg/gameguild/commit/46be10dadcaf145d31f946ecc0583268060a18db))
* **apps/web:** Enhance TenantsList component with filtering, sorting, and pagination ([0e5f911](https://github.com/gameguild-gg/gameguild/commit/0e5f911935b74eb90fd45b1e3da01c321518fb63))
* **apps/web:** Improve project tags handling and enhance update debugging ([a50b72c](https://github.com/gameguild-gg/gameguild/commit/a50b72c9a54d0a58629276a2ee27ba915478bbfa))
* **apps/web:** Migrate achievements page to use server-side data fetching ([1c7d340](https://github.com/gameguild-gg/gameguild/commit/1c7d340294f361f8de2262eeeb473b1b5d0ed658))
* **apps/web:** Replace testing session API with server actions and add tenant tab placeholders ([c705795](https://github.com/gameguild-gg/gameguild/commit/c705795ae236e6a0894dec7dbabcbdd8b293a172))
* **apps/web:** unify testing session data and update session UI links and details component ([5da3bdb](https://github.com/gameguild-gg/gameguild/commit/5da3bdbcff70e75d9678cefeb71e64bb59129c36))
* **apps/web:** Update testing lab sessions page to use new session fetch action ([a3214d7](https://github.com/gameguild-gg/gameguild/commit/a3214d722d8b39f405d9cb17f9ede5f9067446db))
* **audit:** Implement cryptographic signing service for audit logs ([801a1a2](https://github.com/gameguild-gg/gameguild/commit/801a1a25dc98e3587a004b7e4c4bc86352c6bb5c))
* **auth/mfa:** align controller with new service API and safer user handling ([0220493](https://github.com/gameguild-gg/gameguild/commit/02204935c5ba166ab76a044da20492dc0cf39c40))
* **auth:** add comprehensive authentication and audit services with MFA and session management ([187e577](https://github.com/gameguild-gg/gameguild/commit/187e57706981b0caa8abdf7e4034bf0871219a56))
* **auth:** add comprehensive authentication controller and clean up repositories ([170262e](https://github.com/gameguild-gg/gameguild/commit/170262efeb8456a5f4632305512b18dcc18ae56d))
* **auth:** add CQRS using to notification classes and standardize UserSignedUpNotification formatting ([cd7249f](https://github.com/gameguild-gg/gameguild/commit/cd7249f1ac608f4c0cb88188800f798b96bada15))
* **authentication:** add comprehensive user auth events and handlers with enhanced sign-in/out logging and token management ([6b57cf6](https://github.com/gameguild-gg/gameguild/commit/6b57cf61d1d70d12c9415e48ea7941267043ebe5))
* **authentication:** Enhance refresh token handling with anomaly detection and repository usage ([dc7c0fb](https://github.com/gameguild-gg/gameguild/commit/dc7c0fb7480d5f979965c62062a3f993eb49efa1))
* **authentication:** implement comprehensive auth service with anomaly detection and repositories ([cd6f966](https://github.com/gameguild-gg/gameguild/commit/cd6f966a172da2cb2f3bdf79179982e36b4224da))
* **authentication:** implement comprehensive user authentication and token management ([3bc9f93](https://github.com/gameguild-gg/gameguild/commit/3bc9f9358151d3655f6d4028d6d5d0ce1bdb40f0))
* **authentication:** integrate permission service for tenant user management and token enhancements ([49f64f1](https://github.com/gameguild-gg/gameguild/commit/49f64f13a4831bd63af2da081f79302d9ee9d764))
* **authentication:** switch to permission service for tenant data and set current tenant context ([4f837c7](https://github.com/gameguild-gg/gameguild/commit/4f837c70d3d0dd5502c6568eea0a8eb62f5a716e))
* **auth:** modernize authorization configuration ([9ae5cf3](https://github.com/gameguild-gg/gameguild/commit/9ae5cf3340f69ddc2ccce4ddc527ad5f0a2f8cd8))
* **auth:** tenant token enrichment respects expiration and emits permission claims ([ae77496](https://github.com/gameguild-gg/gameguild/commit/ae77496130bf961e024ddb6289ce1d0701ccf590))
* **billing:** add webhook processing pipeline for Stripe and PayPal ([a97a29d](https://github.com/gameguild-gg/gameguild/commit/a97a29d842f3d0014fe88175741f0b8ec03539e1))
* **certificates:** add certificate enums and verification method ([7db9736](https://github.com/gameguild-gg/gameguild/commit/7db9736695d0fabb9cc5ad3ff0e707add3be1feb))
* **common:** introduce request context services and wire up DI + middleware ([c40785f](https://github.com/gameguild-gg/gameguild/commit/c40785f2268530b2f3762619a62e43815dc819d4))
* **config:** introduce unified security, auth, rate limiting, and validation settings per environment ([947f6c9](https://github.com/gameguild-gg/gameguild/commit/947f6c9cafa8237b8613601ae3db119f5800092f))
* **content:** add visibility, moderation, development, progress, and project enums ([91aa253](https://github.com/gameguild-gg/gameguild/commit/91aa253a5f85e87435b0c1fb1ae95e4ccf666d7d))
* **core,cqrs:** introduce legacy ValidationError types and deprecate ValidationResult in favor of unified Error/Result ([5d2f419](https://github.com/gameguild-gg/gameguild/commit/5d2f4198740e4affde611e0224fa992bd85d958b))
* **core:** add ASP.NET Core route parameter transformers (kebab/slug/snake) and refine ToKebabParameterTransformer ([80f8d5b](https://github.com/gameguild-gg/gameguild/commit/80f8d5b01a7b8a2448c042d46138f12b6e159b29))
* **core:** add comprehensive CQRS infrastructure with optimized mediator and pipeline behaviors ([db912e5](https://github.com/gameguild-gg/gameguild/commit/db912e575523ec47459e31d66fa5436767cb9717))
* **core:** add domain error types (Error, ErrorType, ValidationError) ([2e2f7c3](https://github.com/gameguild-gg/gameguild/commit/2e2f7c3f9e2b8957a2b5403eb310101a7ab58952))
* **core:** add ToUniqueSlugCase extension and update SlugCase docs ([ec4c8fa](https://github.com/gameguild-gg/gameguild/commit/ec4c8faa8058ddd6e3600a3e4ddb3910ea0b3c4c))
* **core:** add UnifiedExceptionHandler and relocate GlobalExceptionHandler to Core/Exceptions ([269ea9a](https://github.com/gameguild-gg/gameguild/commit/269ea9a935be66eb96af7881efecd10edf035258))
* **core:** implement Cloudflare Dynamic DNS service with support classes and configuration ([8a41122](https://github.com/gameguild-gg/gameguild/commit/8a41122c5dbd63515a68919cd0bacf2bc5ce4c71))
* **core:** improve exception handler visibility and add cache get-or-set method ([7366f6d](https://github.com/gameguild-gg/gameguild/commit/7366f6dbc6f5fcda682d6ba19a9f92c8fa66ab8c))
* **core:** introduce CustomResults for ProblemDetails mapping ([a52798d](https://github.com/gameguild-gg/gameguild/commit/a52798da569ef8d446314ad17df8527aad01a896))
* **core:** move IValidator to Core.Behaviors and return Result with FluentValidation context ([a4fdc31](https://github.com/gameguild-gg/gameguild/commit/a4fdc31b54cedebe02854e2133a8d5fc3596daa9))
* **core:** register external module services in AddExternalServices ([6e34afa](https://github.com/gameguild-gg/gameguild/commit/6e34afa406902300c8584f25a0f1e3557339afc7))
* **cors:** enhance CORS policy logic and apply to GraphQL endpoint; add development CORS config ([0bba7a5](https://github.com/gameguild-gg/gameguild/commit/0bba7a5a2b7eb9e532e1987084f5b106147685e7))
* **cqrs:** add PaginatedQuery<TResult> base class and SortDirection enum ([eeeebbb](https://github.com/gameguild-gg/gameguild/commit/eeeebbbd41e60c4f99db5ad2967c8c48a55ae3d5))
* **credentials:** Add comprehensive credentials REST API controller using CQRS pattern ([dfa76d1](https://github.com/gameguild-gg/gameguild/commit/dfa76d18cb705bdd60efbc26a4982a555b8b6e75))
* **credentials:** add full CQRS commands, events, and validations for credential management including soft-deletes ([c659093](https://github.com/gameguild-gg/gameguild/commit/c65909372c63556ccbbc97fb917658525fa84bed))
* **credentials:** Add validator for creating user credentials ensuring data integrity ([dce6a2d](https://github.com/gameguild-gg/gameguild/commit/dce6a2dbc643e94a40337f1c92915baee2ca7baf))
* **credentials:** refactor to hexagonal architecture with CQRS and repository pattern ([182ecb9](https://github.com/gameguild-gg/gameguild/commit/182ecb9a14a19e87d073f56fcd8c860d7ed2d396))
* **database:** introduce streamlined ApplicationDbContext and seeding with language defaults ([48c394a](https://github.com/gameguild-gg/gameguild/commit/48c394a88071ec01acf25c661ed7dc10714184b3))
* **db:** add DbSets for billing, feature flags, and resource quotas; move FinancialTransaction DbSet; add required usings ([5fc5a18](https://github.com/gameguild-gg/gameguild/commit/5fc5a18eda2cbf81dbb2a06e068b050f9369fad7))
* **ef:** add ModelBuilder extensions for base entities and soft delete filters ([17fedbb](https://github.com/gameguild-gg/gameguild/commit/17fedbbd71ba739e8aee2c715ee446fedc88ae3e))
* **features:** add DatabaseFeatureFlagProvider backed by EF Core ([cc0c958](https://github.com/gameguild-gg/gameguild/commit/cc0c958a377716beadef7f8b8d76202a1364ad02))
* **features:** add feature flags system with targeting, caching, and analytics ([dfac125](https://github.com/gameguild-gg/gameguild/commit/dfac125fb7cddfca86fd57a84e9dcd6a31b0e8a9))
* **features:** integrate OpenFeature DB provider and adjust feature flag schema ([2e74b97](https://github.com/gameguild-gg/gameguild/commit/2e74b97e033611dc8836301a4c70344a94dc6a51))
* **features:** integrate OpenFeature SDK and register feature flag services ([ac521ad](https://github.com/gameguild-gg/gameguild/commit/ac521ad7ab80a74022f3aab2345214c25748267a))
* **feedback:** add FeedbackFormQuestionType enum ([7a0f111](https://github.com/gameguild-gg/gameguild/commit/7a0f11180edf4d8d69b47856645e8bf079e82031))
* **gglexical:** code editor cpp runner ([5230749](https://github.com/gameguild-gg/gameguild/commit/5230749f1cd8848337f421cb03310f5f5e71d0d6))
* **gglexical:** code editor display grid layout ([1490fef](https://github.com/gameguild-gg/gameguild/commit/1490fefb139853fd7b8c8a5f985fd2b101d82228))
* **gglexical:** code editor duplicate name dialog ([ddcc0ef](https://github.com/gameguild-gg/gameguild/commit/ddcc0ef6595f17dfe44412e8458d7c4dc914d7a8))
* **gglexical:** code editor executing state progress ([9035555](https://github.com/gameguild-gg/gameguild/commit/9035555cee9871a5b75ad196d13917f67c4f5dec))
* **gglexical:** code editor file explorer delete confirm ([d5ac97d](https://github.com/gameguild-gg/gameguild/commit/d5ac97d9f87a6e06056ca0337f13cf79880cfe39))
* **gglexical:** code editor file explorer drag drop ([60c1603](https://github.com/gameguild-gg/gameguild/commit/60c160343b44788998c2e143145a0d3a89dced93))
* **gglexical:** code editor file explorer vertical menu ([b523f42](https://github.com/gameguild-gg/gameguild/commit/b523f429d67facd34ef5f7342195c59b232971e5))
* **gglexical:** code editor file-tabs ([959a5d8](https://github.com/gameguild-gg/gameguild/commit/959a5d8c3197cc531aab7b9c8a48e4aa86a6805c))
* **gglexical:** code editor file-tabs better user experience ([586a6c0](https://github.com/gameguild-gg/gameguild/commit/586a6c0086a61eea7a35f0a193785a05de3d4ee7))
* **gglexical:** code editor file-tabs drag-drop ([a095b6a](https://github.com/gameguild-gg/gameguild/commit/a095b6ad9d6f3aebe5c4d32bb067802a9e9ae5fb))
* **gglexical:** code editor grid dimensions ([72e35ea](https://github.com/gameguild-gg/gameguild/commit/72e35ea2b50d29e0077c6afd23b7f026af08a57c))
* **gglexical:** code editor link confirmation ([63704e7](https://github.com/gameguild-gg/gameguild/commit/63704e7c844146c519dd1f17877403d38b053f56))
* **gglexical:** code editor lua runner ([04683f2](https://github.com/gameguild-gg/gameguild/commit/04683f2f8779b91e6399882a8e0628d479ff0f0b))
* **gglexical:** code editor multiple and unique instancies ([8f198a2](https://github.com/gameguild-gg/gameguild/commit/8f198a22ad7600b863a889696e3342a52fe6a809))
* **gglexical:** code editor no open file message ([63fd7f1](https://github.com/gameguild-gg/gameguild/commit/63fd7f1031e3d4dae8d9b3ff0f91c6fb3ebc1021))
* **gglexical:** code editor python path support ([b62cad2](https://github.com/gameguild-gg/gameguild/commit/b62cad291cf43009df0f6da56365440585fa558e))
* **gglexical:** code editor python support ([45a31b3](https://github.com/gameguild-gg/gameguild/commit/45a31b34e8cb1a75a4e434177fad47451e0a0414))
* **gglexical:** code editor reduce download wasm ([1ecd823](https://github.com/gameguild-gg/gameguild/commit/1ecd82342a8b6a07e689d7c4279434bd9d7e0e22))
* **gglexical:** code editor shiki Highlighter ([69d58f1](https://github.com/gameguild-gg/gameguild/commit/69d58f1c92288f264210f5d915f1de2ed78ada9b))
* **gglexical:** code editor studio/viewer node from display-1 ([a9e3b04](https://github.com/gameguild-gg/gameguild/commit/a9e3b04d06fe3455c0a6738b561ec921ecf705a4))
* **gglexical:** code editor templates and support for more languages ([7dfaf2d](https://github.com/gameguild-gg/gameguild/commit/7dfaf2d2d78ac856785c7de47b2b12d8a0e1904d))
* **gglexical:** code editor unified runner and JS/TS ([69fa793](https://github.com/gameguild-gg/gameguild/commit/69fa7935dbd42b3ce182c11932901943bbed912d))
* **gglexical:** code editor virtual file system ([669eb5b](https://github.com/gameguild-gg/gameguild/commit/669eb5b0c310185ddcd3cb4823c781a0421152dd))
* **gglexical:** code editor xterm addons ([cc0692e](https://github.com/gameguild-gg/gameguild/commit/cc0692eda7673b87da32cb81759672fb193c6a1f))
* **gglexical:** code editor xterm request input with Lua ([4d3e070](https://github.com/gameguild-gg/gameguild/commit/4d3e07036e332c7f7b0cf1d1969809301e976022))
* **gglexical:** link confirm dialog ([0facbd2](https://github.com/gameguild-gg/gameguild/commit/0facbd240261ab38cafd8a4cef5a573afbdd4d80))
* **gglexical:** script update wasm packages ([6bd2f7b](https://github.com/gameguild-gg/gameguild/commit/6bd2f7bb525bd5fbcd21e1ccaef27bdcc7318abf))
* **gglexical:** xterm link confirm ([73d63cd](https://github.com/gameguild-gg/gameguild/commit/73d63cda0b322ad314570ab6b6e29736a66b7b8b))
* **graphql-schema:** regenerate schema with program CRUD & extended entities ([46371f6](https://github.com/gameguild-gg/gameguild/commit/46371f682191853ffc158dbde26ba76d4d71673e))
* **graphql-security:** support batched GraphQL operation introspection in security middleware ([d97cd0f](https://github.com/gameguild-gg/gameguild/commit/d97cd0f0847283ee6dad9876055444bb48b09d02))
* **graphql:** add base Query and Mutation root types ([8fd5c6a](https://github.com/gameguild-gg/gameguild/commit/8fd5c6a3fc79284f025029ed27eb1be542a0c5c5))
* **graphql:** add permission queries and mutations ([a07741c](https://github.com/gameguild-gg/gameguild/commit/a07741cbb07fc23c577cf01fbf6fe32411ad2369))
* **graphql:** introduce 3-layer DAC authorization (attributes, middleware, directive, extensions) ([9299e55](https://github.com/gameguild-gg/gameguild/commit/9299e55da1535207ea4dc28d3b71aedd97c3a3c8))
* **identity:** add HTTP-based context implementations, adapters, and request logging middleware ([654eea9](https://github.com/gameguild-gg/gameguild/commit/654eea9d32a5abc9647cb1239fa10e4c32456358))
* **identity:** introduce core identity context interfaces and remove legacy duplicates ([da32cfc](https://github.com/gameguild-gg/gameguild/commit/da32cfcb4a6a88c115280bb84458e99e777b0fec))
* Implement billing webhook processing module ([5e27837](https://github.com/gameguild-gg/gameguild/commit/5e2783717c9314700856a13c08e2b4b387d6e4db))
* Implement country-based targeting handler for feature flags ([5a2c135](https://github.com/gameguild-gg/gameguild/commit/5a2c135c3fc3f0fe56230c6bb60050a9a5f5f0b0))
* Implement Just-in-Time (JIT) permission elevation handlers and services ([ad4ea57](https://github.com/gameguild-gg/gameguild/commit/ad4ea57d4d263b1e6d711c89d1710cfbbec617da))
* Implement notification management system ([50c246d](https://github.com/gameguild-gg/gameguild/commit/50c246de0beb699b9edadcba4ca1bbbde61cf71f))
* Implement ProcessPayPalWebhookCommandHandler and integrate CQRS in billing module ([bf99457](https://github.com/gameguild-gg/gameguild/commit/bf9945712ff577d2cd070e95f891fd74b6406e6b))
* Implement SIEM integration service for security event logging ([6122609](https://github.com/gameguild-gg/gameguild/commit/61226094b5b31464aa52d41fb754a97cbc3520dc))
* Implement Testing Lab module with services, validators, and settings management ([02bdc8d](https://github.com/gameguild-gg/gameguild/commit/02bdc8db10915da87ab8100774369d3cb4041cb6))
* Implement user profile and user management queries with pagination and filtering ([895cc1f](https://github.com/gameguild-gg/gameguild/commit/895cc1f81fb9f96d27f745c6ad3afcfd4cf4d519))
* **infra:** Cloudflare Dynamic DNS service and hosted worker ([b6cee86](https://github.com/gameguild-gg/gameguild/commit/b6cee869b15a2306c02ae7971f6bc66dce622226))
* Integrate Audit module with necessary services and database context updates ([5d7a5aa](https://github.com/gameguild-gg/gameguild/commit/5d7a5aa28a11630001c433c3d62d0b7617a0dbfc))
* **json:** introduce centralized JsonSerializerConfiguration and apply across API, SignalR, OAuth, tests ([4bec959](https://github.com/gameguild-gg/gameguild/commit/4bec9595a56b475b968ddb120980924cf8e62b95))
* **kyc:** add KYC provider and verification status enums ([74279eb](https://github.com/gameguild-gg/gameguild/commit/74279ebe0b0a8baaa3ade913b2a0b0001fc5b491))
* **learning:** add program and product learning domain enums ([362f6fe](https://github.com/gameguild-gg/gameguild/commit/362f6feeea052678c28bb837390f6c079f954980))
* **logging:** bootstrap Serilog early and enable structured request logging ([9e3a78a](https://github.com/gameguild-gg/gameguild/commit/9e3a78a01a908d6062aa2aba115abc8eb16b9b71))
* **notifications:** add in-app notifications with preferences and bulk operations ([9005744](https://github.com/gameguild-gg/gameguild/commit/90057446c3dc4becb1906a3a033186e26b4d9803))
* **notifications:** consolidate module namespaces, split DTOs/enums, add service and controller, register DbSets ([1609fa9](https://github.com/gameguild-gg/gameguild/commit/1609fa9c9da84387354a41f8cd9f86b1a96d9100))
* **observability:** bind OpenTelemetry options and adjust development exporter defaults ([8ab3d31](https://github.com/gameguild-gg/gameguild/commit/8ab3d31b49f50273063908c98acc0693520a0e9a))
* **payments:** add advanced payment commands, result models, and service interface ([dcb1c5c](https://github.com/gameguild-gg/gameguild/commit/dcb1c5ca990f4a3553459bdebb19384d25b1ebce))
* **payments:** add gateways, methods, transaction types/statuses, and wallet status ([ffaa365](https://github.com/gameguild-gg/gameguild/commit/ffaa36571eb43acd295e6cc91d00c8fc89a6893a))
* **permissions-domain:** add DAC/Module permission contracts and permission entities ([426697a](https://github.com/gameguild-gg/gameguild/commit/426697a1592cce42aa533a72c72f53fe02dbc0b9))
* **permissions:** add comprehensive multi-layer permission module with caching and auditing ([90863be](https://github.com/gameguild-gg/gameguild/commit/90863bed9fd9e5558b496bd6fe60f20f2516327f))
* **permissions:** implement DAC resolver and permission services; update DbContext and seeders ([85c1811](https://github.com/gameguild-gg/gameguild/commit/85c1811aaeffed9cce265e35afd47a2e3a8187a6))
* **products-graphql:** add authenticated product queries and diagnostic auth logging ([082a104](https://github.com/gameguild-gg/gameguild/commit/082a104ae2a04208777365f27963a5167c52f3be))
* **products-graphql:** integrate Program relationships into Product schema ([33ba038](https://github.com/gameguild-gg/gameguild/commit/33ba038db432f4a4abe462ea817da4ee0f5c2e97))
* **programs-graphql:** add Program GraphQL module with queries, mutations, type definitions, and DI registration ([a4bb9f0](https://github.com/gameguild-gg/gameguild/commit/a4bb9f0276716cd500ef98c4166fd27fcd70a2bd))
* **programs:** adopt GameGuild.Authorization namespace in GraphQL ([540f614](https://github.com/gameguild-gg/gameguild/commit/540f614c78287b2fc2cf4e190bc92c6f07689c32))
* **programs:** expand IProgramService contract with detailed operations and docs ([8566034](https://github.com/gameguild-gg/gameguild/commit/8566034776ff9c2e098b03c32e122deae3f6fdb5))
* register Resources, Tenants, and Projects modules in DI ([20895a6](https://github.com/gameguild-gg/gameguild/commit/20895a6ce36cd9fa4ace7230c45c15d9e383d0f8))
* Remove development configuration files for GameGuild ([3e5c2bc](https://github.com/gameguild-gg/gameguild/commit/3e5c2bc5e90f41b99369e4e04ff0bcd9b9e2d288))
* Remove GameGuild project files and configuration settings ([4760366](https://github.com/gameguild-gg/gameguild/commit/4760366458f6bd2414958d75c3f18e63a7fb85bf))
* **resources:** add resource quota and permission service abstractions and implementations ([917ea51](https://github.com/gameguild-gg/gameguild/commit/917ea51a888a1931aec0712e9264781b362744f0))
* **resources:** introduce tenant resource quotas, usage tracking and admin APIs ([44c577d](https://github.com/gameguild-gg/gameguild/commit/44c577d2c91d34649bf0167046a7ca358f7dc428))
* **seeding:** add database seeder and contract ([606146d](https://github.com/gameguild-gg/gameguild/commit/606146df94ada3460b1d3204a52d477c2237d788))
* **shared:** add core value objects (Address, EmailAddress, PhoneNumber, Money, BillingCycle) ([576ca9f](https://github.com/gameguild-gg/gameguild/commit/576ca9f849605b69c55f9f81e8ed2a4ca6ee0a37))
* **SLA Monitoring:** Implement SLO compliance and violation queries ([ad85657](https://github.com/gameguild-gg/gameguild/commit/ad856576e9933b5a65efb7665c443b549c542f6e))
* **subscriptions:** add repository, CQRS commands/handlers, controller, events, and DI registration ([cac1bde](https://github.com/gameguild-gg/gameguild/commit/cac1bde720c37962485d1ce9b2bbf78745b205ce))
* **subscriptions:** add subscription types/intervals/statuses, acquisition/access, and promos ([d1d391e](https://github.com/gameguild-gg/gameguild/commit/d1d391eff63c84da676c9fd11819ea489d91c45e))
* **subscriptions:** Add Subscriptions module with clean architecture ([4147055](https://github.com/gameguild-gg/gameguild/commit/4147055ce13033ed9c1eab8cce1e2d767255743a))
* **subscriptions:** Complete SubscriptionRepository implementation (92→65 errors, 27 fixed) ([572b56b](https://github.com/gameguild-gg/gameguild/commit/572b56b611a709735554f36cdf3eee0561603fba))
* **subscriptions:** enhance UserSubscription domain and service behaviors ([61d38e1](https://github.com/gameguild-gg/gameguild/commit/61d38e102d51d7c22db73c9ad8f99dd1a1125a65))
* **subscriptions:** register repository and add required usings ([822d1e6](https://github.com/gameguild-gg/gameguild/commit/822d1e655548275c5cc815cd05f94b8bc50b9dea))
* **tags:** add TagType, TagRelationshipType, and SkillProficiencyLevel enums ([f526503](https://github.com/gameguild-gg/gameguild/commit/f5265033c0fbb039911f1609659178ebfa3356f2))
* **telemetry:** refine OpenTelemetry resource config and align instrumentation with available APIs ([c18113d](https://github.com/gameguild-gg/gameguild/commit/c18113db2ad3cf4e0ab54447be151ceab48a694a))
* **tenants,users:** Add detailed validators, enhance handlers, and improve EF configurations ([792722e](https://github.com/gameguild-gg/gameguild/commit/792722e4aceed49f416bc0850f3179ad3a31ff2c))
* **tenants:** add caching, context, and core services; remove legacy commands and controllers ([3c6eded](https://github.com/gameguild-gg/gameguild/commit/3c6ededf3e81dd00a3be29ec7eee1b5edc118b2b))
* **tenants:** add tenant defaulting and settings integration ([e379af0](https://github.com/gameguild-gg/gameguild/commit/e379af0b9dcfbd82d63d62197cdc741996fce811))
* **tenants:** enhance Tenant entity with slug index, fields, and helpers ([c99a7b1](https://github.com/gameguild-gg/gameguild/commit/c99a7b1c77745c03a1e4df66644d2075fc81bb20))
* **tenants:** separate tenant domains and settings into dedicated modules and services ([72835d2](https://github.com/gameguild-gg/gameguild/commit/72835d22239a973bb0a748dadd616f7dfcb80967))
* **TestingLab:** Complete TestingLab module integration ([8673463](https://github.com/gameguild-gg/gameguild/commit/86734638cbe171129e5bf160c9a7c215874da2b1))
* **TestingLab:** Integrate TestingLab module with custom CQRS ([1df8f04](https://github.com/gameguild-gg/gameguild/commit/1df8f044746797a6529e3ee6265cc8d2b5261a7c))
* update CourseCard component to use a new CourseCardCourse type for better type safety ([80f92a9](https://github.com/gameguild-gg/gameguild/commit/80f92a9749d7d800878e7a91ff994c1df9fa1581))
* Update HealthController and HealthEndpoint for anonymous access; refactor namespaces and add integration tests ([96fad4c](https://github.com/gameguild-gg/gameguild/commit/96fad4c036c3a6d973c92172e818ca566cbc5acd))
* **user-profiles:** replace GraphQL with repository pattern and add user profile service and handlers; extend user entity with names ([af0af99](https://github.com/gameguild-gg/gameguild/commit/af0af99f0a25cd7a069ecc4d5cbee478d0341784))
* **user-profiles:** restructure module with CQRS, validation, and handlers ([bfa4083](https://github.com/gameguild-gg/gameguild/commit/bfa4083b6cca7fce9b53c2df89d6f2a295930e11))
* **users:** extend User entity with phone, last seen, and domain helpers ([8d6ac59](https://github.com/gameguild-gg/gameguild/commit/8d6ac596d24a47a97d858763518e67a634aac7b9))
* **users:** increase balance precision and align EF configuration; improve create handler ([997f255](https://github.com/gameguild-gg/gameguild/commit/997f255fb8e2c21998786616a3b5f00f8b421cef))
* **users:** overhaul user module with CQRS, services, and API controller ([2e27066](https://github.com/gameguild-gg/gameguild/commit/2e270660d9dd9c9f9229f9c9842d63b1e18f5aee))
* **web/courses:** switch to myPrograms hook, add null-safe filtering/sorting, remove debug UI ([108978e](https://github.com/gameguild-gg/gameguild/commit/108978e967e6785bc235c39b7f7b8182b835da3a))
* **web/graphql:** refresh codegen and queries; align mutations with API changes ([98ae7ae](https://github.com/gameguild-gg/gameguild/commit/98ae7aef5a25aab5988ad10d0e96882d3fdd9134))
* **web:** Enhances testing lab UI and functionality ([cdcad06](https://github.com/gameguild-gg/gameguild/commit/cdcad0623ad0c2bf7dd3e7e13a4d0a85e7b2142d))
* **web:** improve Apollo provider auth flow and provider composition ([1694d2f](https://github.com/gameguild-gg/gameguild/commit/1694d2f828664e7e61cbc7a44d98248af195a5ef))
* **web:** integrate GraphQL program & product operations into courses UI ([1b4af93](https://github.com/gameguild-gg/gameguild/commit/1b4af932469362806b843cbacaeeb4175d02187b))
* **web:** migrate course detail pages to server components with GraphQL-backed actions ([1e52b9c](https://github.com/gameguild-gg/gameguild/commit/1e52b9c8cef7f348f9495086d794f5984ae7b575))


### Performance Improvements

* **rate-limiting:** switch to source-generated Regex and set headers safely ([682817e](https://github.com/gameguild-gg/gameguild/commit/682817ebb07c6bf4caf071a251c554d4f1f77b8b))
* **regex:** adopt source-generated Regex across normalization and transformers ([17e59ef](https://github.com/gameguild-gg/gameguild/commit/17e59ef27a6a8c00f96745c2cd6e495aa2ebef36))

# [2.24.0](https://github.com/gameguild-gg/gameguild/compare/v2.23.1...v2.24.0) (2025-11-13)


### Features

* contact/links page ([9e48e60](https://github.com/gameguild-gg/gameguild/commit/9e48e60b9a60f8b833e649b0091f1b63a0e4b6f7))

## [2.23.1](https://github.com/gameguild-gg/gameguild/compare/v2.23.0...v2.23.1) (2025-11-07)


### Bug Fixes

* **gglexical:** admonition dark mode ([730c746](https://github.com/gameguild-gg/gameguild/commit/730c74634a0dae8b94ae010b01486b2bbcdf8c4f))
* **gglexical:** admonition mouse capture ([0f5981c](https://github.com/gameguild-gg/gameguild/commit/0f5981c3c04e93ab66cae1a7d968597aff9a2807))
* **gglexical:** audio node fix ([dc4931c](https://github.com/gameguild-gg/gameguild/commit/dc4931c910f0a36ef28be0bd4b7612a11061573d))
* **gglexical:** button and divider mouse capture ([12f6f2d](https://github.com/gameguild-gg/gameguild/commit/12f6f2d8daee70dfe7233bc9bdce0b9e64c8553c))
* **gglexical:** buttons studio and viewer open with button code 1 mouse ([15ba1be](https://github.com/gameguild-gg/gameguild/commit/15ba1bea17ff44be6d33e7d8328dc5ac6697dfa5))
* **gglexical:** download dialog ([acbaabb](https://github.com/gameguild-gg/gameguild/commit/acbaabb217040e3e52d6f04f111ae71d20e511f3))
* **gglexical:** edit-button works ([38a09df](https://github.com/gameguild-gg/gameguild/commit/38a09df46eb7dc83093cdce1ade7e91ded17e876))
* **gglexical:** filters ([434ecae](https://github.com/gameguild-gg/gameguild/commit/434ecae25f03294128cd8c1fb1603261fafd3995))
* **gglexical:** fix mermaid template quadrant-chart ([59039cb](https://github.com/gameguild-gg/gameguild/commit/59039cb1cb535ef1590698f17d9959e96299b241))
* **gglexical:** fix theme dark mode in editor vega ([62ba7b2](https://github.com/gameguild-gg/gameguild/commit/62ba7b232504a0ec6d427e2c0bfe3449fad5b324))
* **gglexical:** fixes in mermaid ([2b71f33](https://github.com/gameguild-gg/gameguild/commit/2b71f3369a41dca736f1b2316fe402605eb1b860))
* **gglexical:** floating content fixed ([55503b5](https://github.com/gameguild-gg/gameguild/commit/55503b5d84f308694d7f59ca52a4f3d5e65fc4e9))
* **gglexical:** floating toolbar ([3fdc925](https://github.com/gameguild-gg/gameguild/commit/3fdc9256370da45915a77d0fc27a01daf0449edf))
* **gglexical:** media editor mouse capture fix ([3a60f4e](https://github.com/gameguild-gg/gameguild/commit/3a60f4e76c3e7aef71c8ed1267c06629b6572999))
* **gglexical:** mermaid and vega editors dark-mode ([1eb6e9b](https://github.com/gameguild-gg/gameguild/commit/1eb6e9b8abac1680517d703ab5d5d3da0dff5f62))
* **gglexical:** mermaid mouse capture ([b368c4d](https://github.com/gameguild-gg/gameguild/commit/b368c4d1f6d1b10c2f9a41d8860e69c126c54f86))
* **gglexical:** mermaid suggestions ([b2c7f25](https://github.com/gameguild-gg/gameguild/commit/b2c7f25eefddf4cab51e7798051464c200ed981f))
* **gglexical:** mermaid themes dark more light ([4802467](https://github.com/gameguild-gg/gameguild/commit/4802467296590c140ccdbdf8060dc84644ab426b))
* **gglexical:** name vega manager ([55b4101](https://github.com/gameguild-gg/gameguild/commit/55b4101dcce65dca39959826ecd61c25f10a4de2))
* **gglexical:** new atributes for vega theme dark ([c6df683](https://github.com/gameguild-gg/gameguild/commit/c6df683fff7e82267a98ed593263562ce3cb7895))
* **gglexical:** open in new tab ([7d3dfc8](https://github.com/gameguild-gg/gameguild/commit/7d3dfc813ec13c8692ec5eaf037a936d54009e83))
* **gglexical:** open studio/viewer documents ([ddd804e](https://github.com/gameguild-gg/gameguild/commit/ddd804e10073106f5b8599a462f485e2259360c1))
* **gglexical:** quiz mouse capture ([ce3eb2b](https://github.com/gameguild-gg/gameguild/commit/ce3eb2b4bf5d41c2210f2e5a76e5c9e7d89064d5))
* **gglexical:** selected fix ([68094ab](https://github.com/gameguild-gg/gameguild/commit/68094abd6919475c9f9e7f29b15092d7f5740f6f))
* **gglexical:** storageType ([55cdab9](https://github.com/gameguild-gg/gameguild/commit/55cdab963ebdfcb8dff33d137c3c8188c1f3d31f))
* **gglexical:** table mouse capture ([a1f591f](https://github.com/gameguild-gg/gameguild/commit/a1f591fec41e7e09f2d5ac231d94250a78953fed))
* **gglexical:** toolbar not open in line without words ([de9391d](https://github.com/gameguild-gg/gameguild/commit/de9391d681fa0d2de5e1bcbeeb561c8b6e30d6cf))
* **gglexical:** updating vega live preview ([805734e](https://github.com/gameguild-gg/gameguild/commit/805734e623685113b9924a410f19a5380e0e3d07))
* **gglexical:** vega dark theme ([e26a23e](https://github.com/gameguild-gg/gameguild/commit/e26a23e3ee09c01def5ee528415ed73dc7451bbb))
* **gglexical:** vega dimensions ([4cfa803](https://github.com/gameguild-gg/gameguild/commit/4cfa803fcbbb2e648d1b3c07d2045ce61687fd55))
* **gglexical:** vega editor transition in update ([0dc8175](https://github.com/gameguild-gg/gameguild/commit/0dc8175fc01dbacf7483f079358f991d1f97076d))
* **gglexical:** vega fix element center ([2932a68](https://github.com/gameguild-gg/gameguild/commit/2932a6817f311448b088d9f317e3fb2c42ab13f6))
* **gglexical:** vega live preview update ([8106985](https://github.com/gameguild-gg/gameguild/commit/8106985f9f9e736322b2da904baa3b2bc6331a5d))
* **gglexical:** vega themes order ([6c9c46c](https://github.com/gameguild-gg/gameguild/commit/6c9c46c1a7aa6c28ffb92344716ae5f035d67e66))
* **gglexical:** vega update fix ([95f3a2b](https://github.com/gameguild-gg/gameguild/commit/95f3a2b6d369066ca8db789e062b62026fabfb28))
* **gglexical:** vega zoom in viewer ([26f914c](https://github.com/gameguild-gg/gameguild/commit/26f914c4865ed0aa2fc10ce23b9589cc6f01527e))
* **gglexical:** vega-lite mouse capture ([df2243f](https://github.com/gameguild-gg/gameguild/commit/df2243fd2b0f32538402d1e0182e4dddb75f4717))
* **gglexical:** vega-lite template design fix ([fb6df83](https://github.com/gameguild-gg/gameguild/commit/fb6df83f5515d0a436d8d8ef4977ed7e9e0476ec))

# [2.23.0](https://github.com/gameguild-gg/gameguild/compare/v2.22.1...v2.23.0) (2025-11-06)


### Features

* **web:** Add testing session content for Intro2GPro ([bc3ade5](https://github.com/gameguild-gg/gameguild/commit/bc3ade5b0bab75f930aa0fbb65fb439a8df61177))

## [2.22.1](https://github.com/gameguild-gg/gameguild/compare/v2.22.0...v2.22.1) (2025-10-17)


### Bug Fixes

* **editor:** callout to admonition ([1a78079](https://github.com/gameguild-gg/gameguild/commit/1a780792b5d572cb0a6d14da600d707999314f16))
* **editor:** import fix ([f0ba9b3](https://github.com/gameguild-gg/gameguild/commit/f0ba9b32f9c3a8c5d57560472b6370e854999ca8))
* **editor:** logic fix in preview-table-of-contents ([5b7f908](https://github.com/gameguild-gg/gameguild/commit/5b7f908cf573af2c8173460d3b08f24ea2cddac8))
* **editor:** open project fix ([ad87c18](https://github.com/gameguild-gg/gameguild/commit/ad87c18d64fa93ac64b1ae19b20f38b4792a6c12))
* **editor:** preview page = serializedState type ([8fbf60b](https://github.com/gameguild-gg/gameguild/commit/8fbf60bfcc0712a41413c644c0f58fce5dcb44eb))
* **editor:** preview page focus serializedState ([72e941e](https://github.com/gameguild-gg/gameguild/commit/72e941e6448010df3ae9c71855d916c5144ae727))
* **gglexical:** adapt new page names ([46b6f10](https://github.com/gameguild-gg/gameguild/commit/46b6f1081d194ce1ac9ea32387ed1cd936e2802f))
* **gglexical:** api tagData ([d8587e7](https://github.com/gameguild-gg/gameguild/commit/d8587e79578fddf834557eb85de5c3247adbee5e))
* **gglexical:** create dialog inserts correct storageType ([0c5ab81](https://github.com/gameguild-gg/gameguild/commit/0c5ab8128041aa715f31d787c20dc3585b53222f))
* **gglexical:** doc architecture ([dbbb8e2](https://github.com/gameguild-gg/gameguild/commit/dbbb8e24eb316784c50d668dde4f1fc71d8874c7))
* **gglexical:** doc gglexical + storage architecture ([12f18fe](https://github.com/gameguild-gg/gameguild/commit/12f18fee868de43bbcdf0991b91dd01ec40c4b2b))
* **gglexical:** docs architecture ([38a9104](https://github.com/gameguild-gg/gameguild/commit/38a91042383cb24688a123b5db1a90867a95a362))
* **gglexical:** error exception ([cb6bfd2](https://github.com/gameguild-gg/gameguild/commit/cb6bfd23c66384a9e8a4b3d7a33136226c3fec35))
* **gglexical:** fill-in-the-blank fix ([a976121](https://github.com/gameguild-gg/gameguild/commit/a97612111fb3a487ed4430a445e08622f6ba9399))
* **gglexical:** fill-in-the-blank preview fix ([1ab2620](https://github.com/gameguild-gg/gameguild/commit/1ab26204785b59e07fc926ea26f3581a72680d00))
* **gglexical:** fill-in-the-blank preview fix 2 ([05f870c](https://github.com/gameguild-gg/gameguild/commit/05f870cd45bca783513caef4a6c17bf1020c235c))
* **gglexical:** fill-in-the-blank quiz fix ([1af6623](https://github.com/gameguild-gg/gameguild/commit/1af6623656988992df67819354abfcca39419bec))
* **gglexical:** fix storage ([ff24003](https://github.com/gameguild-gg/gameguild/commit/ff2400321cef533aaa8abe293bf57d5316eb1dc7))
* **gglexical:** fixed value for itemsPerPage ([8c1d037](https://github.com/gameguild-gg/gameguild/commit/8c1d03786a01862654e1a520be8eb3f9180a21fd))
* **gglexical:** formatting fix ([a6ed3c4](https://github.com/gameguild-gg/gameguild/commit/a6ed3c49e3fc156773a905ec822a8fb00c73bb3b))
* **gglexical:** google-drive save document ([caca636](https://github.com/gameguild-gg/gameguild/commit/caca636e3922508c73c3dd056821ec2f7f350446))
* **gglexical:** google-drive sync ([182f5b2](https://github.com/gameguild-gg/gameguild/commit/182f5b29b034817cdb1f84616e9fee9e105699ea))
* **gglexical:** hyperlink plugin ([5ce161a](https://github.com/gameguild-gg/gameguild/commit/5ce161aa9f67dae59df9d375bc164f77f39e2028))
* **gglexical:** hyperlink plugin and selector http/https/local ([b1c5352](https://github.com/gameguild-gg/gameguild/commit/b1c53528cd7b039066b1fbe5173e5c3b28001f42))
* **gglexical:** hyperlink plugin component ([65ef83e](https://github.com/gameguild-gg/gameguild/commit/65ef83e405b111d51cad468220aa981cd7dc65b6))
* **gglexical:** hyperlink plugin open past link ([ece04e6](https://github.com/gameguild-gg/gameguild/commit/ece04e62ec9e4436ed4642542a2e9868eb245c30))
* **gglexical:** import project fixes ([a3bad6c](https://github.com/gameguild-gg/gameguild/commit/a3bad6ca330628cf7d01c78f5a68df55b1dafacf))
* **gglexical:** info-dialog tags ([fd096d2](https://github.com/gameguild-gg/gameguild/commit/fd096d2a039364a4ff6ab6e29948d8200b66303f))
* **gglexical:** list color fix ([384cfc9](https://github.com/gameguild-gg/gameguild/commit/384cfc9d555c5d3b420272e6d7939d54f9731e5e))
* **gglexical:** list color in editor ([dbbc213](https://github.com/gameguild-gg/gameguild/commit/dbbc213268f068bf5b84136d865138eb3f9b40ff))
* **gglexical:** list color use color-palette ([d7d3b03](https://github.com/gameguild-gg/gameguild/commit/d7d3b0300e21ea306ddb49fa1e7f85ddee54295e))
* **gglexical:** mode filter list ([a0a2e95](https://github.com/gameguild-gg/gameguild/commit/a0a2e957d21d2da3e2c0e303b46eb16d42bbd5a3))
* **gglexical:** new Google api GIS ([e759953](https://github.com/gameguild-gg/gameguild/commit/e759953977b5ec0c55b5c3711cf0be49cc377694))
* **gglexical:** open project auto sync googledrive ([46ecee2](https://github.com/gameguild-gg/gameguild/commit/46ecee233212fa01cd580ab55e040833db4c53db))
* **gglexical:** open project storage fix ([4757f2b](https://github.com/gameguild-gg/gameguild/commit/4757f2b9fd377be287073bc77328b50af9238895))
* **gglexical:** open project sync google-drive analyzer ([0aceb1f](https://github.com/gameguild-gg/gameguild/commit/0aceb1fd3abc559fe110b71dff43c357c5dadecc))
* **gglexical:** ordered list ([a2c7b0e](https://github.com/gameguild-gg/gameguild/commit/a2c7b0eaf2888b418c3c865f970d790031af6efa))
* **gglexical:** ordered list fix ([a432231](https://github.com/gameguild-gg/gameguild/commit/a43223156656467e9a5d45ce406f44515c038f3e))
* **gglexical:** presentation fixes ([2971c90](https://github.com/gameguild-gg/gameguild/commit/2971c9021446c140a58d5bfd15758768785d470c))
* **gglexical:** preview width ([b2c1e26](https://github.com/gameguild-gg/gameguild/commit/b2c1e26637b9c50e6a00b9c6c08c52898781f1ca))
* **gglexical:** project list line break ([a2099f1](https://github.com/gameguild-gg/gameguild/commit/a2099f1c1af7e8ce0b8ac3326029fd9b75416682))
* **gglexical:** quiz selection visible ([fd906e0](https://github.com/gameguild-gg/gameguild/commit/fd906e0d18294f11cb7cb1fe29790824df595801))
* **gglexical:** save and sync efficient ([80bdc6b](https://github.com/gameguild-gg/gameguild/commit/80bdc6bc62855b5db7124acb6bae45b8600b0052))
* **gglexical:** side error ([9e0f6a9](https://github.com/gameguild-gg/gameguild/commit/9e0f6a9242354b7849486966c5a097b5051acfbd))
* **gglexical:** storage selector fix ([f7c7727](https://github.com/gameguild-gg/gameguild/commit/f7c772763ea119211ebe057c7f101cffe8ceaa02))
* **gglexical:** superscript fix ([b44face](https://github.com/gameguild-gg/gameguild/commit/b44face0ac970b4a7c6a288eebd3d0b5ab46ef89))
* **gglexical:** system api for local, gameguild-cloud and google-drive ([f1d17c0](https://github.com/gameguild-gg/gameguild/commit/f1d17c0b743355f1c2d9f838713a1e0450d5e3f8))
* **gglexical:** table plugin fix ([7d38ae5](https://github.com/gameguild-gg/gameguild/commit/7d38ae5a83bde8789d44f0c77d0ac2dd2b554af4))
* **gglexical:** table-of-contents container ([37d3c52](https://github.com/gameguild-gg/gameguild/commit/37d3c5266e5f12ffe5cbc26a5dc701bef398436a))
* **gglexical:** text color apply color in text not marked ([04bd911](https://github.com/gameguild-gg/gameguild/commit/04bd9116f7c9e5b20a66e38804cb2a1aead8e9f4))
* **gglexical:** TopMenu ([122a38f](https://github.com/gameguild-gg/gameguild/commit/122a38f232946a7745edfd6e0b3ca61e15e36c80))
* **gglexical:** unordered list fix ([6556b6d](https://github.com/gameguild-gg/gameguild/commit/6556b6d942a8ff29d3e34cc20ec392cbb1d57a82))
* **gglexical:** view document list layout fix ([f4afc5e](https://github.com/gameguild-gg/gameguild/commit/f4afc5ea27f3eae507ad0262703ef5c37825f935))


### Reverts

* Revert "refactor(gglexical): search filters" ([2f5e78c](https://github.com/gameguild-gg/gameguild/commit/2f5e78cce0faa4b418f46b9107d64533706722ba))

# [2.22.0](https://github.com/gameguild-gg/gameguild/compare/v2.21.4...v2.22.0) (2025-09-29)


### Features

* **course:** Add 'Automation in Game Development' module to Intro to GPro ([413efd2](https://github.com/gameguild-gg/gameguild/commit/413efd2f6326c6b309811ee1574cbf284d39335e))
* **mermaid:** Standardize chart background and container styling ([6b1af26](https://github.com/gameguild-gg/gameguild/commit/6b1af26dffc29b13ef4807881acd295b157463ef))

## [2.21.4](https://github.com/gameguild-gg/gameguild/compare/v2.21.3...v2.21.4) (2025-09-22)


### Bug Fixes

* **web:** Adds week 5 content on game dev issues ([eddf87c](https://github.com/gameguild-gg/gameguild/commit/eddf87c81a6aad43175f50a004df031b89ee2eaa))

## [2.21.3](https://github.com/gameguild-gg/gameguild/compare/v2.21.2...v2.21.3) (2025-09-22)


### Bug Fixes

* **dsa:** Refactors merge sort explanations ([0ed52dc](https://github.com/gameguild-gg/gameguild/commit/0ed52dc3dcbb60201e5154fba4ccff9a0153f511))

## [2.21.2](https://github.com/gameguild-gg/gameguild/compare/v2.21.1...v2.21.2) (2025-09-22)


### Bug Fixes

* **web:** Adds content for AI for games week 3 and 5 ([4b0d2fe](https://github.com/gameguild-gg/gameguild/commit/4b0d2fe36905fdca92b833997cc5c885ebb11830))

## [2.21.1](https://github.com/gameguild-gg/gameguild/compare/v2.21.0...v2.21.1) (2025-09-20)


### Bug Fixes

* **web:** Adds behaviour and decision trees ([44eead1](https://github.com/gameguild-gg/gameguild/commit/44eead14dd9f0115e38a8644768ef732133f442c))

# [2.21.0](https://github.com/gameguild-gg/gameguild/compare/v2.20.0...v2.21.0) (2025-09-16)


### Features

* **api:** Add TestingLab adapter services ([407fdde](https://github.com/gameguild-gg/gameguild/commit/407fddeebfbb8e4e27882aa8a5286a30e595e73d))
* **api:** Add TestingLab repositories ([e0bba37](https://github.com/gameguild-gg/gameguild/commit/e0bba376e2def4a79844c7212b0d3687c19985df))
* **web:** Add nested content for 'Gamedev Issues' in mock data ([c051067](https://github.com/gameguild-gg/gameguild/commit/c0510671c48775f78ce58e62eea05653bba055b7))
* **web:** Group game publishing content by platform in mock data ([2c6cd95](https://github.com/gameguild-gg/gameguild/commit/2c6cd95b044382f757456a17bd4fc40b830072d5))
* **web:** Refactor course content sidebar for hierarchical display ([0898092](https://github.com/gameguild-gg/gameguild/commit/08980928f7c4826f150544a185409f8bd0d97297))

# [2.20.0](https://github.com/gameguild-gg/gameguild/compare/v2.19.0...v2.20.0) (2025-09-15)


### Features

* **web:** Add game development issues content ([06466e6](https://github.com/gameguild-gg/gameguild/commit/06466e63f878cca5ba571f4f7450ecae73a96753))

# [2.19.0](https://github.com/gameguild-gg/gameguild/compare/v2.18.0...v2.19.0) (2025-09-13)


### Features

* **web:** Adds game publishing course content ([cf9ad40](https://github.com/gameguild-gg/gameguild/commit/cf9ad40642d41cd8ee614b8eed7b6ce3e346e8cd))

# [2.18.0](https://github.com/gameguild-gg/gameguild/compare/v2.17.2...v2.18.0) (2025-09-08)


### Features

* **web:** Adds game development careers content ([4e89619](https://github.com/gameguild-gg/gameguild/commit/4e8961979352c25984a9ce6a31cbc85c0c79b888))

## [2.17.2](https://github.com/gameguild-gg/gameguild/compare/v2.17.1...v2.17.2) (2025-09-06)


### Bug Fixes

* **ai4games:** Corrects cohesion image label ([71d896f](https://github.com/gameguild-gg/gameguild/commit/71d896f1a5075ecbe7f54815496312d3d030ac6d))

## [2.17.1](https://github.com/gameguild-gg/gameguild/compare/v2.17.0...v2.17.1) (2025-09-06)


### Bug Fixes

* **courses:** Corrects cohesion example image link ([a70f1fe](https://github.com/gameguild-gg/gameguild/commit/a70f1fe5297f9d6e079184ee37d129f9db9ee2cf))

# [2.17.0](https://github.com/gameguild-gg/gameguild/compare/v2.16.0...v2.17.0) (2025-09-04)


### Bug Fixes

* **ai4games:** Improve math notation in 'Conway's Game of Life' lesson ([de1331c](https://github.com/gameguild-gg/gameguild/commit/de1331c8274eac08aa59b2582a432c1f0ff2cfb8))


### Features

* **ai4games:** Add 'State Machines' lesson content ([741c1d5](https://github.com/gameguild-gg/gameguild/commit/741c1d5ef503b81e1372cec2fedcf8442ef2a79a))
* **ai4games:** Integrate 'State Machines' content into course mock data ([dde83d5](https://github.com/gameguild-gg/gameguild/commit/dde83d539a6b97e965904c1a5046c073d20ed8d6))

# [2.16.0](https://github.com/gameguild-gg/gameguild/compare/v2.15.3...v2.16.0) (2025-09-01)


### Features

* **courses:** Adds intro to game dev tools content ([4a61039](https://github.com/gameguild-gg/gameguild/commit/4a61039887463ca8e78c55c9eaae1b8d60d8d135))

## [2.15.3](https://github.com/gameguild-gg/gameguild/compare/v2.15.2...v2.15.3) (2025-08-28)


### Bug Fixes

* **courses:** expectations and add week01 content ([2a3e65c](https://github.com/gameguild-gg/gameguild/commit/2a3e65c1e2116467d2a31143024c22ba61db0490))

## [2.15.2](https://github.com/gameguild-gg/gameguild/compare/v2.15.1...v2.15.2) (2025-08-28)


### Bug Fixes

* **ai_for_games:** Wrong image for Cohesion ([640d6c0](https://github.com/gameguild-gg/gameguild/commit/640d6c0557f6544bf780e6f9e1ff181aca28b590))

## [2.15.1](https://github.com/gameguild-gg/gameguild/compare/v2.15.0...v2.15.1) (2025-08-28)


### Bug Fixes

* **dsa:** add more details to dsa content ([ed403f3](https://github.com/gameguild-gg/gameguild/commit/ed403f3443c82b8483dd6ab79bb9e4e7f0902bd8))

# [2.15.0](https://github.com/gameguild-gg/gameguild/compare/v2.14.0...v2.15.0) (2025-08-28)


### Bug Fixes

* Ferpa Waiver ([6b66fe2](https://github.com/gameguild-gg/gameguild/commit/6b66fe2a675d9a4a1c352f570767b9a9db49aa6f))


### Features

* **ai4games:** Add week 01 expectations content file ([346e24b](https://github.com/gameguild-gg/gameguild/commit/346e24b935af65a52ce7fe661e8ee45817f1166b))
* **dsa:** Add week 01 expectations content file ([c27a23a](https://github.com/gameguild-gg/gameguild/commit/c27a23ae0692811a59fd7a3b81e9b0ff381f78ee))
* **mock-data:** Integrate AI for Games expectations into mock data ([99308cc](https://github.com/gameguild-gg/gameguild/commit/99308cce7a9dbb4142b6ada94d5f3bd04fbc5e2f))
* **mock-data:** Integrate DSA expectations and update sort orders ([45a7d58](https://github.com/gameguild-gg/gameguild/commit/45a7d588aa610ebde2c2e9db5b3745d14d2734bd))

# [2.14.0](https://github.com/gameguild-gg/gameguild/compare/v2.13.0...v2.14.0) (2025-08-27)


### Features

* **academic-honesty:** Introduce Academic Honesty Policy page ([3835957](https://github.com/gameguild-gg/gameguild/commit/3835957e7dfdd385515dbec20dd0dd5bab45244d))

# [2.13.0](https://github.com/gameguild-gg/gameguild/compare/v2.12.0...v2.13.0) (2025-08-25)


### Bug Fixes

* setup instructions ([7850ee2](https://github.com/gameguild-gg/gameguild/commit/7850ee2f249bf6082f2905dba45b0454b23ed66e))


### Features

* add Data Structures and Algorithms course content ([64c3161](https://github.com/gameguild-gg/gameguild/commit/64c316179aacdc09df0d6fa01fbfd02eaaef1fb4))
* **apps/api:** Add commands for managing testing requests and sessions ([3ddf1b2](https://github.com/gameguild-gg/gameguild/commit/3ddf1b267fdbd86f230577857cd80edcfa74e6e5))
* **apps/api:** Add comprehensive TestingLab module abstractions and service interfaces ([1b0828b](https://github.com/gameguild-gg/gameguild/commit/1b0828b3c6aec34ccdb58534156a245c6824cab2))
* **apps/api:** Add comprehensive TestingLab module with entities, enums, and permissions ([bbc5b73](https://github.com/gameguild-gg/gameguild/commit/bbc5b7318da1e9888815d32bcdec9780be1199fc))
* **apps/api:** Add handlers for testing requests and sessions with event publishing ([1a306cb](https://github.com/gameguild-gg/gameguild/commit/1a306cb0acefdb44355aca4f42c957484a75895d))
* **apps/api:** Add queries for testing lab data retrieval ([4a4985a](https://github.com/gameguild-gg/gameguild/commit/4a4985ae9b39acfed2540e981dcd48e96a600daf))
* **apps/api:** Add testing lab event notifications for feedback, requests, and sessions ([8e23d4c](https://github.com/gameguild-gg/gameguild/commit/8e23d4cbc08dcb41041ccb44a4593172aa47b1df))
* **apps/api:** Add validators for TestingLab commands ([3798695](https://github.com/gameguild-gg/gameguild/commit/3798695ea4e41fb5a10d9351840b31dc0a7c3c83))
* **apps/api:** Revamp TestingLab module with new entities, services, commands, queries, and permission management ([81dc6cc](https://github.com/gameguild-gg/gameguild/commit/81dc6ccab8ca4ca4c9feae93236c64996975b2a4))
* integrate DSA course into mock data system ([113cdfd](https://github.com/gameguild-gg/gameguild/commit/113cdfd36efea9b5461b13ecbd1e32ad11d2e1c4))

## [2.10.2](https://github.com/gameguild-gg/gameguild/compare/v2.10.1...v2.10.2) (2025-08-23)

### Bug Fixes

* add .vscode settings to prevent markdown
  auto-formatting ([63b9e8c](https://github.com/gameguild-gg/gameguild/commit/63b9e8c87c43be8ca30d764cfb9f9f2bdcf54ca0))
* correct LaTeX formatting in
  flocking.md ([b23fb18](https://github.com/gameguild-gg/gameguild/commit/b23fb183c901a8328ec793fcce8a1b3af5b07341))
* update markdown content and
  renderer ([d70579d](https://github.com/gameguild-gg/gameguild/commit/d70579d85ea7449fd0dd3f6c11a45106006db088))

## [2.10.1](https://github.com/gameguild-gg/gameguild/compare/v2.10.0...v2.10.1) (2025-08-23)

### Bug Fixes

* Update flocking.md
  content ([e8f09c9](https://github.com/gameguild-gg/gameguild/commit/e8f09c9f7d422f8f3bb1919f5f5ad2a7a8bf7d2a))

# [2.10.0](https://github.com/gameguild-gg/gameguild/compare/v2.9.0...v2.10.0) (2025-08-21)

### Features

* add redirect from /p to
  /programs ([378fadc](https://github.com/gameguild-gg/gameguild/commit/378fadcd334c5c498683547caf59d30b3f8e3a03))
* restructure course routes and cleanup empty
  files ([ed7c4c4](https://github.com/gameguild-gg/gameguild/commit/ed7c4c42b9f24462193cf8abd6cf2594bc5f479f))

# [2.9.0](https://github.com/gameguild-gg/gameguild/compare/v2.8.3...v2.9.0) (2025-08-21)

### Bug Fixes

* Add missing username generation for OAuth user
  creation ([6420e6a](https://github.com/gameguild-gg/gameguild/commit/6420e6aa8f337b67cfc294c8aa3f57fa830da8c8))

### Features

* add GitHub issue modals to auth links and remove sign-up
  div ([0a79d0f](https://github.com/gameguild-gg/gameguild/commit/0a79d0f83ddccecc146318fcb85b0e952bf5080e))

## [2.8.3](https://github.com/gameguild-gg/gameguild/compare/v2.8.2...v2.8.3) (2025-08-21)

### Bug Fixes

* cloudflare dns
  update ([d10a377](https://github.com/gameguild-gg/gameguild/commit/d10a37780fe7e55f1d84aa8aa8c071ece8379268))
* DNS update logic and enhance authentication error
  handling ([fe8cf02](https://github.com/gameguild-gg/gameguild/commit/fe8cf02316bd7867a854b9c3269676d9a2b4f48a))

## [2.8.2](https://github.com/gameguild-gg/gameguild/compare/v2.8.1...v2.8.2) (2025-08-21)

### Bug Fixes

* Allow course content access without
  authentication ([3d523e8](https://github.com/gameguild-gg/gameguild/commit/3d523e8eb21306110a8feeebb4b89a0ceb929065))

## [2.8.1](https://github.com/gameguild-gg/gameguild/compare/v2.8.0...v2.8.1) (2025-08-21)

### Bug Fixes

* Add suppressHydrationWarning to gglexical layout body
  element ([d886d14](https://github.com/gameguild-gg/gameguild/commit/d886d1456db47db3687decc414dbf52b5159f8f5))

# [2.8.0](https://github.com/gameguild-gg/gameguild/compare/v2.7.1...v2.8.0) (2025-08-21)

### Bug Fixes

* actions ([5d27c92](https://github.com/gameguild-gg/gameguild/commit/5d27c926b3b9e978f286ae19642169e7c1a8ec17))
* resolve conflicting star exports in activity-tracking
  module ([2d29a60](https://github.com/gameguild-gg/gameguild/commit/2d29a60e6151394304cc6a5c97fd2e76d99b0872))

### Features

* add GitHub fork button
  component ([649af32](https://github.com/gameguild-gg/gameguild/commit/649af326636e6c1be94c1bafa41d8797fe67a099))
* add responsive sidebar infrastructure for course
  content ([0ec4fac](https://github.com/gameguild-gg/gameguild/commit/0ec4fac254768ccb8de4a631f0fed3eb576c1922))
* enhance course content sidebar with theme toggle and
  header ([3d3234e](https://github.com/gameguild-gg/gameguild/commit/3d3234edc423562a29c21a01e3b8188ba031e97b))
* integrate GitHub fork button into default
  header ([0e48290](https://github.com/gameguild-gg/gameguild/commit/0e48290520c309019d4b3c32f50025b915ad2031))
* restructure course content routing with new
  layout ([1893d0e](https://github.com/gameguild-gg/gameguild/commit/1893d0e214111c38d73eb756d51dae34d704e395))

## [2.7.1](https://github.com/gameguild-gg/gameguild/compare/v2.7.0...v2.7.1) (2025-08-20)

### Bug Fixes

* **editor:** callout to
  admonition ([6a24b8b](https://github.com/gameguild-gg/gameguild/commit/6a24b8b6026ba3bda6794aaf41b7fc9a24d55adb))
* **editor:** import
  fix ([641e441](https://github.com/gameguild-gg/gameguild/commit/641e4410ca0b6592a63d82090e149d1e69581895))
* **editor:** logic fix in
  preview-table-of-contents ([365637d](https://github.com/gameguild-gg/gameguild/commit/365637da8ef16f1a1ddfdc90e65a040594f8928a))
* **editor:** open project
  fix ([09561b1](https://github.com/gameguild-gg/gameguild/commit/09561b1ac301af84df013d57dcc82f06cca3ad87))
* **editor:** preview page = serializedState
  type ([3a96333](https://github.com/gameguild-gg/gameguild/commit/3a9633305f82d465853859d0e2cb0717c818d229))
* **editor:** preview page focus
  serializedState ([f21ec4d](https://github.com/gameguild-gg/gameguild/commit/f21ec4dd2a5e8e12a1572ae82c8baac90ce1aea9))
* **layouts:** implement comprehensive hydration and theme
  fixes ([c908e0b](https://github.com/gameguild-gg/gameguild/commit/c908e0bcc74f4c0cc589e55dac70ed475619472b))
* **ui:** resolve hydration issues, theme persistence, and error
  handling ([bce7a6a](https://github.com/gameguild-gg/gameguild/commit/bce7a6a2ec8c6134c49b2ed30c4e809138f4ee07))

# [2.7.0](https://github.com/gameguild-gg/gameguild/compare/v2.6.0...v2.7.0) (2025-08-20)

### Bug Fixes

* **editor:** open project
  fix ([75a2b51](https://github.com/gameguild-gg/gameguild/commit/75a2b51dfc4f7606c3860a8f29e42a4c62a23684))

### Features

* **courses:** add AI for Games course content and update mock
  data ([b54825a](https://github.com/gameguild-gg/gameguild/commit/b54825a748b009f20e0f8c49902fd340d547545b))

# [2.6.0](https://github.com/gameguild-gg/gameguild/compare/v2.5.3...v2.6.0) (2025-08-19)

### Features

* add slug field to ProgramContent
  model ([e124989](https://github.com/gameguild-gg/gameguild/commit/e124989da8afdd60f4393f6475987c02fead65c5))
* add slug field to ProgramContent
  model ([b644747](https://github.com/gameguild-gg/gameguild/commit/b644747a50d153be0a1435cc26f5d5da82b6b2da))

## [2.5.3](https://github.com/gameguild-gg/gameguild/compare/v2.5.2...v2.5.3) (2025-08-19)

### Bug Fixes

* remove /dashboard prefix from course and program viewing
  links ([95c3fea](https://github.com/gameguild-gg/gameguild/commit/95c3feacffefe2d01aa70a701c43d5cc2c363fa6))

## [2.5.2](https://github.com/gameguild-gg/gameguild/compare/v2.5.1...v2.5.2) (2025-08-19)

### Bug Fixes

* **editor:** callout
  component ([439c983](https://github.com/gameguild-gg/gameguild/commit/439c98377fd3c434841746a4e3ded451d0d72d2e))
* **web:** new
  packages ([7f4f7e5](https://github.com/gameguild-gg/gameguild/commit/7f4f7e5f09d625e66c93a9bd3f966458fc6b6b9e))

## [2.5.1](https://github.com/gameguild-gg/gameguild/compare/v2.5.0...v2.5.1) (2025-08-18)

### Bug Fixes

* Add global user permission fallback in
  PermissionService ([b62cd81](https://github.com/gameguild-gg/gameguild/commit/b62cd8136fa0b7aed41e6ac6c11ae2b83b0779c6))

# [2.5.0](https://github.com/gameguild-gg/gameguild/compare/v2.4.2...v2.5.0) (2025-08-18)

### Features

* replace ForbidResult with custom PermissionDeniedResult for better error
  messaging ([bc4d002](https://github.com/gameguild-gg/gameguild/commit/bc4d002705c9717eeb9d28405e3eb04eccde9983))

## [2.4.2](https://github.com/gameguild-gg/gameguild/compare/v2.4.1...v2.4.2) (2025-08-18)

### Bug Fixes

* improve GitHub API error handling and data
  display ([071a85e](https://github.com/gameguild-gg/gameguild/commit/071a85e3400b6d5c2642ee64ba933ab70e356610))

## [2.4.1](https://github.com/gameguild-gg/gameguild/compare/v2.4.0...v2.4.1) (2025-08-18)

### Bug Fixes

* update AGPL v3 license link to point to LICENSE instead of
  LICENSE.md ([1ab28e7](https://github.com/gameguild-gg/gameguild/commit/1ab28e718339ff8a3e31ff48132dc6fca74a1d9a))

# [2.4.0](https://github.com/gameguild-gg/gameguild/compare/v2.3.0...v2.4.0) (2025-08-18)

### Features

* fix post-login UI state update and add password visibility
  toggle ([8e8c677](https://github.com/gameguild-gg/gameguild/commit/8e8c67776b36ad8e5fbc074710668af1d544b9ef))

# [2.3.0](https://github.com/gameguild-gg/gameguild/compare/v2.2.0...v2.3.0) (2025-08-18)

### Features

* add GitHub issue modal to Terms of Service, Privacy, and Cookies
  links ([cc47342](https://github.com/gameguild-gg/gameguild/commit/cc473429cffb2d87599e0df0f1591fa3389e7236))
* update licenses page to fetch from GitHub API with caching and remove SPDX
  identifier ([7065633](https://github.com/gameguild-gg/gameguild/commit/706563356ab5bdb8171135a30f4157d0c8cfc251))

# [2.2.0](https://github.com/gameguild-gg/gameguild/compare/v2.1.0...v2.2.0) (2025-08-18)

### Bug Fixes

* improve GitHub license
  detection ([0b89092](https://github.com/gameguild-gg/gameguild/commit/0b890926afe7ead5e43592f129857fa3d0346463))
* remove note from LICENSE file to restore GitHub AGPL
  detection ([ccf5b16](https://github.com/gameguild-gg/gameguild/commit/ccf5b1608a7a81cf3b1e224d5b72b6ac53dbbee3))

### Features

* restructure licensing with separate license
  files ([26467b6](https://github.com/gameguild-gg/gameguild/commit/26467b67d6cdb8bc066b4b29193a8f9e23a2bd57))

# [2.1.0](https://github.com/gameguild-gg/gameguild/compare/v2.0.4...v2.1.0) (2025-08-18)

### Bug Fixes

* **apps/api:** Add slug generation for projects and fix existing projects without
  slugs ([cb1481f](https://github.com/gameguild-gg/gameguild/commit/cb1481f0e555224107bc7027ad12e0e32f2ee26c))
* **apps/web/src/components/courses/forms/create-course-form.tsx:** Improve error handling and logging in create course
  form submission ([8bf2b7d](https://github.com/gameguild-gg/gameguild/commit/8bf2b7d9c3f3abdd0adaa0bff82484445fa9e1aa))
* **apps/web:** Add safety checks and error handling for
  autoplay ([bfb28dc](https://github.com/gameguild-gg/gameguild/commit/bfb28dc8013d94c6d466f31a501fefdaba889962))
* **apps/web:** Use fallback project ID in project card links and fix sub-nav base
  path ([2740583](https://github.com/gameguild-gg/gameguild/commit/2740583b883ae8312a9528d3f41a14c4cfc01f82))
* **apps/web:** Use project slug in ProjectCard links instead of
  ID ([8f31892](https://github.com/gameguild-gg/gameguild/commit/8f31892fd8ac6a4b5b1b4cd9ece62a7a1749f27a))
* **courses:** Implement better validation for tools in course
  cards ([46f19b4](https://github.com/gameguild-gg/gameguild/commit/46f19b47c5cbcd6a701ec8d4bdabceb7b3103c59))
* **projects:** Refactor project list to support creation and display
  enhancements ([521b6a5](https://github.com/gameguild-gg/gameguild/commit/521b6a592277d31da022a4779358de794e392000))

### Features

* **apps/api:** Add AddCertificateTagDto for managing certificate
  tags ([5f3f79d](https://github.com/gameguild-gg/gameguild/commit/5f3f79d30ca97b7e767ba918a2c096510cdc1227))
* **apps/api:** Add CertificateDtoMappings for entity to DTO
  conversion ([c0da78d](https://github.com/gameguild-gg/gameguild/commit/c0da78d00f8422b2848cdeaa39da7980e238c3d9))
* **apps/api:** Add CreateCertificateDto and UpdateCertificateDto for certificate data
  transfer ([514a898](https://github.com/gameguild-gg/gameguild/commit/514a898a54629b9a4bc1c3af09183fc8130a2354))
* **apps/api:** Add unique username field with auto-generation
  support ([a9d9557](https://github.com/gameguild-gg/gameguild/commit/a9d95575f51768e4a6b7252ca4f1914fab0a9e83))
* **apps/api:** Add username migration
  files ([4cf5c99](https://github.com/gameguild-gg/gameguild/commit/4cf5c998df85ebf2a25462bac45204bb549c216a))
* **apps/api:** Implement ProgramCertificatesController for managing
  certificates ([35961fa](https://github.com/gameguild-gg/gameguild/commit/35961fa2c060ac0aa522a137dbb34aa39bbbcff8))
* **apps/api:** Implement unique username migration and update
  handler ([6f61680](https://github.com/gameguild-gg/gameguild/commit/6f61680b4b308249291cbefbef034b830c8c7408))
* **apps/web/src/app/[locale]/(dashboard)/dashboard/(content)/courses/page.tsx:** Add Create Course button to courses
  list page ([c4fa719](https://github.com/gameguild-gg/gameguild/commit/c4fa7190e70ffde931cbc1015f3f9c51875d3e25))
* **apps/web/src/app/[locale]/(dashboard)/dashboard/(content)/projects/[slug]/distribution/page.tsx:** Implement
  Distribution page with channels and promotion
  cards ([95a68b8](https://github.com/gameguild-gg/gameguild/commit/95a68b8d7ff826d1743f77f93d47cd05ca45cb8f))
* **apps/web/src/app/[locale]/(dashboard)/dashboard/(content)/projects/[slug]/feedbacks/page.tsx:** Add feedbacks page
  with message for no feedback
  yet ([8989246](https://github.com/gameguild-gg/gameguild/commit/89892468d22f058cd9d26a4700fdd226a7fd33ca))
* **apps/web/src/app/[locale]/(dashboard)/dashboard/(content)/projects/[slug]/game-jams/page.tsx:** Add Game Jams page
  with empty participations
  message ([102e10e](https://github.com/gameguild-gg/gameguild/commit/102e10e2ef91e4eb93209acf8b334c7c84dfebed))
* **apps/web/src/app/[locale]/(dashboard)/dashboard/(content)/projects/[slug]/settings/page.tsx:** Replace project
  testing lab settings placeholder with project settings
  card ([ba500e7](https://github.com/gameguild-gg/gameguild/commit/ba500e756460c68aaa02a77b408f3f3549aaee52))
* **apps/web/src/app/[locale]/(dashboard)/dashboard/(content)/projects/[slug]/store-presence/page.tsx:** Add editable
  store presence page with basic info and media asset
  sections ([7f7c258](https://github.com/gameguild-gg/gameguild/commit/7f7c25867e89279f20144f61af71806b6bab6276))
* **apps/web/src/app/[locale]/(dashboard)/dashboard/(content)/projects/[slug]/team/page.tsx:** Implement team page
  displaying team members with avatars and
  roles ([0971dd2](https://github.com/gameguild-gg/gameguild/commit/0971dd232a3fef1473ba31e9e5432dd472971e39))
* **apps/web/src/app/[locale]/(dashboard)/dashboard/(content)/projects/[slug]/testing/page.tsx:** Add testing sessions
  page with table and new session
  button ([be6cd96](https://github.com/gameguild-gg/gameguild/commit/be6cd96f488f9e1372e4f8f4b95472ca31219e5b))
* **apps/web/users:** Add top-right profile action
  buttons ([a445f66](https://github.com/gameguild-gg/gameguild/commit/a445f6606c5095c146d6b83cfc0266d3a5b773ba))
* **apps/web:** Add CourseCard component with default and compact
  views ([8f06574](https://github.com/gameguild-gg/gameguild/commit/8f0657491cc476875e5c3455237d3d44cc5324ea))
* **apps/web:** Add legacy redirects and update
  routing ([b1793de](https://github.com/gameguild-gg/gameguild/commit/b1793def0a55f69749947e06357e30a7ea157db9))
* **apps/web:** Add legacy route rewrites for
  projects ([c125822](https://github.com/gameguild-gg/gameguild/commit/c125822f8b4dc256559dafa2baf89120626490b4))
* **apps/web:** Add project route layout with context provider and
  navigation ([3aa53cf](https://github.com/gameguild-gg/gameguild/commit/3aa53cf8555133d6470a70aad85f62113a83fc53))
* **apps/web:** Add Store Presence page for managing project store info and
  media ([a9b2d6a](https://github.com/gameguild-gg/gameguild/commit/a9b2d6a44fa1f34f280857d30e0ee52c5934e9ac))
* **apps/web:** Add Team page to manage and invite project
  members ([6d15edd](https://github.com/gameguild-gg/gameguild/commit/6d15eddfc363543b129de0ce332ce955e2f58d2f))
* **apps/web:** Add user profile page with username
  lookup ([3729b58](https://github.com/gameguild-gg/gameguild/commit/3729b58047c160838fe544e62b312f14f07c383a))
* **apps/web:** Add Versions page to manage game builds and show upload
  history ([ed8d846](https://github.com/gameguild-gg/gameguild/commit/ed8d8460f16c122ef450b18b9669e010c6721235))
* **apps/web:** Enhance user lookup with auth
  fallback ([d4d5985](https://github.com/gameguild-gg/gameguild/commit/d4d59853663257bc271ec29a38b951ddeba43e0e))
* **apps/web:** Implement CourseCard for program details in list and grid
  view ([d635b75](https://github.com/gameguild-gg/gameguild/commit/d635b753f2f01633a3a1a22a6dfe91e33764dda0))
* **apps/web:** Implement detailed project overview page with stats and
  activities ([279299c](https://github.com/gameguild-gg/gameguild/commit/279299c05c1d9491a31df4c83ab491ed6817ed02))
* **apps/web:** Integrate content management API for
  projects ([723761f](https://github.com/gameguild-gg/gameguild/commit/723761f4dc2884831eae988a638f5a178f0e150b))
* **apps/web:** Update courses and projects dashboard
  pages ([97ff75d](https://github.com/gameguild-gg/gameguild/commit/97ff75dd5cfe248402547ab8e978ea3f3eee3778))
* **apps:** Update GraphQL resolvers, schema and client
  auth ([4a43e4f](https://github.com/gameguild-gg/gameguild/commit/4a43e4f7c2248bdc732a2dcf2db262516a109f80))
* **content-management/programs:** Add program-to-course transformation
  utility ([9176960](https://github.com/gameguild-gg/gameguild/commit/91769608cdb310f16d575ec37e1a52062a88a4db))
* **courses:** Add course card and filtering
  mechanisms ([0ce270f](https://github.com/gameguild-gg/gameguild/commit/0ce270f17980a1f68cc2f7531809d31ca9811fa1))
* **courses:** Create initial course page with mock
  data ([5185c7e](https://github.com/gameguild-gg/gameguild/commit/5185c7ec35936fed60be0278283949ef744746b4))
* **github-issues-modal:** add a new modal to allow users to report issues directly to
  github ([fb1f976](https://github.com/gameguild-gg/gameguild/commit/fb1f976cd9b8139d79fb12e68311db10460d6941))
* implement fail-safe GraphQL codegen to prevent build
  failures ([3b19128](https://github.com/gameguild-gg/gameguild/commit/3b191282948898da60c3a2100a45a41972c11c3e))
* **local-db:** Implement local storage for game projects and
  courses ([20a3fd7](https://github.com/gameguild-gg/gameguild/commit/20a3fd7c6cad160270580912ec7a08897ff0ad41))
* **projects:** Integrate new project management dialog in projects
  page ([881be20](https://github.com/gameguild-gg/gameguild/commit/881be209354da36187f75910a4e88de5e2c35dac))
* **types:** Define Course and related types for course
  management ([a5861cb](https://github.com/gameguild-gg/gameguild/commit/a5861cb67d0a745f47ea49cbca964e2c846b653c))
* **users:** Improve search matching and profile
  redirection ([c6d42d0](https://github.com/gameguild-gg/gameguild/commit/c6d42d06c28bc8fcafd6b063406d902bcbaa2781))

## [2.0.4](https://github.com/gameguild-gg/gameguild/compare/v2.0.3...v2.0.4) (2025-08-13)

### Bug Fixes

* **contributors:** better alignment or contributors
  cards ([d303043](https://github.com/gameguild-gg/gameguild/commit/d30304378d15d7dbb10da334b2dc7bf9cedd4624))

## [2.0.3](https://github.com/gameguild-gg/gameguild/compare/v2.0.2...v2.0.3) (2025-08-13)

### Bug Fixes

* **contributors:** improve numbers and links on
  contributors ([8b59735](https://github.com/gameguild-gg/gameguild/commit/8b59735a5070fa43ec481cfda80c9cef5fe537d0))
* **license:** add dual license identifier so GH capture its
  properly [skip ci] ([3877922](https://github.com/gameguild-gg/gameguild/commit/3877922cf020b9b4d8c88a2b823bb539f17af25d))

## [2.0.2](https://github.com/gameguild-gg/gameguild/compare/v2.0.1...v2.0.2) (2025-08-13)

### Bug Fixes

* **header:** fix header collapsible
  menu ([66cdfd8](https://github.com/gameguild-gg/gameguild/commit/66cdfd8045da56888c630ca169d93507d6a6edab))

## [2.0.1](https://github.com/gameguild-gg/gameguild/compare/v2.0.0...v2.0.1) (2025-08-13)

### Bug Fixes

* **contributors:** horizontal
  scroll ([03122b4](https://github.com/gameguild-gg/gameguild/commit/03122b42f707db0f0bad9a550acd225d877b558a))
* **header:** add drop shadow to the
  logo ([4b9e21f](https://github.com/gameguild-gg/gameguild/commit/4b9e21feb72ae583ed6d9283bd6bd2eb6bc5dfd6))
* **header:** in small
  screens ([f65e9d5](https://github.com/gameguild-gg/gameguild/commit/f65e9d573c83527c14d8830d2c5759071d2989d6))

# [2.0.0](https://github.com/gameguild-gg/gameguild/compare/v1.21.8...v2.0.0) (2025-08-12)

### Bug Fixes

* add next-intl configuration to resolve internationalization
  error ([46aa730](https://github.com/gameguild-gg/gameguild/commit/46aa73086672f400f44ba67213ae5087f3c0e0f1))
* api build ([1500a76](https://github.com/gameguild-gg/gameguild/commit/1500a7611bd9d8c1e415e6c26925eee448495cc7))
* **api/migrations:** Remove IsDefault column from
  TenantUserGroups ([baca158](https://github.com/gameguild-gg/gameguild/commit/baca1582c5682a3ad5b38254dc116a0a724e8661))
* **api/tests:** Update endpoints and DbContext factory
  usage ([cf9d20a](https://github.com/gameguild-gg/gameguild/commit/cf9d20a96648207d8825ca281e7e7325e553a3cb))
* **api/tests:** Use Append for tenant header and update assembly info
  version ([0bdafa6](https://github.com/gameguild-gg/gameguild/commit/0bdafa6e861dc46d1142e4dd9d71d1261f6819a8))
* **api:** Change API port from 5001 to
  5000 ([ed02711](https://github.com/gameguild-gg/gameguild/commit/ed027111bc7461c211e87e39f0dcd2431aadf617))
* **api:** cors ([62cf69c](https://github.com/gameguild-gg/gameguild/commit/62cf69c5391e7f078bfdeed648d625c10b10d8a8))
* **apigen:** fix enum
  serialization ([5ff3b64](https://github.com/gameguild-gg/gameguild/commit/5ff3b641cfdab5409223b54b4a131356b51521c8))
* **api:** Improve error handling and domain
  deserialization ([9b21db4](https://github.com/gameguild-gg/gameguild/commit/9b21db42b47cb4f2f9836cf28352a6b1a799b1b9))
* **api:** swagger
  generation ([61defa2](https://github.com/gameguild-gg/gameguild/commit/61defa2af37c4a98486ed9145b41a35bd7ec5ff4))
* **api:** Update endpoints and tests for user
  profiles ([793882e](https://github.com/gameguild-gg/gameguild/commit/793882e31d794eb26aaaf07a9dc6301ac84b937c))
* **apps/api/TestingLab:** Add sessionId parameter to resource permission
  attribute ([74ad256](https://github.com/gameguild-gg/gameguild/commit/74ad256198c9424ef6ef90f0852cfa707b0cde3f))
* **apps/api:** Add null fallbacks in achievement
  module ([a08a049](https://github.com/gameguild-gg/gameguild/commit/a08a049e6709f1b8f415eefedb1c64dfd6ec9197))
* **apps/api:** Add resource id parameter for permission
  attribute ([a069d8a](https://github.com/gameguild-gg/gameguild/commit/a069d8ac525ed9885d64f92c2fa5e15bab17a478))
* **apps/api:** Default test status to
  Draft ([2a6c592](https://github.com/gameguild-gg/gameguild/commit/2a6c5927187ac76178bc036a9605fdc0fbca0f2f))
* **apps/api:** Improve introspection, validation, and error
  handling ([fb22c7d](https://github.com/gameguild-gg/gameguild/commit/fb22c7d68d2b21b9d2b75a5705ab941c2431c765))
* **apps/api:** Update permission attribute with sessionId
  parameter ([2334337](https://github.com/gameguild-gg/gameguild/commit/233433784b5cd82fda67ff8baef63503560bd7ec))
* **apps/web/dashboard:** Handle tenant load error
  gracefully ([1ff35df](https://github.com/gameguild-gg/gameguild/commit/1ff35df6aaac89eb73a32aae46d134c84f9106f6))
* **apps/web/tenants:** Update tenant management header
  text ([c325751](https://github.com/gameguild-gg/gameguild/commit/c32575102d5249f8120ca802e63e699c4ceae1ef))
* **apps/web:** Add missing comma after zod
  dependency ([4146f0b](https://github.com/gameguild-gg/gameguild/commit/4146f0b1592d63ca5bd580f7f55e39bf65802245))
* **apps/web:** Add project list to the creators
  dashboard. ([84ea4d3](https://github.com/gameguild-gg/gameguild/commit/84ea4d359340b085a26f8d9bc7f78cb5dad0b97c))
* **apps/web:** Add web vitals and feedback floating
  button ([a1d1650](https://github.com/gameguild-gg/gameguild/commit/a1d165069f42a664bdfc3ccfbd2309d01b87e18d))
* **apps/web:** clean up the
  chaos ([bb5aa47](https://github.com/gameguild-gg/gameguild/commit/bb5aa47b0addb2169660f795495605dfbaa34c31))
* **apps/web:** Handle undefined testing requests
  count ([4ef979c](https://github.com/gameguild-gg/gameguild/commit/4ef979c7d3138a787d4d5e6f4664846b0fdb4f39))
* **apps/web:** Replace testing request call with feedback
  call ([9462550](https://github.com/gameguild-gg/gameguild/commit/9462550f63cf7acf8610edf8182c2d94ca5f9138))
* **apps/web:** Set locale prefix: '
  as-needed' ([265cc34](https://github.com/gameguild-gg/gameguild/commit/265cc342fa9bc491501ea2265d9ccf65e5301ed3))
* **apps/web:** Update auth flow to use client-side signIn and direct fetch
  endpoints ([260a801](https://github.com/gameguild-gg/gameguild/commit/260a801f59e5a9e17f5620e12873e7c998faa399))
* **apps/web:** Update errors import paths and sign in
  URL ([24bfdaf](https://github.com/gameguild-gg/gameguild/commit/24bfdaf7845b5f48e57b48855afdd62ce2dc3ea6))
* **apps/web:** Use fallback arrays and remove unused feed
  component ([e789026](https://github.com/gameguild-gg/gameguild/commit/e7890262dbf8f9b969e54966d462c819410906f2))
* **apps/web:** Use optional chaining for testing requests length
  checks ([ce161a6](https://github.com/gameguild-gg/gameguild/commit/ce161a64d2521e36104c9cf50d0aee0801865a02))
* **apps/web:** Use session.api accessToken for program
  actions ([2745707](https://github.com/gameguild-gg/gameguild/commit/2745707ec367c82a43394087ccd69213e6bb6490))
* **apps/web:** Use session.api.accessToken for auth
  headers ([485fc73](https://github.com/gameguild-gg/gameguild/commit/485fc73cb35355c8d1437c53c7b84369d0c1ad75))
* **apps/web:** WIP create project
  form ([99f5d66](https://github.com/gameguild-gg/gameguild/commit/99f5d6670e28322ff6218d9b09743339faab5e22))
* **auth:** Fix early return in permission checking
  logic ([dc80726](https://github.com/gameguild-gg/gameguild/commit/dc807264ccb200287e505728e96c57771fca23cf))
* **auth:** fix refresh
  token ([a1840a6](https://github.com/gameguild-gg/gameguild/commit/a1840a6c3da5b81506abe51f2969a2f55f5e70c4))
* **auth:** user
  image ([df3d157](https://github.com/gameguild-gg/gameguild/commit/df3d1570c79bbc31bc37f10f788562542d6b75e3))
* build api via
  docker ([0288679](https://github.com/gameguild-gg/gameguild/commit/0288679f78104216bf600229d19ae1b30fbdd4b2))
* build web via
  docker ([171027d](https://github.com/gameguild-gg/gameguild/commit/171027dcbacddbe5f11acfa179310668bba5e11f))
* build with
  docker-compose ([501821d](https://github.com/gameguild-gg/gameguild/commit/501821d22ec404d7f0f9f6fe90108b1150ed730f))
* **cf:** fix cloudflare
  redirects ([9113b0c](https://github.com/gameguild-gg/gameguild/commit/9113b0cb2dc5a088911b46e0265ff49b813bced0))
* **cf:** fix
  redirect ([1c065bd](https://github.com/gameguild-gg/gameguild/commit/1c065bd86dcd5d44acdf543c6eeb14e3cd0ef525))
* **cf:** fix
  redirect ([c0248a4](https://github.com/gameguild-gg/gameguild/commit/c0248a4dfaed919998bb2e1dd565d1cc9296fc4c))
* **cf:** redirect auth
  issues ([0b3991c](https://github.com/gameguild-gg/gameguild/commit/0b3991cdc3f1f13db35a0c5961a629f1e5aa83bd))
* **cloudflare:** add debug info to detect env vars
  errors ([67467f6](https://github.com/gameguild-gg/gameguild/commit/67467f67cd5bb95dcbbe5bc899d83fdf44520ac4))
* **cms:** Update slugify cache key
  prefix ([dd1d5ed](https://github.com/gameguild-gg/gameguild/commit/dd1d5ed4b361d16d375f7cf21c4505e003ebb399))
* **courses:** courses python now reads data from
  markdown ([d5eb0b9](https://github.com/gameguild-gg/gameguild/commit/d5eb0b91c4128c5c6a20ea0fd0a90e4773f9c13a))
* **courses:** finish adding mock
  data ([6785bd7](https://github.com/gameguild-gg/gameguild/commit/6785bd780999309243831d1258d64d740089f343))
* **courses:** markdown code and quiz
  activity ([503f247](https://github.com/gameguild-gg/gameguild/commit/503f24707bd16e479da23d31123a7b291dbfd4f2))
* **courses:** minor issues on rendering courses, only first python content is
  there ([eadbb18](https://github.com/gameguild-gg/gameguild/commit/eadbb18844965d74cdc98f4ba2e1160feba17d18))
* **courses:** placeholder
  fixes ([d0f00c6](https://github.com/gameguild-gg/gameguild/commit/d0f00c6d4efb6a8e933ea1ef805921b040656a36))
* **dashboard:** Update testing feedback
  labels ([ec95b57](https://github.com/gameguild-gg/gameguild/commit/ec95b57e3c1e37e4da313564a86f5c55ef52abdf))
* **dbml:** Fix formatting in program database schema
  document ([04a3dd6](https://github.com/gameguild-gg/gameguild/commit/04a3dd6953ce9066ad208e101322d3befb5eebcc))
* **editor:** add pyodide to
  python-executor ([3d16cd7](https://github.com/gameguild-gg/gameguild/commit/3d16cd7181e0cd8c64eb6be4d63c858e7510d3a4))
* **editor:** Bold and Italic
  fix ([ce8d87d](https://github.com/gameguild-gg/gameguild/commit/ce8d87d5319793ef32d1c41c25ddda5b4f97b5f7))
* **editor:** color palette fix
  order ([3416185](https://github.com/gameguild-gg/gameguild/commit/341618518f2abb453f4e2e44618c17809f98e813))
* **editor:** color palette fix
  order ([96b118c](https://github.com/gameguild-gg/gameguild/commit/96b118cf01d7fb72ac7f0bbcd66a1e1ef664c0ef))
* **editor:** fix
  executors ([3e48cf8](https://github.com/gameguild-gg/gameguild/commit/3e48cf8e962178071248bc389d14afb2eebe6810))
* **editor:** font
  bold ([3639ee7](https://github.com/gameguild-gg/gameguild/commit/3639ee7aceac208e03f6504d8fd72ead8a470994))
* **editor:** general
  fixes ([ba2c86e](https://github.com/gameguild-gg/gameguild/commit/ba2c86e919ebca3ae90bb79ba6d41a62bdae8421))
* **editor:**
  imports ([ed051a2](https://github.com/gameguild-gg/gameguild/commit/ed051a263386df9c8d73f0c27334efc739899652))
* **editor:** return python
  executor ([8cc27da](https://github.com/gameguild-gg/gameguild/commit/8cc27da324f20c97ad2ba3938c0521a82764af7e))
* **editor:** several
  fixes ([ec4af81](https://github.com/gameguild-gg/gameguild/commit/ec4af8131090eef7f93c09245bd165340ba93e20))
* **editor:** Translate /page.tsx to en-us
  language ([1edd9ef](https://github.com/gameguild-gg/gameguild/commit/1edd9efc52b986e37b2fca7abcbe52ad8a9644f4))
* **editor:** Translate page to en-us and update track
  paths ([0b6ef47](https://github.com/gameguild-gg/gameguild/commit/0b6ef47b91dcab39f349af24c103c11831258586))
* env vars ([7e871dc](https://github.com/gameguild-gg/gameguild/commit/7e871dcb86a3f6b8ccc8213142f66b3fe09819c4))
* env vars ([17c28fb](https://github.com/gameguild-gg/gameguild/commit/17c28fb25a40696a9f091de5294cb313bc116b32))
* env vars ([1826d20](https://github.com/gameguild-gg/gameguild/commit/1826d2005f0b72c3c046e54251114df891c180e1))
* etc host
  patching ([ae6d5ac](https://github.com/gameguild-gg/gameguild/commit/ae6d5ac344c89e6b91e70b080c5512c829b90f6f))
* github actions
  fix ([7a65086](https://github.com/gameguild-gg/gameguild/commit/7a6508658448c5efd90cfa734e7bf28ed3be3c83))
* github actions
  fix ([6d08456](https://github.com/gameguild-gg/gameguild/commit/6d08456865e8b19127e898a7c060b6a32e73baaa))
* **graphql:** add graphql to
  contents ([ad01f10](https://github.com/gameguild-gg/gameguild/commit/ad01f10e8ac646886b55ebe927ce07d81f0ebe82))
* home by adding
  placeholders ([1507c8c](https://github.com/gameguild-gg/gameguild/commit/1507c8caac19227ac7dbeef10035ec1735a2d3e6))
* ignore lint errors on
  build ([e4c0857](https://github.com/gameguild-gg/gameguild/commit/e4c0857d8535448010a51842ad3fa533d08016d7))
* **jwt:** fix jwt compilation bug, it should have used the interface
  ref ([c21f542](https://github.com/gameguild-gg/gameguild/commit/c21f542dfde51d3a3d407d9517282733c97f30e9))
* login tab navigation now works properly, and slugify is properly
  implemented ([a0f7a9a](https://github.com/gameguild-gg/gameguild/commit/a0f7a9a2bb1e1ee8c6eccce266e64733bf114739))
* make domain name explicit and patch
  /etc/hosts ([6eee4c8](https://github.com/gameguild-gg/gameguild/commit/6eee4c8cb709611272d239d7d21bbb073c6339e0))
* **mermaid:** I am tired of trying to fix the svg scaling issue. It will stay like that for
  now ([8ca50b7](https://github.com/gameguild-gg/gameguild/commit/8ca50b7e3c1018b18d79a3f07edeb7b214a27b81))
* **migrations:** FIX
  MIGRATIONS ([75765d0](https://github.com/gameguild-gg/gameguild/commit/75765d068a1e0f1b4cad1d27215750940b985546))
* **permission:** Reorder SoftDelete and Delete enum values with clarifying
  comments ([e819e4d](https://github.com/gameguild-gg/gameguild/commit/e819e4d6cf6396cfaf9ad7e15b2f96d3e6fe142d))
* program dbml ([c60bf54](https://github.com/gameguild-gg/gameguild/commit/c60bf54c23d188bcc176fd4cfa853a3ed2d98e16))
* **program:** add graphql initial
  support ([9ab18a3](https://github.com/gameguild-gg/gameguild/commit/9ab18a38505ecb5851ec6182c98bff09a183f3e0))
* **program:** boilerplate for program
  management ([ac842e2](https://github.com/gameguild-gg/gameguild/commit/ac842e2ca6b1cf40849b034c85577aec72839cd8))
* **program:** fix migration issues for the
  program ([e955bc4](https://github.com/gameguild-gg/gameguild/commit/e955bc4156b93abff91221c99310bef1179a10d8))
* resolve CSS issues in footer
  component ([9ab4bc4](https://github.com/gameguild-gg/gameguild/commit/9ab4bc4e8d2f5dcd5416568ece34a4ae169601ef))
* resolve ESLint configuration error for API
  generation ([c79412b](https://github.com/gameguild-gg/gameguild/commit/c79412b46222f87fc74c61ff2c1cf907bda19ab6))
* server side auth
  redirect ([92c2ac5](https://github.com/gameguild-gg/gameguild/commit/92c2ac57f4b74a1171becf348794b700b7720084))
* **setup:** Fix npm dependencies version
  mismatches ([0c44ec3](https://github.com/gameguild-gg/gameguild/commit/0c44ec3086747ed08931aa9adc6c20c2faef465b))
* **setup:** format and fix dependency
  mess ([c266471](https://github.com/gameguild-gg/gameguild/commit/c266471a625e342be44f52a6052df5e43896f9a0))
* **signin:** improve email hint sign
  in ([99f6265](https://github.com/gameguild-gg/gameguild/commit/99f6265e90ee4e16d95d8424d3d5403ca55d5764))
* slug error ([b8d3a47](https://github.com/gameguild-gg/gameguild/commit/b8d3a47646ed0344cfcf4c8db24d7733abf313b8))
* **slugify:** now uses
  speakingurl ([15df033](https://github.com/gameguild-gg/gameguild/commit/15df0330bea883211d1baa1b4db83d9d8030ec78))
* **tenant:** Dispatch tenant payload and remove session token
  check ([3977f87](https://github.com/gameguild-gg/gameguild/commit/3977f87683227bdbfe03a14234e385f19afc69ae))
* **testing-lab:** Fix navigation anchor and update admin section
  title ([09e2842](https://github.com/gameguild-gg/gameguild/commit/09e2842f96741be025a0316bed0db49e96f85135))
* **tests/api:** Detach entity for fresh feedback
  retrieval ([99feb89](https://github.com/gameguild-gg/gameguild/commit/99feb89bb284c82ce02e8ea69d7eaf306c096e71))
* **tests/api:** Update assembly info, category and factory
  usage ([8bed12f](https://github.com/gameguild-gg/gameguild/commit/8bed12fa53fb1441a67fcdfc787bddf0814f8cd2))
* **tests/api:** Update product type and
  disposal ([ef8e5a2](https://github.com/gameguild-gg/gameguild/commit/ef8e5a2ce1c3d2d89b7223a9f00c842d9f0064b4))
* web next health
  check ([4f1c60b](https://github.com/gameguild-gg/gameguild/commit/4f1c60bf6e576cdd8a8b6fdcefa5e50d96a57fb0))
* **web:** add
  newrelic ([cd04727](https://github.com/gameguild-gg/gameguild/commit/cd047270ea4c2988341a4ad4337e5557b2345abe))
* **web:** add
  newrelic ([57c2007](https://github.com/gameguild-gg/gameguild/commit/57c2007eddefa0cc0e944ab1903ee64aa04f891f))
* **web:** adjust docker build on
  apps/web ([9910213](https://github.com/gameguild-gg/gameguild/commit/99102139c2e3f7925238f42bdda85dcb8cef83f7))
* **web:** css docker
  build ([5dc0e18](https://github.com/gameguild-gg/gameguild/commit/5dc0e18a28ab19989698e1a51c41ccde378e249f))
* **web:** fix next
  dockerfile ([eea13fc](https://github.com/gameguild-gg/gameguild/commit/eea13fc8f37d2d3797e82cbc09295154d339e134))
* **web:** Fix type errors and resolve dependency
  issues ([c1bb88b](https://github.com/gameguild-gg/gameguild/commit/c1bb88b2e0e43bf899f6577c13d5e9f07d3710a0))
* **web:** infinite
  redirect ([a165407](https://github.com/gameguild-gg/gameguild/commit/a16540782751f3df7f1017f5e7649179896d8b5b))
* **web:** type generation for
  api ([17e01b1](https://github.com/gameguild-gg/gameguild/commit/17e01b1397432b8998b6d0ab533328de1fc2aa44))
* **web:** type generation for
  api ([1575f1d](https://github.com/gameguild-gg/gameguild/commit/1575f1ddff74abeffb97d2e0a598b6d4e30fc138))
* **web:** wrong health
  route ([e0bb150](https://github.com/gameguild-gg/gameguild/commit/e0bb15092fd833893231ebbee180e1e3a541b643))

### Code Refactoring

* **api:** Remove chapter-related entities and
  references ([5204c51](https://github.com/gameguild-gg/gameguild/commit/5204c5148bc1778db4bfb0ccb9b0098984fb0f8e))

### Features

* **admin:** Add user management, roles and analytics
  features ([6c85f40](https://github.com/gameguild-gg/gameguild/commit/6c85f40c6b3ea1986383438691ebfe5efe5de548))
* **analytics/web-vitals:** Enable sendBeacon and fetch
  reporting ([c1100f1](https://github.com/gameguild-gg/gameguild/commit/c1100f14366a6d878213d2d57bf1e140c6be5b75))
* **api/payments:** Add comprehensive payment
  tests ([b52af26](https://github.com/gameguild-gg/gameguild/commit/b52af26472a7111578ddf2c6785a4826fab2c154))
* **api/products:** Add publish and unpublish commands with role
  validation ([054a78c](https://github.com/gameguild-gg/gameguild/commit/054a78c7017873acf452c1e2f509c9a1365fcf51))
* **api/swagger:** Add JWT authentication support to Swagger
  UI ([d7be8cd](https://github.com/gameguild-gg/gameguild/commit/d7be8cdbe4ca48cd65cb8a5d7e065850652e3845))
* **api/web:** Add attendance endpoints and reporting
  features ([2a46f5e](https://github.com/gameguild-gg/gameguild/commit/2a46f5e0e3ab5a7381c30cdb1e43a00750caaf09))
* **api/web:** Add GET /users/me endpoint and integrate with web
  app ([eace8e6](https://github.com/gameguild-gg/gameguild/commit/eace8e6d1ba3e0592a30e509f303a28b0831aaa6))
* **api/web:** Add Google token auth and course
  enhancements ([4ec7e3f](https://github.com/gameguild-gg/gameguild/commit/4ec7e3f7d23fb5d939400358c03b64e5e77f754e))
* **api/web:** Add UpdateAttendance DTO and explicit score type
  annotation ([ff858cd](https://github.com/gameguild-gg/gameguild/commit/ff858cd541eb79eec0b19912bbbd4546325344d5))
* **api/web:** Enhance tenant management with global admin
  support ([172f721](https://github.com/gameguild-gg/gameguild/commit/172f7216b4514f77a7ee913571ca3b3db59f57de))
* **api/web:** Update role template endpoints for id/name
  support ([daac91f](https://github.com/gameguild-gg/gameguild/commit/daac91fc9ae920fb7f52f1b27115f529b2c7ff21))
* **api:** Add NameIdentifier claim and improve session
  tests ([29b46d8](https://github.com/gameguild-gg/gameguild/commit/29b46d85c143d0ad080bcfd2f7c3a1b04e31b39e))
* **api:** Add optimized projects query & enforce DataLoader
  defaults ([6c859b9](https://github.com/gameguild-gg/gameguild/commit/6c859b9811ced06aa05aaaebafb86a54451cc315))
* **api:** Add TestingSession permissions and
  seeder ([8ccbfeb](https://github.com/gameguild-gg/gameguild/commit/8ccbfeb18e851180f84c8dba3775f375cd6bf5eb))
* **api:** Enhance logging and update GraphQL
  tests ([67df9fd](https://github.com/gameguild-gg/gameguild/commit/67df9fd8a435be131482ac8f411117912d6f9fad))
* **api:** Enhance product queries with
  filters ([56bfc70](https://github.com/gameguild-gg/gameguild/commit/56bfc703f9371df1e65a2258b2aebb3ec4aa4efc))
* **api:** Refine tenant permissions and auth
  tests ([89bb021](https://github.com/gameguild-gg/gameguild/commit/89bb0212701e06c23a9f175ee2053cddc01e6f56))
* **apps/api,apps/web:** Add courses pages, notifications, header, and migration
  updates ([46841fa](https://github.com/gameguild-gg/gameguild/commit/46841fa3bd3af5437b72b3a23e1ce5f73dd758d7))
* **apps/api,apps/web:** Integrate Google OAuth sign-in and profile
  creation ([ce7f427](https://github.com/gameguild-gg/gameguild/commit/ce7f427d76c87a002cd152e6648b57e42bb28d0b))
* **apps/api,apps/web:** Update testing workflow and sidebar
  integration ([f8d408a](https://github.com/gameguild-gg/gameguild/commit/f8d408a2f64489db31babcb244eb913c95396853))
* **apps/api/programs:** Add rating model and update enrollment
  status ([146ef7e](https://github.com/gameguild-gg/gameguild/commit/146ef7e1138afd54fb8dfdaeaf1fd8e437666759))
* **apps/api/tenants:** Implement CQRS endpoints and
  validators ([5cb192c](https://github.com/gameguild-gg/gameguild/commit/5cb192c82caebc6a644db609cb9aae29e1414fb9))
* **apps/api/tests:** Update GraphQL introspection
  config ([e1cd163](https://github.com/gameguild-gg/gameguild/commit/e1cd16387e702e3ee32aafae5c1e73e9db395a02))
* **apps/api:** Add achievement configurations and default DataLoader
  options ([50f2784](https://github.com/gameguild-gg/gameguild/commit/50f27847e4dbf5be466777f31eec71742fcb03c5))
* **apps/api:** Add ActivityGrade and ContentInteraction modules with permission
  inheritance ([b1815b1](https://github.com/gameguild-gg/gameguild/commit/b1815b10d7c60a9241e6384cd7e7f2d1b55876c3))
* **apps/api:** Add ActivityGrade and ContentInteraction modules with permission
  inheritance ([1ced743](https://github.com/gameguild-gg/gameguild/commit/1ced7439e9bc5278bd9dcf25525178dd86b7ac19))
* **apps/api:** Add auth, payment, and subscription
  modules ([160a8ee](https://github.com/gameguild-gg/gameguild/commit/160a8ee4364197ea9acd7c8bcf0398069574d935))
* **apps/api:** Add auto-enrollment and program CQRS
  endpoints ([cc86f22](https://github.com/gameguild-gg/gameguild/commit/cc86f22eabb0d3bde7d8752df948926f3b945ead))
* **apps/api:** Add bulk user profile commands and update migration
  names ([6c9dabe](https://github.com/gameguild-gg/gameguild/commit/6c9dabe1d1ef57acc34e12dbbfefa9d72ae7e6c9))
* **apps/api:** Add cancel payment command
  handler ([23ecab5](https://github.com/gameguild-gg/gameguild/commit/23ecab5dad1e8fecf845a01e7be7303cfce21de0))
* **apps/api:** Add comprehensive mock data
  seeding ([a56cfc9](https://github.com/gameguild-gg/gameguild/commit/a56cfc984f14a3f460267474c1d61ce0f498128d))
* **apps/api:** Add DbContextFactory support for GraphQL
  DataLoaders ([73cc000](https://github.com/gameguild-gg/gameguild/commit/73cc000cba16755046acd161a55ebdc0006b2ff8))
* **apps/api:** Add default CORS and conditional sample
  seeding ([2327a6d](https://github.com/gameguild-gg/gameguild/commit/2327a6dc2ef28395305c85efcd5ee428f9214717))
* **apps/api:** Add domain events, resource config and user profiles
  handlers ([92e0f17](https://github.com/gameguild-gg/gameguild/commit/92e0f177687f1ecde690292cc58ab28ce196d9a3))
* **apps/api:** Add email lookup and duplicate check in user
  service ([6c2e82b](https://github.com/gameguild-gg/gameguild/commit/6c2e82be7be7c6e9993330b55294a232068db6df))
* **apps/api:** Add feedback reporting and rating
  endpoints ([830d6e6](https://github.com/gameguild-gg/gameguild/commit/830d6e627af353fe221e0ece7020558ef00bef83))
* **apps/api:** Add GET user memberships endpoint and update
  tests ([faaf36a](https://github.com/gameguild-gg/gameguild/commit/faaf36a43f891e42270cf699936e3021233a5e88))
* **apps/api:** Add ManagerId field to
  sessions ([14e8a67](https://github.com/gameguild-gg/gameguild/commit/14e8a674d6423ed5bb71645b83aee50134efadbf))
* **apps/api:** Add payments, products, and programs
  modules ([14f40c1](https://github.com/gameguild-gg/gameguild/commit/14f40c18b698c55b07f5f6b3a285e77fa4624d10))
* **apps/api:** Add product handlers and update status
  enum ([b39078b](https://github.com/gameguild-gg/gameguild/commit/b39078b4e014e0422b66b3edf49c9935a16b680f))
* **apps/api:** Add product stats query and update deletion test
  threshold ([86eea2d](https://github.com/gameguild-gg/gameguild/commit/86eea2d570c62669ea0d2a5318f571db47470483))
* **apps/api:** Add program flow
  documentation ([60af355](https://github.com/gameguild-gg/gameguild/commit/60af3553943679b5f74741cb41d37f2731ae17a3))
* **apps/api:** Add tenant module commands, handlers, queries, and
  validators ([e345395](https://github.com/gameguild-gg/gameguild/commit/e3453958c66d71649ca0e291680ad90a0f9d6623))
* **apps/api:** Add testing lab URL and achievements
  schema ([0a2471f](https://github.com/gameguild-gg/gameguild/commit/0a2471fe2adc82d994a839a45f71f3fa473ecdd0))
* **apps/api:** Add TestingLab GraphQL types and test
  helper ([581b8b6](https://github.com/gameguild-gg/gameguild/commit/581b8b6ffb0cad2da1d84c979aca913fb1c79bd2))
* **apps/api:** Add user and tenant context middleware and
  docs ([74e5005](https://github.com/gameguild-gg/gameguild/commit/74e50052f0cbde0ce8d6caa24dc7523b2f5438bf))
* **apps/api:** Add user profiles, auth logging, program access checks and balance
  normalization ([9f722fe](https://github.com/gameguild-gg/gameguild/commit/9f722fe0ef951acc54ff5fdc7dbc63904d55727c))
* **apps/api:** Adopt CQRS pattern and add test module
  support ([1e536ce](https://github.com/gameguild-gg/gameguild/commit/1e536ce21b80e757659f8ec4b8d9ffd33f1509af))
* **apps/api:** Enhance DAC and module permission
  management ([4003284](https://github.com/gameguild-gg/gameguild/commit/4003284ca3bac197662a42a102beb5486bf93f6f))
* **apps/api:** Enhance payments module and add user
  context ([78a67b3](https://github.com/gameguild-gg/gameguild/commit/78a67b39322b8b9a44dd0de1c58f11adf16e1612))
* **apps/api:** Enhance user profiles with bulk ops and
  validators ([871fb0f](https://github.com/gameguild-gg/gameguild/commit/871fb0f641cabdcfcae639d0f733defb7973b50e))
* **apps/api:** Exclude soft-deleted products from global
  statistics ([ec0e585](https://github.com/gameguild-gg/gameguild/commit/ec0e585084715c7a8acbe215587cd50253439893))
* **apps/api:** Implement module-based permissions with testing lab
  support ([1bbb823](https://github.com/gameguild-gg/gameguild/commit/1bbb823b3af538725190989c74946cc02030c91a))
* **apps/api:** Integrate MediatR CQRS and add GraphQL data
  loaders ([b7a09dd](https://github.com/gameguild-gg/gameguild/commit/b7a09dd752a5e0a2d6b69dac514f521fcd1701b8))
* **apps/api:** Modernize authentication with CQRS & JWT
  support ([14cc316](https://github.com/gameguild-gg/gameguild/commit/14cc316bfd896a99eed721bb2085dc8fa66933b4))
* **apps/api:** Pass logger to GraphQL
  config ([a6fbf47](https://github.com/gameguild-gg/gameguild/commit/a6fbf47792a08387a6813c97e31a1adaf90c2fc2))
* **apps/api:** Refactor user GraphQL inputs and bulk
  handlers ([4356420](https://github.com/gameguild-gg/gameguild/commit/4356420ee237082c1290934b2dc40b9587594706))
* **apps/api:** Replace Error with ErrorMessage for localized error
  handling ([a6a65df](https://github.com/gameguild-gg/gameguild/commit/a6a65df9a68626286215e4fd1e09c941d411d55b))
* **apps/api:** Toggle mock seeding via env
  var ([76dcac6](https://github.com/gameguild-gg/gameguild/commit/76dcac6f2a87123cde45abf6f9bccf97499e0314))
* **apps/api:** Update payment flow and API
  endpoints ([12a9109](https://github.com/gameguild-gg/gameguild/commit/12a9109642a1ef0981eea241b9cc84f0df433306))
* **apps/api:** Update ratings schema and add new
  tables ([eda8402](https://github.com/gameguild-gg/gameguild/commit/eda8402ccb18ac48b1899c1c848753dfaa1abd33))
* **apps/api:** Update test fixture and endpoint
  routes ([ea860d2](https://github.com/gameguild-gg/gameguild/commit/ea860d2a68276bb45e1ab3441cd8fc1ae169e939))
* **apps/api:** Update to global project default
  permissions ([5b09eb2](https://github.com/gameguild-gg/gameguild/commit/5b09eb2f749a0f4acd4fc8764738d682043d4b2c))
* **apps/cms/auth:** Integrate CQRS, JWT and refactor
  namespaces ([acc1494](https://github.com/gameguild-gg/gameguild/commit/acc149485aed28ddff4493d2e1edc7ae08be32bf))
* **apps/cms:** Add bulk tenant permission and register HTTP context
  accessor ([44a6384](https://github.com/gameguild-gg/gameguild/commit/44a6384b5a346d82cafb428071d7cebc8540d700))
* **apps/cms:** Add comment, follower, rating, vote, and project module
  entities ([48f5546](https://github.com/gameguild-gg/gameguild/commit/48f5546137fd575457bcbbe0458e87c8ed125dbf))
* **apps/cms:** Add Jam, Reputation and UserProfile modules and refactor project and team
  models ([e91ceff](https://github.com/gameguild-gg/gameguild/commit/e91ceff434f38f1b9eb51e668a618a7cf70bd0bc))
* **apps/cms:** Add localization and permission
  entities ([9ae9020](https://github.com/gameguild-gg/gameguild/commit/9ae9020aa857b42580a3cbc5ca9e3bc31b26fde4))
* **apps/cms:** Add project module with permissions and GraphQL
  integration ([3316dcb](https://github.com/gameguild-gg/gameguild/commit/3316dcb3741fb3570e829703887ef3848796a80c))
* **apps/cms:** Add testing lab module
  models ([5f0d49c](https://github.com/gameguild-gg/gameguild/commit/5f0d49ca0d9b3075c0356a30416115108ffc7aec))
* **apps/cms:** Add TestingLab module with controllers, models, services and
  tests ([41cc19d](https://github.com/gameguild-gg/gameguild/commit/41cc19d1abfac3a0e1eb5f996a18167466ee8e56))
* **apps/cms:** Enhance project models, permissions, and
  tests ([bacabd7](https://github.com/gameguild-gg/gameguild/commit/bacabd7e8071a31a8a4f2a5ef364471907a41941))
* **apps/cms:** Implement BaseEntity and modular CMS
  architecture ([8cf4b33](https://github.com/gameguild-gg/gameguild/commit/8cf4b338c2d19bd85f36a9915b73687ddf8157c8))
* **apps/cms:** Integrate project permissions and auto-generate
  slugs ([1cb6ed1](https://github.com/gameguild-gg/gameguild/commit/1cb6ed1b192d5f80c768917dfe634739e69a4ac5))
* **apps/cms:** Integrate reputation system and remove legacy
  roles ([43c6836](https://github.com/gameguild-gg/gameguild/commit/43c683643bacf64fd78bd67e9c9153050d408ee8))
* **apps/cms:** Integrate TestingLab GraphQL API, DTOs, and
  tests ([90b4667](https://github.com/gameguild-gg/gameguild/commit/90b4667e596f72ee1f546d5e65dbf7fdcb58551b))
* **apps/cms:** Introduce three-layer permission
  system ([1107ad3](https://github.com/gameguild-gg/gameguild/commit/1107ad36102b57e636cf7969c2f2a569b11a9e9c))
* **apps/cms:** Update project schema with slug validation and new
  tables ([0ae0bc5](https://github.com/gameguild-gg/gameguild/commit/0ae0bc5c0500e63c25d32a1057abc82e14c270af))
* **apps/course-editor:** Add context, reducer, and server
  actions ([89fad5e](https://github.com/gameguild-gg/gameguild/commit/89fad5e4d2d41ab46655911a42f36d10c0ced899))
* **apps/dashboard/testing-lab:** Add sidebar with calendar and quick
  stats ([145784f](https://github.com/gameguild-gg/gameguild/commit/145784fa801702adf58c7f22abb55510493722b2))
* **apps/dashboard:** Add dashboard header and sidebar content
  components ([93746e2](https://github.com/gameguild-gg/gameguild/commit/93746e22a97379f0adc831b83fd10ca633da18e4))
* **apps/dashboard:** Remove redundant analytics pages and add refresh
  button ([85922ee](https://github.com/gameguild-gg/gameguild/commit/85922eec16c03fcca6ffd9bb8c217f8383fb42b7))
* **apps/testing-lab/management:** Add UI components for feedback, requests, and
  sessions ([965296a](https://github.com/gameguild-gg/gameguild/commit/965296a530f3857cc8d227711e03927954a4724d))
* **apps/web-users:** Add SDK actions for
  users ([3f80b9b](https://github.com/gameguild-gg/gameguild/commit/3f80b9b4011839b90aff3b0a8f37d7a49bc08c48))
* **apps/web/activity-grades:** Add API actions for activity
  grades ([d8939c2](https://github.com/gameguild-gg/gameguild/commit/d8939c2940f5e63ea1183a02fbcb78b23c81739c))
* **apps/web/auth:** Revamp auth config and token
  management ([ccabe92](https://github.com/gameguild-gg/gameguild/commit/ccabe92f073e520f176b581876c96b04d08be172))
* **apps/web/components/content/markdown:** Add markdown content
  component ([3527431](https://github.com/gameguild-gg/gameguild/commit/35274318e994fb1a89781555ba04950c43a476dd))
* **apps/web/cookies:** Add enhanced consent and preferences
  UI ([72d78a0](https://github.com/gameguild-gg/gameguild/commit/72d78a0ed824ca11eb92a39d1ffc5d066d3ad747))
* **apps/web/courses:** Add course list UI and filtering
  components ([7cb2f57](https://github.com/gameguild-gg/gameguild/commit/7cb2f576659e2cf667ee64bc6373988017e465bd))
* **apps/web/dashboard:** Add fallback pages for subroutes and remove test
  page ([5375e01](https://github.com/gameguild-gg/gameguild/commit/5375e012a29d5e0d5c021d15d9a9ed338fafa2b0))
* **apps/web/dashboard:** Add modular dashboard overview
  components ([cc332d6](https://github.com/gameguild-gg/gameguild/commit/cc332d60ddcd93e44016f5a17ca80c6044843733))
* **apps/web/dashboard:** Revamp course editor
  pages ([f13fabc](https://github.com/gameguild-gg/gameguild/commit/f13fabce5dbc881543a6ce855b16c02b240e2782))
* **apps/web/dashboard:** Revise error, loading and not found
  pages ([b9ba45e](https://github.com/gameguild-gg/gameguild/commit/b9ba45e2a7005ead80acccb23e3b179ac87b1f1f))
* **apps/web/dashboard:** Update courses page with new API and course list
  wrapper ([c3ecaf3](https://github.com/gameguild-gg/gameguild/commit/c3ecaf3386ee87c26d26d1eace3fb9547bad63ed))
* **apps/web/editor:** Implement enhanced editor page with autosave and IndexedDB
  support ([6f46cc9](https://github.com/gameguild-gg/gameguild/commit/6f46cc91796cbe1524dd6a1e10d1c0af9cc159c7))
* **apps/web/feed:** Add recent activity and cache refresh
  actions ([9ec8474](https://github.com/gameguild-gg/gameguild/commit/9ec847499d17aa2342b8ade98a7f038c5420953e))
* **apps/web/legal/licenses:** Implement dynamic license
  page ([a229aa8](https://github.com/gameguild-gg/gameguild/commit/a229aa82dfc47ce17e49aebc96fa154b10d95bd5))
* **apps/web/posts:** Replace legacy actions with authenticated API
  calls ([54d9e32](https://github.com/gameguild-gg/gameguild/commit/54d9e32668f0ed42c329747a57b98a8e199e2571))
* **apps/web/testing-lab:** Add session grid, row, and table
  views ([261d736](https://github.com/gameguild-gg/gameguild/commit/261d73628cfe0651008ac8bea6fe389ee7ea51ae))
* **apps/web/testing-lab:** Add testing feedback list
  component ([e93a01e](https://github.com/gameguild-gg/gameguild/commit/e93a01e314b5500775b707ff2c66ded1742cf10b))
* **apps/web/testing-lab:** Refactor components to use server
  data ([48652c1](https://github.com/gameguild-gg/gameguild/commit/48652c18265092b0a18b799082029fd709cd8e46))
* **apps/web/testing-lab:** Revamp session pages and UI
  components ([ba7f2b4](https://github.com/gameguild-gg/gameguild/commit/ba7f2b42f64eb9aba719885e5ee555a00a3d3e94))
* **apps/web/testing-lab:** Update UI components and add session
  form ([4bfcd57](https://github.com/gameguild-gg/gameguild/commit/4bfcd57f1b5fc319291c09897db82e23b764fef2))
* **apps/web/users:** Add dynamic user detail and permissions
  pages ([ca76f8b](https://github.com/gameguild-gg/gameguild/commit/ca76f8bbc52e8a7c93504ae6dee6651bf954d061))
* **apps/web:** Add activity grading, achievements, profiles, and users
  modules ([658b18f](https://github.com/gameguild-gg/gameguild/commit/658b18f1b8db68a11ece57f8480c28d1f1a85dfe))
* **apps/web:** Add activity submission, reporting and peer review
  features ([4882207](https://github.com/gameguild-gg/gameguild/commit/4882207f9807aeb2b9938f552861f9d72c3539fd))
* **apps/web:** Add animated floating icons to testing lab
  landing ([ff5c224](https://github.com/gameguild-gg/gameguild/commit/ff5c224cd38087a80d003c5fa781ce7e82f4fd17))
* **apps/web:** Add attendance tracker
  component ([36056e4](https://github.com/gameguild-gg/gameguild/commit/36056e4910ed97e3b40b76d01035e084e44764bd))
* **apps/web:** Add auth debug and refine session
  configuration ([15d0b58](https://github.com/gameguild-gg/gameguild/commit/15d0b588c751e2b61ded51c179b6d7c6f70c8268))
* **apps/web:** Add auth error page and track context
  exports ([23482ed](https://github.com/gameguild-gg/gameguild/commit/23482ed8c7c4d87fc1b5f90e1545b6cc2820c1cb))
* **apps/web:** Add authenticated client and generated API
  client ([e4655a4](https://github.com/gameguild-gg/gameguild/commit/e4655a42e7fca08433b5d22f99b0a00cce9994f7))
* **apps/web:** Add certificate generation and progress
  tracker ([9eb03aa](https://github.com/gameguild-gg/gameguild/commit/9eb03aa61db10fac3a4181b11ee68845d1bb18b5))
* **apps/web:** Add cn utility for merging Tailwind
  classes ([9e1831e](https://github.com/gameguild-gg/gameguild/commit/9e1831eb37e44660e60ba8a6d061346b38c1397b))
* **apps/web:** Add community feed and improve sessions
  handling ([e02aff0](https://github.com/gameguild-gg/gameguild/commit/e02aff05a80323f13e2b9e41faa5ab8b347c3b44))
* **apps/web:** Add comprehensive community feed
  system ([2def27d](https://github.com/gameguild-gg/gameguild/commit/2def27da3928aed5fdd72bb3207057865657b7c4))
* **apps/web:** Add comprehensive component showcase and
  examples ([ade2720](https://github.com/gameguild-gg/gameguild/commit/ade272075cce2a37f6ad42ea489e15acb19fd2e0))
* **apps/web:** Add comprehensive content interaction
  endpoints ([7a0b82f](https://github.com/gameguild-gg/gameguild/commit/7a0b82f63a4153a55981cb2d292e2c3b3d69d03b))
* **apps/web:** Add comprehensive server actions across
  modules ([fa02cba](https://github.com/gameguild-gg/gameguild/commit/fa02cbaf28b5b89d8cba9f86c92aef0b5d01d8d2))
* **apps/web:** Add content interaction SDK
  actions ([5c07ae9](https://github.com/gameguild-gg/gameguild/commit/5c07ae97e757005bb9fea59690be411ebe3cb48c))
* **apps/web:** Add course catalog page and filtering
  components ([ced1c0a](https://github.com/gameguild-gg/gameguild/commit/ced1c0a2661490205b78a01be6bae46b7a607231))
* **apps/web:** Add course detail, location selector and create form
  components ([3b41c50](https://github.com/gameguild-gg/gameguild/commit/3b41c505adfd05b354403704acd4451f75e64f3a))
* **apps/web:** Add courses actions, context, and track
  hook ([48873b4](https://github.com/gameguild-gg/gameguild/commit/48873b4edbb9444daef699da23269859c99116c7))
* **apps/web:** Add courses and tracks layouts, error and loading
  states ([d198d91](https://github.com/gameguild-gg/gameguild/commit/d198d9147969e6fdd0e38533535a126182799fcd))
* **apps/web:** Add create project form and move version submission
  form ([0eb3d4e](https://github.com/gameguild-gg/gameguild/commit/0eb3d4e7c30d173a11806d16d8b3e17c90045419))
* **apps/web:** Add credentials actions for CRUD
  operations ([6411f65](https://github.com/gameguild-gg/gameguild/commit/6411f659933583516932155c44a0ca3746c0ae1c))
* **apps/web:** Add dashboard course editor
  pages ([35d01ce](https://github.com/gameguild-gg/gameguild/commit/35d01ce656c4a9fb7d6094653110505c619f6f40))
* **apps/web:** Add default modal and support error
  children ([6d9a1b4](https://github.com/gameguild-gg/gameguild/commit/6d9a1b4301e551cf721ad6aa659161a43354bf4b))
* **apps/web:** Add enhanced E2E tests for frontend-API
  integration ([bc4a9c8](https://github.com/gameguild-gg/gameguild/commit/bc4a9c8e86b3a9553c21e540fdea838ba9dfeacf))
* **apps/web:** Add enhanced permission and notification
  types ([d2382ca](https://github.com/gameguild-gg/gameguild/commit/d2382caeb8dd9ba7224ed94ef9e848f1710aea93))
* **apps/web:** Add enrollment and course detail
  pages ([f98187c](https://github.com/gameguild-gg/gameguild/commit/f98187c2fae870adb9db15e6e0b9ba2940f0a4da))
* **apps/web:** Add GitHub license content retrieval
  function ([60b78d6](https://github.com/gameguild-gg/gameguild/commit/60b78d6c09130deab0ebb444cc4d8b1a83646e22))
* **apps/web:** Add GraphQL, feed & tenant
  tabs ([cc80d95](https://github.com/gameguild-gg/gameguild/commit/cc80d959f0d357653c693ede27388360871510be))
* **apps/web:** Add health and database status
  endpoints ([aa64591](https://github.com/gameguild-gg/gameguild/commit/aa645917317c05f936583c66b2191a7580148924))
* **apps/web:** Add health check
  endpoint ([5d87723](https://github.com/gameguild-gg/gameguild/commit/5d8772388eafa2e17d2a7e6d4d873a2dd728a646))
* **apps/web:** Add i18n module with en-US
  messages ([629b7a0](https://github.com/gameguild-gg/gameguild/commit/629b7a0847c9f73b52e57d00b9ce44099fbe0f26))
* **apps/web:** Add instrumentation client for
  monitoring ([57f8ec3](https://github.com/gameguild-gg/gameguild/commit/57f8ec36f67da5709874fc9495865082a05ae39a))
* **apps/web:** Add join session form
  component ([0ed37db](https://github.com/gameguild-gg/gameguild/commit/0ed37dbe7beb35d80317c3d255fdb052dac8b284))
* **apps/web:** Add legal and social media links
  components ([cbc0156](https://github.com/gameguild-gg/gameguild/commit/cbc01568e59f68a75602bb0593dad14f87bbbf6b))
* **apps/web:** Add Lexical dependencies and update Monaco
  editor ([021c838](https://github.com/gameguild-gg/gameguild/commit/021c838cf67e61f879897a3e3ddb54ae085eb2eb))
* **apps/web:** Add local auth support and update
  endpoints ([364b45c](https://github.com/gameguild-gg/gameguild/commit/364b45c489792ac31227d1d48064f36d4bf64b22))
* **apps/web:** Add manifest, robots, and sitemap
  endpoints ([507d7b2](https://github.com/gameguild-gg/gameguild/commit/507d7b291d0a6596d33fd05ba3d06fbb79055433))
* **apps/web:** Add MarkdownContent and refactor feedback
  UI ([6db2d98](https://github.com/gameguild-gg/gameguild/commit/6db2d98521aba5a3ae906e416e4bf30c92684e74))
* **apps/web:** Add metadata debug log and update layout
  imports ([f8dd6fc](https://github.com/gameguild-gg/gameguild/commit/f8dd6fc6dfcc65a8d7e9bae88feaaabf20a778d7))
* **apps/web:** Add modular course editor
  pages ([f86fed2](https://github.com/gameguild-gg/gameguild/commit/f86fed2c5056e8091e695220743651e374ef913a))
* **apps/web:** Add new editor and course
  sync ([8012576](https://github.com/gameguild-gg/gameguild/commit/8012576d7c8d6b281bc29e28558ed09f508c69cc))
* **apps/web:** Add new subscription actions with API
  integration ([22b2791](https://github.com/gameguild-gg/gameguild/commit/22b2791de4b6fc80b0a668f7b7fc57cb56853a77))
* **apps/web:** Add not-found page and update dashboard
  imports ([d6635bb](https://github.com/gameguild-gg/gameguild/commit/d6635bb0a16039b9b96f282d062772aff2bb1c2d))
* **apps/web:** Add onRequestError logging
  function ([8349362](https://github.com/gameguild-gg/gameguild/commit/83493620dae72e5e84679c3d324a0923ed105152))
* **apps/web:** Add payments SDK action
  endpoints ([657ce51](https://github.com/gameguild-gg/gameguild/commit/657ce51c3ebf64f68a6b26931cda88c518a729e0))
* **apps/web:** Add posts actions
  endpoints ([e664479](https://github.com/gameguild-gg/gameguild/commit/e664479de2e244085a2121caf011bcdd91dcccef))
* **apps/web:** Add program management
  suite ([d23b6ac](https://github.com/gameguild-gg/gameguild/commit/d23b6acf862e7671854553d41f81b0449e0f4758))
* **apps/web:** Add programs SDK
  actions ([d892b2c](https://github.com/gameguild-gg/gameguild/commit/d892b2cdfcf4e30b1a4f536e3c02d4393221ef52))
* **apps/web:** Add project and testing lab pages with server
  actions ([80a83c7](https://github.com/gameguild-gg/gameguild/commit/80a83c7b72a0144ce1c1b4fa6f30b99c3e1cd57e))
* **apps/web:** Add refresh token debug
  info ([05cf649](https://github.com/gameguild-gg/gameguild/commit/05cf64999ed4935c91d4dd741457addf80e40071))
* **apps/web:** Add roadmap, stats, and updated contribution
  UI ([4cac003](https://github.com/gameguild-gg/gameguild/commit/4cac003cbb3a29dd80b9b37193d113ad6433375d))
* **apps/web:** Add slug generation and update course
  links ([aae436b](https://github.com/gameguild-gg/gameguild/commit/aae436b1bdb8d418890b2f6511436d30c6553dcf))
* **apps/web:** Add sticky headers and collapsible sections to course
  pages ([07a51df](https://github.com/gameguild-gg/gameguild/commit/07a51dff75af9f0642dffde8d94ab4118e1378bb))
* **apps/web:** Add support to
  themes ([78e9b7a](https://github.com/gameguild-gg/gameguild/commit/78e9b7a5fc03e4b9f700728e88dccae626b05009))
* **apps/web:** Add system and database health
  actions ([7219cd7](https://github.com/gameguild-gg/gameguild/commit/7219cd7a169418c8ecc3daef9a051a029406a424))
* **apps/web:** Add tenant domains actions and update token
  usage ([44c5e65](https://github.com/gameguild-gg/gameguild/commit/44c5e659122efe9a525ae5b94d80d3146833ff55))
* **apps/web:** Add testing feedback list UI and update
  dependencies ([b515fb6](https://github.com/gameguild-gg/gameguild/commit/b515fb60c3158f369cee0bab0c9070567793ce0d))
* **apps/web:** Add testing feedback SDK
  actions ([686df2e](https://github.com/gameguild-gg/gameguild/commit/686df2e9a0dbe28244658f88bcbcf2438d8157c7))
* **apps/web:** Add testing lab auth layout and error/loading
  components ([e533028](https://github.com/gameguild-gg/gameguild/commit/e5330280ab54369890977c3ea8c0b026fa6ccf0b))
* **apps/web:** Add Testing Lab
  functionality ([bb7a8e2](https://github.com/gameguild-gg/gameguild/commit/bb7a8e22fea342dae48afe92656d1493de0d28a7))
* **apps/web:** Add testing lab page with
  overview ([9033f43](https://github.com/gameguild-gg/gameguild/commit/9033f43f3f4b5783f659e743f9d9dc37a134b354))
* **apps/web:** Add testing lab pages and feed server
  actions ([f8d4476](https://github.com/gameguild-gg/gameguild/commit/f8d44767dfa7498989334d48b3a96379388b01f6))
* **apps/web:** Add testing lab pages and progress
  tracker ([8be6661](https://github.com/gameguild-gg/gameguild/commit/8be66618434ac9cce56372c63b44ad4dee07103f))
* **apps/web:** Add testing lab requests management
  UI ([49c1aaf](https://github.com/gameguild-gg/gameguild/commit/49c1aaff13b3817cebd64399b6ca29a7b7828fdf))
* **apps/web:** Add testing lab roles actions and new dashboard
  header ([206cfa1](https://github.com/gameguild-gg/gameguild/commit/206cfa1b9f0b412e41d911ad51fe10897b54d60c))
* **apps/web:** Add testing lab roles and user role
  endpoints ([65e294e](https://github.com/gameguild-gg/gameguild/commit/65e294e6a193b7d11744019fd750263cef172792))
* **apps/web:** Add testing lab sessions UI
  components ([b87ab09](https://github.com/gameguild-gg/gameguild/commit/b87ab0997ad7400c031fd4baec0a161e4df98b3b))
* **apps/web:** Add testing lab settings
  page ([4fa8ceb](https://github.com/gameguild-gg/gameguild/commit/4fa8ceb4f92e2cde3a8c568e7188eeb9ed179521))
* **apps/web:** Add testing lab, courses, and peer review
  features ([f3c187d](https://github.com/gameguild-gg/gameguild/commit/f3c187d3ceeaf3fcd2c94d1c8019aa25678bbd8f))
* **apps/web:** Add Toaster to
  layout ([26cd534](https://github.com/gameguild-gg/gameguild/commit/26cd534c2cf9a1e20551b0d493998d3d74da97ab))
* **apps/web:** Add user form
  component ([5e1fc18](https://github.com/gameguild-gg/gameguild/commit/5e1fc186026ef5f7b880689c479ea7c0f2acb74b))
* **apps/web:** Add user profile and not found
  pages ([5e31334](https://github.com/gameguild-gg/gameguild/commit/5e313348821713da073dd47da95548bd3ed9143d))
* **apps/web:** Always fetch all tenants and add default tenant
  fallback ([4deb02b](https://github.com/gameguild-gg/gameguild/commit/4deb02b4c1fce0d2fed6b446c5ad62b31fcbf6d1))
* **apps/web:** Create testing requests
  dashboard ([1b95f70](https://github.com/gameguild-gg/gameguild/commit/1b95f706f0c7e052f2a1e838de2bef0fd09f25af))
* **apps/web:** Enable active link highlighting using current
  pathname ([7fcb6a9](https://github.com/gameguild-gg/gameguild/commit/7fcb6a9ffec02a70d7874d1f40c3332f13da682f))
* **apps/web:** Enforce key existence and add value
  extractor ([7e04131](https://github.com/gameguild-gg/gameguild/commit/7e0413103a5e1618582b1473c89865009b116a98))
* **apps/web:** Enhance auth callbacks for profile and tenant
  updates ([09bab5f](https://github.com/gameguild-gg/gameguild/commit/09bab5f98be0bfa34f8fa211997c7e672763c094))
* **apps/web:** Enhance auth token refresh and add achievements
  actions ([cea92ef](https://github.com/gameguild-gg/gameguild/commit/cea92efa4f4a2cbaee4cd4154890c73bf56c3bae))
* **apps/web:** Enhance authentication with tenant and session
  management ([6d91d87](https://github.com/gameguild-gg/gameguild/commit/6d91d875a6864ef1131f8f8f693b9f384b874394))
* **apps/web:** Enhance contributors UI and stats
  layout ([afcf2b9](https://github.com/gameguild-gg/gameguild/commit/afcf2b9b4c5cbe8639b62ad49898539c4463224c))
* **apps/web:** Enhance contributors UI with GitHub
  integration ([6a7e5c7](https://github.com/gameguild-gg/gameguild/commit/6a7e5c76663eaf024492dcc1ebfa8d7d40b321a3))
* **apps/web:** Enhance header and notifications UI
  styles ([32d4d11](https://github.com/gameguild-gg/gameguild/commit/32d4d11a449a345fdce50d2881a26f4a5d1df744))
* **apps/web:** Enhance intl and tenant
  middleware ([a0c6506](https://github.com/gameguild-gg/gameguild/commit/a0c65062e9b35d2a2d1593a65aa1db0bb4aa9470))
* **apps/web:** Enhance period selector and sessions UI
  design ([1defe6e](https://github.com/gameguild-gg/gameguild/commit/1defe6e9588ea0ecb2f020a67eae482233a684f2))
* **apps/web:** Enhance role editing with aggregated
  permissions ([6ef7b7b](https://github.com/gameguild-gg/gameguild/commit/6ef7b7b5a14128d17b10002fd40bfecc862c8b68))
* **apps/web:** Enhance session UI and
  logging ([ab2cf79](https://github.com/gameguild-gg/gameguild/commit/ab2cf79765868d5372b00821cdbd341b6ef57da2))
* **apps/web:** Enhance testing feedback list with filtering and UI
  components ([6eaacda](https://github.com/gameguild-gg/gameguild/commit/6eaacda59b7851960702dd0169346d15ffa66ffe))
* **apps/web:** Enhance testing lab sessions with filters and view
  modes ([d7963f3](https://github.com/gameguild-gg/gameguild/commit/d7963f3b9ab6cbe8b7f783b09f8a2783a7e3aa4d))
* **apps/web:** Enhance UI styling with gradients and
  tooltips ([9728df0](https://github.com/gameguild-gg/gameguild/commit/9728df0ff3094499b684f71218ce299cd465f6ea))
* **apps/web:** Expand role permissions mapping
  conversion ([0e927c4](https://github.com/gameguild-gg/gameguild/commit/0e927c4b172b9f9a71730d5affea1902c334308e))
* **apps/web:** Implement achievements API
  actions ([e37aa3c](https://github.com/gameguild-gg/gameguild/commit/e37aa3c8bfef267e337d0c6a4b6fafdbb92a4549))
* **apps/web:** Implement course content viewer and learning
  modules ([1fc56f5](https://github.com/gameguild-gg/gameguild/commit/1fc56f5ef88a597cc6f031026cd9bd0d4e7bd52b))
* **apps/web:** Implement session-based testing lab
  pages ([2d69106](https://github.com/gameguild-gg/gameguild/commit/2d691066da1455b16c63844ffb247557ddc8c86f))
* **apps/web:** Implement testing lab
  dashboards ([3ab421a](https://github.com/gameguild-gg/gameguild/commit/3ab421ae0d6aa379790fc4df5672e9e61e2665e7))
* **apps/web:** Implement testing session details
  page ([19f9e14](https://github.com/gameguild-gg/gameguild/commit/19f9e14714bb6d8e66a732e6acac81540cca304e))
* **apps/web:** Implement unified course editor and enhanced
  listings ([0654ec6](https://github.com/gameguild-gg/gameguild/commit/0654ec64e4e26352c5ab0f2c3935943332a090ea))
* **apps/web:** Import sidebar
  components ([a8a9329](https://github.com/gameguild-gg/gameguild/commit/a8a932969788a64d955f366b8a6adeebeb879402))
* **apps/web:** Improve testing lab location
  management ([3459cb2](https://github.com/gameguild-gg/gameguild/commit/3459cb2501c757c407c6af58fc37d91acf00bc4c))
* **apps/web:** Improve testing lab UI and session
  sorting ([7f6e615](https://github.com/gameguild-gg/gameguild/commit/7f6e61595457444b4ecd83a48f47fbdbc3008847))
* **apps/web:** Improve testing sessions mapping and dynamic
  refresh ([a296fdb](https://github.com/gameguild-gg/gameguild/commit/a296fdbab5be0f297eef85eb916e85292c40b27b))
* **apps/web:** Integrate API for testing lab
  components ([0034c37](https://github.com/gameguild-gg/gameguild/commit/0034c3777f6e0a560dcec11f2c143b848efe9f25))
* **apps/web:** Integrate auth for user
  endpoints ([1449152](https://github.com/gameguild-gg/gameguild/commit/14491521f0e48d59665b43c73ae67278d603aeba))
* **apps/web:** Integrate backend notifications and reorganize dashboard
  actions ([d3e9d58](https://github.com/gameguild-gg/gameguild/commit/d3e9d58b050861bcb3c8506f455c3183e37adcad))
* **apps/web:** Integrate dashboard layout in feedback
  page ([8462a55](https://github.com/gameguild-gg/gameguild/commit/8462a5548cedcacb146af36f87d47897b6d185b2))
* **apps/web:** Integrate GitHub data in stats and update navigation
  links ([17a2e69](https://github.com/gameguild-gg/gameguild/commit/17a2e6985a738352f118c1c6967f60e63e246a6d))
* **apps/web:** Integrate session
  authentication ([3e537e1](https://github.com/gameguild-gg/gameguild/commit/3e537e184962a717bdcfaf7d6e10a3e1e6c716a6))
* **apps/web:** Migrate editor page and update UI
  imports ([4a659aa](https://github.com/gameguild-gg/gameguild/commit/4a659aa18fa5988a3fe2b53cd3ee9f0e41dac92b))
* **apps/web:** Migrate tenant management to client auth
  actions ([dd36412](https://github.com/gameguild-gg/gameguild/commit/dd36412b69253a4f3ca7e5dc648ed399b590419f))
* **apps/web:** Modularize code structure and update
  imports ([69ffa43](https://github.com/gameguild-gg/gameguild/commit/69ffa43cfd08a71b4c2c75a1d6f701e0afcf9b8d))
* **apps/web:** Normalize session status display in detail
  component ([c052c68](https://github.com/gameguild-gg/gameguild/commit/c052c683c707ec0ac393f2e1600a01e0df0f8a7c))
* **apps/web:** Refactor footer into modular
  components ([0401f7a](https://github.com/gameguild-gg/gameguild/commit/0401f7a39f5d20bc7fcd10c2ad74b10243b293b8))
* **apps/web:** Refactor GitHub integration and project stats
  enhancements ([d7932cf](https://github.com/gameguild-gg/gameguild/commit/d7932cfdb50db0704d30f7a8476c12f527675c32))
* **apps/web:** Refactor project actions with centralized auth
  client ([67f3a35](https://github.com/gameguild-gg/gameguild/commit/67f3a35160ee44e646c058189a08842a606db801))
* **apps/web:** Refactor testing lab overview and improve auth
  config ([0ff755c](https://github.com/gameguild-gg/gameguild/commit/0ff755c95de88f8234d10f41ecaca961668d2c60))
* **apps/web:** Refactor testing lab pages to use dashboard
  layout ([2a45966](https://github.com/gameguild-gg/gameguild/commit/2a45966e4dd77a6b4110b6fe90eba32f02be9c8c))
* **apps/web:** Refine Testing Lab UI
  Components ([342b272](https://github.com/gameguild-gg/gameguild/commit/342b2722c620f4fb079a084f8a6740ff0f250a79))
* **apps/web:** Replace form action with custom submit
  handler ([ca9cbef](https://github.com/gameguild-gg/gameguild/commit/ca9cbef5c6c60688854ea384b0ff133bc497792a))
* **apps/web:** Replace image banner with CSS gradient and drop
  shadow ([d7411a4](https://github.com/gameguild-gg/gameguild/commit/d7411a4f30b7f909d390a637deb81c1339c22dec))
* **apps/web:** Replace legacy project list with enhanced
  overview ([ed2bdce](https://github.com/gameguild-gg/gameguild/commit/ed2bdce2f342ba450bca14d19cbd68ecb2e8f9dc))
* **apps/web:** Restructure course catalog and add landing
  pages ([51598bd](https://github.com/gameguild-gg/gameguild/commit/51598bde414a2cacd2a0fd5d133c049bdb075a1d))
* **apps/web:** Revamp auth and dashboard
  layouts ([09354dc](https://github.com/gameguild-gg/gameguild/commit/09354dc3ce504cadb19a6c1d79fd436d0a0afa0f))
* **apps/web:** Revamp contributors UI layout and
  styling ([8fc0ae4](https://github.com/gameguild-gg/gameguild/commit/8fc0ae4ec0d4855e8a678657551a4536b0c2268c))
* **apps/web:** Revamp tenant management and
  switching ([86629d1](https://github.com/gameguild-gg/gameguild/commit/86629d10b2bafc6213f735f47801a2cd8491d377))
* **apps/web:** Revamp testing lab pages and
  filters ([9295f7a](https://github.com/gameguild-gg/gameguild/commit/9295f7ae5f4009ec4410ff6ca9b010a3d8d50ce1))
* **apps/web:** Revamp testing lab pages and
  types ([2c4b943](https://github.com/gameguild-gg/gameguild/commit/2c4b94365b0b4f55d82a78c97ae265f97de652bc))
* **apps/web:** Send Web Vitals to Goggle Analytics as custom
  metrics ([00dd2c0](https://github.com/gameguild-gg/gameguild/commit/00dd2c0049faba91b1460340cdae5f98d53c0511))
* **apps/web:** Update auth errors, branding and community
  pages ([077da0b](https://github.com/gameguild-gg/gameguild/commit/077da0b115df765fc1ca20f40c766a9f0466c53b))
* **apps/web:** Update auth redirects, i18n routing and middleware
  config ([5879a4e](https://github.com/gameguild-gg/gameguild/commit/5879a4ed6daeb1c46d158f49e4f0425b80db0440))
* **apps/web:** Update contributors header and add contribution
  guide ([c65d14c](https://github.com/gameguild-gg/gameguild/commit/c65d14cf810753d97faa8eb02d6a46a93fd6177e))
* **apps/web:** Update course actions and add course editor
  context ([d6e12e4](https://github.com/gameguild-gg/gameguild/commit/d6e12e4c3a4425556872c5f9faa4e65d88076e98))
* **apps/web:** Update dashboard layout with auth and
  styling ([89c1d1f](https://github.com/gameguild-gg/gameguild/commit/89c1d1fbacf3a0b64c0ebea26f0b32ea4a8f5c35))
* **apps/web:** Update testing lab feedback page with dashboard
  layout ([da4336e](https://github.com/gameguild-gg/gameguild/commit/da4336e2b570c03147b6268dbff8fd836a1061a3))
* **apps/web:** Update testing lab pages with
  placeholders ([ef4ddcb](https://github.com/gameguild-gg/gameguild/commit/ef4ddcb9f771f2e361c8e0f3219f9a77117f3185))
* **apps/web:** Update token refresh logic for update
  events ([58839d2](https://github.com/gameguild-gg/gameguild/commit/58839d26f9888e946b54b62ff8d7f55d55d5b5f8))
* **apps/web:** Update user profile to use session and add tenant
  switcher ([3a16d9e](https://github.com/gameguild-gg/gameguild/commit/3a16d9eb661697775e0a04375c4e950d36045623))
* **apps/web:** Use slugs for test session
  routes ([756d7e9](https://github.com/gameguild-gg/gameguild/commit/756d7e90d51dbf77b8bbe65a2d58c8ed52da160e))
* **apps/web:** Wrap testing pages in dashboard
  layout ([5bd3ccc](https://github.com/gameguild-gg/gameguild/commit/5bd3ccc4d6842fdaa07c91ce5dfa41ec06ffc91b))
* **apps:** Add DAC auth and tenant mutations with enhanced error
  handling ([97f19a8](https://github.com/gameguild-gg/gameguild/commit/97f19a89cbc4babe4eeaead409ac0b7aabea8963))
* **apps:** Add DB seeding, refine auth and Next.js
  migration ([cd35a37](https://github.com/gameguild-gg/gameguild/commit/cd35a37c488d77f18a12b16c7f33e45468428f8f))
* **apps:** Add learning tracks and course
  dashboards ([57ef341](https://github.com/gameguild-gg/gameguild/commit/57ef3415a579bdc347574ed79b3948d6a8de41d6))
* **apps:** Add project version endpoints and UI
  improvements ([9eee0cd](https://github.com/gameguild-gg/gameguild/commit/9eee0cdbfff523cc8a2763f59a141cb94134ab14))
* **apps:** Add tenant management, admin login and super admin
  seeding ([7950ee3](https://github.com/gameguild-gg/gameguild/commit/7950ee3c9840b173a50139fbd6cc0419f1313f25))
* **apps:** Enhance logging, permissions and UI theme
  toggle ([d930ecd](https://github.com/gameguild-gg/gameguild/commit/d930ecdd115c11e01b5c9757ee22a879aea22e59))
* **apps:** Enhance token expiry, tenant context, and
  UI ([10cd356](https://github.com/gameguild-gg/gameguild/commit/10cd3565e856bc6910e5e49511bd1fa26ce11fef))
* **auth/integration:** Implement NextAuth authentication and tenant
  integration ([1523555](https://github.com/gameguild-gg/gameguild/commit/1523555d9e0cb0c1e729a5319779952f058e3110))
* **auth:** Add permission-based authorization
  attributes ([bc9ef86](https://github.com/gameguild-gg/gameguild/commit/bc9ef8660d7e22c69d0593e427a5ecd9fd20d823))
* **auth:** Add tenant support to auth and JWT
  tokens ([e5dbeac](https://github.com/gameguild-gg/gameguild/commit/e5dbeacfac644832dcd8b23a03828d8473aa054e))
* **auth:** Convert auth actions and JWT utilities to
  async ([ecf32e3](https://github.com/gameguild-gg/gameguild/commit/ecf32e3c0a0b2202f348b53df1d7d25bf93c8af9))
* **auth:** Enhance token refresh logging and
  debugging ([164d562](https://github.com/gameguild-gg/gameguild/commit/164d562bbec8d34e6aa32c8639b4f5c9959aea90))
* **auth:** Improve authentication and token refresh
  integration ([eda7e4e](https://github.com/gameguild-gg/gameguild/commit/eda7e4e72fd0d5d8236bb58fe642300684156e25))
* **auth:** Improve refresh token rotation, logging and DB
  schema ([0a7576c](https://github.com/gameguild-gg/gameguild/commit/0a7576ce21148648d7fd9aca49b51e36dba0d0c2))
* **auth:** Improve token refresh and auth hooks
  integration ([63bd7eb](https://github.com/gameguild-gg/gameguild/commit/63bd7eb7faa0589449a6742d5981efff7e783d03))
* **auth:** Set 5 min JWT clock skew and add JWT
  utils ([c31815e](https://github.com/gameguild-gg/gameguild/commit/c31815ee7bba4c9a2c4dd72f6b66288892584f61))
* **auth:** Use server actions for backend
  authentication ([d8243fe](https://github.com/gameguild-gg/gameguild/commit/d8243fe2f9d9a5e2162bc017d028e837bfdcc809))
* **cms/graphql:** Add GraphQL type annotations to permission
  entities ([c26bcfa](https://github.com/gameguild-gg/gameguild/commit/c26bcfab4f305353bd1e8f4297a6f316376376d2))
* **cms/tenant:** Enhance auth, DTO mapping, and auto-assign
  endpoints ([737502a](https://github.com/gameguild-gg/gameguild/commit/737502a2e5d46a8410285a44dae06dd83f6c6c91))
* **cms:** Add comment permissions system with DAC layer 3
  support ([b13f635](https://github.com/gameguild-gg/gameguild/commit/b13f635be800b51937b294f6eb75d2868dfa1da3))
* **cms:** Add ProductPermission model with full DAC
  integration ([9af1a87](https://github.com/gameguild-gg/gameguild/commit/9af1a87d6eaf655a9540a516a3f3d492cb3f78c4))
* **cms:** Create CMS module with RESTful and GraphQL
  APIs ([2f6a774](https://github.com/gameguild-gg/gameguild/commit/2f6a774be7426822b952c6210c1daf6d4d0ad83b))
* **cms:** Implement ContentType permissions layer with
  migration ([00b9a6b](https://github.com/gameguild-gg/gameguild/commit/00b9a6bece1604b4bfe1cb25ca55c938e369a874))
* **cms:** Implement Discretionary Access Control permission
  system ([ccc41a8](https://github.com/gameguild-gg/gameguild/commit/ccc41a8d6e52dfffe6b435129e4e335a17d4709b))
* **cms:** Remove ResourcePermission entity and
  references ([834764c](https://github.com/gameguild-gg/gameguild/commit/834764cdac0e9ad9d23155e92123074198994280))
* **cms:** Remove role-based permission system and prepare for
  DAC ([528d271](https://github.com/gameguild-gg/gameguild/commit/528d2716a8a1ac3ce440be471e16e2a2cd8c5a73))
* **common/filters:** Improve period selectors with type
  safety ([810c1ec](https://github.com/gameguild-gg/gameguild/commit/810c1ec902d178e7c2479e63529e42664b707eaf))
* **common/header:** Add user profile dropdown and sign-in
  flow ([2c51ae2](https://github.com/gameguild-gg/gameguild/commit/2c51ae213b45d7ff3bcb5e670c45856f7a546b69))
* **common:** Add string conversion utilities for snake_case and
  slugs ([b49e05e](https://github.com/gameguild-gg/gameguild/commit/b49e05eac86b0e87a6147b737e4184d053adcb0e))
* complete course content integration and fix image loading
  issues ([b127462](https://github.com/gameguild-gg/gameguild/commit/b1274629e1899ef853e8d055a693d259514a6411))
* **components/filters:** Enhance period selector with tooltips and dynamic
  quarters ([bb0e7f8](https://github.com/gameguild-gg/gameguild/commit/bb0e7f84c39afd0ea6598eaaf55daa1b1fc83f2a))
* **components:** Add generic data views and enhanced filter
  system ([287940d](https://github.com/gameguild-gg/gameguild/commit/287940d322c971a0b668b6a0d55a92c347f96445))
* **components:** Add reusable data display and filter
  components ([9c167a9](https://github.com/gameguild-gg/gameguild/commit/9c167a90963a09c46bee9f432188cd3f6e883556))
* consolidate API generation and improve developer
  experience ([e445e21](https://github.com/gameguild-gg/gameguild/commit/e445e21fc810572db2ac78c46cca4af1ebf76bcd))
* **content/coding:** Integrate clang/pyodide and restructure
  editor ([b12c867](https://github.com/gameguild-gg/gameguild/commit/b12c86744b0d0940278a022de8bfba457fb29a66))
* **course-editor:** Add certificates, delivery, help, publish, SEO and content structure
  pages ([83d0b97](https://github.com/gameguild-gg/gameguild/commit/83d0b97fc588641fff11144c1e6f08c806aaf718))
* **courses:** add some old courses
  content ([95cc111](https://github.com/gameguild-gg/gameguild/commit/95cc111afc3c0f945bce5ffdc2c32c10f5e9ac84))
* **dashboard/layout:** Remove sidebar trigger and add sidebar
  component ([a818506](https://github.com/gameguild-gg/gameguild/commit/a8185069609379ccb9f86c3debdf1c54fe577d00))
* **dashboard/sidebar:** Update icon containers and
  labels ([b478559](https://github.com/gameguild-gg/gameguild/commit/b478559d851fa14a4f2aac90ca491b355e1f176f))
* **dashboard/tenant:** Reorganize UI components and add tenant
  utilities ([d578f74](https://github.com/gameguild-gg/gameguild/commit/d578f74257a0493b8daae3e1bd103fda3c3aeaa9))
* **dashboard/testing-lab:** Add reports feedback page and remove submit
  page ([0001a9c](https://github.com/gameguild-gg/gameguild/commit/0001a9cfc7b617b473e142754cf03007c9df0e75))
* **dashboard/testing-lab:** Enhance sessions UI with detailed list and
  table ([7eb8907](https://github.com/gameguild-gg/gameguild/commit/7eb8907fbafad6b232259158407ec6d35aa7f3ad))
* **dashboard/testing-lab:** Pass testing requests data to
  list ([d96bceb](https://github.com/gameguild-gg/gameguild/commit/d96bceb3ed68996debc01d4786eb44b36e696954))
* **dashboard:** Integrate API endpoints and SSR
  actions ([a6a3664](https://github.com/gameguild-gg/gameguild/commit/a6a366459b4cb5260b0bb965cba71f4a01be9a4e))
* **dashboard:** Revamp courses, achievements, tenants and user detail
  pages ([111916a](https://github.com/gameguild-gg/gameguild/commit/111916a1e104509689a259fb12bb0f1b7c5b6f7d))
* **dashboard:** Revamp tenant and user pages and remove legacy
  achievements ([92f0e8d](https://github.com/gameguild-gg/gameguild/commit/92f0e8dd5b5da7726637f6a02ec3e84b76476f8e))
* **dashboard:** Revamp testing lab layouts and
  sidebar ([019cca7](https://github.com/gameguild-gg/gameguild/commit/019cca7e709db7257c4275a8fc94582c3ac0673a))
* **dashboard:** Update analytics and overview
  pages ([bb95dac](https://github.com/gameguild-gg/gameguild/commit/bb95dac94b0b3224e10bc3478cb19a1556583e80))
* **docker:** Add multi-stage Dockerfile and update docker-compose
  configuration ([ba2f7f2](https://github.com/gameguild-gg/gameguild/commit/ba2f7f2aae1a8575449abde202816ad3f0e3125a))
* **editor:** API client and new GUI for
  editor ([3c44204](https://github.com/gameguild-gg/gameguild/commit/3c442047833a5727161b5bcf7a3a34ac0a010d80))
* **graphql:** Add 3-layer DAC authorization for GraphQL
  APIs ([0fd472d](https://github.com/gameguild-gg/gameguild/commit/0fd472d40287a594aeb09a5a2a231927f3badcd6))
* **lib/sync:** Implement multi-adapter sync
  system ([b3c727d](https://github.com/gameguild-gg/gameguild/commit/b3c727d2bf595147fd17e79a3da32627e6583b25))
* **markdown:** add markdown
  renderer ([d7d7679](https://github.com/gameguild-gg/gameguild/commit/d7d76791a43716c16b63878abb806d3ea9de44c6))
* **mods/programs:** Add progress and
  enrollment ([9c25f08](https://github.com/gameguild-gg/gameguild/commit/9c25f08464ad44db55743c3fe1ef19d2755ccafb))
* **modules/auth:** Implement complete authentication
  module ([7481505](https://github.com/gameguild-gg/gameguild/commit/7481505285d581a150594384559820ef641dac19))
* **modules/team:** Add team models and
  enums ([71a9086](https://github.com/gameguild-gg/gameguild/commit/71a9086a3ffe680a7196e5fd1e5e8cad6b92cd92))
* **modules/tenant:** Add tenant domain and auto-assignment
  functionality ([bf1afc2](https://github.com/gameguild-gg/gameguild/commit/bf1afc24ed20a12da956b75589da73338cb26dec))
* **modules/tenant:** Implement multi-tenancy and RBAC
  support ([c9622a8](https://github.com/gameguild-gg/gameguild/commit/c9622a8a02767637ffb8607449a04ecdefc1eb99))
* **modules/user-achievements:** Add user achievements
  module ([c094cd0](https://github.com/gameguild-gg/gameguild/commit/c094cd003178a789408161b91672a1fbb880b14b))
* **modules/userprofile:** Add user profile
  module ([10be4a8](https://github.com/gameguild-gg/gameguild/commit/10be4a80d09a4789fbfe5c162b494c58c4b27e3a))
* **modules/users:** Revamp user commands, events, and GraphQL
  endpoints ([f9d8fce](https://github.com/gameguild-gg/gameguild/commit/f9d8fced3f3d97873d86a742593e4ae4437d3a10))
* **modules/vote:** Add new vote model, type, and service
  interface ([943e70a](https://github.com/gameguild-gg/gameguild/commit/943e70a6078438d1a4b677864ad5f38c05633c96))
* **modules:** Add certificate, feedback, kyc, payment, product, program, tag, subscription
  modules ([5d874a9](https://github.com/gameguild-gg/gameguild/commit/5d874a97c772bbb0cf4b9a0ec73dcb9b73954dee))
* **payment-commerce:** Add payments, subscriptions and analytics
  modules ([2aa86f1](https://github.com/gameguild-gg/gameguild/commit/2aa86f1fa93d61e5f7e2ed74c3544421d3696092))
* **payments:** Add payments actions API
  endpoints ([e992a93](https://github.com/gameguild-gg/gameguild/commit/e992a93f7b3ab8f4e5313ff6a46ebf90316bb2d3))
* **permission:** Add Delete permission as alias for
  SoftDelete ([f2a8f09](https://github.com/gameguild-gg/gameguild/commit/f2a8f096dd1234594e81ddd74779ffe652e66d05))
* **permission:** Implement modular permission architecture with capability-based DAC
  strategy ([02d29a2](https://github.com/gameguild-gg/gameguild/commit/02d29a20c086603a381d0efbcff2781198030e39))
* **permission:** Implement modular permission system with enhanced
  granularity ([4a2ed87](https://github.com/gameguild-gg/gameguild/commit/4a2ed877abf378fcdfb09ee6ab3a12282a3bd865))
* **permissions:** Add SimplePermissionService and default permission template
  handling ([82c47f3](https://github.com/gameguild-gg/gameguild/commit/82c47f32f52bf478c430a0756b45db55b11beb04))
* **posts:** Add posts module with event-driven social
  features ([b67e041](https://github.com/gameguild-gg/gameguild/commit/b67e041d5a64179e80f60ccf71882deff23983fc))
* **prettier-config:** Update config
  settings ([0f4c6e3](https://github.com/gameguild-gg/gameguild/commit/0f4c6e3b2c9c0126de15d75b0fa344c5e8792a6f))
* **program:** Add Program module with DAC
  permissions ([f0c650c](https://github.com/gameguild-gg/gameguild/commit/f0c650c4831bd8336cada31b9ce962a148735f21))
* **program:** Add ProgramContent module with permissions
  inheritance ([38baace](https://github.com/gameguild-gg/gameguild/commit/38baace804f03abd87a9e16b9f2a3115f969ce46))
* **program:** Add verification, enrollment status and wishlist
  features ([cf272d9](https://github.com/gameguild-gg/gameguild/commit/cf272d90b979e9a89114a9cf3f3ae2f9416a08bb))
* **projects/database:** Create comprehensive project platform
  schema ([a576f30](https://github.com/gameguild-gg/gameguild/commit/a576f30ad4c2ee154aef8bb3b228fa66b47191cd))
* **projects:** Add comprehensive project platform database
  schema ([cbee521](https://github.com/gameguild-gg/gameguild/commit/cbee521e01adae7aa2158b86402b0f16d21e5bbd))
* **projects:** Add onProjectCreated callback and update project UI
  styles ([c7a1153](https://github.com/gameguild-gg/gameguild/commit/c7a11530b50f21ce3f31a7e079e3afe1a86ed409))
* remove USE_IN_MEMORY_DB and clean up database
  configuration ([59413ae](https://github.com/gameguild-gg/gameguild/commit/59413ae509df79b45f41e5b0580a8c19556a731c))
* **server-actions:** Add comprehensive modules for activity tracking, commerce, communication and content
  management ([cf003d3](https://github.com/gameguild-gg/gameguild/commit/cf003d398a7eb27c36280930bf5b7b4ac62a6846))
* **sync:** Add storage adapters, caching & sync
  provider ([bb48a19](https://github.com/gameguild-gg/gameguild/commit/bb48a19579423eff90892720059e7f991fc37772))
* **system:** Integrate payment gateway and course completion
  UI ([d5618ad](https://github.com/gameguild-gg/gameguild/commit/d5618ad4b2e9b4ef0ffddc0f71a1499799ed9673))
* **tenant:** Add tenant detail view, update auth token and remove legacy
  managers ([55db845](https://github.com/gameguild-gg/gameguild/commit/55db845a1927f6f4b3ca2b569672798e7ca90a61))
* **tenant:** Replace UserTenant with TenantPermission for flexible permission
  system ([964b374](https://github.com/gameguild-gg/gameguild/commit/964b3748559cfa9ecaf16e5731999e5447a7d5d1))
* **tenants:** Add tenant actions and update import
  paths ([5c3a8e8](https://github.com/gameguild-gg/gameguild/commit/5c3a8e8a85af90943d7010d2918565b59f6573e1))
* **testing-lab/users:** Add comprehensive user testing
  actions ([bbf1e65](https://github.com/gameguild-gg/gameguild/commit/bbf1e65e51e9aba92a546ef2e0fd614b40793f57))
* **testing-lab:** Add attendance endpoints and tracker
  props ([876bd17](https://github.com/gameguild-gg/gameguild/commit/876bd1733c9e6dfbacb2f161c56170feefb009f2))
* **testing-lab:** Add comprehensive server actions and reorganize
  structure ([7b408d4](https://github.com/gameguild-gg/gameguild/commit/7b408d48052828204f6faf5deb7930fedc5a1a8c))
* **testing-lab:** Add detail components for feedback and
  session ([a59e25e](https://github.com/gameguild-gg/gameguild/commit/a59e25e1d3ba65828658418bf3025a5dce8f1921))
* **testing-lab:** Add manage permissions
  button ([b726bdc](https://github.com/gameguild-gg/gameguild/commit/b726bdc1c5cfc4c8419ee367ecf7d483b1b0f50a))
* **testing-lab:** Add settings module with API endpoints and web
  integration ([1f95e3c](https://github.com/gameguild-gg/gameguild/commit/1f95e3c4163022944c7ddfa5b14e7eb4d0634eef))
* **testing-lab:** Add simplified testing workflow endpoints and
  UI ([c5a3b14](https://github.com/gameguild-gg/gameguild/commit/c5a3b1472aefdfde80a95d68dfaa4e5e1fdc5c1c))
* **testing-lab:** Replace role-based fetch with server
  actions ([3a47f71](https://github.com/gameguild-gg/gameguild/commit/3a47f7137701cdff34e066b4e58114c795a29739))
* **testing-lab:** Replace todos with API-driven data
  fetching ([d2c9a92](https://github.com/gameguild-gg/gameguild/commit/d2c9a92fac79c3fa3a445f057f6da2e855534afb))
* **testing-lab:** Update layout and UI
  styling ([60d5ba7](https://github.com/gameguild-gg/gameguild/commit/60d5ba758d13839281a0142508dbccfd3be8b7ae))
* **testing-lab:** Update UI styling and
  layout ([6e755c5](https://github.com/gameguild-gg/gameguild/commit/6e755c59f6bf4a366cb37fb055e6bade7abfd11e))
* **tests/api:** Add API tests project and reorganize solution
  structure ([b857d90](https://github.com/gameguild-gg/gameguild/commit/b857d90ba559aa3f31d35428cd8cac90ec36db99))
* **tests:** Add integration tests & style
  config ([55a0ae6](https://github.com/gameguild-gg/gameguild/commit/55a0ae608d8cc71c93cc5243ac816f1d52ab00c0))
* **ui:** Add comprehensive UI component
  library ([f79305b](https://github.com/gameguild-gg/gameguild/commit/f79305b8e51a2d0f3a798fc2e1ccc77a9f123f37))
* **user-management:** Add comprehensive and enhanced user
  actions ([c37fe1d](https://github.com/gameguild-gg/gameguild/commit/c37fe1d36ce558f6365dd7352fc808e69b1d921c))
* **users:** Add enhanced filtering and management
  UI ([ddda13f](https://github.com/gameguild-gg/gameguild/commit/ddda13fce6258060ef43d6885da6af7288f55df3))
* **web:** Add API type generation and client
  integration ([2c36823](https://github.com/gameguild-gg/gameguild/commit/2c368239be368a290a9f1a5cfdeb7715b13a5cab))
* **web:** Add auth page, improvements docs, and track
  data ([19654cc](https://github.com/gameguild-gg/gameguild/commit/19654cc80500df62b585f2f43af6bb2bc829d244))
* **web:** New middleware for subdomain-aware i18n
  routing ([aa96982](https://github.com/gameguild-gg/gameguild/commit/aa969827c2e63d6f50eea72d1a3bd8e36e19d058))

### Performance Improvements

* **tests/performance:** Optimize batch test data and update
  thresholds ([d04b49f](https://github.com/gameguild-gg/gameguild/commit/d04b49f1ab23182c7b37f51254e062a143df7ce0))
* **web:** improvement on the
  middleware ([17bc112](https://github.com/gameguild-gg/gameguild/commit/17bc112aa5d7947614f70653fe795deb6bc3ef39))

### BREAKING CHANGES

* **api:** Removes chapter entity and its related associations

## [1.21.8](https://github.com/gameguild-gg/website/compare/v1.21.7...v1.21.8) (2025-05-24)

### Bug Fixes

* **lint:** run lint fix on the
  api ([1e9e6e1](https://github.com/gameguild-gg/website/commit/1e9e6e15dfe6899bed61d341fb93aaa06e8bec04))
* **program:** add entities converted from dbml to entities via
  ai ([924d1cc](https://github.com/gameguild-gg/website/commit/924d1cce4388e143fdeceba184d84e60eb356c68))

## [1.21.7](https://github.com/gameguild-gg/website/compare/v1.21.6...v1.21.7) (2025-05-21)

### Bug Fixes

* **courses:** add more data to
  dbml ([aa0f41b](https://github.com/gameguild-gg/website/commit/aa0f41ba98a5ae0bb34d2604af52218257729c50))

## [1.21.6](https://github.com/gameguild-gg/website/compare/v1.21.5...v1.21.6) (2025-05-20)

### Bug Fixes

* **courses:** add courses db
  planning ([251cec1](https://github.com/gameguild-gg/website/commit/251cec184ca438cd0a915a3ead0613945ad823ff))
* **courses:** add more planning to the courses db structure.
  wip ([89d1c0b](https://github.com/gameguild-gg/website/commit/89d1c0b35617bb5aea7066ea4e66b9707204e1d1))

## [1.21.5](https://github.com/gameguild-gg/website/compare/v1.21.4...v1.21.5) (2025-05-15)

### Bug Fixes

* **contributors:** improve the way we generate git statistics and show
  versioning ([c472153](https://github.com/gameguild-gg/website/commit/c4721539b13ea8b2199be556e6d0a6298cccbeb5))

## [1.21.4](https://github.com/gameguild-gg/website/compare/v1.21.3...v1.21.4) (2025-05-15)

### Bug Fixes

* **contributors:** fix contributors page
  generation ([f73b5a8](https://github.com/gameguild-gg/website/commit/f73b5a85563c8b6732ca74b07d3ad87584b6bc3f))

## [1.21.3](https://github.com/gameguild-gg/website/compare/v1.21.2...v1.21.3) (2025-05-15)

### Bug Fixes

* **cleanup:** remove unused
  components ([718a85f](https://github.com/gameguild-gg/website/commit/718a85f6eb756788724fc001d14020becec9dfc2))

## [1.21.2](https://github.com/gameguild-gg/website/compare/v1.21.1...v1.21.2) (2025-05-15)

### Bug Fixes

* **cleanup:** remove unused
  components ([1db2242](https://github.com/gameguild-gg/website/commit/1db2242826b09f02ee5ada52463e46bab14777f2))

## [1.21.1](https://github.com/gameguild-gg/website/compare/v1.21.0...v1.21.1) (2025-05-15)

# [1.21.0](https://github.com/gameguild-gg/website/compare/v1.20.5...v1.21.0) (2025-05-15)

### Features

* **code:** remove wasmer and use the new custom wrapper more than 10x smaller, and way
  faster ([e7eedcb](https://github.com/gameguild-gg/website/commit/e7eedcb8feed411cffc17f451d936028ee3f3d3f))

## [1.20.5](https://github.com/gameguild-gg/website/compare/v1.20.4...v1.20.5) (2025-05-15)

### Bug Fixes

* **build:** better mjs
  exclusion ([7a1ab6a](https://github.com/gameguild-gg/website/commit/7a1ab6a99a2e2e6ce7d85c079b64b711777f0829))

## [1.20.4](https://github.com/gameguild-gg/website/compare/v1.20.3...v1.20.4) (2025-05-15)

### Bug Fixes

* **build:** update nome radix-ui
  packages ([15f36cf](https://github.com/gameguild-gg/website/commit/15f36cffdc6b2f6cf2b7dd8b83190d6b07e65c63))

## [1.20.3](https://github.com/gameguild-gg/website/compare/v1.20.2...v1.20.3) (2025-05-14)

### Bug Fixes

* **auth:** make auth be dynamic
  page ([25647c4](https://github.com/gameguild-gg/website/commit/25647c47914cab0800326f522728538c9f3fcf16))

## [1.20.2](https://github.com/gameguild-gg/website/compare/v1.20.1...v1.20.2) (2025-05-14)

### Bug Fixes

* conflicting radix-ui
  package ([ede9775](https://github.com/gameguild-gg/website/commit/ede97758f17d94bf232b4783dc20fd85463a9791))

## [1.20.1](https://github.com/gameguild-gg/website/compare/v1.20.0...v1.20.1) (2025-05-14)

### Bug Fixes

* **mailer:** fix mail
  send ([ac509b5](https://github.com/gameguild-gg/website/commit/ac509b5a2e07e3268261998a078923dcf50df584))

# [1.20.0](https://github.com/gameguild-gg/website/compare/v1.19.13...v1.20.0) (2025-05-13)

### Bug Fixes

* institutional page routing.
  closes [#101](https://github.com/gameguild-gg/website/issues/101) ([b188aba](https://github.com/gameguild-gg/website/commit/b188aba7c3e6daf741f3026e54d027b4b4921872))
* **manifest:**
  closes [#33](https://github.com/gameguild-gg/website/issues/33) ([b72e196](https://github.com/gameguild-gg/website/commit/b72e196e5b0cf5d24bae208bbdef363f22366002))
* **manifest:** fix manifest
  type ([333b1d7](https://github.com/gameguild-gg/website/commit/333b1d75cd38bfc4ca3b63cc73d10384ac517cfc))

### Features

* **robots:**
  closes [#35](https://github.com/gameguild-gg/website/issues/35) ([2f2a20d](https://github.com/gameguild-gg/website/commit/2f2a20d069e50aa1130bc8ea943a73c19d05761d))

## [1.19.13](https://github.com/gameguild-gg/website/compare/v1.19.12...v1.19.13) (2025-05-12)

### Bug Fixes

* **code:** add a unified way to code on the
  browser ([b356821](https://github.com/gameguild-gg/website/commit/b356821e0bc3bec331060cfa51ba0653ff7dbcd0))

## [1.19.12](https://github.com/gameguild-gg/website/compare/v1.19.11...v1.19.12) (2025-05-12)

### Bug Fixes

* **courses:** improve programming course
  types ([a6b4ea9](https://github.com/gameguild-gg/website/commit/a6b4ea929c6e0d5d8dddc6ad722246a0dc11b6f0))
* **usecode:** begining of the refactor to usecode instead of usepyodide or
  useclang ([8075941](https://github.com/gameguild-gg/website/commit/8075941c87f0d3f3244a0155f4c2441ddcfdd810))

## [1.19.11](https://github.com/gameguild-gg/website/compare/v1.19.10...v1.19.11) (2025-05-06)

### Bug Fixes

* **code:** code test
  type ([dbb7df9](https://github.com/gameguild-gg/website/commit/dbb7df96a90d165ba03492902d219716803cf8d3))

## [1.19.10](https://github.com/gameguild-gg/website/compare/v1.19.9...v1.19.10) (2025-05-06)

### Bug Fixes

* add mindmaps of future
  works ([1b7c711](https://github.com/gameguild-gg/website/commit/1b7c711bd8457a29b65c6900460413bb6a0faddb))

## [1.19.9](https://github.com/gameguild-gg/website/compare/v1.19.8...v1.19.9) (2025-05-02)

### Bug Fixes

* **code:** improve architecture of the code
  executor ([630c6cc](https://github.com/gameguild-gg/website/commit/630c6cc66847d8c74d63db6d80c18349b892f230))

## [1.19.8](https://github.com/gameguild-gg/website/compare/v1.19.7...v1.19.8) (2025-05-02)

### Bug Fixes

* **chess:** change moduleResolution to build
  frontend ([6785c0b](https://github.com/gameguild-gg/website/commit/6785c0b0cceefb893c9d71439e9e3d8242fffde7))
* **chess:** fix tournament ranking
  value ([1154a16](https://github.com/gameguild-gg/website/commit/1154a16c95e7bde68576f27975b40df62b116b97))
* **wasm:** message passing is now through
  callbacks ([179517f](https://github.com/gameguild-gg/website/commit/179517fe89c556e08e8770ccfcbd684883ea7373))
* **wasm:** remove
  timeouts ([e4942ce](https://github.com/gameguild-gg/website/commit/e4942ceb83f64f5efe033cd6468782fd49acc574))

## [1.19.7](https://github.com/gameguild-gg/website/compare/v1.19.6...v1.19.7) (2025-04-25)

### Bug Fixes

* **wasm:** clean
  up ([580c88f](https://github.com/gameguild-gg/website/commit/580c88f5fb10b2e1ccab282d297ea693421e2f0b))
* **wasm:** fix type for file
  directory ([acab593](https://github.com/gameguild-gg/website/commit/acab593cc8e85f45c575e799a818bd4cdcec0dbb))
* **wasm:** move location of
  types ([c8852f0](https://github.com/gameguild-gg/website/commit/c8852f0e69ccbf41444ed5122c0c6fc0879f1259))
* **wasm:** refactor clang api
  directory ([c5c0d44](https://github.com/gameguild-gg/website/commit/c5c0d4497083f4cc4052a9e1702cc471eecbb23f))
* **wasm:** remove cpp
  example ([6e16e2b](https://github.com/gameguild-gg/website/commit/6e16e2b37602cba7ddd0e0b65eb9a36028120003))
* **wasm:** remove duplicity of worker code on clang
  runner ([c92a764](https://github.com/gameguild-gg/website/commit/c92a7640b3313881c8168bdecc15bdc346b2a6d4))
* **wasm:** remove unused
  components ([213de38](https://github.com/gameguild-gg/website/commit/213de3879ec067ce876cbe16757cf90b83b45e9e))
* **wasm:** removed clang runner with
  zustand ([d60f3b6](https://github.com/gameguild-gg/website/commit/d60f3b6e3d625493c950c769f123679579d2f3e4))

## [1.19.6](https://github.com/gameguild-gg/website/compare/v1.19.5...v1.19.6) (2025-04-25)

### Bug Fixes

* **wasm:** fix clang
  api ([8095fbe](https://github.com/gameguild-gg/website/commit/8095fbe853c2de247392be72234a7e1f74f9e1a5))
* **wasm:** fix clang
  demo ([06f4a21](https://github.com/gameguild-gg/website/commit/06f4a21096becf08a72442ec064167dccae15acb))
* **wasm:** fix clang interface for
  debug; ([b7377c5](https://github.com/gameguild-gg/website/commit/b7377c5096a40d97609a531515a2ebe1e515fcc5))

## [1.19.5](https://github.com/gameguild-gg/website/compare/v1.19.4...v1.19.5) (2025-04-22)

### Bug Fixes

* **wasm:** better interfaces for
  wasmer ([cda7275](https://github.com/gameguild-gg/website/commit/cda7275fdfc148d9481d8be7fa35351c80096594))
* **wasm:** better pyodide load
  process ([3263e26](https://github.com/gameguild-gg/website/commit/3263e26c59053917d8dfc095ed06cacf8abb9c72))

## [1.19.4](https://github.com/gameguild-gg/website/compare/v1.19.3...v1.19.4) (2025-04-22)

### Bug Fixes

* **wasm:** convert to
  ts ([b7d99fc](https://github.com/gameguild-gg/website/commit/b7d99fc3c6737ca71eeeb5493407c65f61df9a39))
* **wasm:** fix
  build ([fdd1df4](https://github.com/gameguild-gg/website/commit/fdd1df47f02eaa26d5bd9a1733c43ba404611c73))
* **wasm:** fix
  compilation ([1ef886f](https://github.com/gameguild-gg/website/commit/1ef886f48b08daff0393354b6339f798db3dd4c2))
* **wasm:** now clang show properly the execution
  message ([c1a999a](https://github.com/gameguild-gg/website/commit/c1a999a8e9d0730dc0204018efb53e2af9a93f12))
* **wasm:** remove timings and console
  colors ([9b72136](https://github.com/gameguild-gg/website/commit/9b7213643c610cdd0ba698d02b36cc8ef469ecb5))

## [1.19.3](https://github.com/gameguild-gg/website/compare/v1.19.2...v1.19.3) (2025-04-18)

### Bug Fixes

* **courses:** add monte carlo tree search decision
  taking ([e1feba0](https://github.com/gameguild-gg/website/commit/e1feba0e99655f292d6542da6c65867eec6e0f7e))
* **courses:** monte carlo tree
  search ([b65cc5a](https://github.com/gameguild-gg/website/commit/b65cc5ac39624631621501c00fbc5ded2181dd11))
* **plos:** update plos from
  champlain ([4ee3c78](https://github.com/gameguild-gg/website/commit/4ee3c7843901c0c2c1698e8b4e61c7091b0a641b))
* **wasm:** fix build
  issue ([d53d2ea](https://github.com/gameguild-gg/website/commit/d53d2ea743038a46e80092ddfbceecb58b2ac375))

## [1.19.2](https://github.com/gameguild-gg/website/compare/v1.19.1...v1.19.2) (2025-04-17)

### Bug Fixes

* **clang:** simplified and streamlined execution
  flow ([98d853b](https://github.com/gameguild-gg/website/commit/98d853b106bdcb6e21f9babfb9dd1ea0776c037f))

## [1.19.1](https://github.com/gameguild-gg/website/compare/v1.19.0...v1.19.1) (2025-04-15)

### Bug Fixes

* **clang:** one shot load and
  run ([9db5535](https://github.com/gameguild-gg/website/commit/9db5535b3ba3c6fd0b6a34cb343a3c1017a665d0))

# [1.19.0](https://github.com/gameguild-gg/website/compare/v1.18.5...v1.19.0) (2025-04-15)

### Bug Fixes

* **clang:** fix clang demo
  dependency ([09f8a22](https://github.com/gameguild-gg/website/commit/09f8a22e9ad5714caa25ae9a56ea24a153beda5a))
* **courses:** add monte carlo tree
  search ([24892a9](https://github.com/gameguild-gg/website/commit/24892a982a5733949d7d1abef06169059ab5015a))
* package json dependency
  hell ([08c4bf2](https://github.com/gameguild-gg/website/commit/08c4bf26158cb3d52d2c4844f14aeae7400bb627))
* **schema:** fix schema dump to be consistent between
  runs ([7b3ca93](https://github.com/gameguild-gg/website/commit/7b3ca93d9e7dd3af51320136d006dd18be645535))

### Features

* **latex:** add latex support for markdown
  renderer ([636af3d](https://github.com/gameguild-gg/website/commit/636af3daff92673b4e8c568bd79c4cefe842fb8f))

## [1.18.5](https://github.com/gameguild-gg/website/compare/v1.18.4...v1.18.5) (2025-04-11)

### Bug Fixes

* **wasm:** add clang worker
  wrapper ([61a15f0](https://github.com/gameguild-gg/website/commit/61a15f0cb26f92a42ff19efaf7784d7ab5436787))

## [1.18.4](https://github.com/gameguild-gg/website/compare/v1.18.3...v1.18.4) (2025-04-11)

### Bug Fixes

* **champlain:** add better plo graph
  visualization ([7b355b0](https://github.com/gameguild-gg/website/commit/7b355b005876a6edd8e0fdb5d1e73776e7147c8e))
* **champlain:** add plos
  page ([e6b0a6e](https://github.com/gameguild-gg/website/commit/e6b0a6e95a3b2af16be6efc1816888396d5e7e66))
* **wasm:** add workers for wasm
  interfaces ([e17b32f](https://github.com/gameguild-gg/website/commit/e17b32f951be6dbfa1575d365b363d9da7e3b8fb))

## [1.18.3](https://github.com/gameguild-gg/website/compare/v1.18.2...v1.18.3) (2025-04-08)

### Bug Fixes

* **courses:** fix
  typo ([cee2ecc](https://github.com/gameguild-gg/website/commit/cee2ecca2fabe21740e4e7fb9e06c2ac2d87e855))

## [1.18.2](https://github.com/gameguild-gg/website/compare/v1.18.1...v1.18.2) (2025-04-08)

### Bug Fixes

* **courses:** fix min max example
  code ([c625987](https://github.com/gameguild-gg/website/commit/c625987e11331f51d94aa8b2b4574033d301c293))

## [1.18.1](https://github.com/gameguild-gg/website/compare/v1.18.0...v1.18.1) (2025-04-08)

### Bug Fixes

* **courses:** add boilerplate for
  min-max ([b66c449](https://github.com/gameguild-gg/website/commit/b66c449360ecab7a3c16353caa2c331f4a9ec1d0))

# [1.18.0](https://github.com/gameguild-gg/website/compare/v1.17.27...v1.18.0) (2025-04-08)

### Features

* **clang-wasm:** add support for c++
  compilation ([5a6eb40](https://github.com/gameguild-gg/website/commit/5a6eb4025e96036fa54cddf575bf963ad3d2b906))

## [1.17.27](https://github.com/gameguild-gg/website/compare/v1.17.26...v1.17.27) (2025-04-05)

## [1.17.26](https://github.com/gameguild-gg/website/compare/v1.17.25...v1.17.26) (2025-04-04)

### Bug Fixes

* **courses:** fix
  typo ([28a7354](https://github.com/gameguild-gg/website/commit/28a7354cdf80046dd4c9f2bb23ce26b9d1e94f5a))

## [1.17.25](https://github.com/gameguild-gg/website/compare/v1.17.24...v1.17.25) (2025-04-04)

### Bug Fixes

* **chess:** add better error
  handling ([c9a6ee4](https://github.com/gameguild-gg/website/commit/c9a6ee4e27f2af4f4a775f8b90858aa90b19036c))

## [1.17.24](https://github.com/gameguild-gg/website/compare/v1.17.23...v1.17.24) (2025-04-03)

### Bug Fixes

* **courses:** fix another
  typo ([e77caa2](https://github.com/gameguild-gg/website/commit/e77caa25194d39a17876a95f7cf0579b91c047bd))

## [1.17.23](https://github.com/gameguild-gg/website/compare/v1.17.22...v1.17.23) (2025-04-03)

### Bug Fixes

* **courses:** fix
  typo ([d22b6d4](https://github.com/gameguild-gg/website/commit/d22b6d46f849c2412eeb79f53b5bbea478a62c9a))

## [1.17.22](https://github.com/gameguild-gg/website/compare/v1.17.21...v1.17.22) (2025-04-03)

### Bug Fixes

* **courses:** add api calls content to
  python ([9b691cf](https://github.com/gameguild-gg/website/commit/9b691cfe18ee1238dc46462c3f20a15f902408d1))

## [1.17.21](https://github.com/gameguild-gg/website/compare/v1.17.20...v1.17.21) (2025-04-02)

### Bug Fixes

* **courses:** acing
  interviews ([af12810](https://github.com/gameguild-gg/website/commit/af12810baed7244ba3e7967da7238927082f3ff2))
* **courses:**
  links ([1487020](https://github.com/gameguild-gg/website/commit/148702083eebde33d99b0c860d6a7fa4fe665516))
* **courses:**
  typos ([a6c14fc](https://github.com/gameguild-gg/website/commit/a6c14fc6246057dc83383a77fe9ebc7faa48e3b2))

## [1.17.20](https://github.com/gameguild-gg/website/compare/v1.17.19...v1.17.20) (2025-04-01)

### Bug Fixes

* **cors:** trying to fix cors once
  more ([0774380](https://github.com/gameguild-gg/website/commit/0774380370eda91df7a0661c53e8b1d2a98ca56d))

## [1.17.19](https://github.com/gameguild-gg/website/compare/v1.17.18...v1.17.19) (2025-04-01)

### Bug Fixes

* **courses:** chess min max
  lecture ([cd25081](https://github.com/gameguild-gg/website/commit/cd250817c0bbc6b304bc7128669295b06fb92319))

## [1.17.18](https://github.com/gameguild-gg/website/compare/v1.17.17...v1.17.18) (2025-03-28)

### Bug Fixes

* **courses:** chess
  fixes ([249c330](https://github.com/gameguild-gg/website/commit/249c330320cd50b33842d70949882be740874515))

## [1.17.17](https://github.com/gameguild-gg/website/compare/v1.17.16...v1.17.17) (2025-03-28)

### Bug Fixes

* **courses:** fix typo on chess class. square definition was
  wrong ([e01ee64](https://github.com/gameguild-gg/website/commit/e01ee64898c0f4d3514343fcb233f92810dbf353))

## [1.17.16](https://github.com/gameguild-gg/website/compare/v1.17.15...v1.17.16) (2025-03-28)

### Bug Fixes

* **courses:** add chess board huffman
  code ([957b4ec](https://github.com/gameguild-gg/website/commit/957b4eca07f423d39a9088b0bfc7dc0f636c1df0))

## [1.17.15](https://github.com/gameguild-gg/website/compare/v1.17.14...v1.17.15) (2025-03-27)

### Bug Fixes

* **courses:** add exception to python
  class ([95eca3f](https://github.com/gameguild-gg/website/commit/95eca3ffdeffebf7920767a45c94c36bf787ed0f))

## [1.17.14](https://github.com/gameguild-gg/website/compare/v1.17.13...v1.17.14) (2025-03-26)

### Bug Fixes

* **courses:** add assignment for portfolio
  classes ([8ca8d4c](https://github.com/gameguild-gg/website/commit/8ca8d4c16a2a01490b130d767578f1f9a66c681a))

## [1.17.13](https://github.com/gameguild-gg/website/compare/v1.17.12...v1.17.13) (2025-03-25)

### Bug Fixes

* **courses:** fix missing bytes on the python file
  course ([eb7f550](https://github.com/gameguild-gg/website/commit/eb7f55003614f8ae454086a7a4cb9133f73e4b70))
* **markdown-renderer:** fix markdown code activity for
  cpp ([cdf2eb6](https://github.com/gameguild-gg/website/commit/cdf2eb62cb8e62700de87d33b5240fc59ab0f575))

## [1.17.12](https://github.com/gameguild-gg/website/compare/v1.17.11...v1.17.12) (2025-03-25)

### Bug Fixes

* **courses:** chess assignment random
  movement ([6e600ac](https://github.com/gameguild-gg/website/commit/6e600ac1257445a2da9cb7e9c40e8c9ec4f8b86e))

## [1.17.11](https://github.com/gameguild-gg/website/compare/v1.17.10...v1.17.11) (2025-03-24)

### Bug Fixes

* **courses:** typo ([eb0aa11](https://github.com/gameguild-gg/website/commit/eb0aa116687038f3094892f66070a8c47b2925cf))

## [1.17.10](https://github.com/gameguild-gg/website/compare/v1.17.9...v1.17.10) (2025-03-24)

### Bug Fixes

* **courses:** add files and exceptions
  content ([bb25072](https://github.com/gameguild-gg/website/commit/bb25072537ea498debb70c3d7c7cb5b582f26bb9))

## [1.17.9](https://github.com/gameguild-gg/website/compare/v1.17.8...v1.17.9) (2025-03-19)

### Bug Fixes

* **chess:** replay functionality is working
  now ([c67b771](https://github.com/gameguild-gg/website/commit/c67b771b71f969441f3a534f72de45468e4f6ccd))

## [1.17.8](https://github.com/gameguild-gg/website/compare/v1.17.7...v1.17.8) (2025-03-19)

### Bug Fixes

* **courses:** add section to portfolio
  class ([ce1b8a1](https://github.com/gameguild-gg/website/commit/ce1b8a1d09261d33a0ce24dbff08726a75b63d7a))

## [1.17.7](https://github.com/gameguild-gg/website/compare/v1.17.6...v1.17.7) (2025-03-19)

### Bug Fixes

* **courses:** add portfolio class about finalizing demo
  reels ([abb2556](https://github.com/gameguild-gg/website/commit/abb25562b3340eebee00a75880e19fa9b61cf27a))

## [1.17.6](https://github.com/gameguild-gg/website/compare/v1.17.5...v1.17.6) (2025-03-19)

### Bug Fixes

* **chess:** challenge another
  player ([7cbf19a](https://github.com/gameguild-gg/website/commit/7cbf19a0a6eb0c2faff3d0b6de68335979e3f575))

## [1.17.5](https://github.com/gameguild-gg/website/compare/v1.17.4...v1.17.5) (2025-03-18)

### Bug Fixes

* **courses:** add more courses into ai
  chess ([bd72644](https://github.com/gameguild-gg/website/commit/bd72644f9e689d7f3c1820c40a4acd0be59491c4))

## [1.17.4](https://github.com/gameguild-gg/website/compare/v1.17.3...v1.17.4) (2025-03-17)

### Bug Fixes

* **courses:** add python dictionary to python
  course ([af05799](https://github.com/gameguild-gg/website/commit/af0579925b73bc4fb135674b0aa23e0db73884c0))

## [1.17.3](https://github.com/gameguild-gg/website/compare/v1.17.2...v1.17.3) (2025-03-13)

### Bug Fixes

* **chess:** now you can play against
  bots. ([ed70f2c](https://github.com/gameguild-gg/website/commit/ed70f2ca9bb2dd904ff427a6a19c0add804a80c4))

## [1.17.2](https://github.com/gameguild-gg/website/compare/v1.17.1...v1.17.2) (2025-03-12)

### Bug Fixes

* **chess:** fix chess matches
  view ([9af330a](https://github.com/gameguild-gg/website/commit/9af330a3817a6c66e9ed493cdae15ccb194ba0c4))

## [1.17.1](https://github.com/gameguild-gg/website/compare/v1.17.0...v1.17.1) (2025-03-12)

### Bug Fixes

* **chess:** fix leaderboard api
  response ([203ed80](https://github.com/gameguild-gg/website/commit/203ed80a0e83af26bfc9fea0677011ebaa542935))

# [1.17.0](https://github.com/gameguild-gg/website/compare/v1.16.3...v1.17.0) (2025-03-12)

### Bug Fixes

* **auth:** fix auth related
  issues ([d7f76dc](https://github.com/gameguild-gg/website/commit/d7f76dce1060a5fec394a5fe35e400f3361a5404))
* **courses:** portfolio week 8 - visual
  cues ([703b330](https://github.com/gameguild-gg/website/commit/703b3303105bfd490ea093f793acdcfe7c5f4f19))

### Features

* **chess:** add chess web
  boilerplate ([1110d75](https://github.com/gameguild-gg/website/commit/1110d75201b2553c7fcd97791807424f49c64aab))

## [1.16.3](https://github.com/gameguild-gg/website/compare/v1.16.2...v1.16.3) (2025-02-28)

### Bug Fixes

* **courses:** add metrics
  analytics ([b163a67](https://github.com/gameguild-gg/website/commit/b163a67704107139bd7500807e5bf81b2e9d3c1c))

## [1.16.2](https://github.com/gameguild-gg/website/compare/v1.16.1...v1.16.2) (2025-02-26)

### Bug Fixes

* **courses:** fix portfolio
  quiz ([ef62a53](https://github.com/gameguild-gg/website/commit/ef62a5324cd19ea03b4b7f0ee9576e85ff5c8819))

## [1.16.1](https://github.com/gameguild-gg/website/compare/v1.16.0...v1.16.1) (2025-02-26)

### Bug Fixes

* **courses:** add portfolio piece
  touchpoint ([048a27d](https://github.com/gameguild-gg/website/commit/048a27d96092f77a7ac69f74f1003f701569bc6d))

# [1.16.0](https://github.com/gameguild-gg/website/compare/v1.15.26...v1.16.0) (2025-02-24)

### Bug Fixes

* **coding-environment:** remove especific icons to standard
  icon ([0cc69d3](https://github.com/gameguild-gg/website/commit/0cc69d3c0c95f355dbcf479c8d1531a1bd1a1ccc))
* **courses:** add nested loop course to
  python ([bc5ca4a](https://github.com/gameguild-gg/website/commit/bc5ca4a5c859e1488e53d94ae1fc946798d86495))
* **db-cleaning:** now if you rename a table or you generate make an orphan table, the startup process will warn you
  until you clean
  it ([a4b34a2](https://github.com/gameguild-gg/website/commit/a4b34a2fbfe12bbd0df7fbadb90f575257b5c950))

### Features

* **table_schema:** generate a sql dump of the current table
  schema ([e60228c](https://github.com/gameguild-gg/website/commit/e60228ccbe01290db0984ba20aec1859bf1b795f))

## [1.15.26](https://github.com/gameguild-gg/website/compare/v1.15.25...v1.15.26) (2025-02-19)

### Bug Fixes

* **courses:** add cover letter course to portfolio
  classes ([0a393d3](https://github.com/gameguild-gg/website/commit/0a393d341f976d247acb9d62301b579f91210050))

## [1.15.25](https://github.com/gameguild-gg/website/compare/v1.15.24...v1.15.25) (2025-02-18)

### Bug Fixes

* **courses:** add htn to ai
  classes ([9c5b63a](https://github.com/gameguild-gg/website/commit/9c5b63ab560c1831cf64f1dc16798625d890634b))

## [1.15.24](https://github.com/gameguild-gg/website/compare/v1.15.23...v1.15.24) (2025-02-17)

### Bug Fixes

* **courses:** fix an activity on python lists
  class ([f1dde2d](https://github.com/gameguild-gg/website/commit/f1dde2ddfd8b721c50eaa172c771a77f808e73a5))

## [1.15.23](https://github.com/gameguild-gg/website/compare/v1.15.22...v1.15.23) (2025-02-17)

### Bug Fixes

* **courses:** fix summary of the python loop
  lecture ([61146fd](https://github.com/gameguild-gg/website/commit/61146fd3deb1466a81b7b75ca5352de9feca3da6))

## [1.15.22](https://github.com/gameguild-gg/website/compare/v1.15.21...v1.15.22) (2025-02-17)

### Bug Fixes

* **typo:** fix missing python tag for syntax
  highlight ([d48edf1](https://github.com/gameguild-gg/website/commit/d48edf1d1925de50525eaffc5e8bc77f83e1e6f3))

## [1.15.21](https://github.com/gameguild-gg/website/compare/v1.15.20...v1.15.21) (2025-02-17)

### Bug Fixes

* **courses:** add python loops
  content ([b578657](https://github.com/gameguild-gg/website/commit/b5786570c24cbe597635abc3940d3f9d55759a93))

## [1.15.20](https://github.com/gameguild-gg/website/compare/v1.15.19...v1.15.20) (2025-02-15)

### Bug Fixes

* **codeactivity:** add confetti on
  success ([e444235](https://github.com/gameguild-gg/website/commit/e444235b4c009b36d2d3e1019977d10ad176d5cc))

## [1.15.19](https://github.com/gameguild-gg/website/compare/v1.15.18...v1.15.19) (2025-02-14)

### Bug Fixes

* **courses:** better coding error
  descriptions ([9e7dd48](https://github.com/gameguild-gg/website/commit/9e7dd48364a3cffb57f61257f2769b4fff45ad4a))

## [1.15.18](https://github.com/gameguild-gg/website/compare/v1.15.17...v1.15.18) (2025-02-14)

### Bug Fixes

* **code activity:** fix code activity dynamic
  scaling ([a3c5881](https://github.com/gameguild-gg/website/commit/a3c58817bce0d53e2b599f5fc798017e6257c28c))

## [1.15.17](https://github.com/gameguild-gg/website/compare/v1.15.16...v1.15.17) (2025-02-14)

### Bug Fixes

* **courses:** minor typo on python exercise
  1 ([778c052](https://github.com/gameguild-gg/website/commit/778c052c05a4d6b94c18e8d748311c09439d5d72))

## [1.15.16](https://github.com/gameguild-gg/website/compare/v1.15.15...v1.15.16) (2025-02-14)

### Bug Fixes

* **courses:** add one more exercises to python lists
  course ([f223a36](https://github.com/gameguild-gg/website/commit/f223a36c991a5813186cb3c5afbbe8f50d4c1152))

## [1.15.15](https://github.com/gameguild-gg/website/compare/v1.15.14...v1.15.15) (2025-02-14)

### Bug Fixes

* **courses:** add the first coding exercise for
  python ([4845c1c](https://github.com/gameguild-gg/website/commit/4845c1c1450928e00cf81a2d8c185534dfcbff27))

## [1.15.14](https://github.com/gameguild-gg/website/compare/v1.15.13...v1.15.14) (2025-02-14)

### Bug Fixes

* **codeactivity:** fix code activity size
  scaling ([94b10b0](https://github.com/gameguild-gg/website/commit/94b10b0be16173f878a29f828c74d9a5b1f7ca31))

## [1.15.13](https://github.com/gameguild-gg/website/compare/v1.15.12...v1.15.13) (2025-02-14)

### Bug Fixes

* **codeactivity:** hide blocks if not needed
  anymore ([f53b133](https://github.com/gameguild-gg/website/commit/f53b133157c47eb61ef552bbf3d6bb53eaceef64))

## [1.15.12](https://github.com/gameguild-gg/website/compare/v1.15.11...v1.15.12) (2025-02-14)

### Bug Fixes

* **courses:** typo ([a5b0200](https://github.com/gameguild-gg/website/commit/a5b0200cf4169097b4ff9a1b16139964bebf63f5))

## [1.15.11](https://github.com/gameguild-gg/website/compare/v1.15.10...v1.15.11) (2025-02-14)

### Bug Fixes

* **courses:** add lecture for GOAP in
  C# ([f2a8f2b](https://github.com/gameguild-gg/website/commit/f2a8f2b32503a9d259c9c2bd885fc2146352d457))

## [1.15.10](https://github.com/gameguild-gg/website/compare/v1.15.9...v1.15.10) (2025-02-14)

### Bug Fixes

* **codeactivity:** wip code
  activity ([706d108](https://github.com/gameguild-gg/website/commit/706d108bc7a58f58481862df8b0c2f6b92fb8afc))

## [1.15.9](https://github.com/gameguild-gg/website/compare/v1.15.8...v1.15.9) (2025-02-14)

### Bug Fixes

* **codeactivity:** add first version of code activity embedded inside
  markdown ([fa717e8](https://github.com/gameguild-gg/website/commit/fa717e871526f84670c5f1445c487773012f75da))

## [1.15.8](https://github.com/gameguild-gg/website/compare/v1.15.7...v1.15.8) (2025-02-13)

### Performance Improvements

* **wasmer:** add better way to call
  wasmer ([1420cb4](https://github.com/gameguild-gg/website/commit/1420cb48f08135389d8e59d8c9eb04e3fc2444b2))

## [1.15.7](https://github.com/gameguild-gg/website/compare/v1.15.6...v1.15.7) (2025-02-13)

### Bug Fixes

* **conduct:** add code of
  contuct ([e784a58](https://github.com/gameguild-gg/website/commit/e784a5866ebcd52c679d9e07694505c0699c847e))

## [1.15.6](https://github.com/gameguild-gg/website/compare/v1.15.5...v1.15.6) (2025-02-12)

## [1.15.5](https://github.com/gameguild-gg/website/compare/v1.15.4...v1.15.5) (2025-02-12)

### Bug Fixes

* **wasmer:** better interface for calling
  wasmer ([f3c7765](https://github.com/gameguild-gg/website/commit/f3c7765a3e7dbf36f1051182f0922f814e50232e))

## [1.15.4](https://github.com/gameguild-gg/website/compare/v1.15.3...v1.15.4) (2025-02-10)

### Bug Fixes

* **courses:** add list lecture on python
  course ([daa2f79](https://github.com/gameguild-gg/website/commit/daa2f795f6d4d0ffc654c93c4436f972b75a71d6))

## [1.15.3](https://github.com/gameguild-gg/website/compare/v1.15.2...v1.15.3) (2025-02-09)

### Bug Fixes

* **wasmer:** Improve wasmer to be lazy
  loaded. ([395e97e](https://github.com/gameguild-gg/website/commit/395e97e6101a87978e9774f4755283cec0096983))

## [1.15.2](https://github.com/gameguild-gg/website/compare/v1.15.1...v1.15.2) (2025-02-09)

### Bug Fixes

* **licences:** fix
  licenses ([1225a39](https://github.com/gameguild-gg/website/commit/1225a3983eea5556f33abfc286e125aa037641e4))
* **packages:** fix installations issues on
  github ([737e084](https://github.com/gameguild-gg/website/commit/737e084cdc44a09b06d54103f76575d2d1b072c3))
* **readme:** add gitflow to readme and
  docs ([0006e43](https://github.com/gameguild-gg/website/commit/0006e4312a6b99fb8c8dd3de0a2bd15efa7791b2))

## [1.15.1](https://github.com/gameguild-gg/website/compare/v1.15.0...v1.15.1) (2025-02-07)

### Bug Fixes

* **courses:** fix style on courses
  page ([00c344d](https://github.com/gameguild-gg/website/commit/00c344d710e03a6041941caa17f68ca30cff72c1))

# [1.15.0](https://github.com/gameguild-gg/website/compare/v1.14.1...v1.15.0) (2025-02-06)

### Bug Fixes

* **auth:** fixed
  auth.ts ([ba4d506](https://github.com/gameguild-gg/website/commit/ba4d5064deb15075606efababd8f7b2c971254ed))
* **courses:** fix course
  portfolio ([2d20828](https://github.com/gameguild-gg/website/commit/2d2082831074d15b4c9a922b0d88214d05250d24))
* **courses:** fix portfolio assignment related to github
  readmes ([f5ee71f](https://github.com/gameguild-gg/website/commit/f5ee71f1402f5c8c9d6bdceb75caeee6971d63c0))
* **courses:** github
  readmes ([7bd672e](https://github.com/gameguild-gg/website/commit/7bd672e517e785b4108c017620acdab849d0cb34))
* **eslint:** fixed
  eslint ([e0e9c32](https://github.com/gameguild-gg/website/commit/e0e9c32562c515b85c325d6b15a7bcec3a47558b))
* **monorepo:** fixed eslint, prettier, typescript base
  configs ([0639c94](https://github.com/gameguild-gg/website/commit/0639c94ebf80080aa66eb6c56c3d97985448b742))
* **monorepo:** fixed eslint, prettier, typescript base
  configs ([a152ba8](https://github.com/gameguild-gg/website/commit/a152ba89f6cf31672618a2686a34b1cbf5ada410))
* **typescript:** fixed
  typescript-config ([b89d82f](https://github.com/gameguild-gg/website/commit/b89d82fb36f85f2e3d38457e8fd021f746bac82c))

### Features

* **wasmer:** added wasmer web
  worker ([2e14de9](https://github.com/gameguild-gg/website/commit/2e14de9abd8f36566fa18a5031b74f0125302fc5))

## [1.14.1](https://github.com/gameguild-gg/website/compare/v1.14.0...v1.14.1) (2025-02-05)

### Bug Fixes

* **courses:** add courses hero
  section ([e07a8dc](https://github.com/gameguild-gg/website/commit/e07a8dc4933383d41620b0527c54612efec7233c))

# [1.14.0](https://github.com/gameguild-gg/website/compare/v1.13.8...v1.14.0) (2025-02-05)

### Bug Fixes

* **pyodide:** fixed pyodide web
  worker ([8936377](https://github.com/gameguild-gg/website/commit/893637718f813f5dcb3566692bfa0da2a54804d8))

### Features

* **pyodide:** added small code
  editor ([bda9244](https://github.com/gameguild-gg/website/commit/bda924477fd97e61e85bb19ad1a3fea688a56340))

## [1.13.8](https://github.com/gameguild-gg/website/compare/v1.13.7...v1.13.8) (2025-02-04)

### Bug Fixes

* **courses:** add python quizzes for loops and
  conditionals ([8587729](https://github.com/gameguild-gg/website/commit/85877291920c31e3490497d0ebf6f11bba286e67))

## [1.13.7](https://github.com/gameguild-gg/website/compare/v1.13.6...v1.13.7) (2025-02-04)

### Bug Fixes

* **courses:** add quizzes to the goap
  explanation ([ddad9c2](https://github.com/gameguild-gg/website/commit/ddad9c2f27122fd30eb0d6233ca8795dc4feb5c1))
* **courses:** markdown quiz
  test ([fce53dc](https://github.com/gameguild-gg/website/commit/fce53dc7e13611872240fc171bd5ee8fbb3f77bd))

## [1.13.6](https://github.com/gameguild-gg/website/compare/v1.13.5...v1.13.6) (2025-02-04)

### Bug Fixes

* **courses:** add GOAP
  assignment ([b338737](https://github.com/gameguild-gg/website/commit/b3387370d48711a0156987fd813db4520fbf7224))

## [1.13.5](https://github.com/gameguild-gg/website/compare/v1.13.4...v1.13.5) (2025-02-04)

### Bug Fixes

* **courses:** add goap
  lecture ([aa9c841](https://github.com/gameguild-gg/website/commit/aa9c84152a47228de354b2bab41e7086815029be))
* **style:** rollback style
  linter ([2f8c425](https://github.com/gameguild-gg/website/commit/2f8c4250c50e69902efa02c5a268d6c6302da46a))

## [1.13.4](https://github.com/gameguild-gg/website/compare/v1.13.3...v1.13.4) (2025-02-03)

### Bug Fixes

* **courses:** fix quizes
  renderer ([d22c90d](https://github.com/gameguild-gg/website/commit/d22c90df12ee10c5051aa369f198da71a5587155))

## [1.13.3](https://github.com/gameguild-gg/website/compare/v1.13.2...v1.13.3) (2025-02-03)

### Bug Fixes

* **course:** add python lecture about loops and
  conditionals ([07e9914](https://github.com/gameguild-gg/website/commit/07e9914eb3b4ccf640b668ff52ea442bd6654b3b))

## [1.13.2](https://github.com/gameguild-gg/website/compare/v1.13.1...v1.13.2) (2025-02-03)

### Bug Fixes

* **courses:** add embed quizzes / code.
  wip ([12c4eca](https://github.com/gameguild-gg/website/commit/12c4ecaa9be123f9f2643e4442cdee5a6a485278))

## [1.13.1](https://github.com/gameguild-gg/website/compare/v1.13.0...v1.13.1) (2025-02-02)

### Bug Fixes

* **courses:** add metadata to courses and lectures.
  fix [#104](https://github.com/gameguild-gg/website/issues/104) ([bae8b46](https://github.com/gameguild-gg/website/commit/bae8b46de204b24c1c869a4a2afefa5234832537))

# [1.13.0](https://github.com/gameguild-gg/website/compare/v1.12.2...v1.13.0) (2025-02-01)

### Features

* **analytics:** Added a boilerplate for
  web-vitals ([b81f1c6](https://github.com/gameguild-gg/website/commit/b81f1c69748ccd2c46b56bf1ece0c2b61b13fef6))

## [1.12.2](https://github.com/gameguild-gg/website/compare/v1.12.1...v1.12.2) (2025-02-01)

### Bug Fixes

* **learn:** fix coding environment
  style ([6f2b5b0](https://github.com/gameguild-gg/website/commit/6f2b5b0849add59de3cde6b26c3cdb97b60a17be))
* **packages:** fix package
  location ([280682e](https://github.com/gameguild-gg/website/commit/280682ef3109c6a4f0a9b85d5c6eaa581665b360))

## [1.12.1](https://github.com/gameguild-gg/website/compare/v1.12.0...v1.12.1) (2025-02-01)

### Bug Fixes

* **ci:** github actions are failing with the latest ubuntu
  version ([42221af](https://github.com/gameguild-gg/website/commit/42221afcdbefff080492d0f0667796e355db8aa3))
* **errors:** fix [#14](https://github.com/gameguild-gg/website/issues/14),
  fix [#15](https://github.com/gameguild-gg/website/issues/15) error
  pages ([812e714](https://github.com/gameguild-gg/website/commit/812e714cf80b77e4ffc16e617a3fc4e71f3ff944))
* **routes:**
  redirect ([5c91c8b](https://github.com/gameguild-gg/website/commit/5c91c8bedf811a2a9de5dd4db7394b0ad34da87e))

# [1.12.0](https://github.com/gameguild-gg/website/compare/v1.11.33...v1.12.0) (2025-01-31)

### Bug Fixes

* **contributors:** add a link to the
  stargazers ([d7cfa8f](https://github.com/gameguild-gg/website/commit/d7cfa8f01e71be4634db6a74e2d92e67094ea6ad))
* **course/page:** include carousel
  system ([121c9c1](https://github.com/gameguild-gg/website/commit/121c9c158d5de4a6ab73f8cf68ad86e04620a244))
* **courses:** improve ai engine
  graph ([f56ee32](https://github.com/gameguild-gg/website/commit/f56ee325bbb25d9b85fea504cc98aff297ac165c))
* **courses:** markdown
  content ([9d9e130](https://github.com/gameguild-gg/website/commit/9d9e13058b4bc5a97efa9bf23bd887c062009f49))
* dbml schema and
  swagger ([230de22](https://github.com/gameguild-gg/website/commit/230de22c88ea2150c69438544458ae5f129b1127))
* **edutech:** fix
  types ([b3a788d](https://github.com/gameguild-gg/website/commit/b3a788d8894f669f51d0b3d56b26a36244fcbaa1))
* **edutech:** merge edutech into
  main ([f0e8d53](https://github.com/gameguild-gg/website/commit/f0e8d5373103d4e6733d4e47eba65b31602e5daf))
* fetch courses ([c9d2fe5](https://github.com/gameguild-gg/website/commit/c9d2fe5a75daa83b098e5bc0628d23ab5ff0fab8))
* **header/index:** include 'Courses'
  button ([4a13e3c](https://github.com/gameguild-gg/website/commit/4a13e3c40817cdf058b270ca6f256c9313008323))
* improved code
  generation ([aad0f94](https://github.com/gameguild-gg/website/commit/aad0f94cfd0977007bd6d7962441864a93aae6f2))
* **learn:** "add run python
  language" ([c937a5d](https://github.com/gameguild-gg/website/commit/c937a5da541af809ea27951b71d770c142c56767))
* **learn:** "layouts
  visible" ([fdb939d](https://github.com/gameguild-gg/website/commit/fdb939d50908d80153d9555ae4d2a1dae6457848))
* **learn:** 3s to local save and 'addinput' to add input to
  code ([55e5062](https://github.com/gameguild-gg/website/commit/55e506263e3d5d6a459a724c8894e74cfb9fbc5c))
* **learn:** add filter
  submissions ([c8c7f2f](https://github.com/gameguild-gg/website/commit/c8c7f2ff460b0dfb9667131cb2452d8615317868))
* **learn:** build
  run ([6ae9a6c](https://github.com/gameguild-gg/website/commit/6ae9a6c9dd7111b11af20ccc56825d2bd79ac62c))
* **learn:** code flux
  adjusts ([8442742](https://github.com/gameguild-gg/website/commit/8442742a7298973bce585e5fb769edb1a84ec03f))
* **learn:** fix
  layouts ([bfb29f7](https://github.com/gameguild-gg/website/commit/bfb29f74a28250b28ea96884113f956ac7b94607))
* **learn:** fix
  links ([cefd547](https://github.com/gameguild-gg/website/commit/cefd5471afc484a05cc2a1ab4d66db69ea047814))
* **learn:** menu links for Learn and Code also page
  construction ([4340d50](https://github.com/gameguild-gg/website/commit/4340d501c6f06842352d26568265dd72f3e18923))
* **learn:** more
  fixes ([e9061d2](https://github.com/gameguild-gg/website/commit/e9061d2c2b1006651fef0592753f3ad09072b1dc))
* **learn:** python run message and initial implementation to run
  Ruby ([3d3ffd4](https://github.com/gameguild-gg/website/commit/3d3ffd4d0536da99645855439906b67a1d080409))
* **merge:** fixing some merging
  issues ([6bd64c3](https://github.com/gameguild-gg/website/commit/6bd64c3200c9aceb6d2d0697116f52399ec5537b))
* **projects:** fix project
  creation ([d0fff66](https://github.com/gameguild-gg/website/commit/d0fff66468214d79858bf5cb710f8db4d33ba91e))
* **routing:** fix recursive routing
  issue ([6bee09d](https://github.com/gameguild-gg/website/commit/6bee09d44dc6048acecdf539a507ef90f9ac3b9e))

### Features

* **code:** new feature added code editor. it is wip, but it is starting to get
  stable ([9da6d5d](https://github.com/gameguild-gg/website/commit/9da6d5dd747c179c79a4960e394297a47ae3b9fe))
* **courses:** add grading instructions to
  quiz ([be67c64](https://github.com/gameguild-gg/website/commit/be67c64d6a4fbab552da833b32af2fdecb195ceb))
* **courses:** courses api
  base ([107ed9f](https://github.com/gameguild-gg/website/commit/107ed9fd9154bd95acbf595951c0e0552803e82f))
* **courses:** courses api
  base ([4ebc47a](https://github.com/gameguild-gg/website/commit/4ebc47a50e488b7006e842623f098bcc99354cfc))
* **quiz:** boilerplate for the quiz
  api ([471779e](https://github.com/gameguild-gg/website/commit/471779e4ea9949057a23864bc7f12e42a25bd1c5))

## [1.11.33](https://github.com/gameguild-gg/website/compare/v1.11.32...v1.11.33) (2025-01-31)

### Bug Fixes

* **routing:** trying to fix the issue with multiple
  domains ([3c81465](https://github.com/gameguild-gg/website/commit/3c81465c0727f7b8dcf74525cb5d9deefccc9159))

## [1.11.32](https://github.com/gameguild-gg/website/compare/v1.11.31...v1.11.32) (2025-01-31)

### Bug Fixes

* **courses:** add missing cstdint on the a-star
  example ([f955c59](https://github.com/gameguild-gg/website/commit/f955c59e57d1b29358126168d9c86110666d3874))

## [1.11.31](https://github.com/gameguild-gg/website/compare/v1.11.30...v1.11.31) (2025-01-31)

### Bug Fixes

* **courses:** add a-star
  content ([5276fcd](https://github.com/gameguild-gg/website/commit/5276fcd5d165852986304c1b5f18d525c721db43))

## [1.11.30](https://github.com/gameguild-gg/website/compare/v1.11.29...v1.11.30) (2025-01-31)

### Bug Fixes

* **courses:** add a-star
  content ([559ce3f](https://github.com/gameguild-gg/website/commit/559ce3f43549b51fd78fb56faa6283ab7cdde53d))
* **courses:** add a-star
  content ([440e512](https://github.com/gameguild-gg/website/commit/440e51287e8981915932581069cbce59e04295fa))

## [1.11.29](https://github.com/gameguild-gg/website/compare/v1.11.28...v1.11.29) (2025-01-30)

### Bug Fixes

* **courses:** added generateMetadata and SSR as possible on the courses
  pages. ([afcefd4](https://github.com/gameguild-gg/website/commit/afcefd4f043d483b1854a84e0e2830c2282d2830))

## [1.11.28](https://github.com/gameguild-gg/website/compare/v1.11.27...v1.11.28) (2025-01-29)

### Bug Fixes

* **courses:** portfolio assignment
  reference ([52e2108](https://github.com/gameguild-gg/website/commit/52e21086160835046532b244e7784c12728f859e))

## [1.11.27](https://github.com/gameguild-gg/website/compare/v1.11.26...v1.11.27) (2025-01-29)

### Bug Fixes

* **courses:** linkedin
  content ([659afa8](https://github.com/gameguild-gg/website/commit/659afa8fff3c1ecc8cb018718fdb69837fa32f1c))

## [1.11.26](https://github.com/gameguild-gg/website/compare/v1.11.25...v1.11.26) (2025-01-28)

### Bug Fixes

* **institutional:** delete deprecated
  pages ([11dc136](https://github.com/gameguild-gg/website/commit/11dc136b018501b569ae04488eedaa552a383bfb))

## [1.11.25](https://github.com/gameguild-gg/website/compare/v1.11.24...v1.11.25) (2025-01-28)

### Bug Fixes

* **courses:** fix [#97](https://github.com/gameguild-gg/website/issues/97) double scroll bars on courses
  layout ([e547d5c](https://github.com/gameguild-gg/website/commit/e547d5c4c333b65eab5cc177496b31ecb6273213))

## [1.11.24](https://github.com/gameguild-gg/website/compare/v1.11.23...v1.11.24) (2025-01-28)

### Bug Fixes

* **courses:** add game ai
  lectures ([2d3303f](https://github.com/gameguild-gg/website/commit/2d3303ffaf120c5d4fe1082dc34795ff6eaa3ced))

## [1.11.23](https://github.com/gameguild-gg/website/compare/v1.11.22...v1.11.23) (2025-01-27)

### Bug Fixes

* **courses:** add python week03
  lectures ([cbe496a](https://github.com/gameguild-gg/website/commit/cbe496abc1fd45ff6431cdc565c9a347fe3066de))

## [1.11.22](https://github.com/gameguild-gg/website/compare/v1.11.21...v1.11.22) (2025-01-24)

### Bug Fixes

* **deploy:** hack to ensure new versions are being published
  properly ([c1b1d50](https://github.com/gameguild-gg/website/commit/c1b1d50fa4a2ef26192b6ed50eb018253628bbfb))

## [1.11.21](https://github.com/gameguild-gg/website/compare/v1.11.20...v1.11.21) (2025-01-24)

### Bug Fixes

* **courses:** code is now soft wrapping
  properly ([17f18bf](https://github.com/gameguild-gg/website/commit/17f18bfe80104624c5330dd6fb309d37a50a0c27))
* **courses:** fix minor typo on the python
  courses ([b0db6be](https://github.com/gameguild-gg/website/commit/b0db6bee7e604e0ae1f78b0ef9b07d05c4e315e5))

## [1.11.20](https://github.com/gameguild-gg/website/compare/v1.11.19...v1.11.20) (2025-01-24)

### Bug Fixes

* **courses:** improve syntax highlight on the markdown
  renderer ([07dd3bc](https://github.com/gameguild-gg/website/commit/07dd3bc5288870a015a04e4c6d5f0af0f1cfe0a6))

## [1.11.19](https://github.com/gameguild-gg/website/compare/v1.11.18...v1.11.19) (2025-01-24)

### Bug Fixes

* **blog:** fix [#91](https://github.com/gameguild-gg/website/issues/91) blog renderer, now it will redirect to the
  proper domain ([aa6a212](https://github.com/gameguild-gg/website/commit/aa6a212ca3136ea1929ca205f3fdfc6ace96483a))

## [1.11.18](https://github.com/gameguild-gg/website/compare/v1.11.17...v1.11.18) (2025-01-24)

### Bug Fixes

* **courses:** add syntax highlight to
  reveal ([73625ec](https://github.com/gameguild-gg/website/commit/73625ecea482392de9ebd98b3b63ea4822124ff9))

## [1.11.17](https://github.com/gameguild-gg/website/compare/v1.11.16...v1.11.17) (2025-01-24)

### Bug Fixes

* **courses:** reveal is now
  markdown ([f805d99](https://github.com/gameguild-gg/website/commit/f805d9978a09cedeadde649728e4eef3ca9e4b12))

## [1.11.16](https://github.com/gameguild-gg/website/compare/v1.11.15...v1.11.16) (2025-01-24)

### Bug Fixes

* **courses:** fix full screen
  presentations ([24d06e9](https://github.com/gameguild-gg/website/commit/24d06e9138a8374a5ca600eb274f0f3bd15cf2bb))

## [1.11.15](https://github.com/gameguild-gg/website/compare/v1.11.14...v1.11.15) (2025-01-24)

### Bug Fixes

* **courses:** fix reveal full screen and
  navigation ([a4d7acb](https://github.com/gameguild-gg/website/commit/a4d7acba7e52b7f16ee54f2cf48647e68eaa449c))

## [1.11.14](https://github.com/gameguild-gg/website/compare/v1.11.13...v1.11.14) (2025-01-24)

### Bug Fixes

* **courses:** better
  sidebar ([6724d65](https://github.com/gameguild-gg/website/commit/6724d65e9b25e6b7b3c1fdd6db717346365664e0))

## [1.11.13](https://github.com/gameguild-gg/website/compare/v1.11.12...v1.11.13) (2025-01-23)

### Bug Fixes

* **courses:** add python week 2
  class ([7f0b888](https://github.com/gameguild-gg/website/commit/7f0b888c460a3bf747a3df2de610d147117940af))
* **courses:** markdown
  renderer ([d469025](https://github.com/gameguild-gg/website/commit/d469025a502bb57149375f50117c57b0166f0ee5))

## [1.11.12](https://github.com/gameguild-gg/website/compare/v1.11.11...v1.11.12) (2025-01-22)

### Bug Fixes

* **courses:** add link to figma
  templates ([93eda9b](https://github.com/gameguild-gg/website/commit/93eda9b842138482aa0951a700300e1d4f8d3f90))

## [1.11.11](https://github.com/gameguild-gg/website/compare/v1.11.10...v1.11.11) (2025-01-22)

### Bug Fixes

* **courses:** add one more assignment to portfolio
  classes ([4a45581](https://github.com/gameguild-gg/website/commit/4a45581f2baa91bdbd1bcf580518e0417ad06c6c))
* **courses:** add portfolio class
  week02 ([93786bd](https://github.com/gameguild-gg/website/commit/93786bd47d138ef282d64d9cdfb8838fe27b19ee))
* **legal:** html to markdown
  text ([bd3ebe1](https://github.com/gameguild-gg/website/commit/bd3ebe1c59ca16c2774bd4bad51b7aadbe36f1fa))
* **legal:** html to markdown
  text ([7cfba9d](https://github.com/gameguild-gg/website/commit/7cfba9dd787def1b96aa480ab71826e7e43806ef))
* **legal:** links and new
  terms ([6408d82](https://github.com/gameguild-gg/website/commit/6408d82395d5e3ebd7449c02f7bcd04f5f7cf3ea))
* **legal:** links and new
  terms ([8c382e0](https://github.com/gameguild-gg/website/commit/8c382e08ed01fc97e0454959097a71c806e57add))

## [1.11.10](https://github.com/gameguild-gg/website/compare/v1.11.9...v1.11.10) (2025-01-21)

### Bug Fixes

* **courses:** rename type to
  renderer ([0371358](https://github.com/gameguild-gg/website/commit/0371358b086954a5c1d11bf9915035638e3d20d9))

## [1.11.9](https://github.com/gameguild-gg/website/compare/v1.11.8...v1.11.9) (2025-01-21)

### Bug Fixes

* **header:** fix header institutional
  links ([67240f4](https://github.com/gameguild-gg/website/commit/67240f4b048d42a5ed139dc2022dc0289831f5be))

## [1.11.8](https://github.com/gameguild-gg/website/compare/v1.11.7...v1.11.8) (2025-01-21)

### Bug Fixes

* **courses:** add wave function
  collapse ([84da4ab](https://github.com/gameguild-gg/website/commit/84da4abcf374eed2d0ce1be8c17499fa7bf4f3f6))

## [1.11.7](https://github.com/gameguild-gg/website/compare/v1.11.6...v1.11.7) (2025-01-20)

### Bug Fixes

* **courses:** add pcg
  presentation ([f849a5b](https://github.com/gameguild-gg/website/commit/f849a5bd469df231c98e41e11c6e5a17f27751fc))

## [1.11.6](https://github.com/gameguild-gg/website/compare/v1.11.5...v1.11.6) (2025-01-20)

### Bug Fixes

* **courses:** fix support to revealjs
  presentation ([780a0f5](https://github.com/gameguild-gg/website/commit/780a0f5fbb205199412e42dec3f02c96d77044d8))

## [1.11.5](https://github.com/gameguild-gg/website/compare/v1.11.4...v1.11.5) (2025-01-20)

### Bug Fixes

* **courses:** add lecture type to be able to switch between render reveal and
  markdown ([904de04](https://github.com/gameguild-gg/website/commit/904de045df95006721a985893a7a14676ac54445))
* **courses:** render with
  revealjs ([72edea0](https://github.com/gameguild-gg/website/commit/72edea069b73ba20120978eef4136a5503b803d7))

## [1.11.4](https://github.com/gameguild-gg/website/compare/v1.11.3...v1.11.4) (2025-01-20)

### Bug Fixes

* **contributors:** improve contributor loc/contrib
  counter ([a52ebd3](https://github.com/gameguild-gg/website/commit/a52ebd33b139a9853f2c92087c9c54ee3cf599cf))

## [1.11.3](https://github.com/gameguild-gg/website/compare/v1.11.2...v1.11.3) (2025-01-20)

### Bug Fixes

* **homepage:** fix homepage layout.
  fix [#89](https://github.com/gameguild-gg/website/issues/89) ([b34e8bd](https://github.com/gameguild-gg/website/commit/b34e8bd84a22f0d6a998d731e2c7ed5a9650b7b0))

## [1.11.2](https://github.com/gameguild-gg/website/compare/v1.11.1...v1.11.2) (2025-01-19)

### Bug Fixes

* **courses:** python
  course ([205eaf0](https://github.com/gameguild-gg/website/commit/205eaf0076216a98f70c73e6e513439e2f0010e0))

## [1.11.1](https://github.com/gameguild-gg/website/compare/v1.11.0...v1.11.1) (2025-01-19)

### Bug Fixes

* **courses:** ai courses
  week01 ([f8ebd50](https://github.com/gameguild-gg/website/commit/f8ebd50776ee231a496efdadfd9f822970505e8f))

# [1.11.0](https://github.com/gameguild-gg/website/compare/v1.10.9...v1.11.0) (2025-01-18)

### Bug Fixes

* ci workflows ([fdd3610](https://github.com/gameguild-gg/website/commit/fdd36104cd2947bcce2de8fcc81a278294d66bdc))
* **contributors:** add ratio of LoC and
  contributions ([8b3be0c](https://github.com/gameguild-gg/website/commit/8b3be0c764e6eb03ae4a1b3522105a284dcf7854))
* **courses:** add courses
  material ([b6a1ceb](https://github.com/gameguild-gg/website/commit/b6a1ceb6908c01fc58b2e8d884a442f9f0d1f821))
* **courses:** collapsible sections on course
  navigation ([491a187](https://github.com/gameguild-gg/website/commit/491a1870a0966e43033c88641cf46350a087d751))
* **courses:** fix ai course syllabus and add
  lecture ([aaab8a2](https://github.com/gameguild-gg/website/commit/aaab8a23666c10df6fc22a34fa2462ae7d112a71))
* **portfolio:** render properly the portfolio
  pages ([d4086ce](https://github.com/gameguild-gg/website/commit/d4086cec1c9d0ddae219e407fe08e170f1b049ed))
* **python:** add python
  content ([5c127a1](https://github.com/gameguild-gg/website/commit/5c127a16661ab62cde2b42ab03689f4c4b11f0c9))

### Features

* **portfolio:** add new course entry:
  portfolio ([871eaf9](https://github.com/gameguild-gg/website/commit/871eaf9df234ce435c7f834623b82c9ce7f21bc0))

## [1.10.9](https://github.com/gameguild-gg/website/compare/v1.10.8...v1.10.9) (2025-01-15)

### Bug Fixes

* **apiclient:** regenerate api
  client ([37a5f00](https://github.com/gameguild-gg/website/commit/37a5f001cf6e4dcb8099937760a986613f99fb3d))

## [1.10.8](https://github.com/gameguild-gg/website/compare/v1.10.7...v1.10.8) (2025-01-14)

### Bug Fixes

* **contributors:** better filtering to remove some generated
  files ([540f465](https://github.com/gameguild-gg/website/commit/540f465a6e195e54763ceb2c237b8d620188dd26))

## [1.10.7](https://github.com/gameguild-gg/website/compare/v1.10.6...v1.10.7) (2025-01-14)

### Bug Fixes

* **contributors:** improve line
  count ([45b5abb](https://github.com/gameguild-gg/website/commit/45b5abbde0df91e4c812bfd1c336e0efb7c1bf8a))
* **courses:** ai
  image ([33f0b72](https://github.com/gameguild-gg/website/commit/33f0b728760d29eeda27aa3dc9742c88d777bb58))

## [1.10.6](https://github.com/gameguild-gg/website/compare/v1.10.5...v1.10.6) (2025-01-13)

### Bug Fixes

* **issues:** fix bug report
  link ([1e1e18d](https://github.com/gameguild-gg/website/commit/1e1e18d6674e04b523629649c1e0ee22618991f5))

## [1.10.5](https://github.com/gameguild-gg/website/compare/v1.10.4...v1.10.5) (2025-01-13)

### Bug Fixes

* **mermaid:** fix mermaid
  renderer ([fec50d8](https://github.com/gameguild-gg/website/commit/fec50d8b8dae93d2ad5208c1ed08438feebc80e5))

## [1.10.4](https://github.com/gameguild-gg/website/compare/v1.10.3...v1.10.4) (2025-01-13)

### Bug Fixes

* **mermaid:** mermaid
  issue ([5de3658](https://github.com/gameguild-gg/website/commit/5de3658444d32d597a4fcadea53e503a3bedea1e))

## [1.10.3](https://github.com/gameguild-gg/website/compare/v1.10.2...v1.10.3) (2025-01-13)

### Bug Fixes

* **code-highlight:** improved code
  higlight ([bf39613](https://github.com/gameguild-gg/website/commit/bf39613a72722864873b68c5448df65452bdd7ea))

## [1.10.2](https://github.com/gameguild-gg/website/compare/v1.10.1...v1.10.2) (2025-01-13)

### Bug Fixes

* replacing video with optimized
  Next/Image ([bdf68e3](https://github.com/gameguild-gg/website/commit/bdf68e36bd23efb5326b8c4a9e14cbde99fbfdbc))

## [1.10.1](https://github.com/gameguild-gg/website/compare/v1.10.0...v1.10.1) (2025-01-12)

### Bug Fixes

* **markdown:** add markdown support for
  courses ([e349609](https://github.com/gameguild-gg/website/commit/e3496097c55f32d4c45c6f9635f1641c50499e50))

# [1.10.0](https://github.com/gameguild-gg/website/compare/v1.9.1...v1.10.0) (2025-01-12)

### Features

* **markdown:** add support of local markdown files via
  raw-loader ([a9424eb](https://github.com/gameguild-gg/website/commit/a9424eb6256dfdd027c8574cd40f8fe65f397153))

## [1.9.1](https://github.com/gameguild-gg/website/compare/v1.9.0...v1.9.1) (2025-01-12)

### Bug Fixes

* **domain:** fix domain
  url ([33c43aa](https://github.com/gameguild-gg/website/commit/33c43aa85674c47bdffff381513a1e21c7e21962))

# [1.9.0](https://github.com/gameguild-gg/website/compare/v1.8.3...v1.9.0) (2025-01-11)

### Bug Fixes

* **courses:** remove old style courses without
  permissions ([3a57f68](https://github.com/gameguild-gg/website/commit/3a57f68c98f304498b6c76b7412c2d3a77d2c542))

### Features

* **courses:** minimum functioning courses
  webpage ([fd9b081](https://github.com/gameguild-gg/website/commit/fd9b081a40ba20983e6d5608cef331737cb4ee6a))

## [1.8.3](https://github.com/gameguild-gg/website/compare/v1.8.2...v1.8.3) (2025-01-11)

### Bug Fixes

* **contributors:** add a link to the
  stargazers ([6ed6cf4](https://github.com/gameguild-gg/website/commit/6ed6cf4048eb0860db02258870011018b06f7a86))

## [1.8.2](https://github.com/gameguild-gg/website/compare/v1.8.1...v1.8.2) (2025-01-10)

### Bug Fixes

* **issues:** move server version to an
  api ([9da9ced](https://github.com/gameguild-gg/website/commit/9da9ced39e40185837482c7070c1e518424f86a0))

## [1.8.1](https://github.com/gameguild-gg/website/compare/v1.8.0...v1.8.1) (2025-01-10)

### Bug Fixes

* **issues:** improve issues
  reporting ([f2ccc9e](https://github.com/gameguild-gg/website/commit/f2ccc9e02da2b368b21daf459b1ab36ec37379fc))

# [1.8.0](https://github.com/gameguild-gg/website/compare/v1.7.0...v1.8.0) (2025-01-09)

### Features

* **issues): Add feedback button. fix(dynamic-pages:** add force-dynamic to some
  pages ([039bff5](https://github.com/gameguild-gg/website/commit/039bff5ee95e5d0c99eb91b9ee17e5385066fd3e))

# [1.7.0](https://github.com/gameguild-gg/website/compare/v1.6.0...v1.7.0) (2025-01-08)

### Features

* **issues:** add better links to issue generation on
  github ([c4de113](https://github.com/gameguild-gg/website/commit/c4de113824eb17fbe19228b66de30f0842498dbe))

# [1.6.0](https://github.com/gameguild-gg/website/compare/v1.5.3...v1.6.0) (2025-01-08)

### Bug Fixes

* **issues:** fix dynamic
  api ([4e248b2](https://github.com/gameguild-gg/website/commit/4e248b23dd57ce1192604023faffb743da8500aa))
* **issues:** fix github issues
  page ([89a71fe](https://github.com/gameguild-gg/website/commit/89a71fe299a427aa666177137f2ce26645c4ca91))
* **issues:** github issues
  footer ([f35763f](https://github.com/gameguild-gg/website/commit/f35763fb8036ef1b310b022a8f16cdffffef0f5f))

### Features

* **issues:** add issues page to track the project progress
  publicly ([0b13d49](https://github.com/gameguild-gg/website/commit/0b13d49f8cf546596f2e4a054f2bf2f75f313fc3))

## [1.5.3](https://github.com/gameguild-gg/website/compare/v1.5.2...v1.5.3) (2025-01-08)

### Bug Fixes

* **contributors:** add better metadata generation to get the right
  domain ([29c848c](https://github.com/gameguild-gg/website/commit/29c848cb6f1acbcbe8dddac1d999a3ef0dc81058))

## [1.5.2](https://github.com/gameguild-gg/website/compare/v1.5.1...v1.5.2) (2025-01-08)

### Bug Fixes

* **contributors:** add video gource to the contributors
  page ([6e985ad](https://github.com/gameguild-gg/website/commit/6e985ad62aff3188491f7daa13486ba0ea961ac5))
* **headers:** add institutional
  headers ([6173c16](https://github.com/gameguild-gg/website/commit/6173c16fd14bb053b4dd2af7b6c70f5131c8b257))

## [1.5.1](https://github.com/gameguild-gg/website/compare/v1.5.0...v1.5.1) (2025-01-07)

### Bug Fixes

* **contributors:** add gource to
  contributors ([f5ac100](https://github.com/gameguild-gg/website/commit/f5ac100cd4940e47e835856da134bcee377b2dd6))
* **footer:** remove cookies consent for
  now ([899aa36](https://github.com/gameguild-gg/website/commit/899aa36d6de6c91f7653696a9b0a3a3168cde511))

# [1.5.0](https://github.com/gameguild-gg/website/compare/v1.4.11...v1.5.0) (2025-01-07)

### Bug Fixes

* **api:** fix minor typo preventing api
  build ([cdd07d2](https://github.com/gameguild-gg/website/commit/cdd07d2592034e814591e729daf4600e3f01d829))
* **asset:** minio port
  config ([e47cd83](https://github.com/gameguild-gg/website/commit/e47cd83636a1866da231f656d8b0a9404d42734a))
* **asset:** now data can be stored properly on S3 compatible api such as
  minio ([c48cd57](https://github.com/gameguild-gg/website/commit/c48cd57bf6d1b66f78144e3cde502ae0a3e257df))
* **assets:** asset key
  var ([b839353](https://github.com/gameguild-gg/website/commit/b83935372a67373708422d05f62a663ab61449ad))
* **auth:** hack to avoid infinite redirect. increase the token life
  time ([f0ac1dc](https://github.com/gameguild-gg/website/commit/f0ac1dc7b0a34f0f7929fd8d05877a670c849ce3))
* **contributors:** add cache manager and fix contributor
  card ([1d77891](https://github.com/gameguild-gg/website/commit/1d778911789509747dc76497f4bc77ff540403b2))
* **contributors:** add meta
  tags ([68d1394](https://github.com/gameguild-gg/website/commit/68d1394cba23b09bccdc82e9aa0078eb90d4fea3))
* logout ([347074a](https://github.com/gameguild-gg/website/commit/347074aab4220e65ffd023c2cc25be48cb353924))
* **metadata:** improved metadata generation for
  contributors ([2c72611](https://github.com/gameguild-gg/website/commit/2c72611c21b167da056c4cf9efad82533ec2ff63))
* **pathmap:** remove function to generate map of paths from the
  web ([a99fed8](https://github.com/gameguild-gg/website/commit/a99fed80e16850e7e7db053e2ed3e6ec003b63b7))
* **projects:** fix project
  creation ([46e2afe](https://github.com/gameguild-gg/website/commit/46e2afe8fe27aeae0d0d796b69e188b357aa895f))
* **swagger:** lazy load swagger generation and api update in order to speedup the nest
  startup ([588a995](https://github.com/gameguild-gg/website/commit/588a995cd39e9481f0ceeb2e62cdbb255983ad59))
* **types:** add routes to change profile
  picture ([e547b40](https://github.com/gameguild-gg/website/commit/e547b40100f5fb63619b369ed2126dd5ecccefc6))
* **types:** fix some random type issues preventing to
  compile ([20f0a4c](https://github.com/gameguild-gg/website/commit/20f0a4c04eaf9be2ca4d26d896da7f6aa38fd56b))

### Features

* **asset:** add minio to
  docker-compose ([dd68f12](https://github.com/gameguild-gg/website/commit/dd68f1281d4431616c4fd65b091adc8140322baa))
* **asset:** profile image api
  storage ([d4431cc](https://github.com/gameguild-gg/website/commit/d4431cce676d044d5cf35764f564da930e56f52c))
* **assets:** api rework to accept multiple file
  upload ([3321dba](https://github.com/gameguild-gg/website/commit/3321dbacd5662fcec3562f3a8e35771b6e09dc2d))
* **assets:** asset management brainstorm and
  scaffold ([d3bbc13](https://github.com/gameguild-gg/website/commit/d3bbc13030ad4cc5a08579070dfb31f7c10dedbb))
* **assets:** enable data compression
  gzip ([329bb97](https://github.com/gameguild-gg/website/commit/329bb974c8421842aeed8f6ca3a024b3cdab528a))
* **assets:** upload base
  function ([2c29dbf](https://github.com/gameguild-gg/website/commit/2c29dbf3f0d31a77c3e954939000aa79b6a44244))
* **contributors:** add details to code
  contributors ([096f832](https://github.com/gameguild-gg/website/commit/096f832e68c1f468ce1776bb762b415664ae6086))
* **courses:** courses
  api ([135c749](https://github.com/gameguild-gg/website/commit/135c74954d243c98b8c9ce0eef13eb276f1e3dc9))
* **funding:** add funding descriptions and
  options ([52c4f9e](https://github.com/gameguild-gg/website/commit/52c4f9e396b8f8d73f5ede6b51bcdb076c173870))
* **license:** add dual-licencing
  model ([ee492f6](https://github.com/gameguild-gg/website/commit/ee492f69fcbe248cb9ff00aba991a54159197013))
* **minio:** add minio control
  functions ([9aba488](https://github.com/gameguild-gg/website/commit/9aba488202e448b8a30dea6ca684a8fb76c153d7))
* **profile:** better edit profile validation
  fields ([a3254e1](https://github.com/gameguild-gg/website/commit/a3254e103848d2337cf94d99ce21bb0d1f464e5c))
* **profile:** improvements on profile edit in order to be able to change profile
  picture ([0686b83](https://github.com/gameguild-gg/website/commit/0686b83dc43e0b9629746dda1060affda875cd89))
* **project:** content base thumbnail is now an
  ImageEntity ([8f60445](https://github.com/gameguild-gg/website/commit/8f604457f0af07294ca7c55bb49a52d31c0bfaa7))
* **projects:** new interface for projects submission
  form ([3863a4e](https://github.com/gameguild-gg/website/commit/3863a4e198c735da08d480ca06cdb50401cdb0a4))

## [1.4.11](https://github.com/gameguild-gg/website/compare/v1.4.10...v1.4.11) (2025-01-07)

### Bug Fixes

* **auth:** hack to avoid infinite redirect. increase the token life
  time ([d2f024e](https://github.com/gameguild-gg/website/commit/d2f024e2593b8105c287920376741673008bd412))

## [1.4.10](https://github.com/gameguild-gg/website/compare/v1.4.9...v1.4.10) (2025-01-06)

### Bug Fixes

* **contributors:** sort contributors by
  LoC ([2326dcc](https://github.com/gameguild-gg/website/commit/2326dccf53789ae967de6216629e9b3cb14fe618))

## [1.4.9](https://github.com/gameguild-gg/website/compare/v1.4.8...v1.4.9) (2025-01-06)

### Bug Fixes

* **gource:** improve gif
  generation ([ea3014d](https://github.com/gameguild-gg/website/commit/ea3014d672391305c39c9d8e96023249900faf2a))

## [1.4.8](https://github.com/gameguild-gg/website/compare/v1.4.7...v1.4.8) (2025-01-06)

### Bug Fixes

* **gource:** add gif to
  gource ([70ab3d4](https://github.com/gameguild-gg/website/commit/70ab3d4d8192e01973099dc04878dbd5e1fa56fb))

## [1.4.7](https://github.com/gameguild-gg/website/compare/v1.4.6...v1.4.7) (2025-01-06)

### Bug Fixes

* **readme:** add star
  history ([e5d9321](https://github.com/gameguild-gg/website/commit/e5d9321640c66cf8a628f504acba1cc1cc8f8ea8))

## [1.4.6](https://github.com/gameguild-gg/website/compare/v1.4.5...v1.4.6) (2025-01-06)

### Bug Fixes

* **contributors:** try fix git processing on
  backend ([9914ef7](https://github.com/gameguild-gg/website/commit/9914ef7543ba9b6e3ef6149744c4f98ab37f0d46))

## [1.4.5](https://github.com/gameguild-gg/website/compare/v1.4.4...v1.4.5) (2025-01-06)

### Bug Fixes

* **contributors:** allow .git folder to persist on docker so the api can generate
  statistics ([c0ae1e1](https://github.com/gameguild-gg/website/commit/c0ae1e160544e8795785b1ca5e42e4a391e6b631))
* **dbml:** generate dbml sorted to maintain
  consistency ([c96b6c3](https://github.com/gameguild-gg/website/commit/c96b6c30e00ea1dfa11de45df8cc295069d9b670))

## [1.4.4](https://github.com/gameguild-gg/website/compare/v1.4.3...v1.4.4) (2025-01-06)

### Bug Fixes

* **contributors:** add LoC on contributors
  page ([98d4dae](https://github.com/gameguild-gg/website/commit/98d4daed8cdea4b50a38eca8cd79f57f23822f31))

## [1.4.3](https://github.com/gameguild-gg/website/compare/v1.4.2...v1.4.3) (2025-01-06)

### Bug Fixes

* private route dynamic
  SSR ([5abfbd8](https://github.com/gameguild-gg/website/commit/5abfbd86b448b3844813d49a44e012c1db3bc223))
* removed _app.tsx ([037625a](https://github.com/gameguild-gg/website/commit/037625a7fe9a6c4b2b1e573c8ce4fe555b131106))

## [1.4.2](https://github.com/gameguild-gg/website/compare/v1.4.1...v1.4.2) (2025-01-06)

### Bug Fixes

* **jobs:** fix jobs
  build ([5dbbe82](https://github.com/gameguild-gg/website/commit/5dbbe82f100803a0f8eebcfae170f80ba9ed0968))

## [1.4.1](https://github.com/gameguild-gg/website/compare/v1.4.0...v1.4.1) (2024-12-28)

### Bug Fixes

* **feed:** try to fix bug "Page changed from static to dynamic at
  runtime" ([8b164de](https://github.com/gameguild-gg/website/commit/8b164deffcc8b2ed3a3401318912125e66f45d04))

# [1.4.0](https://github.com/gameguild-gg/website/compare/v1.3.3...v1.4.0) (2024-12-27)

### Features

* **contributors:** add contributors
  page ([d0df3a4](https://github.com/gameguild-gg/website/commit/d0df3a4849b0c822275ddf3545169c56e7ee0f07))

## [1.3.3](https://github.com/gameguild-gg/website/compare/v1.3.2...v1.3.3) (2024-12-26)

### Bug Fixes

* revert middleware ([d79a0b6](https://github.com/gameguild-gg/website/commit/d79a0b621e11039fe3f0446b987c19e7601f8820))

## [1.3.2](https://github.com/gameguild-gg/website/compare/v1.3.1...v1.3.2) (2024-12-09)

### Bug Fixes

* improved code
  generation ([e12c746](https://github.com/gameguild-gg/website/commit/e12c746d07493e8bd357a8da177b28a78025ff41))
* withAuth HOC ([438a534](https://github.com/gameguild-gg/website/commit/438a5342466e4681c16af00e40f460331b2c8f7d))

## [1.3.1](https://github.com/gameguild-gg/website/compare/v1.3.0...v1.3.1) (2024-11-11)

### Bug Fixes

* private layout ([e09a953](https://github.com/gameguild-gg/website/commit/e09a953be75e9e4650b9bbda9e241e8dd6430be7))

# [1.3.0](https://github.com/gameguild-gg/website/compare/v1.2.0...v1.3.0) (2024-11-04)

### Bug Fixes

* add <Header> to <SessionProvider> to get user information like avatar on the
  header. ([c7602d4](https://github.com/gameguild-gg/website/commit/c7602d4647c1dba39537f4fb2a8a61c57f630205))
* browse jobs page minor
  adjustments ([663dad7](https://github.com/gameguild-gg/website/commit/663dad721929b7d56f825fea8e6497a42158f041))
* missin JobApplicationEntity properties on frontend
  api ([f802e3b](https://github.com/gameguild-gg/website/commit/f802e3b15b9ca7009cbb60b560e502775c6a1d11))
* move edit profile
  page ([b5e80fc](https://github.com/gameguild-gg/website/commit/b5e80fc72a05b5c8ad2e61a78d9dcb78de6716e8))
* post-merge fixes ([dc9b086](https://github.com/gameguild-gg/website/commit/dc9b0860684f2161e4d9af19819f3535d69901c9))
* session authentication
  errors ([fc50e1c](https://github.com/gameguild-gg/website/commit/fc50e1cb83c4e84a759774658479c669ae1d97a6))
* withdrawn collumn
  migration ([10ef688](https://github.com/gameguild-gg/website/commit/10ef688e51fc254839c4cfddbaab8528387bde83))
* wrong job application information on job
  board ([016800b](https://github.com/gameguild-gg/website/commit/016800ba613d6529a9a6123e76afa17758d6ce3e))

### Features

* 2 buttons per card on 'my applications'
  page ([2237daf](https://github.com/gameguild-gg/website/commit/2237dafcc0fa250ac171f5f91ed6b92660c06001))
* all job applications page now uses real
  data ([c30e24e](https://github.com/gameguild-gg/website/commit/c30e24e6c38b7dd2d9455f32aa241242bfd1606b))
* check many job applications
  page ([3a4f30b](https://github.com/gameguild-gg/website/commit/3a4f30b9027948cd7aef5460dea9927249a7ef79))
* edit profile page ([105cce5](https://github.com/gameguild-gg/website/commit/105cce543227cb77bcb5ae37ff2b64fe9f2c68b6))
* job applicant management
  page ([63ae9b2](https://github.com/gameguild-gg/website/commit/63ae9b26b99284ae83c25e42dd5f8a3212067ba8))
* job application withdraw feature on
  backend ([739b5ad](https://github.com/gameguild-gg/website/commit/739b5adfd342bf66886a8fc28ca669fb1e995644))
* my application page basic backend
  interaction ([74b9ddb](https://github.com/gameguild-gg/website/commit/74b9ddb5ecdc2af95690df9c44fb5713e995f9b5))
* my individual job application now takes real
  data ([2b1f84f](https://github.com/gameguild-gg/website/commit/2b1f84f7775f81a5c91ded74311dc6c5dc2b7552))
* my job
  applications ([3326262](https://github.com/gameguild-gg/website/commit/33262620477c8b8e434256a78efc0eb26c442944))
* new api calls for the jobs
  system. ([71f49b9](https://github.com/gameguild-gg/website/commit/71f49b9e127b021dad4e46f1eea460ef330cf049))
* new header and job application progress
  page ([69273d1](https://github.com/gameguild-gg/website/commit/69273d1ab87bbb8ce256ef92da4c7e15abc8b433))
* new header first
  draft ([1b4c3c6](https://github.com/gameguild-gg/website/commit/1b4c3c66e334adc0067afa6e17f3a0c0ac19ef0c))
* new pagination system on feed and my
  applications ([e3ad2c8](https://github.com/gameguild-gg/website/commit/e3ad2c88be3ea30dd356d54a56129f9c0390f28d))
* return button on my job application detailed
  page ([83ff470](https://github.com/gameguild-gg/website/commit/83ff4701312b3dc6b1631805cd1e6a54c80ace2e))

# [1.2.0](https://github.com/gameguild-gg/website/compare/v1.1.10...v1.2.0) (2024-11-04)

### Bug Fixes

* form actions ([c5c0561](https://github.com/gameguild-gg/website/commit/c5c05610d36c1687caf9c710f72b416dc7aaa40a))
* routing for creating a
  project ([bf952bd](https://github.com/gameguild-gg/website/commit/bf952bdb115dfe8255fc142521e82a86744f9533))
* show error on connect
  page ([65bdf57](https://github.com/gameguild-gg/website/commit/65bdf576398570077220a810df687906aebfe596))

### Features

* create project
  flow ([ac7592d](https://github.com/gameguild-gg/website/commit/ac7592dc14c383181ba5a794b6a0fc9ce2f671e1))
* **slug:** project page
  slug ([276edf5](https://github.com/gameguild-gg/website/commit/276edf5cb95900aef6ecaf80cb82e3f723c8c84a))

## [1.1.10](https://github.com/gameguild-gg/website/compare/v1.1.9...v1.1.10) (2024-11-01)

### Bug Fixes

* make project form receive props about create or
  edit ([c0d8f67](https://github.com/gameguild-gg/website/commit/c0d8f6797e26eba7b3bdda375da2b10a839b5564))

## [1.1.9](https://github.com/gameguild-gg/website/compare/v1.1.8...v1.1.9) (2024-10-31)

### Bug Fixes

* **courses:** better courses routing for
  web ([041645d](https://github.com/gameguild-gg/website/commit/041645d5c1bc804fec8fd2fdf64d8ed76152e2e4))
* disable middleware redirect if it is not logged
  in ([d91a90b](https://github.com/gameguild-gg/website/commit/d91a90be685ee2e240fb99cee499fa1c263a66c7))

## [1.1.8](https://github.com/gameguild-gg/website/compare/v1.1.7...v1.1.8) (2024-10-30)

### Bug Fixes

* jwt import ([68cba9f](https://github.com/gameguild-gg/website/commit/68cba9ff022c54ed671587e073a21ebfb80e3735))

## [1.1.7](https://github.com/gameguild-gg/website/compare/v1.1.6...v1.1.7) (2024-10-30)

### Bug Fixes

* **private:** private
  path ([7f91d71](https://github.com/gameguild-gg/website/commit/7f91d71f7e7d756dd1371eeb20321d2b34fd2a6f))

## [1.1.6](https://github.com/gameguild-gg/website/compare/v1.1.5...v1.1.6) (2024-10-25)

### Bug Fixes

* disconnect if it receives a 401 request out of nowhere from the
  server ([8be629f](https://github.com/gameguild-gg/website/commit/8be629f6e09e348fed7e6ac944507b19cf0d73da))
* **web:** fix missing
  router ([91318fe](https://github.com/gameguild-gg/website/commit/91318fe626a898edc692d2cc6cf1186d34530f2f))

## [1.1.5](https://github.com/gameguild-gg/website/compare/v1.1.4...v1.1.5) (2024-10-25)

### Bug Fixes

* add typings to the dashboard
  job ([001dc6d](https://github.com/gameguild-gg/website/commit/001dc6d77e034a1f0bd89defc38f518ccaaed837))
* **web:** heavy weight
  cookies ([bc20736](https://github.com/gameguild-gg/website/commit/bc20736424933f25cc2cea65cf3f455b7f4c7fb8))
* **web:** provide session only to private
  routes ([be67fcd](https://github.com/gameguild-gg/website/commit/be67fcd659f18148c2cf69b473f49df0c21a0114))

## [1.1.4](https://github.com/gameguild-gg/website/compare/v1.1.3...v1.1.4) (2024-10-25)

### Bug Fixes

* metamask issue ([1cf4ff4](https://github.com/gameguild-gg/website/commit/1cf4ff45dd7d8e1420c8885fb9871c770b189ba3))
* slug issue ([65f81b7](https://github.com/gameguild-gg/website/commit/65f81b7da103e78a7aca86d8ebe7d282fbb1fa15))

## [1.1.3](https://github.com/gameguild-gg/website/compare/v1.1.2...v1.1.3) (2024-10-22)

### Bug Fixes

* rename MANAGER_ROUTE to
  EDITOR_ROUTE ([4e8434a](https://github.com/gameguild-gg/website/commit/4e8434a88a741507024fcc8e78053a2308811c68))
* **web3:** add torus connect
  button ([d560462](https://github.com/gameguild-gg/website/commit/d5604624d69a24d50f6cc9e0cc284df4d7e3cf4c))

## [1.1.2](https://github.com/gameguild-gg/website/compare/v1.1.1...v1.1.2) (2024-10-19)

### Bug Fixes

* trying to fix name replacements for
  gource ([2c586d9](https://github.com/gameguild-gg/website/commit/2c586d9b16a4168a93846ea03283452559cc81d1))

## [1.1.1](https://github.com/gameguild-gg/website/compare/v1.1.0...v1.1.1) (2024-10-18)

### Bug Fixes

* gource now have
  captions ([2fb6695](https://github.com/gameguild-gg/website/commit/2fb6695b350f025dece791347db78078b3b41ac0))

# [1.1.0](https://github.com/gameguild-gg/website/compare/v1.0.7...v1.1.0) (2024-10-18)

### Bug Fixes

* many job type and application
  fixes ([af0fd26](https://github.com/gameguild-gg/website/commit/af0fd2672dce9df52191a0c61337196781ebfe0d))
* post-merge fix on job
  controller ([b84a2ff](https://github.com/gameguild-gg/website/commit/b84a2ff374310c3a19bab53680b4bac7e4a43ff0))
* reworking job system
  backend ([2525a63](https://github.com/gameguild-gg/website/commit/2525a630733697bce77d8ffe857f6c28373dc09e))

### Features

* Job type and job search
  system ([61c5424](https://github.com/gameguild-gg/website/commit/61c542446348a9b7879dd7ac51544aac02fa3f09))
* new unified job application progress
  page ([b4e6a28](https://github.com/gameguild-gg/website/commit/b4e6a2835c2d6a90c4dea9e7eb8ef4382eb2bf65))
* WithRoles and JobPostWithApllied new
  methods ([91ec94d](https://github.com/gameguild-gg/website/commit/91ec94d6b697bbff060c775c7ff4650281ba2a60))

## [1.0.7](https://github.com/gameguild-gg/website/compare/v1.0.6...v1.0.7) (2024-10-18)

### Bug Fixes

* gource ([191edfc](https://github.com/gameguild-gg/website/commit/191edfcc0a40e0e67bcbcf74710f0f288fe72662))
* gource ([4592cc1](https://github.com/gameguild-gg/website/commit/4592cc1f5b6f14ee3d2b3edefec5b04ff909a1e1))

## [1.0.6](https://github.com/gameguild-gg/website/compare/v1.0.5...v1.0.6) (2024-10-18)

### Bug Fixes

* node version for
  web ([3f03463](https://github.com/gameguild-gg/website/commit/3f03463bd6bb7ecc938f1222543fa8373f9df6bf))

## [1.0.5](https://github.com/gameguild-gg/website/compare/v1.0.4...v1.0.5) (2024-10-18)

### Bug Fixes

* docker build web ([1373e56](https://github.com/gameguild-gg/website/commit/1373e56e951ee6d7aeab23f8a27dbdeb23c18824))

## [1.0.4](https://github.com/gameguild-gg/website/compare/v1.0.3...v1.0.4) (2024-10-18)

### Bug Fixes

* web build ([72b790a](https://github.com/gameguild-gg/website/commit/72b790a4e28d9a961bf267149eca52188764bdc9))

## [1.0.3](https://github.com/gameguild-gg/website/compare/v1.0.2...v1.0.3) (2024-10-18)

### Bug Fixes

* bad include on
  game-card ([65fa176](https://github.com/gameguild-gg/website/commit/65fa1767bb9fb62cc05006b80a4096f5ea03c57e))

## [1.0.2](https://github.com/gameguild-gg/website/compare/v1.0.1...v1.0.2) (2024-10-17)

### Bug Fixes

* gource highlight
  usernames ([3977cac](https://github.com/gameguild-gg/website/commit/3977cac4e9ba24ea75836d2305ae28f471c3ce82))

## [1.0.1](https://github.com/gameguild-gg/website/compare/v1.0.0...v1.0.1) (2024-10-17)

### Bug Fixes

* gource video
  generation ([e4d386d](https://github.com/gameguild-gg/website/commit/e4d386d89754828c916d1cde9a5b69d82955d69f))

# 1.0.0 (2024-10-17)

### Bug Fixes

* add cls again for user context
  store ([bfc5c37](https://github.com/gameguild-gg/website/commit/bfc5c37b1fc46721a5fc8c0f66bf9faa45467bdf))
* add new api for course
  creation ([566bf0d](https://github.com/gameguild-gg/website/commit/566bf0de2bf29250e7144cdbe644d019b2dc830f))
* add new api for course
  creation ([d858f51](https://github.com/gameguild-gg/website/commit/d858f51708ccadff44c65daac4bcae1c57bbd619))
* add recharts ([91a47a1](https://github.com/gameguild-gg/website/commit/91a47a109e65654b3a8a3fc344e4ea749c33c7b0))
* add requirements and web3
  boilerplate ([c777778](https://github.com/gameguild-gg/website/commit/c7777787cd7fdf8000f8bfe7aad8aeed7a8bafe3))
* added cookie consent
  popup ([bc8c1b4](https://github.com/gameguild-gg/website/commit/bc8c1b4a219b144a6d992ecfebf72a38213c445b))
* added private route
  group ([509a135](https://github.com/gameguild-gg/website/commit/509a135eeb820133c5f7b6c4c267913f94120b2b))
* added session provider to root
  layout ([2c7a066](https://github.com/gameguild-gg/website/commit/2c7a06678aa0b217ba8ea5dc257bf8db7a19f38f))
* ajuste de UI para dispositivos
  móveis. ([5814596](https://github.com/gameguild-gg/website/commit/5814596097c6df5d4250d2f43c6bda9ed28d3f96))
* antd and competition build
  bugs ([ca09aa0](https://github.com/gameguild-gg/website/commit/ca09aa04a745b7bc092bb8e4dceb0fd529b772d4))
* api calls ([dc4163f](https://github.com/gameguild-gg/website/commit/dc4163f52ccdaace6adfb4995d937bdf6163b39b))
* **api:** add new dependency
  lodash ([7f055ab](https://github.com/gameguild-gg/website/commit/7f055ab14bedc6ffb1ee4ebf6e26bce7e0b0ba17))
* **api:** add sendgrid
  dependencies ([7aaf79f](https://github.com/gameguild-gg/website/commit/7aaf79ffa39c9588aa7f2cc37221965339205212))
* **api:** login with username +
  password ([84ba0f6](https://github.com/gameguild-gg/website/commit/84ba0f6253a8581d2a73e985ae485eb8baa198c7))
* apinest.ts ([72e8998](https://github.com/gameguild-gg/website/commit/72e8998c671f2ee1f69ccaa03756a084524853a7))
* **api:** payload too
  large ([92f9771](https://github.com/gameguild-gg/website/commit/92f9771aeb2878ff943c9e00017c603f5db3a2d2))
* **api:** remove
  types ([c734bb1](https://github.com/gameguild-gg/website/commit/c734bb143bda15454ac79cbca3eaf3fe6cc7b739))
* **api:** username
  exists ([6553daf](https://github.com/gameguild-gg/website/commit/6553daf2827d0b2fd8abcc7bc6e1bac678f99887))
* auth config ([615e92b](https://github.com/gameguild-gg/website/commit/615e92bc0c1f00570cbd239989f4a0acaaf57df8))
* auth config for
  web3 ([8e148bc](https://github.com/gameguild-gg/website/commit/8e148bc7f1a4a83efd543ff5cdc1f60d07194838))
* auth config with refresh token. I hope it work
  properly ([5274311](https://github.com/gameguild-gg/website/commit/527431119772d306c7354d007fd0f3f1f3cf7efe))
* auth login ([b2f54b9](https://github.com/gameguild-gg/website/commit/b2f54b9ef7424ca3c4c26f38474f761ab4253053))
* auth modals ([a4172b9](https://github.com/gameguild-gg/website/commit/a4172b972cc4398ca3f0ec66b3272e1a7a307f05))
* auth ssr ([e8d7ab0](https://github.com/gameguild-gg/website/commit/e8d7ab032f7e815b90ad95bbd0f03fbbbe82d9ac))
* auth type ([4afb28b](https://github.com/gameguild-gg/website/commit/4afb28b9ac04cfa35b92de6075d318735582ed5e))
* **auth:** first time google login is
  working ([0a888f1](https://github.com/gameguild-gg/website/commit/0a888f1b6c8463345cb327b97cfd3806cc0a9365))
* **auth:** fix session parameter
  data ([039698d](https://github.com/gameguild-gg/website/commit/039698d6f13815d20fb006f0526744c289b17603))
* authjs trust host ([f10efac](https://github.com/gameguild-gg/website/commit/f10efacb990b6d84b79af11c5bc82f461dea6b8a))
* **auth:** reduce uniqueness of some fields of user
  entity ([79653d2](https://github.com/gameguild-gg/website/commit/79653d2a7a50846a0ec0fd8c181a9aeed8daff57))
* automation to tag on ci/cd
  push ([5875c46](https://github.com/gameguild-gg/website/commit/5875c46ce4a2f404bf0157259d9514e82c629c81))
* backend responses ([bf123a4](https://github.com/gameguild-gg/website/commit/bf123a40fb142d83732b8c734a28e678c94442fd))
* Base front. ([2bc63f2](https://github.com/gameguild-gg/website/commit/2bc63f2e7c739b4fbb4d8109d67e631e82be55cc))
* better competition folder
  structure ([a81c859](https://github.com/gameguild-gg/website/commit/a81c85952e22197dbdd87690923879fc08a7643d))
* blog and post pages to
  ssr ([7d1214a](https://github.com/gameguild-gg/website/commit/7d1214af7b0fb5e933ca86a143b6008290f360ae))
* blog layout ([30284aa](https://github.com/gameguild-gg/website/commit/30284aa6c3332b1c16311b2542d3f9f157fd0d7b))
* broken build from typescript
  types. ([ec60e2d](https://github.com/gameguild-gg/website/commit/ec60e2d8f273ae10a0f8d09c76c0636e8f6c8674))
* bug to infinite fetch data if there is no
  data ([fb6f0d7](https://github.com/gameguild-gg/website/commit/fb6f0d7a09d96c8a009f0a27bdc614cbf2a058d3))
* bug when the git folder is
  empty ([d16aefc](https://github.com/gameguild-gg/website/commit/d16aefc697cfafe122295c06a5bb3786c51ea946))
* build ([af49a9c](https://github.com/gameguild-gg/website/commit/af49a9c8ae123838108b9b258e1e5586d7ffc6c8))
* build ([65dd268](https://github.com/gameguild-gg/website/commit/65dd2683c40ec1fd496c423441acef34a603b7ee))
* build error on buttonprops
  interface ([c225425](https://github.com/gameguild-gg/website/commit/c2254254acfa3ae5927486af5d36c8182297d57d))
* by default all post returns 201 if it returns
  data ([354cca5](https://github.com/gameguild-gg/website/commit/354cca51dd27b61957550c4242eabc53ec6df5c9))
* cache bug ([5b7b01d](https://github.com/gameguild-gg/website/commit/5b7b01d9c9a6b49b44560d1e78d20c0bcbfed23c))
* card hover effect ([0cbed24](https://github.com/gameguild-gg/website/commit/0cbed2480a977b80eb07dcf4a4aa9e452d961a8a))
* change default elo value to
  400 ([cd0809e](https://github.com/gameguild-gg/website/commit/cd0809ee56d5c27560b263465080872e3021af27))
* change leaderboard to top
  5 ([e4f7d1a](https://github.com/gameguild-gg/website/commit/e4f7d1a338739c58543018c086eb36f72cc3fcde))
* change port to
  8081 ([03dc3ac](https://github.com/gameguild-gg/website/commit/03dc3ac54dbd71efb3c2d3534ad600221fcd2226))
* change port to
  8081 ([577abd7](https://github.com/gameguild-gg/website/commit/577abd725f1f5e7b6fb3167ad8209aa036f0280f))
* change priv / pub key to use
  base64 ([2dc9946](https://github.com/gameguild-gg/website/commit/2dc994679e72bd5a8c0f26907f51d73aff68f28d))
* chess competition play
  page ([b5b2247](https://github.com/gameguild-gg/website/commit/b5b22474ad2d5765c1216318b45b006bc628af07))
* chess competition
  report ([9cd29e1](https://github.com/gameguild-gg/website/commit/9cd29e15d0db07a5c6c56b75f689bd28046c6a19))
* chess return type for
  match ([59b1df1](https://github.com/gameguild-gg/website/commit/59b1df1fb06a70590dc61864c91a6f1ea60475ed))
* **chess:** fix competition return
  types ([d51bf24](https://github.com/gameguild-gg/website/commit/d51bf2446b308cb82aa089a6910f8b08186d1a61))
* codeium
  dependencies ([175edf0](https://github.com/gameguild-gg/website/commit/175edf01131ab3ef9e60a81a657724ea01f2664d))
* comment some unused
  code ([3978249](https://github.com/gameguild-gg/website/commit/3978249ab55e0c12738a817941df06da559b58d4))
* competition
  basics ([9cf3d94](https://github.com/gameguild-gg/website/commit/9cf3d947191ce82473c1b473063ea8553c9a5b1b))
* competition service with
  elo ([ab0e07c](https://github.com/gameguild-gg/website/commit/ab0e07c684754aaf40139eed2c9d5d1e31016186))
* compilation
  issues ([366365b](https://github.com/gameguild-gg/website/commit/366365bb22d3f9c4adf46b50f2e3f7f2b053e3ed))
* compilation issues related to
  typing ([c60c366](https://github.com/gameguild-gg/website/commit/c60c366b71631942887f1ff02468e9cc570e51ee))
* dashboard and auth
  layout ([ab9f773](https://github.com/gameguild-gg/website/commit/ab9f7733ce73de9be1ae8e10a9305c6f1f195570))
* dashboard layout base
  structure ([a435fb6](https://github.com/gameguild-gg/website/commit/a435fb64e8dd8e71863f4d778e6b7ea2426080b3))
* dashboard layout base
  structure ([05a9140](https://github.com/gameguild-gg/website/commit/05a91403f2aee7477ac7f66bc3eb89da5d44834b))
* default route of
  competition ([f59c492](https://github.com/gameguild-gg/website/commit/f59c492bbc90e859f16e5244b364c7c3724cd217))
* deleted_at ([3a549cd](https://github.com/gameguild-gg/website/commit/3a549cd928815354b350ef6c2979855577163b94))
* deploy web in dev mode for
  now ([87a63c7](https://github.com/gameguild-gg/website/commit/87a63c75482c9a199609680b307cc7b382f590d7))
* disable lint on
  build ([36d5927](https://github.com/gameguild-gg/website/commit/36d5927f9286f94511931063340041b567dbc8b6))
* docker web build ([755a75f](https://github.com/gameguild-gg/website/commit/755a75f9360d7e19cda4d60ebbb8f61602c77f3f))
* dockercompose
  port ([e02eee8](https://github.com/gameguild-gg/website/commit/e02eee829015e5267be4aead04fa579918ae852c))
* dockerfile to run api
  properly ([1b34dd8](https://github.com/gameguild-gg/website/commit/1b34dd83c19574e75c6dbc589ebc11dbd3533a7a))
* dockerfile with stockfish from
  sources ([8b91d83](https://github.com/gameguild-gg/website/commit/8b91d838b87872e4612ff3d7a95bae3f2578aedc))
* documentation
  redirect ([22bc436](https://github.com/gameguild-gg/website/commit/22bc43659ec257177a7fd0a3d65dfcc207d4a211))
* downgrate to node18 for compatibility
  reasons ([2e2b8c0](https://github.com/gameguild-gg/website/commit/2e2b8c05ff0c1281d364759fb98501700d5742e5))
* email.dto ([d7b56c6](https://github.com/gameguild-gg/website/commit/d7b56c6f52c2937453a5c0d5bd62ae1adbce011c))
* entity base path ([e7c36a1](https://github.com/gameguild-gg/website/commit/e7c36a193776ae824c44b8854072059b2cc3e47a))
* envs ([2176b1d](https://github.com/gameguild-gg/website/commit/2176b1d30691651be45bc8d263e5d88e7455e8d7))
* errors of ssr ([309d2c9](https://github.com/gameguild-gg/website/commit/309d2c9755cf886b5e25ab3732b53ac0fddda4f9))
* express and multer
  types ([f9e3dc3](https://github.com/gameguild-gg/website/commit/f9e3dc30a387d4aeb8f9005b736aefd78b474fe1))
* fetch posts and
  pagination. ([726ffca](https://github.com/gameguild-gg/website/commit/726ffcad89bceffbb15049a781e09693d7fa8597))
* fixed post pages from query params to url
  params ([7cdf12c](https://github.com/gameguild-gg/website/commit/7cdf12c366dac9dc1f1afc53c2f23f8fa2c1ad8d))
* fixed post pages from query params to url
  params ([739a0ca](https://github.com/gameguild-gg/website/commit/739a0ca5e2e3bac3cf970aaa7aceaeeb0f235e06))
* foldersync for
  windows ([53e6c85](https://github.com/gameguild-gg/website/commit/53e6c85c058380f4c8e3bc32444f210a4d90c5eb))
* foldersync for
  windows ([dd86322](https://github.com/gameguild-gg/website/commit/dd863225adcdb8db38b5f40d4554d2d3f136c13d))
* Front base
  layout. ([5ec98f8](https://github.com/gameguild-gg/website/commit/5ec98f8f49fbb05a1fcbc6f81c8c300b5740f687))
* front compilation ([df5fff3](https://github.com/gameguild-gg/website/commit/df5fff3a71fb8625ee2a07c7a855ccd48bc14b89))
* front login black font and reject wrong
  credentials ([2964d41](https://github.com/gameguild-gg/website/commit/2964d41053e76df762c4dd57c2364674f56f35db))
* front page+misc
  errors ([9625473](https://github.com/gameguild-gg/website/commit/96254736b6006db2c302180fff9846f030e99a1f))
* Front ts config. ([31675f9](https://github.com/gameguild-gg/website/commit/31675f9f008ecd97a3a0c079f191e1c143651bb9))
* Front ts config. ([b357244](https://github.com/gameguild-gg/website/commit/b357244a04d9196df3e96984a23c4c90bf3e851d))
* front without
  lexical ([03e8a57](https://github.com/gameguild-gg/website/commit/03e8a574f1aff7fa490a375e7820df71307a0735))
* frontend menu
  selection ([5ff61de](https://github.com/gameguild-gg/website/commit/5ff61de24870c2e9d7843cc9bd7569d7987d3255))
* frontend
  structure ([107679f](https://github.com/gameguild-gg/website/commit/107679fe723822ae3f9afe4bf4e8a3e70f342679))
* frontend ui, intl ([9b8eb30](https://github.com/gameguild-gg/website/commit/9b8eb3064423cc0b3dd1493e7244a434ec40752b))
* **frontend:** chess auto play random
  move ([be017f0](https://github.com/gameguild-gg/website/commit/be017f0abdc508eb50ba9ff73801d5eb4bf2635d))
* game feedback
  results ([4a85ea8](https://github.com/gameguild-gg/website/commit/4a85ea8276c13b5f074291aa000a88ce8e309f9e))
* gitkeep ([a34fca9](https://github.com/gameguild-gg/website/commit/a34fca99889c3e581cfd844e55d29f9a8cf3ac5a))
* gitkeep ([64bc681](https://github.com/gameguild-gg/website/commit/64bc6812c2c01d401e442e524548049b8ebaa3cc))
* google auth with backend
  data ([a1a6cde](https://github.com/gameguild-gg/website/commit/a1a6cde4c0465d65a3c37b2fef73cdbbce531e28))
* google login on localhost. I hope it works on
  prod ([3efc683](https://github.com/gameguild-gg/website/commit/3efc6832c30a5715bec5cba80d4c3a227be26fd0))
* google sign in ([a0bc166](https://github.com/gameguild-gg/website/commit/a0bc166164148c3ab6f1464ed09fbc9c2e5471d0))
* google sign-in using auth.js (
  next-auth.js) ([a619752](https://github.com/gameguild-gg/website/commit/a619752feb6f35b89a5c5d6815dcb41f5341c14f))
* image linking
  error ([ec7f1d4](https://github.com/gameguild-gg/website/commit/ec7f1d44d040af577ebe0ed9c7d0cf7a0e545618))
* improve error handling on move
  exceptions ([093c50a](https://github.com/gameguild-gg/website/commit/093c50aaeed2e53a0a4a590c1a1128c72bbb865c))
* improved login
  button ([8e4c40b](https://github.com/gameguild-gg/website/commit/8e4c40be5cc18c92418cc266190df813ab77556b))
* install compiler
  dependencies ([b154eb7](https://github.com/gameguild-gg/website/commit/b154eb7ea1fce391510a2a3017e30f7c1a4f227d))
* install web ([071d011](https://github.com/gameguild-gg/website/commit/071d011b2a63b3015ba7e2ddb83f69d4937ffd4e))
* **ipfs:** disable ipfs upload for
  now ([13363b2](https://github.com/gameguild-gg/website/commit/13363b2b423f6800068546d4e7c0872f6f0ecd2e))
* is owner
  interceptor ([c8a8894](https://github.com/gameguild-gg/website/commit/c8a889475bc7424c6ed8ee1711e039a75da6f26a))
* lint and add better search for
  matches ([f34325e](https://github.com/gameguild-gg/website/commit/f34325ebf8f4a7edb5d5dea7391422cc070260ff))
* **lint:** delete unused
  file ([0308811](https://github.com/gameguild-gg/website/commit/03088118d428e0335c3ec9bb7a824d01446b8682))
* login ([f5fb6b0](https://github.com/gameguild-gg/website/commit/f5fb6b0c5f1dee52c233d1339a7f616d5938c5ec))
* login flow is not bugged
  anymore ([ddfd5b5](https://github.com/gameguild-gg/website/commit/ddfd5b56c107b5ba0c404451f1c2b5e72f2cca84))
* login working? ([4282f82](https://github.com/gameguild-gg/website/commit/4282f82c95c920e7d417ded96d77ddcf7cad9af6))
* magic link auth ([80f2504](https://github.com/gameguild-gg/website/commit/80f25042c20ed67d102232f4563272f5cae14878))
* mail service ([48e4135](https://github.com/gameguild-gg/website/commit/48e41357d9f93af7704ceb77747704daf4ee628a))
* make protected routes protected
  again ([c45692f](https://github.com/gameguild-gg/website/commit/c45692ffa90031779d12845dda6c8a43f0c283bb))
* matchsearchresponse is now visible on
  swagger ([217bdd1](https://github.com/gameguild-gg/website/commit/217bdd1aa0193f2128b6f8df0c48ff9193413cd6))
* metamask login ([1bcffd7](https://github.com/gameguild-gg/website/commit/1bcffd7d6ed6e83ca36ca1a8bccf6e27bac48dcc))
* migration ([10dfc46](https://github.com/gameguild-gg/website/commit/10dfc46f928ed647e2dae5e8eb4a4436d4ec65cb))
* minor fixes on chess
  competition ([d89bebf](https://github.com/gameguild-gg/website/commit/d89bebfa8e74c79e7e38d736f61167cfd752d9f6))
* minor issues ([acb3417](https://github.com/gameguild-gg/website/commit/acb3417843f50b351c788ff950fa005283ecda74))
* minor issues on
  back ([339a912](https://github.com/gameguild-gg/website/commit/339a9128026e965df1ceade3216d69fcd541c36c))
* missing react-chess
  lib ([577bbbf](https://github.com/gameguild-gg/website/commit/577bbbfa0ce232057bcbb204e394341df1a5057c))
* missing run ([e2c1638](https://github.com/gameguild-gg/website/commit/e2c1638b66ed423b09030214935156d63685a8f0))
* missing web-3 dto ([527fb22](https://github.com/gameguild-gg/website/commit/527fb22be67a2e32cf33f698873668059457d7f3))
* module import ([80a951b](https://github.com/gameguild-gg/website/commit/80a951be75e35937dfaa446bdfe64211a5b73b08))
* more rigid requirements for env
  vars ([d7229e0](https://github.com/gameguild-gg/website/commit/d7229e0dfc3451972fbf3867410d91b8509dc67b))
* move orm to root ([0753932](https://github.com/gameguild-gg/website/commit/07539323a7ab6e88f203dca5d3dd0a575650bc53))
* moved dockerfile to root to fix parent reference
  issues ([bfecf99](https://github.com/gameguild-gg/website/commit/bfecf997eb7efc7cc192cf7d8ef56f34504a72b8))
* nest version ([05d5417](https://github.com/gameguild-gg/website/commit/05d541739f1940c6a58e9119968e4540a012e966))
* nestjs versions ([35500c0](https://github.com/gameguild-gg/website/commit/35500c0e90b1af2a7414b6baaa67ab193642a381))
* nestjs-cls enable start
  tournament ([06b750c](https://github.com/gameguild-gg/website/commit/06b750c744629ac6dbc4109fa2d9a6b822f1d791))
* **next-nest:** single next
  config ([5246f7a](https://github.com/gameguild-gg/website/commit/5246f7a9f24483a0c005d48f627384429615d68a))
* next.js router in
  competion ([39887de](https://github.com/gameguild-gg/website/commit/39887de60cb384cd3be0d6e042daf63cbf49e344))
* notification via email and config
  service ([989ab1f](https://github.com/gameguild-gg/website/commit/989ab1fe558c321cfee8022b6c4d5efe1adb0d84))
* now competition have
  parameters ([ac7c3c1](https://github.com/gameguild-gg/website/commit/ac7c3c1b0dde5863c50e34cab2aa116db7dfa411))
* now most calls to api auth are using new api call
  method ([f2b2fce](https://github.com/gameguild-gg/website/commit/f2b2fce65a22f919f3a887d4ca1c362668282cc3))
* npm dependencies:
  tryghost ([33e2a30](https://github.com/gameguild-gg/website/commit/33e2a3068ea13c686412127dda91c3f9ae92cb46))
* npm install ([3def186](https://github.com/gameguild-gg/website/commit/3def186108163e0ad70bf02a2f479325b46fbe0f))
* npm install depenencies for
  ubuntu ([bd527c5](https://github.com/gameguild-gg/website/commit/bd527c582b2a64cba12cfd4f03ff500c7a0c8be1))
* npm run again ([aaef468](https://github.com/gameguild-gg/website/commit/aaef46835b4bcd93fb2114c399944aa2d0ee8b48))
* npm run build ([8d36acc](https://github.com/gameguild-gg/website/commit/8d36accd50f0d3cc3ada77b6b1eee98c9596af86))
* openapi
  generation ([a824888](https://github.com/gameguild-gg/website/commit/a824888ddbb6e1840071e1d8a9088210fbb9aff8))
* package.json ([79ec108](https://github.com/gameguild-gg/website/commit/79ec108f773d97b1071d3f16757601c7a63738e2))
* package.json settings for fixing pnpm
  commands ([b6c54cc](https://github.com/gameguild-gg/website/commit/b6c54cc74f4c481585071d6e72b104b1b54a2e55))
* **package:** add front
  locals ([ca9324e](https://github.com/gameguild-gg/website/commit/ca9324ed9a2c258bece09442f001a9a70a593f4c))
* packages
  references ([79a2994](https://github.com/gameguild-gg/website/commit/79a29942ec3d5ab5f11283bfe017dfafb0600586))
* pagination bug ([b61304a](https://github.com/gameguild-gg/website/commit/b61304a67bee86e20621e1d495c7722b8d919db0))
* permissions
  hierarchy ([7b28697](https://github.com/gameguild-gg/website/commit/7b286976a3de248c94565f9d4e18fd2629cbea95))
* potentially make refresh token
  work ([f13275d](https://github.com/gameguild-gg/website/commit/f13275d2f77aa440709ef84a6085b0470faed06b))
* project create /
  fetch ([d0ffe53](https://github.com/gameguild-gg/website/commit/d0ffe536bb0116a2e5cd693c08a14fd7ce14e121))
* proposal data
  structures ([c37c71a](https://github.com/gameguild-gg/website/commit/c37c71a7938a552733ba461fd22a54b3e6a5f6a8))
* protect multiple competition to run at the same
  time ([259724c](https://github.com/gameguild-gg/website/commit/259724ce601a52f255657ac47a238f4e89e5e1ba))
* public decorator for api
  routes ([a94a5e0](https://github.com/gameguild-gg/website/commit/a94a5e0c35434bac00f3518b331fb087358ea505))
* public routes should be
  public ([8892425](https://github.com/gameguild-gg/website/commit/8892425430b3b0751fae190aad8dbd559b3d7161))
* refactor dto routes and
  styles ([f24cdc5](https://github.com/gameguild-gg/website/commit/f24cdc5fcf68eef19503cf5062c123bd64b128cb))
* remove @game-guild/courses from
  package.json ([f62f584](https://github.com/gameguild-gg/website/commit/f62f5844523e3b463c476e34b27c834c76ed803d))
* remove a bit more of those env
  vars ([c7be2d2](https://github.com/gameguild-gg/website/commit/c7be2d2edaa11e6ba6fba8075172bf49ca975f69))
* remove cls ([697710c](https://github.com/gameguild-gg/website/commit/697710c92ae40ba17515ad67ac081bd91a5d39a7))
* remove
  HttpAdapterHost ([35e4de2](https://github.com/gameguild-gg/website/commit/35e4de2efa55964ff707a3d1372cd9ce13c10776))
* remove last state from the match
  history ([d5a8bbb](https://github.com/gameguild-gg/website/commit/d5a8bbbfdccc801632db7d57c108a440c095c5df))
* remove outdated api
  connections ([371c44a](https://github.com/gameguild-gg/website/commit/371c44abf2d0bbece528308df0807acbc3a6bf24))
* remove relation
  problems ([2ec1cf6](https://github.com/gameguild-gg/website/commit/2ec1cf6ea60f7dd0ed62748540aca99dbf94f7f0))
* removed modals and updated the sign-in and sign-up
  page ([6297b55](https://github.com/gameguild-gg/website/commit/6297b55af89177a3cb60d9cabe841f60f6785044))
* rename game to
  project ([475e493](https://github.com/gameguild-gg/website/commit/475e49367519ab3c137b18d6fbbd645c3dcb8936))
* restart database
  structures ([93ab1c6](https://github.com/gameguild-gg/website/commit/93ab1c61e4df17dbb34f165a88236b14c93c2663))
* restrictions ([dd1ccbd](https://github.com/gameguild-gg/website/commit/dd1ccbd77cc4048a55be041246e8c14132b8afc4))
* root layout ([020cf05](https://github.com/gameguild-gg/website/commit/020cf0570b137712adb8371b560600e32fc88082))
* routing from matches to replays and vice
  versa ([9a147b6](https://github.com/gameguild-gg/website/commit/9a147b6b0656f099065a0d4e241a68f7a7a74729))
* security fix on job post
  ownership. ([3aa5244](https://github.com/gameguild-gg/website/commit/3aa5244579a715bd2396672b77b097510e4bb42a))
* send package lock to try to pin
  versions ([adfbb12](https://github.com/gameguild-gg/website/commit/adfbb1263e9bbd8553ec9b3eaf84053024fbe997))
* session
  competition ([2fc4bdd](https://github.com/gameguild-gg/website/commit/2fc4bdd06aafc53763961afadda04e2dc4160aa1))
* sign in google in the web (
  next.js) ([2bc5fb3](https://github.com/gameguild-gg/website/commit/2bc5fb3d28419d0566e791d3832ff6baab0bf044))
* sign in google in the web (
  next.js) ([c4e3246](https://github.com/gameguild-gg/website/commit/c4e32462b5d8e8fab1cb53677518632901e5a11b))
* sign-in form ([f6ee121](https://github.com/gameguild-gg/website/commit/f6ee121bc11e4d270f6d0f9fdfbc802f0cd61ba1))
* sign-up form ([bf0f60d](https://github.com/gameguild-gg/website/commit/bf0f60d42888d0e5c81a3e88d72803c7c0dee2b3))
* small empty space at the right side of the
  screen ([51e4f5e](https://github.com/gameguild-gg/website/commit/51e4f5eb38cfc6708c386ea0cfedbb06a97dfc1e))
* stockfish ([91dcc91](https://github.com/gameguild-gg/website/commit/91dcc91f2745792cec22111852d26db5836a129a))
* stockfish app ([0a295e3](https://github.com/gameguild-gg/website/commit/0a295e345a5732a5db0d193b7be7536e10ad9a35))
* stockfish
  installation ([215d407](https://github.com/gameguild-gg/website/commit/215d40707f0edb70061fedb2e05d7c2915184a1b))
* summary home ([f4de681](https://github.com/gameguild-gg/website/commit/f4de6812209b5d270c663f841d7e6b41c65d9392))
* the way we call
  apis ([82a4ef9](https://github.com/gameguild-gg/website/commit/82a4ef92ac69ec410618781565c996add439cb21))
* tolsta machine
  data ([4ff53cd](https://github.com/gameguild-gg/website/commit/4ff53cdb0cb7b1a36d89330c829b03ea65a083da))
* tournament page ([ec84260](https://github.com/gameguild-gg/website/commit/ec84260a5ed347a7c228701b5c2f298cf729e737))
* tsconfig.server. ([8c32458](https://github.com/gameguild-gg/website/commit/8c32458f2294f8c9226401f42f085e7ddb22b9b8))
* tsconfig.server. ([354570b](https://github.com/gameguild-gg/website/commit/354570bd85fd7f7e6b45086a8b02b8ea7fecc1ec))
* typescript typings
  errors ([9514bb7](https://github.com/gameguild-gg/website/commit/9514bb7c72b65496a1c3216c4863f202cbd6eb5a))
* typings ([2636f79](https://github.com/gameguild-gg/website/commit/2636f79b5fd25ed21501e5dfd82107d83cf312fc))
* use header on the
  feed ([f2c3df3](https://github.com/gameguild-gg/website/commit/f2c3df3f12d62e862637c45fe1a58d396e09d48b))
* user creation + user login
  flow ([33dc50b](https://github.com/gameguild-gg/website/commit/33dc50bff7b5718dcd9fc8edbc1f0303413c124e))
* validade login via metamask; feat: add game-feedback
  struture ([2b8923e](https://github.com/gameguild-gg/website/commit/2b8923ede0821b4dabe1446d0b84290a771ad9c8))
* web apinest package
  dependencies ([06d1557](https://github.com/gameguild-gg/website/commit/06d1557c2842f8d25718e58d6016d8486905ff7d))
* web-3 auth ([4845412](https://github.com/gameguild-gg/website/commit/4845412b86e1f3c375263fdbf0ebb784cb00a728))
* web3 button login ([6a62d54](https://github.com/gameguild-gg/website/commit/6a62d540bd19ae1372bc0b20a2b61ad3fe873073))
* **web3:** [wip] web3 + magic
  link ([8b5619d](https://github.com/gameguild-gg/website/commit/8b5619db11f045de2f67c68ff9aa5a36f400fc6e))
* **web3:** add wallet to user
  session ([0dd48ed](https://github.com/gameguild-gg/website/commit/0dd48ed64ad6040f72dfc0f24a712b982282c3bc))
* **web3auth:** now metamask login is
  working ([4371463](https://github.com/gameguild-gg/website/commit/437146363a9a2464d316abd445b665b4c06cbd5f))
* **web3:** backend now signs with
  siwe ([e828849](https://github.com/gameguild-gg/website/commit/e8288493b06e3501494f866f815a405c5c900344))
* **web:** deploy in production
  mode ([1726794](https://github.com/gameguild-gg/website/commit/1726794b0d5df82aba90c91317ec9adf40d724db))

### Features

* add antd ([a703a06](https://github.com/gameguild-gg/website/commit/a703a06d0862d3aa085b91d8f7b3ab2481df39e1))
* add automatic
  versioning ([5ec3244](https://github.com/gameguild-gg/website/commit/5ec32445f46c6f804943a927d0eb4f004f0c6adf))
* Add base entities for Event and
  User. ([0c4e1ce](https://github.com/gameguild-gg/website/commit/0c4e1cea67ab8658e944c8c7b74ffb9d4d0e9691))
* add social ids, email/pass and wallet to user
  entity ([683027c](https://github.com/gameguild-gg/website/commit/683027c1e656856692e6f3fa0383d849fbd72e83))
* added editor to
  dashboard ([405d23d](https://github.com/gameguild-gg/website/commit/405d23db159f42b047abcad7bd8aadd0fd9b3d92))
* added header and footer to blog
  page ([c344f46](https://github.com/gameguild-gg/website/commit/c344f468dbcba9c9c4d454fa74aa9942c5c4c167))
* agg bar ([afdc2fb](https://github.com/gameguild-gg/website/commit/afdc2fb9b19baf14c127c8e7971edf5fd0c22148))
* ahh syntax
  training ([4b91e46](https://github.com/gameguild-gg/website/commit/4b91e4625aed923c76531dde2b3d57742c2fd333))
* **api-competition:** add
  competition ([a050476](https://github.com/gameguild-gg/website/commit/a050476e11ad9d7902d5ab178fa634c9d976025e))
* **api:** add competition
  module ([f2642d9](https://github.com/gameguild-gg/website/commit/f2642d94e86f3fd08a73396a94491091e7cc4d90))
* **api:** add simple
  register ([16464a7](https://github.com/gameguild-gg/website/commit/16464a7a0f4e781c0b9cf94e09b6921d7cad12b7))
* **api:** prepare chess
  submission ([100f713](https://github.com/gameguild-gg/website/commit/100f7134ea886ca8d77a52de8af0b478545e81f5))
* auth on index
  page ([2844abe](https://github.com/gameguild-gg/website/commit/2844abed34a5d0575dc262005e1db25886392cd0))
* **Auth:** Add base
  module. ([46bcd0a](https://github.com/gameguild-gg/website/commit/46bcd0aee1f02b274556ad76b104ad505d17c3a7))
* **auth:** improve profile
  fields ([b1e8cac](https://github.com/gameguild-gg/website/commit/b1e8cac11328e0a7e9ec5e5b890216e49989439a))
* **auth:** wip fix web3
  login ([d259624](https://github.com/gameguild-gg/website/commit/d259624571078e39dc7331b9c30d0ac04266f9ec))
* automatically install apiclient on web after a bockend
  change ([46beb6a](https://github.com/gameguild-gg/website/commit/46beb6a5afbaa3e6da3ab13df0621e74f7d1688a))
* **backend:** add competition backend
  WiP; ([a84b650](https://github.com/gameguild-gg/website/commit/a84b650341ed98a670a40df2025fcae6b8b76446))
* beginning of course system
  UI ([7c1b8cd](https://github.com/gameguild-gg/website/commit/7c1b8cd9cd1f96464aacf5f3a937bb231c9bc04f))
* blog first
  version. ([6021e65](https://github.com/gameguild-gg/website/commit/6021e65e62fe347a6cecb4c4a8c4df07adff68a7))
* blog reading ([6f4a5ea](https://github.com/gameguild-gg/website/commit/6f4a5ea5cfd692672a9408e3f8a5fc967c7418fd))
* boilerplate ([2016b44](https://github.com/gameguild-gg/website/commit/2016b441c7a13a169d8494143f90482229c75e88))
* challenge a bot ([83488f2](https://github.com/gameguild-gg/website/commit/83488f2b10c84be175f81fb2eb84c2c31c9b594d))
* Changed DefaultLayout, create Navbar and
  Footer. ([6d62b0d](https://github.com/gameguild-gg/website/commit/6d62b0d6a9c320f6f0b06e3205b6e81b5526ae59))
* chess play now can set who is the
  player ([a919f9c](https://github.com/gameguild-gg/website/commit/a919f9c1e3a6ff901a0ec3e4c56274b461337756))
* chess random movement
  agent ([f6e3377](https://github.com/gameguild-gg/website/commit/f6e3377b0053c8cde312fb7fc692b7dfff23e3d7))
* **chess:** accept submissions, zip and store on the db the
  binary ([cfcc80b](https://github.com/gameguild-gg/website/commit/cfcc80ba28660efcf8ca8ebc94f27c1183101072))
* **chess:** add match run
  system ([7f08e3d](https://github.com/gameguild-gg/website/commit/7f08e3d250f8a26935439da4ee8bd96d769c896c))
* **chess:** api for requesting a move for an
  user ([e1393f9](https://github.com/gameguild-gg/website/commit/e1393f977fc8da2090cb04e4d256d1a343cf367c))
* **chess:** get match
  id ([fc72dc1](https://github.com/gameguild-gg/website/commit/fc72dc166da92fa4709d616803ddf42ef1b84015))
* **chess:** now matches are
  searchable ([6e34172](https://github.com/gameguild-gg/website/commit/6e34172b629f0215c66155ff012f8b07582f73e7))
* chose the bot you want to play
  against ([0670ea5](https://github.com/gameguild-gg/website/commit/0670ea5c3031ba8f7e2596e7162214ad88ad2bf3))
* cms test ([17c564d](https://github.com/gameguild-gg/website/commit/17c564d3a7fa8cbc6f036748f0e7f4ffe42ae319))
* competition
  report ([67fb5aa](https://github.com/gameguild-gg/website/commit/67fb5aa5ae141a2f13fe2075865e01dcfe529808))
* competition
  reports ([e9a2216](https://github.com/gameguild-gg/website/commit/e9a221643fc9ac22bb6fa3449d0cc06d3d91f136))
* competition
  reports ([42a4e1a](https://github.com/gameguild-gg/website/commit/42a4e1a8b26c836eb6b4ae6eea0d7a06a8dc4ef2))
* **competition:** add competition
  dashboard ([52d13fe](https://github.com/gameguild-gg/website/commit/52d13fed4f2cff8fe99b1abf7d8ca2286f4f6ddd))
* compile ([e0d6ada](https://github.com/gameguild-gg/website/commit/e0d6ada462dc0683def1ba009522a535cd72806b))
* comptition rework ([fad3557](https://github.com/gameguild-gg/website/commit/fad35577bdd4dffd22a09d13dc6ca31411634f72))
* container and autentication
  page ([7ff9504](https://github.com/gameguild-gg/website/commit/7ff95041509013a516e5d399373cce543aa04015))
* content management
  system ([c70f008](https://github.com/gameguild-gg/website/commit/c70f00856768e6c77368a5a7429cc085a9622c5b))
* content management
  system ([dad5bbc](https://github.com/gameguild-gg/website/commit/dad5bbc05d1a87e8d0e53cd2e07434994fa3821d))
* course chapter ([35533e5](https://github.com/gameguild-gg/website/commit/35533e57a7e2fc09763daf58e7f629deddb6a40e))
* creation of
  enumerations. ([f3df378](https://github.com/gameguild-gg/website/commit/f3df37813d25d42e8e116069b88c5ec170cf0a75))
* dashboard and new
  header. ([a4ef86e](https://github.com/gameguild-gg/website/commit/a4ef86ec0d7f7c4325365cd6b2a155e0caf6bdae))
* debug log the username when the movement
  fails ([ed97566](https://github.com/gameguild-gg/website/commit/ed975664067918db9c477e836985b64a47a03cf8))
* email validation ([5644266](https://github.com/gameguild-gg/website/commit/56442665b462a176279eb6f4420c1f9ae4c185f0))
* **Entities:** Add base
  entities. ([1ef09c7](https://github.com/gameguild-gg/website/commit/1ef09c7805dd3bb0db3f9261f047f78e680108e5))
* **Event:** Add base
  module. ([5a529d3](https://github.com/gameguild-gg/website/commit/5a529d3d49f68f938472c8a7735069571cb0d1ca))
* **Event:** Add base
  module. ([12d7126](https://github.com/gameguild-gg/website/commit/12d7126cb6458047fcbea7e8d9d00cde837d4cb6))
* extract profile from
  user ([e32eb6f](https://github.com/gameguild-gg/website/commit/e32eb6f4038f2273849c062ac674a31efdc87024))
* fetching Jobs ([701efab](https://github.com/gameguild-gg/website/commit/701efab1acb42314cd764ab9c52c440c102674b1))
* first migration ([5e12e24](https://github.com/gameguild-gg/website/commit/5e12e24b1e7c4c25ef2d4cd6885cb22a00c4c5de))
* first time on atomic design
  pattern ([d0469a6](https://github.com/gameguild-gg/website/commit/d0469a6fba5d3f637744386a2c2f0b330c5668ff))
* fix backend for create
  project ([753dc68](https://github.com/gameguild-gg/website/commit/753dc68af7c6b37948d136ca9ea377bf5c9f425f))
* fix docker build with
  stockfish ([300c3da](https://github.com/gameguild-gg/website/commit/300c3da35b0ec8da4e7608405ff6a9ec85282083))
* fixed isolation between nest and next
  errors ([095ef38](https://github.com/gameguild-gg/website/commit/095ef387727ace4ea1b3876d389b520439c841de))
* folder sync for
  dtos ([82d92a9](https://github.com/gameguild-gg/website/commit/82d92a93ecdf3eb0c3b76c26ea4e0af1664870a0))
* folder sync for
  dtos ([800273f](https://github.com/gameguild-gg/website/commit/800273f6e7166f9c1ef9d0dd211a78ff7612e5c2))
* force the player to always chose himself as a player when challenging a
  bot ([8a58154](https://github.com/gameguild-gg/website/commit/8a581544083364d37e4d34f0fcf86011b7e8732f))
* **Front:** Add base front
  layouts. ([87b12fd](https://github.com/gameguild-gg/website/commit/87b12fd0fcef6570cdcb0a0bfdeaebd1a277a740))
* **Front:** Add base front
  layouts. ([fcf8eda](https://github.com/gameguild-gg/website/commit/fcf8edaf874c3b1784da65148cea2fcb5669aee1))
* **front:** add front to
  dockercompose ([8a9f465](https://github.com/gameguild-gg/website/commit/8a9f4653008cd44bcc3390df1328e062851d7dce))
* **front:** add new
  template ([38aca24](https://github.com/gameguild-gg/website/commit/38aca249e7f659c31e6a3a8ac4217df9a3f2d7aa))
* game-version ([17ed824](https://github.com/gameguild-gg/website/commit/17ed824e97e310277afee93504e964844f3b3d10))
* generate dbml ([a9466d6](https://github.com/gameguild-gg/website/commit/a9466d6024dd4bc9cd3d07c168e9d3aeff13c9a2))
* generated new apicontent package extracted from nestjs
  swagger ([c6e3cad](https://github.com/gameguild-gg/website/commit/c6e3cad1545d1030220464e5f028342e243e8674))
* get match and replay
  it ([170e8b8](https://github.com/gameguild-gg/website/commit/170e8b8b9db4675e7e0f189845e08cdee8953433))
* google login
  button ([38def8c](https://github.com/gameguild-gg/website/commit/38def8ce2d633eb54fa77a57857684185dd689e6))
* **google-auth:** connect front and back
  login ([eaa2840](https://github.com/gameguild-gg/website/commit/eaa2840c5cf9fa5edae684cf8609095175aae87e))
* initial gtl pages ([a30adcc](https://github.com/gameguild-gg/website/commit/a30adccc92349bf0cf4c97bd13c4cc0dfe68450e))
* intl, auth and
  cookies ([41aecf4](https://github.com/gameguild-gg/website/commit/41aecf4833656da015091c6c89f7d33ca9de76b6))
* ipfs storage ([650931c](https://github.com/gameguild-gg/website/commit/650931c5135826c6de44dde727002778d6bb63a9))
* isolate ghost api ([c5ae1cf](https://github.com/gameguild-gg/website/commit/c5ae1cf2dc55bb09809ff62337030b3c37cdf95d))
* job aplication basic
  functionality ([c5dfabc](https://github.com/gameguild-gg/website/commit/c5dfabcb1a14c97b237f8a9ef4b0659fcce5a392))
* job aplication progress first
  UI ([0fa0ff2](https://github.com/gameguild-gg/website/commit/0fa0ff237855466e2bb2572adddcf2a7f13935c3))
* Job Tag system working for Job creation and
  viewing ([af2856f](https://github.com/gameguild-gg/website/commit/af2856f6a7d50eabac62dd1753ba6aceb431693e))
* Job typeorm initial
  structure ([cc35625](https://github.com/gameguild-gg/website/commit/cc35625070a6aaadfc98eafa23da390a8170b7a2))
* jobs page first
  draft ([d54b5ba](https://github.com/gameguild-gg/website/commit/d54b5ba501f88e41125bd46b22132ea1be6df20d))
* leaderboard ([06199ff](https://github.com/gameguild-gg/website/commit/06199ff376a214aaf31594d847030e45d97115f4))
* leaderboard ([ad0bd67](https://github.com/gameguild-gg/website/commit/ad0bd675fe5672245ebafcb55ce0ec849761a8ca))
* list last matches ([8e71d6e](https://github.com/gameguild-gg/website/commit/8e71d6eec2c87214c3805ae94c84e85bda626918))
* login / register ([0983627](https://github.com/gameguild-gg/website/commit/0983627af906bf9dfb54e9bfdb474d4aed3f7911))
* login and route to
  dashboard ([2ba2f6c](https://github.com/gameguild-gg/website/commit/2ba2f6c34b61747e737fb42149666833a9983463))
* login page ([255e0f3](https://github.com/gameguild-gg/website/commit/255e0f3d23bb4addd9a8d432b3148773fe8fd71e))
* **login:** rework on login / signup
  page ([d4c67da](https://github.com/gameguild-gg/website/commit/d4c67daca34086c5fd003f412505c25124adef05))
* **magic-link:** wip magic link
  frontend ([b996c2a](https://github.com/gameguild-gg/website/commit/b996c2ae4f93d07aaff09ead6f25b8eed1181462))
* mail sender and errors for user
  registering ([562d212](https://github.com/gameguild-gg/website/commit/562d212e1226ae83cd727906f11a1cf30d1defb7))
* make human play against ai
  bots ([3c99304](https://github.com/gameguild-gg/website/commit/3c993040ed50ecc49135993119cee196ae19e114))
* meamask page connect by
  ethers ([48e0917](https://github.com/gameguild-gg/website/commit/48e091735c690fb815720e5f01968c6d78abda58))
* merge alec work and add competition
  tab ([af3386f](https://github.com/gameguild-gg/website/commit/af3386f4e6e6fb794afb85864c0cc330f454b2a3))
* **Module:** Add base
  modules. ([24fa8d1](https://github.com/gameguild-gg/website/commit/24fa8d15e17bff005a53540e5b2ac9153ed73c94))
* move front to a
  subfolder ([d95ccd0](https://github.com/gameguild-gg/website/commit/d95ccd0d4fa9786f0bc37e7b497fac3f47edcc1c))
* new feed and main dashboard
  pages ([f9130eb](https://github.com/gameguild-gg/website/commit/f9130eb8aa8514c6d40cbb4cd6528904cc888e0f))
* new grid ([285fdee](https://github.com/gameguild-gg/website/commit/285fdee09872020b8a54c138b55f0bd7f143771a))
* notification component for
  frontend ([f54364a](https://github.com/gameguild-gg/website/commit/f54364a0b9dc740f0d71c71ce103f31fabdc12a9))
* **Post:** Add base
  module. ([f2e584c](https://github.com/gameguild-gg/website/commit/f2e584c19a72d9ceca615db04fbd1488495a34c8))
* projects ([7a2a4a9](https://github.com/gameguild-gg/website/commit/7a2a4a90286d117890c24a3a4be9373f4f5930a4))
* proposal ([556fe4d](https://github.com/gameguild-gg/website/commit/556fe4d158cc638fc585a9f723c4e86e109f47f7))
* refactor to use dtos properly from
  swagger ([0d5a510](https://github.com/gameguild-gg/website/commit/0d5a51079663bfcb351a4162e13adba55f7a495d))
* refresh token ([be949fc](https://github.com/gameguild-gg/website/commit/be949fcb2a2b7de9f2beca3c72553e2924736270))
* remove symlinks ([c7e74fb](https://github.com/gameguild-gg/website/commit/c7e74fb37736547024aeee2c7d29b62d6ab2381a))
* remove symlinks ([eefa381](https://github.com/gameguild-gg/website/commit/eefa381e4d7c6efa0df2ba0877e8283f70b1c260))
* remove tailwind and add
  mui ([fa6cebd](https://github.com/gameguild-gg/website/commit/fa6cebd979cf33e393f67a394750a2cdc542f088))
* rename user to
  userdata ([4a19e22](https://github.com/gameguild-gg/website/commit/4a19e224d1aaa93141d400b7ead3a962f8f413de))
* replay match ([54abcc1](https://github.com/gameguild-gg/website/commit/54abcc1b6b779958f939be87acbb29d73aa86fca))
* search in bread cumb, no aton
  yet ([ab8414f](https://github.com/gameguild-gg/website/commit/ab8414fee5b218229cd51a3599137003ed6381ca))
* signup page ([8e89247](https://github.com/gameguild-gg/website/commit/8e8924741bf5bcc672abec1b7482f1b1d841be3b))
* simplified
  boileplate ([bb0bbc5](https://github.com/gameguild-gg/website/commit/bb0bbc5a73e4c5b543bd5f34ad7658cc60871aed))
* slugify the title on
  creation ([35ab483](https://github.com/gameguild-gg/website/commit/35ab483c6bebd1527c1095e07864bba1c7e04f10))
* start of individual course
  page ([5c98ef5](https://github.com/gameguild-gg/website/commit/5c98ef561338bb52a7c0218e251e34a1bef8fe13))
* start page rework for
  GTL ([8bf8661](https://github.com/gameguild-gg/website/commit/8bf8661cc5da104de2e20f6ed43426cd182b0a9b))
* summary
  description ([a1d6a31](https://github.com/gameguild-gg/website/commit/a1d6a31ca5c9c0d82d8210f6faf37e3fcfd4677c))
* swagger ([a05dc88](https://github.com/gameguild-gg/website/commit/a05dc88fc501fe35ff8408cf7b69ec78c3809b6f))
* **swagger:** add automation for sdk generation to import swagger and create bindings on
  web ([8899ba1](https://github.com/gameguild-gg/website/commit/8899ba178f2de1edaecbffc1c3617a0377a0aedb))
* tailwind
  boilerplate ([f2a0e08](https://github.com/gameguild-gg/website/commit/f2a0e0801267df5ac16e21f5d45f492e50da6954))
* transport error in plain
  sight ([edef699](https://github.com/gameguild-gg/website/commit/edef699c48dfdc87c3e405ca499b46d72fe754f7))
* typeorm scaffold ([6c3d68e](https://github.com/gameguild-gg/website/commit/6c3d68e08f2cad3656992e8c05868f480822fb3a))
* upload bot ([e2f0b0d](https://github.com/gameguild-gg/website/commit/e2f0b0dbe795bfc853a793bd8710589e9038ecb1))
* **Upload:** Add base abstract
  service. ([c39691c](https://github.com/gameguild-gg/website/commit/c39691c5cabd9e2f3c518e3f833f5e9913cbb92a))
* user settings
  area ([a168b08](https://github.com/gameguild-gg/website/commit/a168b08a3cf97708be122cfc29749a78ae778132))
* web3 auth ([d5f3775](https://github.com/gameguild-gg/website/commit/d5f37750bcc4d5ea9d4dd4962491006bc2367f8f))
* **web3:** [back] auth with web3 and save in cache the message and wallet to valitate
  later ([b4a166c](https://github.com/gameguild-gg/website/commit/b4a166c2fef559d5c64584c7f592a4acf5c4acf0))
* **web3aut:** controller for challenge and login with web3
  eth ([77af186](https://github.com/gameguild-gg/website/commit/77af1868186219ccc4ff46ba1ca09ee0d0f4e48a))
* **web3auth:** api - sign message with
  wallet ([8b6b747](https://github.com/gameguild-gg/website/commit/8b6b7478ce13bdc1d99814bdde259b4a59650cb3))
* **web3auth:** controller for challenge and login with web3
  eth ([7a958d1](https://github.com/gameguild-gg/website/commit/7a958d1ef195013aa4cab43144a4d894329d806e))
* wip auth process ([3a948d8](https://github.com/gameguild-gg/website/commit/3a948d882a7069d39e2caa9d65547b8baee43d78))
* WiP DAO white
  paper ([9abaf7f](https://github.com/gameguild-gg/website/commit/9abaf7f9d778b5a4f9aa8c2482593ef467bf3f54))
* with-roles
  refactor ([c67e8b0](https://github.com/gameguild-gg/website/commit/c67e8b02c26b8d8741da774aa02199ef28bfb597))
* withroles ([1cb4e4b](https://github.com/gameguild-gg/website/commit/1cb4e4b3e024692fbe79fc248d77cc8f01521c87))
* workflow for stale
  issues ([6230cd6](https://github.com/gameguild-gg/website/commit/6230cd6a4d3d48e06ccb35c4079df8b0ccc061af))

### Performance Improvements

*

create-project.dto ([d0f1afd](https://github.com/gameguild-gg/website/commit/d0f1afd178274a7c573abbae302109da4fbec1bf))
