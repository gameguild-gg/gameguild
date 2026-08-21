/*
 * Brotli decompression wrapper for Emscripten.
 *
 * Built into a MODULARIZE'd ES module by tools/emception/scripts/build-brotli.ts
 * and consumed by tools/emception/src/worker-entry.ts as a fallback for browsers
 * (or worker contexts) where DecompressionStream('br') is not available.
 *
 * Public API (called via cwrap from JS):
 *   uint8_t* brotli_decompress_buffer(const uint8_t* in, size_t in_len, size_t* out_len);
 *   void     brotli_free_buffer(uint8_t* ptr);
 *   const char* brotli_get_last_error_message(void);
 */

#include <stdlib.h>
#include <stddef.h>
#include <stdint.h>
#include <emscripten.h>

#include "brotli/decode.h"

static const char* g_last_error = NULL;

EMSCRIPTEN_KEEPALIVE
uint8_t* brotli_decompress_buffer(const uint8_t* input, size_t input_len, size_t* out_len) {
  g_last_error = NULL;
  if (out_len) *out_len = 0;

  /* Streaming decompress with growing output buffer. */
  size_t cap = input_len * 4 + 1024;
  uint8_t* out = (uint8_t*)malloc(cap);
  if (!out) {
    g_last_error = "out of memory";
    return NULL;
  }

  BrotliDecoderState* st = BrotliDecoderCreateInstance(NULL, NULL, NULL);
  if (!st) {
    free(out);
    g_last_error = "BrotliDecoderCreateInstance failed";
    return NULL;
  }

  const uint8_t* next_in = input;
  size_t avail_in = input_len;
  uint8_t* next_out = out;
  size_t avail_out = cap;
  size_t total_out = 0;

  BrotliDecoderResult r;
  for (;;) {
    r = BrotliDecoderDecompressStream(st, &avail_in, &next_in, &avail_out, &next_out, &total_out);

    if (r == BROTLI_DECODER_RESULT_SUCCESS) {
      break;
    } else if (r == BROTLI_DECODER_RESULT_NEEDS_MORE_OUTPUT) {
      size_t produced = (size_t)(next_out - out);
      size_t newcap = cap * 2;
      uint8_t* nb = (uint8_t*)realloc(out, newcap);
      if (!nb) {
        free(out);
        BrotliDecoderDestroyInstance(st);
        g_last_error = "out of memory";
        return NULL;
      }
      out = nb;
      next_out = out + produced;
      avail_out = newcap - produced;
      cap = newcap;
    } else {
      /* BROTLI_DECODER_RESULT_ERROR or NEEDS_MORE_INPUT (unexpected EOF). */
      if (r == BROTLI_DECODER_RESULT_NEEDS_MORE_INPUT) {
        g_last_error = "unexpected end of input";
      } else {
        g_last_error = BrotliDecoderErrorString(BrotliDecoderGetErrorCode(st));
      }
      free(out);
      BrotliDecoderDestroyInstance(st);
      return NULL;
    }
  }

  BrotliDecoderDestroyInstance(st);
  if (out_len) *out_len = (size_t)(next_out - out);
  return out;
}

EMSCRIPTEN_KEEPALIVE
void brotli_free_buffer(uint8_t* ptr) {
  free(ptr);
}

EMSCRIPTEN_KEEPALIVE
const char* brotli_get_last_error_message(void) {
  return g_last_error ? g_last_error : "";
}
