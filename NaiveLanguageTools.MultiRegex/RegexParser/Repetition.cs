using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.MultiRegex.RegexParser
{
    internal class Repetition
    {
        public readonly int Min;
        public readonly int? Max;

        private Repetition(int min, int? max)
        {
            this.Min = min;
            this.Max = max;

            if (Max.HasValue && Max == 0)
                throw ParseControlException.NewAndRun("Max repetition cannot be zero.");
            if (Max.HasValue && Max < Min)
                throw ParseControlException.NewAndRun("Max repetition less than min repetition.");
        }
        public static Repetition Create(int min, int max)
        {
            return new Repetition(min, max );
        }
        public static Repetition CreateWithMin(int min)
        {
            return new Repetition(min,null);
        }
        public static Repetition CreateWithMax(int max)
        {
            return new Repetition(0,max);
        }

        public override string ToString()
        {
            return "{" + Min + "," + (Max.HasValue ? Max.Value.ToString() : "") + "}";
        }
    }
}
