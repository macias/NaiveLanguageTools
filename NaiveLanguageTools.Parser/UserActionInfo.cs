using System;
using System.Text.RegularExpressions;
using System.Linq;
using NaiveLanguageTools.Common;
using System.Collections.Generic;

namespace NaiveLanguageTools.Parser
{
    public sealed class UserActionInfo
    {
        public static bool ExecutionEquals<TREE_NODE>(UserActionInfo<TREE_NODE> thisAction, int thisRhsSeenCount,
            UserActionInfo<TREE_NODE> otherAction, int otherRhsSeenCount)
        where TREE_NODE : class
        {
            if (thisAction == null && otherAction == null)
                return true;
            if (thisAction == null || otherAction == null)
                return false;

            if (thisAction.Code != otherAction.Code)
                return false;

            if (thisRhsSeenCount != otherRhsSeenCount)
                throw new ArgumentException("Should not happen.");

            // if we passed by unused params it means the execution varies (depending on passed, i.e. active, parameters)
            if (thisRhsSeenCount > thisAction.RhsUnusedParamsCount || otherRhsSeenCount > otherAction.RhsUnusedParamsCount) 
                return false;

            return true;
        }
    }
 
    public sealed class UserActionInfo<TREE_NODE>
        where TREE_NODE : class
    {
        // how many parameters (counting without interruption, from left) are not used in user code
        // giving always 0 is safe, but kills optimization (which happens for manually added productions, in C# directly)
        public readonly int RhsUnusedParamsCount; 
        public readonly Func<object[],TREE_NODE> Code;

        public UserActionInfo(int unused,Func<object[], TREE_NODE> code)
        {
            this.RhsUnusedParamsCount = unused;
            this.Code = code;
        }

        public override bool Equals(System.Object other)
        {
            return Equals(other as UserActionInfo<TREE_NODE>);
        }
        public bool Equals(UserActionInfo<TREE_NODE> other)
        {
            if (Object.ReferenceEquals(other, null))
                return false;

            if (Object.ReferenceEquals(this, other))
                return true;

            return Object.Equals(this.Code, other.Code) && this.RhsUnusedParamsCount == other.RhsUnusedParamsCount;
        }
        public override int GetHashCode()
        {
            return RhsUnusedParamsCount ^ Code.GetHashCode();
        }
    }

}

