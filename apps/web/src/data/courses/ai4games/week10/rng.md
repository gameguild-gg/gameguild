# Pseudo Random Number Generation

You are a game developer in charge to create a fast an reliable random number generator for a procedural content generation system. The requirements are:

- Do not rely on external libraries;
- Dont need to be cryptographically secure;
- Be blazing fast;
- Fully reproducible via automated tests if used the same seed;
- Use exactly 32 bits as seed;
- Be able to generate a number between a given range, both inclusive.

So you remembered a strange professor talking about the xorshift algorithm and decided it is good enough for your use case. And with some small research, you found the Marsaglia "Xorshift RNGs". You decided to implement it and test it.

## XorShift

The xorshift is a family of pseudo random number generators created by George Marsaglia. The xorshift is a very simple algorithm that is very fast and have a good statistical quality. It is a very good choice for games and simulations.

`xorshift` is the process of shifting the binary value of a number and then `xor`'ing that binary to the original value to create a new value.

`value = value xor (value shift by number)`

The shift operators can be to the left `<<` or to the right `>>`. When shifted to the left, it is the same thing as multiplying by 2 at the power of the number. When shifted to the right, it is the same thing as dividing.

!!! note

    The value of a << b is the unique value congruent to \(a * 2^{b}\) modulo \( 2^{N} \) where \( N \) is the number of bits in the return type (that is, bitwise left shift is performed and the bits that get shifted out of the destination type are discarded).

    The value of \( a >> b \) is \( a/2^{b} \) rounded down (in other words, right shift on signed a is arithmetic right shift).

The `xorshift` algorithm from Marsaglia is a combination of 3 `xorshifts`, the first one is the seed (or the last random number generated), and the next ones are the result of the previous `xorshift`. The steps are:

1. `xorshift` the value by `13` bits to the left;
2. `xorshift` the value by `17` bits to the right;
3. `xorshift` the value by `5` bits to the left;

At the end of this 3 `xorshifts`, the current state of the value is your current random number.

In order to clamp a random number the value between two numbers (max and min), you should follow this idea:

`value = min + (random % (max - min + 1))`

## Input

Receives the seed `S`, the number `N` of random numbers to be generated and the range `R1` and `R2` of the numbers should be in, there is no guarantee the range numbers are in order. The range numbers are both inclusive. `S` and `N` are both `32 bits` unsigned integers and `R1` and `R2` are both `32 bits` signed integers.

```
1 1 0 99
```

## Output

The list of numbers to be generated, one per line. In this case, it would be only one and the random number should be clamped to be between 0 and 99.

```text
seed in decimal:       1
seed in binary:        0b00000000000000000000000000000001

seed:                  0b00000000000000000000000000000001
seed << 13:            0b00000000000000000010000000000000
seed xor (seed << 13): 0b00000000000000000010000000000001

seed:                  0b00000000000000000010000000000001
seed >> 17:            0b00000000000000000000000000000000
seed xor (seed >> 17): 0b00000000000000000010000000000001

seed:                  0b00000000000000000010000000000001
seed << 5:             0b00000000000001000000000000100000
seed xor (seed << 5):  0b00000000000001000010000000100001

The final result is 0b00000000000001000010000000100001 which is 270369 in decimal.
```

Now in order to clamp it to be between 0 and 99, we do:

```text
value = min + (random % (max - min + 1))
value = 0 + (270369 % (99 - 0 + 1))
value = 0 + (270369 % 100)
value = 0 + 69
value = 69
```

So this output would be:

```text
69
```
