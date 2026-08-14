import { MINI_DOCTEST_H, parseMiniDoctest } from './doctest-header';

describe('parseMiniDoctest', () => {
  it('parses a clean SUCCESS summary', () => {
    const r = parseMiniDoctest(
      [
        'TEST CASE:  0:add',
        '===============================================================================',
        '[doctest] test cases:      1 |      1 passed |      0 failed | 0 skipped',
        '[doctest] assertions:      1 |      1 passed |      0 failed |',
        '[doctest] Status: SUCCESS!',
      ].join('\n'),
    );
    expect(r.status).toBe('success');
    expect(r.casesFailed).toBe(0);
    expect(r.failures).toEqual([]);
  });

  it('parses FAILURE and extracts CHECK failure lines', () => {
    const r = parseMiniDoctest(
      [
        '/home/user/functional_combined_0.cpp:9: ERROR: CHECK( add(2, 3) == 5 ) is NOT correct!',
        '[doctest] test cases:      1 |      0 passed |      1 failed | 0 skipped',
        '[doctest] Status: FAILURE!',
      ].join('\n'),
    );
    expect(r.status).toBe('failure');
    expect(r.casesFailed).toBe(1);
    expect(r.failures).toEqual(['/home/user/functional_combined_0.cpp:9: ERROR: CHECK( add(2, 3) == 5 ) is NOT correct!']);
  });

  it('survives fd_write chunk splits inserted as newlines mid-token', () => {
    // Regression: wasi-run joins stdout chunks with '\n', splitting
    // `SUCCESS!` into `SUCCESS\n!` and breaking line-based regexes.
    const r = parseMiniDoctest(
      [
        'TEST CASE:  0:',
        '',
        '[doctest] test cases:      1 |      1 passed |      0',
        ' failed | 0 skipped',
        '[doctest] Status: SUCCESS',
        '!',
      ].join('\n'),
    );
    expect(r.status).toBe('success');
    expect(r.casesFailed).toBe(0);
  });

  it('reports crash when no status summary was printed', () => {
    const r = parseMiniDoctest('Segmentation fault (core dumped)');
    expect(r.status).toBe('crash');
    expect(r.casesFailed).toBe(-1);
  });
});

describe('MINI_DOCTEST_H', () => {
  it('self-defines main and the registration machinery', () => {
    expect(MINI_DOCTEST_H).toContain('#pragma once');
    expect(MINI_DOCTEST_H).toContain('int main()');
    expect(MINI_DOCTEST_H).toContain('#define TEST_CASE(name)');
    expect(MINI_DOCTEST_H).toContain('#define CHECK(expr)');
    expect(MINI_DOCTEST_H).toContain('[doctest] Status: %s!');
  });
});
