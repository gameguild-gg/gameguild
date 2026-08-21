import { access, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

export const PATCH_SET_VERSION = 'emception-glue-v1';

const ENV_NEEDLE = 'var ENV={};';
const ENV_MARKER = 'moduleArg["ENV"]';
const SYSTEM_NEEDLE = 'if(!command)return 0;return-52';
const SYSTEM_ASYNC_ONLY = 'if(!command)return 0;if(Module["systemCallback"]){return Asyncify.handleAsync(function(){return Module["systemCallback"](UTF8ToString(command))})}return-52';
const SYSTEM_BARE = 'if(!command)return 0;if(Module["systemCallback"]){return Module["systemCallback"](UTF8ToString(command))}return-52';
const SYSTEM_REPLACEMENT = 'if(!command)return 0;if(Module["systemCallbackSync"]){var sr=Module["systemCallbackSync"](UTF8ToString(command));if(sr!==undefined)return sr}if(Module["systemCallback"]){return Asyncify.handleAsync(function(){return Module["systemCallback"](UTF8ToString(command))})}return-52';
const OPENAT_NEEDLE = 'path=SYSCALLS.getStr(path);path=SYSCALLS.calculateAt(dirfd,path);var mode=varargs?syscallGetVarargI():0;';

function patchEnv(content, applied) {
  if (content.includes(ENV_MARKER) || !content.includes(ENV_NEEDLE)) return content;
  applied.push('env');
  return content.replace(
    ENV_NEEDLE,
    `${ENV_NEEDLE}if(moduleArg&&moduleArg["ENV"]){for(var _k in moduleArg["ENV"]){ENV[_k]=moduleArg["ENV"][_k]}}`,
  );
}

function patchSystem(content, filename, applied) {
  if (!content.includes('__emscripten_system') || content.includes('Module["systemCallbackSync"]')) return content;
  const supportedNeedle = [SYSTEM_ASYNC_ONLY, SYSTEM_BARE, SYSTEM_NEEDLE].find((needle) => content.includes(needle));
  if (!supportedNeedle) {
    throw new Error(`${filename}: unsupported __emscripten_system shape`);
  }
  applied.push('system');
  return content.replace(supportedNeedle, SYSTEM_REPLACEMENT);
}

function patchCallMain(content, applied) {
  const oldTail = 'try{var ret=entryFunction(argc,argv);exitJS(ret,true);return ret}catch(e){return handleException(e)}';
  const newTail = 'try{var ret=entryFunction(argc,argv);if(typeof Asyncify!=="undefined"&&Asyncify.currData){return Asyncify.whenDone().then(function(r){try{exitJS(r,true);return r}catch(e){return handleException(e)}},function(e){return handleException(e)});}exitJS(ret,true);return ret}catch(e){return handleException(e)}';
  if (content.includes('args.unshift(thisProgram)') && content.includes(oldTail)) {
    applied.push('callMain');
    return content.replace(oldTail, newTail);
  }
  if (!content.includes('var argc=0;var argv=0;') || content.includes('args.unshift(thisProgram)')) return content;

  const functionStart = content.indexOf('function callMain()');
  const bodyStart = content.indexOf('{', functionStart);
  if (functionStart === -1 || bodyStart === -1) {
    throw new Error('unsupported callMain shape');
  }
  let depth = 0;
  let bodyEnd = -1;
  for (let index = bodyStart; index < content.length; index += 1) {
    if (content[index] === '{') depth += 1;
    if (content[index] === '}') depth -= 1;
    if (depth === 0) {
      bodyEnd = index;
      break;
    }
  }
  if (bodyEnd === -1) throw new Error('unterminated callMain body');

  const replacement = [
    'function callMain(args=[])', '{', 'var entryFunction=_main;', 'args.unshift(thisProgram);',
    'var argc=args.length;', 'var oldPages=(wasmMemory.buffer.byteLength/65536)|0;', 'wasmMemory.grow(1);',
    'updateMemoryViews();', 'var scratch=oldPages*65536;', 'var argv=scratch;',
    'var strBase=scratch+(argc+1)*4;', 'for(var i=0;i<argc;i++){',
    'HEAPU32[(argv>>2)+i]=strBase;', 'var len=lengthBytesUTF8(args[i])+1;',
    'stringToUTF8Array(args[i],HEAPU8,strBase,len);', 'strBase+=len;', '}',
    'HEAPU32[(argv>>2)+argc]=0;', 'try{var ret=entryFunction(argc,argv);',
    'if(typeof Asyncify!=="undefined"&&Asyncify.currData){',
    'return Asyncify.whenDone().then(function(r){try{exitJS(r,true);return r}catch(e){return handleException(e)}},function(e){return handleException(e)});',
    '}', 'exitJS(ret,true);', 'return ret}catch(e){return handleException(e)}', '}',
  ].join('');
  applied.push('callMain');
  return content.slice(0, functionStart) + replacement + content.slice(bodyEnd + 1);
}

function patchOpenat(content, applied) {
  if (content.includes('onPreOpen') || !content.includes(OPENAT_NEEDLE)) return content;
  const subprocess = 'if(path==="/tmp/__dispatch_subprocess__"&&Module["subprocessDispatch"]){return Asyncify.handleAsync(function(){return Module["subprocessDispatch"]().then(function(){return FS.open(path,flags,mode).fd})})}';
  const vfs = 'if((!Module["isCachedSync"]||!Module["isCachedSync"](path))&&Module["onPreOpen"]){return Asyncify.handleAsync(function(){return Module["onPreOpen"](path).then(function(){return FS.open(path,flags,mode).fd})})}';
  applied.push('openat');
  return content.replace(OPENAT_NEEDLE, OPENAT_NEEDLE + subprocess + vfs);
}

export function applyGluePatches(source, filename) {
  const applied = [];
  let content = patchEnv(source, applied);
  content = patchCallMain(content, applied);
  content = patchSystem(content, filename, applied);
  content = patchOpenat(content, applied);
  return { content, applied };
}

async function exists(filePath) {
  try {
    await access(filePath);
    return true;
  } catch {
    return false;
  }
}

export async function patchGlueDirectory({ libDirectory, tools }) {
  let foundFiles = 0;
  let changedFiles = 0;
  let patchCount = 0;
  for (const tool of tools) {
    const gluePath = path.join(libDirectory, `${tool}.mjs`);
    const wasmPath = path.join(libDirectory, `${tool}.wasm`);
    const [hasGlue, hasWasm] = await Promise.all([exists(gluePath), exists(wasmPath)]);
    if (hasGlue !== hasWasm) {
      const present = hasWasm ? `${tool}.wasm` : `${tool}.mjs`;
      const missing = hasWasm ? `${tool}.mjs` : `${tool}.wasm`;
      throw new Error(`${present} exists without ${missing} in ${libDirectory}`);
    }
    if (!hasGlue) continue;
    foundFiles += 1;
    const source = await readFile(gluePath, 'utf8');
    const result = applyGluePatches(source, path.basename(gluePath));
    patchCount += result.applied.length;
    if (result.content !== source) {
      await writeFile(gluePath, result.content);
      changedFiles += 1;
    }
  }
  if (foundFiles === 0) throw new Error(`no generated tool glue found in ${libDirectory}`);
  return { foundFiles, changedFiles, patchCount };
}
