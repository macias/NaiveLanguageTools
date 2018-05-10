using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaiveLanguageTools.Common
{
    // Dictionary which allows nulls 
    public class NullDictionary<K, V> : DynamicDictionary<K,V>
        where K : class
    {
        private Option<V> nullValue;

        public NullDictionary(Func<V> defGen = null) : base(defGen)
        {
        }

        public override V this[K key]
        {
            get
            {
                if (key == null)
                {
                    if (DefGen != null && !nullValue.HasValue)
                        nullValue = new Option<V>(DefGen());
                    if (!nullValue.HasValue)
                        throw new KeyNotFoundException();
                    return nullValue.Value;
                }
                else
                    return base[key];
            }
            set
            {
                if (key == null)
                    nullValue = new Option<V>(value);
                else
                    base[key] = value;
            }
        }

        public override IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            if (nullValue.HasValue)
                return ComboEnumerator.Create(new[] { new KeyValuePair<K, V>(null, nullValue.Value) }.ToList().GetEnumerator(), base.GetEnumerator());
            else
                return base.GetEnumerator();
        }
    }
}
