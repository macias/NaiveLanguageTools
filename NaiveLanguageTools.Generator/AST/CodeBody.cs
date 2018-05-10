using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NaiveLanguageTools.Generator.AST.CodeParser;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;
using NaiveLanguageTools.Generator.Builder;
using NaiveLanguageTools.Generator.Feed;

namespace NaiveLanguageTools.Generator.AST
{
    internal sealed class CodeSnippet
    {
        internal readonly string Body;
        // because we detected semicolon inside during scanning ;-)
        internal readonly bool HasStatemnt;

        internal CodeSnippet(string body, bool isStatement)
        {
            this.Body = body;
            this.HasStatemnt = isStatement;
        }
    }

    public sealed class CodePiece
    {
        public const string PlaceholderSigil = "$";

        public enum TypeEnum
        {
            Placeholder,
            Snippet,
            Identifier
        }


        public readonly TypeEnum Type;
        public readonly string Content;

        public string Identifier
        {

            get
            {
                if (Type != TypeEnum.Identifier)
                    throw new Exception("Current type is " + Type.ToString() + ", while requested Identifier");
                return Content;
            }
        }

        public CodePiece(TypeEnum type, string content)
        {
            this.Type = type;
            this.Content = content;
        }

        private CodePiece(CodePiece src)
        {
            Type = src.Type;
            Content = src.Content;
        }

        internal CodePiece Clone()
        {
            return new CodePiece(this);
        }


        internal static CodePiece Create(TypeEnum type, string content)
        {
            return new CodePiece(type, content);
        }

        internal static CodePiece CreateIdentifier(string content)
        {
            return Create(TypeEnum.Identifier, content);
        }
        internal static CodePiece CreatePlaceholder(string content)
        {
            return Create(TypeEnum.Placeholder, content);
        }
    }

    internal sealed class FuncCallCode : ICode
    {
        public readonly string FuncName;
        internal readonly IEnumerable<ICode> Arguments;

        internal FuncCallCode(string funcName, IEnumerable<ICode> arguments)
        {
            this.FuncName = funcName;
            this.Arguments = arguments.ToArray();

            if (this.FuncName == null)
                throw new ArgumentNullException();
            if (this.Arguments.Any(it => it == null))
                throw new ArgumentNullException();
        }

        public string Make()
        {
            return FuncName + "(" + Arguments.Select(it => it.Make()).Join(",") + ")";
        }

    }

    // code body holds regular C# code plus pseudo-variables
    public sealed class CodeBody : IMixedBody, ICode
    {
        private HashSet<string> placeholders;
        private List<CodePiece> pieces;
        internal IEnumerable<CodePiece> Pieces { get { return pieces; } }
        // typename of the expression of this code
        // it not defined, look up in global definitions
        public string TypeName { get; private set; }
        public bool HasStatement { get; private set; }
        public bool HasContent { get { return AsString().Trim() != ""; } }
        private HashSet<string> identifiersPool;

        public string IdentityIdentifier { get { return pieces.Single().Identifier; } }
        public bool IsIdentity
        {
            get
            {
                return pieces.Count == 1
                    && pieces.Single().Type == CodePiece.TypeEnum.Identifier
                    // it is not enough protection, user could pass not parameter, but global variable
                    && pieces.Single().Content != CodeWords.Null;
            }
        }

        public CodeBody()
        {
            pieces = new List<CodePiece>();
            placeholders = new HashSet<string>();
            identifiersPool = new HashSet<string>();
            HasStatement = false;
        }

        private CodeBody(CodeBody src)
        {
            placeholders = new HashSet<string>(src.placeholders);
            pieces = src.pieces.Select(it => it.Clone()).ToList();
            TypeName = src.TypeName;
            HasStatement = src.HasStatement;
            identifiersPool = new HashSet<string>(src.identifiersPool);
        }

        internal CodeBody Clone()
        {
            return new CodeBody(this);
        }
        /// <summary>
        /// don't use original object after call this method
        /// </summary>
        /// <returns></returns>
        public CodeBody Trim()
        {
            int empty_heads_count = pieces.TakeWhile(it => it.Type == CodePiece.TypeEnum.Snippet && String.IsNullOrWhiteSpace(it.Content)).Count();
            int empty_tails_count = pieces.TakeTailWhile(it => it.Type == CodePiece.TypeEnum.Snippet && String.IsNullOrWhiteSpace(it.Content)).Count();

            if (empty_heads_count + empty_tails_count == 0)
                return this;
            // there may be overlap, if we have only empty elements
            else if (empty_heads_count + empty_tails_count >= pieces.Count)
                return null;
            else
                return new CodeBody()
                {
                    pieces = pieces.Skip(empty_heads_count).SkipTail(empty_tails_count).ToList(),
                    placeholders = placeholders,
                    identifiersPool = identifiersPool,
                    TypeName = TypeName,
                    HasStatement = HasStatement
                };
        }

