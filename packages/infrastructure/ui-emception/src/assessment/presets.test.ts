import { createAssessmentWorkspaceConfig } from './presets';

describe('createAssessmentWorkspaceConfig', () => {
  it('converts the legacy graphics descriptor into the vanilla canvas and toolchain contracts', () => {
    const files = {
      '/user/solution.cpp': { encoding: 'text' as const, content: 'int main() { return 0; }' },
    };

    const config = createAssessmentWorkspaceConfig('sdl-cpp', files);

    expect(config.run.type).toBe('canvas');
    expect(config.compile.toolchain).toBe('sdl-cpp');
    expect(config.compile.sourceDetect?.entryPoint).toBe('/home/user/sdl-main.cpp');
    expect(config.files).toEqual({
      '/home/user/solution.cpp': files['/user/solution.cpp'],
    });
  });

  it('preserves the terminal runtime while declaring the C++ toolchain', () => {
    const config = createAssessmentWorkspaceConfig('cpp', {});

    expect(config.run.type).toBe('wasi-terminal');
    expect(config.compile.toolchain).toBe('cpp');
  });
});
