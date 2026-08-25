import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import test from 'node:test';

async function fixtureRoot(context) {
  const root = await mkdtemp(path.join(tmpdir(), 'emception-source-compatibility-'));
  context.after(() => rm(root, { recursive: true, force: true }));
  return root;
}

async function writeFixture(root, relativePath, content) {
  const filename = path.join(root, relativePath);
  await mkdir(path.dirname(filename), { recursive: true });
  await writeFile(filename, content);
  return filename;
}

test('raylib compatibility patch upgrades Emscripten macros and avoids pointer overflow checks', async (context) => {
  const root = await fixtureRoot(context);
  const miniaudio = await writeFixture(
    root,
    'src/external/miniaudio.h',
    '#if (__EMSCRIPTEN_major__ == 3 && __EMSCRIPTEN_minor__ == 1 && __EMSCRIPTEN_tiny__ >= 70)\n#endif\n',
  );
  const vorbis = await writeFixture(
    root,
    'src/external/stb_vorbis.c',
    'if (f->stream_start + loc >= f->stream_end || f->stream_start + loc < f->stream_start) {\n}\n',
  );
  const { patchRaylibSource } = await import('../lib/source-compatibility.ts');

  assert.equal(patchRaylibSource(root), true);
  assert.equal(
    await readFile(miniaudio, 'utf8'),
    '#if (__EMSCRIPTEN_MAJOR__ == 3 && __EMSCRIPTEN_MINOR__ == 1 && __EMSCRIPTEN_TINY__ >= 70)\n#endif\n',
  );
  assert.equal(
    await readFile(vorbis, 'utf8'),
    'if (loc >= (unsigned int)(f->stream_end - f->stream_start)) {\n}\n',
  );
  assert.equal(patchRaylibSource(root), false, 'patch must be idempotent');
});

