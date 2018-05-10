using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser.InOut;
using NaiveLanguageTools.Parser.Automaton;

namespace NaiveLanguageTools.Parser
{
    public interface ISliceView<T>
    {
        T this[int index] { get; }
    }

    // lame but sufficient for our needs
    public class ArraySlice<T> : ISliceView<T>
    {
        private T[] data;
        public int Offset;

        public T Head { get { return data[Offset]; } }
        public IEnumerable<T> View { get { return data.Skip(Offset); } }

        public T this[int index]
        {
            get
            {
                return data[Offset + index];
            }
        }

        public ArraySlice(T[] data)
        {
            this.data = data;
            this.Offset = 0;
        }
    }


}
