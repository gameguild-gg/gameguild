import fs from 'node:fs';
import path from 'node:path';

interface RequiredSourceEdit {
  readonly label: string;
  readonly before: string;
  readonly after: string;
}

function patchFile(root: string, relativePath: string, edits: readonly RequiredSourceEdit[]): boolean {
  const filename = path.join(root, relativePath);
  let source = fs.readFileSync(filename, 'utf8');
  let changed = false;

  for (const edit of edits) {
    if (source.includes(edit.after)) continue;
    if (source.includes(edit.before)) {
      source = source.replaceAll(edit.before, edit.after);
      changed = true;
      continue;
    }
    throw new Error(`${edit.label}: unsupported upstream source shape in ${filename}`);
  }

  if (changed) fs.writeFileSync(filename, source, 'utf8');
  return changed;
}

export function patchRaylibSource(sourceRoot: string): boolean {
  const miniaudioChanged = patchFile(sourceRoot, 'src/external/miniaudio.h', [
    {
      label: 'raylib miniaudio Emscripten major version macro',
      before: '__EMSCRIPTEN_major__',
      after: '__EMSCRIPTEN_MAJOR__',
    },
    {
      label: 'raylib miniaudio Emscripten minor version macro',
      before: '__EMSCRIPTEN_minor__',
      after: '__EMSCRIPTEN_MINOR__',
    },
    {
      label: 'raylib miniaudio Emscripten tiny version macro',
      before: '__EMSCRIPTEN_tiny__',
      after: '__EMSCRIPTEN_TINY__',
    },
  ]);
  const vorbisChanged = patchFile(sourceRoot, 'src/external/stb_vorbis.c', [
    {
      label: 'raylib stb_vorbis memory stream bounds check',
      before: 'if (f->stream_start + loc >= f->stream_end || f->stream_start + loc < f->stream_start) {',
      after: 'if (loc >= (unsigned int)(f->stream_end - f->stream_start)) {',
    },
  ]);
  return miniaudioChanged || vorbisChanged;
}

export function patchAllegroSource(sourceRoot: string): boolean {
  const changes = [
    patchFile(sourceRoot, 'CMakeLists.txt', [
      {
        label: 'Allegro CMake minimum version',
        before: 'cmake_minimum_required(VERSION 3.0)',
        after: 'cmake_minimum_required(VERSION 3.10)',
      },
      {
        label: 'Allegro optional pkg-config discovery',
        before: 'include(FindPkgConfig)',
        after: 'find_package(PkgConfig QUIET)',
      },
    ]),
    patchFile(sourceRoot, 'src/shader.c', [
      {
        label: 'Allegro current shader prototype',
        before: 'ALLEGRO_SHADER *al_get_current_shader()',
        after: 'ALLEGRO_SHADER *al_get_current_shader(void)',
      },
    ]),
    patchFile(sourceRoot, 'src/sdl/sdl_system.c', [
      {
        label: 'Allegro SDL cursor initialization',
        before: '   SDL_Cursor *cursor;',
        after: '   SDL_Cursor *cursor = NULL;',
      },
    ]),
    patchFile(sourceRoot, 'src/opengl/extensions.c', [
      {
        label: 'Allegro desktop OpenGL extension loader guard',
        before: 'typedef void (*VOID_FPTR)(void);',
        after: '#if !defined ALLEGRO_CFG_OPENGLES\ntypedef void (*VOID_FPTR)(void);',
      },
      {
        label: 'Allegro desktop OpenGL extension loader guard terminator',
        before: '}\n\n\n\n/* Load the extension API addresses into the table.',
        after: '}\n#endif\n\n\n\n/* Load the extension API addresses into the table.',
      },
    ]),
    patchFile(sourceRoot, 'src/opengl/ogl_bitmap.c', [
      {
        label: 'Allegro desktop compressed texture helper guard',
        before: 'static bool can_flip_blocks(ALLEGRO_PIXEL_FORMAT format)',
        after: '#if !defined ALLEGRO_CFG_OPENGLES\nstatic bool can_flip_blocks(ALLEGRO_PIXEL_FORMAT format)',
      },
      {
        label: 'Allegro desktop compressed texture helper guard terminator',
        before: '#undef SWAP\n}\n\nstatic ALLEGRO_LOCKED_REGION *ogl_lock_compressed_region',
        after: '#undef SWAP\n}\n#endif\n\nstatic ALLEGRO_LOCKED_REGION *ogl_lock_compressed_region',
      },
    ]),
    patchFile(sourceRoot, 'addons/audio/audio.c', [
      {
        label: 'Allegro audio device count prototype',
        before: 'int al_get_num_audio_output_devices()',
        after: 'int al_get_num_audio_output_devices(void)',
      },
    ]),
    patchFile(sourceRoot, 'addons/image/pcx.c', [
      {
        label: 'Allegro PCX unsigned pixel destination',
        before: '         char *dest = (char*)lr->data + y*lr->pitch;',
        after: '         unsigned char *dest = (unsigned char*)lr->data + y*lr->pitch;',
      },
    ]),
    patchFile(sourceRoot, 'addons/audio/sdl_audio.c', [
      {
        label: 'Allegro SDL recorder sample size comparison',
        before: '      int count = SDL_min(len, r->samples * r->sample_size);',
        after: '      int count = SDL_min(len, (int)(r->samples * r->sample_size));',
      },
    ]),
    patchFile(sourceRoot, 'addons/primitives/prim_soft.c', [
      {
        label: 'Allegro software primitive cache loop',
        before: '      int ii;\n'
          + '      int n = 0;\n'
          + '      const char* vtxptr = (const char*)vtxs + start * stride;\n'
          + '      for (ii = 0; ii < num_vtx; ii++) {\n'
          + '         convert_vtx(texture, vtxptr, &vertex_cache[ii], decl);\n'
          + '         al_transform_coordinates(global_trans, &vertex_cache[ii].x, &vertex_cache[ii].y);\n'
          + '         n++;\n'
          + '         vtxptr += stride;\n'
          + '      }\n',
        after: '      int ii;\n'
          + '      const char* vtxptr = (const char*)vtxs + start * stride;\n'
          + '      for (ii = 0; ii < num_vtx; ii++) {\n'
          + '         convert_vtx(texture, vtxptr, &vertex_cache[ii], decl);\n'
          + '         al_transform_coordinates(global_trans, &vertex_cache[ii].x, &vertex_cache[ii].y);\n'
          + '         vtxptr += stride;\n'
          + '      }\n',
      },
    ]),
    patchFile(sourceRoot, 'addons/acodec/acodec.c', [
      {
        label: 'Allegro DUMB codec preference configuration',
        before: '   bool acodec_prefer_dumb = false;\n'
          + '   const char* acodec_prefer_dumb_value =\n'
          + '      al_get_config_value(al_get_system_config(), "compatibility", "acodec_prefer_dumb");\n'
          + '   if (acodec_prefer_dumb_value && strcmp(acodec_prefer_dumb_value, "true") == 0)\n'
          + '      acodec_prefer_dumb = true;\n',
        after: '#ifdef ALLEGRO_CFG_ACODEC_DUMB\n'
          + '   bool acodec_prefer_dumb = false;\n'
          + '   const char* acodec_prefer_dumb_value =\n'
          + '      al_get_config_value(al_get_system_config(), "compatibility", "acodec_prefer_dumb");\n'
          + '   if (acodec_prefer_dumb_value && strcmp(acodec_prefer_dumb_value, "true") == 0)\n'
          + '      acodec_prefer_dumb = true;\n'
          + '#endif\n',
      },
    ]),
  ];
  return changes.some(Boolean);
}
