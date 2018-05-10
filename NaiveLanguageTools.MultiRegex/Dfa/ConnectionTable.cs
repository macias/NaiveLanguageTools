using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;

namespace NaiveLanguageTools.MultiRegex.Dfa
{
    public class ConnectionTable
    {
        public const int IntNoValue = -1;

        public static ConnectionTable<T> Create<T>(int offset, T[] targets, params Tuple<int,bool>[] acceptingValues)
        {
            return new ConnectionTable<T>(offset, targets, acceptingValues);
        }
    }


    public class ConnectionTable<T>
    {
        static T noValue;

        static ConnectionTable()
        {
            // for T=class it will be null, so we are good
            ConnectionTable<int>.noValue = ConnectionTable.IntNoValue;
        }

        int offset;
        T[] targets;

        // they are sorted by first elem of pair (rule id), the bool flag -- priority (win fast)
        Tuple<int,bool>[] acceptingValues;
        internal IEnumerable<Tuple<int,bool>> AcceptingValues { get { return acceptingValues; } }

        internal ConnectionTable()
            : this(ConnectionTable.IntNoValue, new T[] { },Enumerable.Empty<Tuple<int,bool>>())
        {
        }

        internal ConnectionTable(int offset, T[] targets, IEnumerable< Tuple<int,bool>> acceptingValues)
        {
            this.acceptingValues = acceptingValues.ToArray();
            this.offset = offset;
            this.targets = targets;
        }

        public string Dump(bool richInfo)
        {
            return (richInfo ? "ConnectionTable.Create" : "")
                + "(" + offset + ","
                + (richInfo ? "new int []" : "")
                + "{" + targets.Select(it => it.ToString()).Join(",") + "}"
                + (acceptingValues.Any() ? "," : "")
                + acceptingValues.Select(it => "Tuple.Create("+ it.Item1+","+(it.Item2?"true":"false")+")").Join(",")
                + ")";
        }

        internal void AddTransition(int trans, T target)
        {
            if (EqualityComparer<T>.Default.Equals(target, noValue))
                throw new ArgumentException();

            if (offset == ConnectionTable.IntNoValue)
            {
                offset = trans;
                targets = new T[1];
            }
            else if (trans < offset)
            {
                targets = Enumerable.Repeat(noValue, offset - trans).Concat(targets).ToArray();
                offset = trans;
            }
            else if (trans >= offset + targets.Length)
                targets = targets.Concat(Enumerable.Repeat(noValue, trans - (offset + targets.Length) + 1)).ToArray();

            targets[trans - offset] = target;
        }


        internal T GetTarget(byte trans)
        {
            if (offset == ConnectionTable.IntNoValue || trans < offset || trans >= offset + targets.Length)
                return noValue;
            else
                return targets[trans - offset];
        }

        internal void SetAcceptingValues(IEnumerable<Tuple<int,bool>> values)
        {
            acceptingValues = values.Distinct().OrderBy(it => it).ToArray();
        }

        internal void AddAcceptingValues(IEnumerable<Tuple<int,bool>> values)
        {
            SetAcceptingValues(acceptingValues.Concat(values));
        }

        internal IEnumerable<Tuple<int, T>> GetConnections()
        {
            return targets
                .ZipWithIndex()
                .Where(it => !EqualityComparer<T>.Default.Equals(it.Item1, noValue))
                .Select(it => Tuple.Create(it.Item2 + offset, it.Item1));
        }


    }
}