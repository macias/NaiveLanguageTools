using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{

    static public class OptionTraits
    {
        static public T? ToNullable<T>(this Option<T> this_)
            where T : struct
        {
            if (this_.HasValue)
                return this_.Value;
            else
                return null;
        }
    }

    public static class Option
    {
        public static Option<T> Create<T>(T value)
        {
            return new Option<T>(value);
        }
    }

    public struct Option<T>
    {
        private readonly T _value;
        public readonly bool HasValue;
        public T DefValue
        {
            get
            {
                if (!HasValue)
                    return default(T);
                return _value;
            }
        }
        public T Value
        {
            get
            {
                if (!HasValue)
                    throw new ArgumentException();
                return _value;
            }
        }

        public override string ToString()
        {
            return (!HasValue?"<unset>":String.Format("{0}",_value.ToString()));
        }
        public override int GetHashCode()
        {
            return HasValue.GetHashCode() ^ (HasValue ? _value.GetHashCode() : 0);
        }
        public override bool Equals(object obj)
        {
            return this.Equals((Option<T>)obj);
        }

        public bool Equals(Option<T> comp)
        {
            if (Object.ReferenceEquals(comp, null))
                return false;

            if (Object.ReferenceEquals(this, comp))
                return true;

            return HasValue.Equals(comp.HasValue) && Object.Equals(_value,comp._value);
        }

        public Option(T value) : this()
        {
            HasValue = true;
            _value = value;
        }

    }

}