test('Allegro compatibility patch fixes diagnostics without disabling core addons', async (context) => {
  const root = await fixtureRoot(context);
  const fixtures = new Map([
    ['CMakeLists.txt', 'cmake_minimum_required(VERSION 3.0)\ninclude(FindPkgConfig)\n'],
    ['src/shader.c', 'ALLEGRO_SHADER *al_get_current_shader()\n'],
    ['src/sdl/sdl_system.c', '   SDL_Cursor *cursor;\n'],
    [
      'src/opengl/extensions.c',
      'typedef void (*VOID_FPTR)(void);\n'
        + '/* GCC extension loader. */\n'
        + 'static VOID_FPTR load_extension(const char* name)\n'
        + '{\n'
        + '   return NULL;\n'
        + '}\n\n\n\n'
        + '/* Load the extension API addresses into the table. */\n',
    ],
    [
      'src/opengl/ogl_bitmap.c',
      'static bool can_flip_blocks(ALLEGRO_PIXEL_FORMAT format)\n'
        + '{\n'
        + '   return true;\n'
        + '}\n\n'
        + 'static void ogl_flip_blocks(ALLEGRO_LOCKED_REGION *lr, int wc, int hc)\n'
        + '{\n'
        + '#define SWAP(x, y) do { unsigned char t = x; x = y; y = t; } while (0)\n'
        + '#undef SWAP\n'
        + '}\n\n'
        + 'static ALLEGRO_LOCKED_REGION *ogl_lock_compressed_region(ALLEGRO_BITMAP *bitmap,\n',
    ],
    ['addons/audio/audio.c', 'int al_get_num_audio_output_devices()\n'],
    ['addons/image/pcx.c', '         char *dest = (char*)lr->data + y*lr->pitch;\n'],
    ['addons/audio/sdl_audio.c', '      int count = SDL_min(len, r->samples * r->sample_size);\n'],
    [
      'addons/primitives/prim_soft.c',
      '      int ii;\n'
        + '      int n = 0;\n'
        + '      const char* vtxptr = (const char*)vtxs + start * stride;\n'
        + '      for (ii = 0; ii < num_vtx; ii++) {\n'
        + '         convert_vtx(texture, vtxptr, &vertex_cache[ii], decl);\n'
        + '         al_transform_coordinates(global_trans, &vertex_cache[ii].x, &vertex_cache[ii].y);\n'
        + '         n++;\n'
        + '         vtxptr += stride;\n'
        + '      }\n',
    ],
    [
      'addons/acodec/acodec.c',
      '   bool acodec_prefer_dumb = false;\n'
        + '   const char* acodec_prefer_dumb_value =\n'
        + '      al_get_config_value(al_get_system_config(), "compatibility", "acodec_prefer_dumb");\n'
        + '   if (acodec_prefer_dumb_value && strcmp(acodec_prefer_dumb_value, "true") == 0)\n'
        + '      acodec_prefer_dumb = true;\n',
    ],
  ]);
  for (const [relativePath, content] of fixtures) await writeFixture(root, relativePath, content);
  const { patchAllegroSource } = await import('../lib/source-compatibility.ts');

  assert.equal(patchAllegroSource(root), true);
  assert.equal(
    await readFile(path.join(root, 'CMakeLists.txt'), 'utf8'),
    'cmake_minimum_required(VERSION 3.10)\nfind_package(PkgConfig QUIET)\n',
  );
  assert.equal(await readFile(path.join(root, 'src/shader.c'), 'utf8'), 'ALLEGRO_SHADER *al_get_current_shader(void)\n');
  assert.equal(await readFile(path.join(root, 'src/sdl/sdl_system.c'), 'utf8'), '   SDL_Cursor *cursor = NULL;\n');
  assert.match(await readFile(path.join(root, 'src/opengl/extensions.c'), 'utf8'), /#if !defined ALLEGRO_CFG_OPENGLES\ntypedef/);
  assert.match(await readFile(path.join(root, 'src/opengl/extensions.c'), 'utf8'), /return NULL;\n}\n#endif/);
  assert.match(await readFile(path.join(root, 'src/opengl/ogl_bitmap.c'), 'utf8'), /#if !defined ALLEGRO_CFG_OPENGLES\nstatic bool/);
  assert.match(await readFile(path.join(root, 'src/opengl/ogl_bitmap.c'), 'utf8'), /#undef SWAP\n}\n#endif/);
  assert.equal(await readFile(path.join(root, 'addons/audio/audio.c'), 'utf8'), 'int al_get_num_audio_output_devices(void)\n');
  assert.match(await readFile(path.join(root, 'addons/image/pcx.c'), 'utf8'), /unsigned char \*dest/);
  assert.match(await readFile(path.join(root, 'addons/audio/sdl_audio.c'), 'utf8'), /SDL_min\(len, \(int\)\(r->samples \* r->sample_size\)\)/);
  assert.equal(
    await readFile(path.join(root, 'addons/primitives/prim_soft.c'), 'utf8'),
    '      int ii;\n'
      + '      const char* vtxptr = (const char*)vtxs + start * stride;\n'
      + '      for (ii = 0; ii < num_vtx; ii++) {\n'
      + '         convert_vtx(texture, vtxptr, &vertex_cache[ii], decl);\n'
      + '         al_transform_coordinates(global_trans, &vertex_cache[ii].x, &vertex_cache[ii].y);\n'
      + '         vtxptr += stride;\n'
      + '      }\n',
  );
  assert.match(await readFile(path.join(root, 'addons/acodec/acodec.c'), 'utf8'), /^#ifdef ALLEGRO_CFG_ACODEC_DUMB/m);
  assert.equal(patchAllegroSource(root), false, 'patch must be idempotent');
});

test('source compatibility patch rejects an unknown upstream source shape', async (context) => {
  const root = await fixtureRoot(context);
  await writeFixture(root, 'src/external/miniaudio.h', 'unexpected source\n');
  await writeFixture(
    root,
    'src/external/stb_vorbis.c',
    'if (f->stream_start + loc >= f->stream_end || f->stream_start + loc < f->stream_start) {\n}\n',
  );
  const { patchRaylibSource } = await import('../lib/source-compatibility.ts');

  assert.throws(() => patchRaylibSource(root), /raylib miniaudio Emscripten major version macro/);
});

test('canvas runtime compiler treats copied sysroot headers as system headers', async () => {
  const { canvasRuntimeCompilerArguments } = await import('../lib/canvas-runtime-build.ts');
  const argumentsList = canvasRuntimeCompilerArguments({
    compiler: '/tool/emcc',
    sourcePath: '/tmp/stub.c',
    libraryPaths: ['/sysroot/usr/lib/liballegro.a'],
    includeDirectories: ['/sysroot/usr/include/allegro5'],
    systemIncludeDirectories: ['/sysroot/usr/include'],
    flags: ['-O2'],
    outputDirectory: '/output',
    runtimeName: 'allegro-runtime',
  }, '/tmp/allegro-runtime.mjs');

  assert.deepEqual(argumentsList, [
    '"/tool/emcc"',
    '"/tmp/stub.c"',
    '"/sysroot/usr/lib/liballegro.a"',
    '-I"/sysroot/usr/include/allegro5"',
    '-isystem "/sysroot/usr/include"',
    '-O2',
    '-o "/tmp/allegro-runtime.mjs"',
  ]);
});
