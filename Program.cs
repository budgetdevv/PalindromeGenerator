using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PalindromeGenerator
{
    internal class Program
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
        
        private static void Main(string[] args)
        {
            string output;
            
            output = "Iteration count";

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
        }

        private const string OUTPUT_PATH = "output.json";
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char GenerateAlphaNumericCharacter(Random random)
        {
            // https://www.asciitable.com/
            
            const int CAPS_OFFSET = 'a' - 'A',
                      A_ASCII = 'A',
                      Z_ASCII = 'Z',
                      ZERO_ASCII = '0',
                      NINE_ASCII = '9';

            // The + 1 -s will be constant folded. They are required because
            // maxValue parameter is exclusive. Too much python...
            var shouldCapitalize = random.Next(-1, 0 + 1);
            
            var shouldGenerateNumbers = random.Next(-1, 0 + 1);
            
            var alphabet = random.Next(A_ASCII, Z_ASCII + 1) + (CAPS_OFFSET & shouldCapitalize);

            var number = random.Next(ZERO_ASCII, NINE_ASCII + 1);
            
            // TODO: Is cmov faster?
            return (char) ((alphabet & ~shouldGenerateNumbers) | (number & shouldGenerateNumbers)); 
        }
        
        private static unsafe void GenerateCharUnit(int iterationCount, Random random, StreamWriter stream)
        {
            const int MAX_CHARS = 100;
            
            var charBuffer = stackalloc char[MAX_CHARS];
            
            for (int currentIterationCount = 1; currentIterationCount <= iterationCount; currentIterationCount++)
            {
                // Generate single character sequence

                var current = GenerateAlphaNumericCharacter(random);
                
                stream.WriteLine(current);

                // Generate even sequence
                
                const int MIN_CHARS = 4;
                
                var currentCount = random.Next(MIN_CHARS, MAX_CHARS);

                currentCount = (currentCount % 2 == 0) ? currentCount : currentCount - 1;

                var currentHalfCount = currentCount / 2;

                //      ( Half )
                //      v
                // [0, 1, 2, 3] Count: 4, Half-count: 2
                var firstHalfLastPtrOffsetByOne = charBuffer + currentHalfCount;

                // Start from last element of second-half
                var secondHalfCurrentPtr = charBuffer + currentCount - 1;

                var firstHalfCurrentPtr = charBuffer;
                
                for (; firstHalfCurrentPtr != firstHalfLastPtrOffsetByOne; firstHalfCurrentPtr++, secondHalfCurrentPtr--)
                {
                    *firstHalfCurrentPtr = *secondHalfCurrentPtr = GenerateAlphaNumericCharacter(random);
                }
                
                stream.WriteLine(new ReadOnlySpan<char>(charBuffer, currentCount));
                
                // Generate odd sequence
                
                currentCount = random.Next(MIN_CHARS, MAX_CHARS);
                
                currentCount = (currentCount % 2 != 0) ? currentCount : currentCount - 1;
                
                // Division truncates. So currentCount == (currentHalfCount * 2) + 1
                currentHalfCount = currentCount / 2;
                
                firstHalfLastPtrOffsetByOne = charBuffer + currentHalfCount;
                
                secondHalfCurrentPtr = charBuffer + currentCount - 1;
                
                firstHalfCurrentPtr = charBuffer;
                
                for (; firstHalfCurrentPtr != firstHalfLastPtrOffsetByOne; firstHalfCurrentPtr++, secondHalfCurrentPtr--)
                {
                    *firstHalfCurrentPtr = *secondHalfCurrentPtr = GenerateAlphaNumericCharacter(random);
                }

                Debug.Assert(firstHalfCurrentPtr == secondHalfCurrentPtr);
                
                *firstHalfCurrentPtr = GenerateAlphaNumericCharacter(random);
                
                stream.WriteLine(new ReadOnlySpan<char>(charBuffer, currentCount));
            }
        }
        
        private static void GenerateWordUnit(int iterationCount, Random random, StreamWriter stream)
        {
            
        }
    }
}