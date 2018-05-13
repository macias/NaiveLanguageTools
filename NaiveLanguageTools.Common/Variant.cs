using System;

namespace NaiveLanguageTools.Common
{
    /// T is common type
    public class Variant<T, R1, R2>
        where R1 : T
        where R2 : T
    {
        private readonly object _value;
        public bool HasValue { get { return _value != null; } }
        public T Value { get { return (T)_value; } }

        public Variant()
        {
            _value = null;
        }
        public Variant(T v)
        {
            _value = v;

            if (_value != null)
                assertValue();
        }

        public override string ToString()
        {
            return HasValue ? Value.ToString() : "∅";
        }
        public override bool Equals(object obj)
        {
            if (obj is Variant<T, R1, R2>)
                return this.Equals(obj as Variant<T, R1, R2>);
            else
                throw new ArgumentException();
        }

        public bool Equals(Variant<T, R1, R2> comp)
        {
            if (Object.ReferenceEquals(comp, null))
                return false;

            if (Object.ReferenceEquals(this, comp))
                return true;

            if (!this.HasValue)
                return !comp.HasValue;
            else if (!comp.HasValue)
                return false;
            else if (this.Is<R1>())
            {
                if (!comp.Is<R1>())
                    return false;
                else
                    return this.As<R1>().Equals(comp.As<R1>());
            }
            else if (this.Is<R2>())
            {
                if (!comp.Is<R2>())
                    return false;
                else
                    return this.As<R2>().Equals(comp.As<R2>());
            }
            else
                throw new NotImplementedException();
        }
        public override int GetHashCode()
        {
            if (!this.HasValue)
                return 0;
            else if (this.Is<R1>())
            {
                return this.As<R1>().GetHashCode();
            }
            else if (this.Is<R2>())
            {
                return this.As<R2>().GetHashCode();
            }
            else
                throw new NotImplementedException();
        }
        protected virtual void assertValue()
        {
            if (!Is<R1>() && !Is<R2>())
                throw new ArgumentException();
        }
        protected virtual void assertType<X>()
        {
            if (typeof(X) != typeof(R1) && typeof(X) != typeof(R2))
                throw new ArgumentException();
        }
        public bool Is<X>()
        {
            assertType<X>();
            return _value is X;
        }
        public X As<X>()
        {
            assertType<X>();
            return (X)_value;
        }
    }

}
