﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PalindromeGenerator
{
    internal static class Program
    {
        private static int PromptForIntegerInput(string promptText, int min = int.MinValue, int max = Int32.MaxValue)
        {
            var diff = max - min;
            
            while (true)
            {
                Console.WriteLine(promptText);
                
                if (int.TryParse(Console.ReadLine(), out var selection) && unchecked((uint) (selection - min)) <= diff)
                {
                    return selection;
                }

                Console.WriteLine($"Invalid integer, please input again [ {min} - {max} ]");
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void Main()
        {
            // How to check codegen:
            // Mac:
            // export DOTNET_JitDisasm="Generate*Unit"
            // dotnet run -c Release
            
            var output = "Iteration count";

            var iterationCount = PromptForIntegerInput(output, 1);
            
            output = "Palindrome type to generate" +
                     "\n1 | Character-unit" +
                     "\n2 | Word-unit";

            var random = new Random();

            var stream = File.CreateText(OUTPUT_PATH);
            
            if (PromptForIntegerInput(output, 1, 2) == 1)
            {
                GenerateCharUnit(iterationCount, random, stream);
            }

            else
            {
                GenerateWordUnit(iterationCount, random, stream);
            }
            
            stream.Close();

            Console.WriteLine($"File generated at: {OUTPUT_PATH}");
        }

        private static readonly string OUTPUT_PATH = Path.GetFullPath("output.json");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char GenerateAlphaNumericCharacter(Random random, bool constantGeneration)
        {
            if (constantGeneration)
            {
                return 'a';
            }
            
            // https://www.asciitable.com/
            
            const int CAPS_OFFSET = 'a' - 'A',
                      A_ASCII = 'A',
                      Z_ASCII = 'Z',
                      ZERO_ASCII = '0',
                      NINE_ASCII = '9';

            // The + 1 -s will be constant folded. They are required because maxValue parameter is exclusive.
            // Too much python...
            var shouldCapitalize = random.Next(-1, 0 + 1);
            
            var shouldGenerateNumbers = random.Next(-1, 0 + 1);
            
            var alphabet = random.Next(A_ASCII, Z_ASCII + 1) + (CAPS_OFFSET & shouldCapitalize);

            var number = random.Next(ZERO_ASCII, NINE_ASCII + 1);
            
            // TODO: Is cmov faster?
            return (char) ((alphabet & ~shouldGenerateNumbers) | (number & shouldGenerateNumbers)); 
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char GenerateNonAlphaNumericCharacter(Random random, bool constantGeneration)
        {
            if (constantGeneration)
            {
                return '@';
            }
            
            // https://www.asciitable.com/
            
            const int START_ASCII = ' ',
                      END_ASCII = '/';

            // The + 1 -s will be constant folded. They are required because maxValue parameter is exclusive.
            // Too much python...
            return (char) random.Next(START_ASCII, END_ASCII + 1);
        }
        
        private const int MAX_CHARS = 100;

        //[StructLayout(LayoutKind.Explicit, Size = MAX_CHARS * sizeof(char))]
        [InlineArray(MAX_CHARS)]
        private struct CharBuffer
        {
            //[FieldOffset(0)]
            public char first;
        }
        
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static unsafe void GenerateCharUnit(int iterationCount, Random random, StreamWriter stream)
        {
            const bool CONSTANT_GENERATION = false,
                       OUTPUT_ENABLED = true,
                       GENERATE_SINGLE_CHAR_SEQUENCE = true,
                       GENERATE_EVEN_SEQUENCE = true,
                       GENERATE_ODD_SEQUENCE = true;
            
            // https://github.com/dotnet/runtime/issues/52979
            // var charBuffer = stackalloc char[MAX_CHARS];
            
            CharBuffer charBufferVar;

            var charBuffer = &charBufferVar.first;
            
            // This includes '\n'
            const int SINGLE_CHAR_VARIANTS_TOTAL_LENGTH = 3;

            ReadOnlySpan<char> singleCharSpan;

            if (OUTPUT_ENABLED)
            {
                singleCharSpan = MemoryMarshal.CreateReadOnlySpan(ref *charBuffer, SINGLE_CHAR_VARIANTS_TOTAL_LENGTH);
            }
            
            for (int currentIterationCount = 1; currentIterationCount <= iterationCount; currentIterationCount++)
            {
                // Generate single character sequence
                if (GENERATE_SINGLE_CHAR_SEQUENCE)
                {
                    // Generate alphanumeric single character
                    *charBuffer = GenerateAlphaNumericCharacter(random, CONSTANT_GENERATION);
                    
                    *(charBuffer + 1) = '\n';
                    
                    // Generate non-alphanumeric single character
                    *(charBuffer + 2) = GenerateNonAlphaNumericCharacter(random, CONSTANT_GENERATION);

                    if (OUTPUT_ENABLED)
                    {
                        stream.WriteLine(singleCharSpan);
                    }
                }
                
                const int MIN_CHARS = 4;

                int currentCount, currentHalfCount;

                char* firstHalfLastPtrOffsetByOne, secondHalfCurrentPtr, firstHalfCurrentPtr;

                ReadOnlySpan<char> span;
                
                // Generate even sequence
                if (GENERATE_EVEN_SEQUENCE)
                {
                    currentCount = random.Next(MIN_CHARS, MAX_CHARS);

                    currentCount = (currentCount % 2 == 0) ? currentCount : currentCount - 1;

                    currentHalfCount = currentCount / 2;

                    //      ( Half )
                    //      v
                    // [0, 1, 2, 3] Count: 4, Half-count: 2
                    firstHalfLastPtrOffsetByOne = charBuffer + currentHalfCount;

                    // Start from last element of second-half
                    secondHalfCurrentPtr = charBuffer + currentCount - 1;

                    firstHalfCurrentPtr = charBuffer;
                
                    for (; firstHalfCurrentPtr != firstHalfLastPtrOffsetByOne; firstHalfCurrentPtr++, secondHalfCurrentPtr--)
                    {
                        *firstHalfCurrentPtr = *secondHalfCurrentPtr = GenerateAlphaNumericCharacter(random, CONSTANT_GENERATION);
                    }
                
                    span = MemoryMarshal.CreateReadOnlySpan(ref *charBuffer, currentCount);

                    // TODO: The JIT compiler should be smart enough to eliminate this whole expression in RELEASE mode
                    ValidateIsCharUnitPalindrome(span, $"1 | {nameof(GenerateCharUnit)}");
                
                    if (OUTPUT_ENABLED)
                    {
                        stream.WriteLine(span);
                    }
                }
                
                // Generate odd sequence
                if (GENERATE_ODD_SEQUENCE)
                {
                    currentCount = random.Next(MIN_CHARS, MAX_CHARS);
                
                    currentCount = (currentCount % 2 != 0) ? currentCount : currentCount - 1;
                
                    // Division truncates. So currentCount == (currentHalfCount * 2) + 1
                    currentHalfCount = currentCount / 2;
                
                    firstHalfLastPtrOffsetByOne = charBuffer + currentHalfCount;
                
                    secondHalfCurrentPtr = charBuffer + currentCount - 1;
                
                    firstHalfCurrentPtr = charBuffer;
                
                    for (; firstHalfCurrentPtr != firstHalfLastPtrOffsetByOne; firstHalfCurrentPtr++, secondHalfCurrentPtr--)
                    {
                        *firstHalfCurrentPtr = *secondHalfCurrentPtr = GenerateAlphaNumericCharacter(random, CONSTANT_GENERATION);
                    }

                    Debug.Assert(firstHalfCurrentPtr == secondHalfCurrentPtr);
                
                    *firstHalfCurrentPtr = GenerateAlphaNumericCharacter(random, CONSTANT_GENERATION);

                    span = MemoryMarshal.CreateReadOnlySpan(ref *charBuffer, currentCount);
                
                    ValidateIsCharUnitPalindrome(span, $"2 | {nameof(GenerateCharUnit)}");
                }
                
                if (OUTPUT_ENABLED)
                { 
                    stream.WriteLine(span);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateIsCharUnitPalindrome(ReadOnlySpan<char> sequence, string methodName)
        {
            #if DEBUG
            // Performance is not paramount here
            
            // ODD
            // Length: 7, Halved ( Round up ): 4
            //   |   4    |
            //            |    4   |
            // [ 0, 1, 2, 3, 4, 5, 6 ]
            
            // EVEN
            // Length: 6, Halved ( Round up ): 3
            //   |  3  |
            //            |  3  |
            // [ 0, 1, 2, 3, 4, 5 ]

            var length = sequence.Length;
            
            var rem = length % 2;
            
            var lengthHalved = (length / 2) + rem;

            var firstHalf = sequence.Slice(0, lengthHalved);
            
            // If it is odd length, lengthHalved is the new start. Otherwise it is lengthHalved - 1
            // ( See above for more info )
            var secondHalfStartIndex = (rem == 0) ? lengthHalved : lengthHalved - 1; 
            
            var secondHalf = sequence.Slice(secondHalfStartIndex, lengthHalved);

            //   0  1  2
            // [ A, B, A ] Length: 3
            // ( 3 - 1 ) - 0 ( Left Index ) = 2, which is the index of the corresponding element ( Right index )
            // A ( Index 0 ) compared against A ( Index 2 )
            // ( 3 - 1 ) - 1 ( Left Index ) = 1, which is the index of the corresponding element ( Right index )
            // B ( Index 1 ) compared against B ( Index 1 )
            var lengthHalvedMinusOne = lengthHalved - 1;
            
            for (int leftIndex = 0; leftIndex < firstHalf.Length; leftIndex++)
            {
                var rightIndex = lengthHalvedMinusOne - leftIndex;

                if (firstHalf[leftIndex] != secondHalf[rightIndex])
                {
                    throw new Exception($"Sequence is NOT valid palindrome! \nMethod Name: {methodName}\nLeft Index: {leftIndex}\nRight Index: {rightIndex} ]");
                }
            }
            #endif
        }
        
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void GenerateWordUnit(int iterationCount, Random random, StreamWriter stream)
        {
            
        }
    }
}