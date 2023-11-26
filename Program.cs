using System.Globalization;
using System.Text;

namespace PalindromeGenerator
{
    internal class Program
    {
        private static int PromptForIntegerInput(string promptText, int min = int.MinValue, int max = Int32.MaxValue)
        {
            var diff = max - min;
            
            while (true)
            {
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

            var stringBuilder = new StringBuilder();
            
            if (PromptForIntegerInput(output, 1, 2) == 1)
            {
                GenerateCharUnit(iterationCount, random, stream);
            }

            else
            {
                GenerateWordUnit(iterationCount, random, stream, stringBuilder);
            }
        }

        private const string OUTPUT_PATH = "output.json";

        private static char GenerateAlphabet(Random random)
        {
            const int CAPS_OFFSET = 'a' - 'A',
                      A_ASCII = 'A',
                      Z_ASCII = 'Z';

            var shouldCapitalize = random.Next(0, -1);

            return (char) (random.Next(A_ASCII, Z_ASCII) + (CAPS_OFFSET & shouldCapitalize));
        }
        
        private static unsafe void GenerateCharUnit(int iterationCount, Random random, StreamWriter stream)
        {
            const int MIN = int.MinValue,
                      MAX = int.MaxValue,
                      MAX_CHARS = 4001,
                      MAX_CHARS_PER_HALF = MAX_CHARS / 2;
            
            var charBuffer = stackalloc char[MAX_CHARS];
            
            for (int currentIterationCount = 1; currentIterationCount <= iterationCount; currentIterationCount++)
            {
                // Generate single character sequence

                var current = (char) random.Next(MIN, MAX);
                
                stream.Write(current);

                var currentCount = random.Next(1, MAX_CHARS_PER_HALF);

                var currentPtr = charBuffer;

                var lastPtr = charBuffer + currentCount;

                for (; currentPtr != lastPtr; currentPtr++)
                {
                    *currentPtr = GenerateAlphabet(random);
                }
            }
        }
        
        private static void GenerateWordUnit(int iterationCount, Random random, StreamWriter streamWriter,
            StringBuilder stringBuilder)
        {
            
        }
    }
}