        public string AsString()
        {
            return pieces.Select(it => it.Content).Join("");
        }
        internal CodeBody AddSnippet(CodeSnippet s)
        {
            if (s.HasStatemnt)
                this.HasStatement = true;
            return AddSnippet(s.Body);
        }
        public CodeBody AddSnippet(string s)
        {
            pieces.Add(CodePiece.Create(CodePiece.TypeEnum.Snippet, s));
            return this;
        }
        // for identifiers which are references by dollar and symbol name,
        // like "PLUS .... $PLUS" instead of setting up identifiers "plus:PLUS ..... plus"
        public CodeBody AddPlaceholder(string s) // use pure name, no dollar in front
        {
            if (s.StartsWith(CodePiece.PlaceholderSigil))
                throw new ArgumentException();

            pieces.Add(CodePiece.Create(CodePiece.TypeEnum.Placeholder, s));
            placeholders.Add(s);
            return this;
        }
        public CodeBody AddIdentifier(string s)
        {
            if (s != null)
            {
                pieces.Add(CodePiece.Create(CodePiece.TypeEnum.Identifier, s));
                identifiersPool.Add(s);
            }
            return this;
        }

        public CodeBody Append(CodeBody other)
        {
            append(other.pieces);

            if (other.HasStatement)
                this.HasStatement = true;

            return this;
        }
        internal CodeBody Append(params CodePiece[] pieces)
        {
            return append(pieces);
        }
        private CodeBody append(IEnumerable<CodePiece> pieces)
        {
            foreach (CodePiece piece in pieces)
                if (piece.Type == CodePiece.TypeEnum.Snippet)
                    AddSnippet(piece.Content);
                else if (piece.Type == CodePiece.TypeEnum.Placeholder)
                    AddPlaceholder(piece.Content);
                else
                    AddIdentifier(piece.Content);

            return this;
        }
        public CodeBody Embed(CodeBody other)
        {
            if (other.HasStatement)
                AddSnippet("{");
            append(other.pieces);
            if (other.HasStatement)
                AddSnippet("}");

            return this;
        }
        public string RegisterNewIdentifier(string name)
        {
            return Grammar.RegisterName(name, identifiersPool);
        }

        public string Make()
        {
            return Make(encloseStatements: true);
        }
        public string Make(bool encloseStatements)
        {
            return Make(new Dictionary<string, string>(), encloseStatements);
        }
        public string Make(Dictionary<string, string> placeholderMapping, bool encloseStatements = true)
        {
            foreach (string s in placeholders)
                if (!placeholderMapping.ContainsKey(s))
                    throw ParseControlException.NewAndRun("Placeholder \"" + s + "\" not mapped.");

            var builder = new StringBuilder();
            foreach (CodePiece piece in pieces)
                if (piece.Type == CodePiece.TypeEnum.Placeholder)
                    builder.Append(placeholderMapping[piece.Content]);
                else
                    builder.Append(piece.Content);

            string result = builder.ToString().Trim();

            if (HasStatement && encloseStatements)
                return "{" + result + "}";
            else
                return result;
        }

        public IEnumerable<string> GetVariables()
        {
            // todo: check if we can remove this -- REMEMBER hashset works as distinct as side effect
            return new HashSet<string>(doGetVariables());
        }
        private IEnumerable<string> doGetVariables()
        {
            if (pieces.Any(it => it.Type == CodePiece.TypeEnum.Placeholder))
                throw new Exception("Cannot work with placeholders.");

            bool incoming_variable = true;

            // get what looks like a variable, meaning first part of fully qualified identifiers
            // like: System.Collections.Generic --> System
            foreach (CodePiece piece in pieces)
                if (piece.Type == CodePiece.TypeEnum.Snippet)
                    incoming_variable = (piece.Content.Trim() != ".");
                else if (piece.Type == CodePiece.TypeEnum.Identifier && incoming_variable)
                    yield return piece.Content;
        }
        public IEnumerable<string> GetPlaceholders()
        {
            foreach (CodePiece piece in pieces)
                if (piece.Type == CodePiece.TypeEnum.Placeholder)
                    yield return piece.Content;
        }

        internal CodeBody SetTypeName(string typename)
        {
            TypeName = typename;
            return this;
        }

        private static TypeNameParser typeNameParser = new TypeNameParser();

        internal string GuessTypeName()
        {
            // it is tempting to use regex, but regex won't work for typenames in C#
            // because of nested angle brackets
            return typeNameParser.GetTypeName(Make());
        }

        internal CodeBody AddWithIdentifier(params string[] ss)
        {
            if (ss.Any())
                AddIdentifier(ss.First()).AddWithSnippet(ss.Skip(1).ToArray());

            return this;
        }
        internal CodeBody AddWithSnippet(params string[] ss)
        {
            if (ss.Any())
                AddSnippet(ss.First()).AddWithIdentifier(ss.Skip(1).ToArray());

            return this;
        }

        internal CodeBody AddCommaSeparatedIdentifiers(IEnumerable<string> identifiers)
        {
            return AddCommaSeparatedElements(identifiers.Select(it => new CodeBody().AddIdentifier(it)));
        }

        internal CodeBody AddCommaSeparatedElements(IEnumerable<CodeBody> elements)
        {
            bool first = true;
            foreach (CodeBody elem in elements)
            {
                if (!first)
                    AddSnippet(",");
                Append(elem);
                first = false;
            }

            return this;
        }

        public void AttachIdentifiersPool(HashSet<string> identifiersPool)
        {
            identifiersPool.AddRange(this.identifiersPool);
            this.identifiersPool = identifiersPool;
        }
        public void ConvertPlaceholderToCode(string placeholder, CodeBody varName)
        {
            var conv = new List<CodePiece>();
            foreach (CodePiece p in pieces)
                if (p.Type == CodePiece.TypeEnum.Placeholder && p.Content == placeholder)
                    conv.AddRange(varName.Clone().Pieces);
                else
                    conv.Add(p);

            pieces = conv;
        }
    }
}