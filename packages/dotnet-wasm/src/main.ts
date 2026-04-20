import { CSharpCompiler } from './index'

/**
 * Main entry point for standalone usage
 * This file can be used for testing and development
 */

async function main() {
  console.log('=== DotNet Web - C# Compiler Demo ===\n')

  const compiler = new CSharpCompiler()

  try {
    console.log('Initializing C# compiler...')
    await compiler.initialize()
    console.log('✓ Compiler ready\n')

    // Example 1: Hello World
    console.log('--- Example 1: Hello World ---')
    const helloWorld = `
using System;

class Program {
    static void Main() {
        Console.WriteLine("Hello from C#!");
    }
}
`
    const result1 = await compiler.execute(helloWorld)
    console.log('Output:', result1.output || result1.error)
    console.log('Execution Time:', result1.executionTime.toFixed(2), 'ms\n')
    console.log('Execution Time:', result1.executionTime.toFixed(2), 'ms\n')

    // Example 2: Math operations
    console.log('--- Example 2: Math Operations ---')
    const mathCode = `
using System;
using System.Linq;

class Program {
    static void Main() {
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var sum = numbers.Sum();
        var average = numbers.Average();
        
        Console.WriteLine($"Sum: {sum}");
        Console.WriteLine($"Average: {average}");
        Console.WriteLine($"Max: {numbers.Max()}");
    }
}
`
    const result2 = await compiler.execute(mathCode)
    console.log('Output:', result2.output || result2.error)
    console.log('Execution Time:', result2.executionTime.toFixed(2), 'ms\n')
    console.log('Execution Time:', result2.executionTime.toFixed(2), 'ms\n')

    // Example 3: Error handling
    console.log('--- Example 3: Compilation Error ---')
    const errorCode = `
using System;

class Program {
    static void Main() {
        Console.WriteLine("Missing semicolon")
        // This should cause a compilation error
    }
}
`
    const result3 = await compiler.execute(errorCode)
    console.log('Error:', result3.error)
    console.log('Execution Time:', result3.executionTime.toFixed(2), 'ms\n')

  } catch (error) {
    console.error('Error:', error)
  }
}

// Run if this file is executed directly
if (import.meta.url === `file://${process.argv[1]}` || 
    typeof window !== 'undefined') {
  main().catch(console.error)
}

export { main }
