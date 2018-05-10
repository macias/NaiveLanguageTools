using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NaiveLanguageTools.Generator.AST;
using NaiveLanguageTools.Lexer;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Generator.Builder;

namespace NaiveLanguageTools.Generator
{
    public class FactoryTypeInfo
    {
        public readonly string ClassName;
        public string Parent { get; private set; }
        public readonly bool WithOverride;
        public CodeBody Code { get; private set; }
        public CodeBody Params { get; private set; }

        public FactoryTypeInfo(string name,string parent, bool withOverride)
        {
            this.ClassName = name;
            this.Parent = parent;
            this.WithOverride = withOverride;
            this.Code = new CodeBody();
            this.Params = new CodeBody();
        }

        public override string ToString()
        {
            throw new Exception("Forbidden call.");
        }

        internal FactoryTypeInfo SetParent(string parent)
        {
            this.Parent = parent;
            return this;
        }

        internal FactoryTypeInfo SetCode(CodeBody code)
        {
            this.Code = code;
            return this;
        }

        internal FactoryTypeInfo SetParams(CodeBody paramsCode)
        {
            this.Params = paramsCode;
            return this;
        }
    }

    internal class PatternsInfo : FactoryTypeInfo
    {
        internal readonly string Namespace;
        internal readonly string DirectoryName;

        internal PatternsInfo(string fqClassname, LexPattern directory)
            : base(dotSplit(fqClassname).Item2, null, false)
        {
            this.Namespace = dotSplit(fqClassname).Item1;
            if (directory != null)
                this.DirectoryName = directory.StringContent().Replace('/', System.IO.Path.DirectorySeparatorChar);
        }
        private static Tuple<string, string> dotSplit(string s)
        {
            if (s == null)
                return Tuple.Create((string)null, (string)null);

            int idx = s.LastIndexOf('.');
            if (idx == -1)
                return Tuple.Create((string)null, s);
            else
                return Tuple.Create(s.Substring(0, idx), s.Substring(idx + 1));
        }
    }

    internal enum ConstMode
    {
        Enum,
        Int
    }

    public class ConstInfo
    {
        internal readonly string ClassName;
        internal readonly ConstMode Mode;

        internal string ElemTypeName { get { return Mode == ConstMode.Int ? "int" : ClassName; } }

        internal ConstInfo(string name, ConstMode mode)
        {
            this.ClassName = name;
            this.Mode = mode;
        }

        public override string ToString()
        {
            throw new Exception("Forbidden call.");
        }

        internal string FieldNameOf(string fieldName)
        {
            return ClassName + "." + fieldName;
        }
    }

    internal class TokenInfo : ConstInfo
    {
        internal TokenInfo(string name, ConstMode mode) : base(name,mode)
        {
        }

    }


    internal interface ILexPattern
    {
    }
    internal sealed class LexPatternName : ILexPattern
    {
        public readonly string Name;

        internal LexPatternName(string name)
        {
            this.Name = name;
        }
    }

    internal interface IScanningRule
    {
    }

    internal sealed class LexPatternVariable : IScanningRule
    {
        public readonly bool WithField;
        public readonly string Name;
        public readonly LexPattern Pattern;

        internal LexPatternVariable(bool withField, string name, LexPattern pattern)
        {
            this.WithField = withField;
            this.Name = name;
            this.Pattern = pattern;
        }
    }
    internal sealed class LexItem : IScanningRule
    {
        // "incoming" states, those which has to be matched 
        public IEnumerable<string> States { get { return states; } }
        private readonly HashSet<string> states;
        public bool HasPriority { get; private set; }

        public const string PopState = "-";
        private const string pushState = "+";

        // in short form, user can pass pair of token and (code/state) -- we cannot tell for sure when parsing
        // but later when we know all states, we can compare states to the probably code, if it is a match, it is not a code, but state
        private bool codeOrStateResolved;
        private string targetState;
        private CodeBody code;

        internal readonly IEnumerable<ILexPattern> InputPatterns; // this comes from the user
        internal LexPattern OutputPattern; // this we use when creating parser (output)
        internal readonly string TerminalName;
        public readonly bool IsExpression;
        // each entry is a sequence of alternative context (meaning this context OR that context)
        internal readonly IEnumerable<string> Context;
        // assigned after (and if) it is stored
        internal int? StorageId;

