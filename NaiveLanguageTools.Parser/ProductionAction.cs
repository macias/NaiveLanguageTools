using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using System.Collections.ObjectModel;

namespace NaiveLanguageTools.Parser
{

    public static class ProductionAction<TREE_NODE>
        where TREE_NODE : class
    {
        private static R cast<R>(object obj)
        {
            try
            {
                return (R)obj;
            }
            catch
            {
                throw new InvalidCastException("Cast from '"+obj.GetType().FullName+"' to '" + typeof(R).FullName + "' failed.");
            }
        }
        public static UserActionInfo<TREE_NODE> Convert(Func<TREE_NODE> action,int unused = 0)
        {
            return  new UserActionInfo<TREE_NODE>(unused,(nodes) => action());
        }
        public static UserActionInfo<TREE_NODE> Convert<T1>(Func<T1, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2>(Func<T1, T2, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast < T2>(nodes[1])));
        }

        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3>(Func<T1, T2, T3, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast < T2>(nodes[1]), cast<T3>(nodes[2])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4>(Func<T1, T2, T3, T4, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast < T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7]), cast<T9>(nodes[8])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7]), cast<T9>(nodes[8]), cast<T10>(nodes[9])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7]), cast<T9>(nodes[8]), cast<T10>(nodes[9]), cast<T11>(nodes[10])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7]), cast<T9>(nodes[8]), cast<T10>(nodes[9]), cast<T11>(nodes[10]), cast<T12>(nodes[11])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7]), cast<T9>(nodes[8]), cast<T10>(nodes[9]), cast<T11>(nodes[10]), cast<T12>(nodes[11]), cast<T13>(nodes[12])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused,  (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7]), cast<T9>(nodes[8]), cast<T10>(nodes[9]), cast<T11>(nodes[10]), cast<T12>(nodes[11]), cast<T13>(nodes[12]),cast<T14>(nodes[13])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7]), cast<T9>(nodes[8]), cast<T10>(nodes[9]), cast<T11>(nodes[10]), cast<T12>(nodes[11]), cast<T13>(nodes[12]), cast<T14>(nodes[13]),cast<T15>(nodes[14])));
        }
        public static UserActionInfo<TREE_NODE> Convert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TREE_NODE> action, int unused = 0)
        {
            return new UserActionInfo<TREE_NODE>(unused, (nodes) => action(cast<T1>(nodes[0]), cast<T2>(nodes[1]), cast<T3>(nodes[2]), cast<T4>(nodes[3]), cast<T5>(nodes[4]), cast<T6>(nodes[5]), cast<T7>(nodes[6]), cast<T8>(nodes[7]), cast<T9>(nodes[8]), cast<T10>(nodes[9]), cast<T11>(nodes[10]), cast<T12>(nodes[11]), cast<T13>(nodes[12]), cast<T14>(nodes[13]), cast<T15>(nodes[14]), cast<T16>(nodes[15])));
        }
    }

}