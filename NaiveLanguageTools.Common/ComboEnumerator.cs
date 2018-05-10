using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    // analog to concatenating two ienumerables -- here we "concatenate" two enumerators
    public static class ComboEnumerator
    {
        public static ComboEnumerator<T> Create<T>(params IEnumerator<T>[] enumerators)
        {
            return new ComboEnumerator<T>(enumerators);
        }
    }
    public class ComboEnumerator<T> : IEnumerator<T>
    {
        private IEnumerator<T>[] enumerators;
        private int index;

        public ComboEnumerator(params IEnumerator<T>[] enumerators)
        {
            this.enumerators = enumerators;
            this.index = 0;
        }
        public T Current
        {
            get { return enumerators[index].Current; }
        }

        public void Dispose()
        {
            enumerators.ForEach(it => it.Dispose());
        }

        object System.Collections.IEnumerator.Current
        {
            get { return this.Current; }
        }

        public bool MoveNext()
        {
            if (index == enumerators.Length )
                return false;
            else if (enumerators[index].MoveNext())
                return true;
            else
            {
                ++index;
                return this.MoveNext();
            }
        }

        public void Reset()
        {
            enumerators.ForEach(it => it.Reset());
            index = 0;
        }
    }
}