        internal string TargetPushState { get { return HasPopState ? null : targetState; } }
        internal bool HasPopState
        {
            get
            {
                if (!codeOrStateResolved)
                    throw new NotImplementedException("Internal error");
                return targetState == PopState;
            }
        }
        internal CodeBody Code
        {
            get
            {
                if (!codeOrStateResolved)
                    throw new NotImplementedException("Internal error");
                return code;
            }
        }
        internal bool HasTargetState
        {
            get
            {
                if (!codeOrStateResolved)
                    throw new NotImplementedException("Internal error");
                return targetState != null;
            }
        }

        private LexItem(IEnumerable<ILexPattern> patterns, IEnumerable<string> contextTokens, string token, string state, bool isExpression, CodeBody code, bool resolved)
        {
            this.states = new HashSet<string>();
            this.InputPatterns = patterns.ToArray();
            this.Context = contextTokens.ToArray();
            this.TerminalName = token;
            this.targetState = state;
            this.code = code;
            this.IsExpression = isExpression;

            this.codeOrStateResolved = (resolved || this.targetState != null || this.code == null);
        }
        private static IEnumerable<IEnumerable<string>> populateContexts(IEnumerable<IEnumerable<string>> context)
        {
            context = (context ?? Enumerable.Empty<IEnumerable<string>>());
            if (!context.Any())
                context = context.ToList().Append(Enumerable.Empty<string>());
            return context;
        }
        internal static IEnumerable<LexItem> AsExpression(IEnumerable<ILexPattern> patterns, IEnumerable<IEnumerable<string>> contextTokens,
            string token, string state, CodeBody code, bool resolved = false)
        {
            // we have to build up at least one empty context, otherwise we will not produce any elements
            return populateContexts(contextTokens).Select(ctx => new LexItem(patterns, ctx, token, state, true, code, resolved)).ToArray();
        }
        internal static IEnumerable<LexItem> AsDetectedStatement(IEnumerable<ILexPattern> patterns, IEnumerable<IEnumerable<string>> contextTokens, string token,
            string state, CodeBody code, bool resolved = false)
        {
            // we have to build up at least one empty context, otherwise we will not produce any elements
            return populateContexts(contextTokens).Select(ctx => new LexItem(patterns, ctx, token, state, !code.HasStatement, code, resolved)).ToArray();
        }

        public void ResolveCodeOrState(StatesInfo statesInfo)
        {
            if (codeOrStateResolved)
                return;

            string code_str = code.AsString().Trim().Replace(" ", "");
            if (PopState == code_str)
            {
                targetState = PopState;
                code = null;
            }
            else
            {
                string state = statesInfo.StateNames.SingleOrDefault(it => pushState + it == code_str);
                if (state != null)
                {
                    targetState = state;
                    code = null;
                }
            }

            codeOrStateResolved = true;
        }

        internal LexItem AppendStates(IEnumerable<string> states)
        {
            this.states.AddRange(states);
            return this;
        }
        internal LexItem EnablePriority(bool value)
        {
            this.HasPriority = value;
            return this;
        }

    }
    public class Production
    {
        public readonly SymbolInfo LhsSymbol;
        public readonly RecursiveEnum Recursive;
        public IEnumerable<AltRule> RhsAlternatives { get { return rhsAlternatives; } }
        private List<AltRule> rhsAlternatives;

        private Production(SymbolInfo lhsSymbol, RecursiveEnum recursive, IEnumerable<AltRule> alternatives)
        {
            this.LhsSymbol = lhsSymbol;
            this.Recursive = recursive;
            this.rhsAlternatives = alternatives.ToList();
        }

        public static Production CreateUser(SymbolInfo lhsSymbol, RecursiveEnum recursive, IEnumerable<AltRule> alternatives)
        {
            return new Production(lhsSymbol, recursive, alternatives);
        }

        public static Production CreateInternally(SymbolInfo lhsSymbol, IEnumerable<AltRule> alternatives)
        {
            return new Production(lhsSymbol, RecursiveEnum.Undef, alternatives);
        }
        internal void SetAltRules(IEnumerable<AltRule> alternations)
        {
            this.rhsAlternatives = alternations.ToList();
        }
    }

}
