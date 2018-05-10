using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using NaiveLanguageTools.Parser;

namespace NaiveLanguageTools.Generator.AST
{
    public enum RepetitionEnum
    {
        Option, // ?
        EmptyOrMany, // *
        OneOrMore, // +
        Once,
        TwoOrMore, // ++
        NullOrMany, // +?
    }

    public interface IRhsEntity
    {
        int Count { get; }
        IEnumerable<RhsSymbol> GetSymbols();
        string ObjName { get; }
         IEnumerable<IRhsEntity> Elements { get;}
    }

    public static class IRhsEntityExtensions
    {
        public static RhsGroup AsGroup(this IRhsEntity __this__)
        {
            return (RhsGroup)__this__;
        }
        public static bool IsGroup(this IRhsEntity __this__)
        {
            return __this__ is RhsGroup;
        }
        public static bool IsSymbol(this IRhsEntity __this__)
        {
            return __this__ is RhsSymbol;
        }
        public static RhsSymbol AsSymbol(this IRhsEntity __this__)
        {
            return (RhsSymbol)__this__;
        }
        public static bool CanBeMultiplied(this RepetitionEnum __this__)
        {
            return __this__.In( RepetitionEnum.OneOrMore,RepetitionEnum.EmptyOrMany ,RepetitionEnum.TwoOrMore,RepetitionEnum.NullOrMany); 
        }
        public static IEnumerable<RhsSymbol> GetSymbols(this IEnumerable<IRhsEntity> __this__)
        {
            return __this__.Select(it => it.GetSymbols()).Flatten();
        }
    }

    public class RhsGroup : IRhsEntity
    {
        public enum ModeEnum
        {
            Set,
            Sequence,
            // from group produce each element (works like a set) 
            // and additionally produce all the elements in the given order (works like a sequence)
            Altogether
        }

        public string ObjName { get; private set; }
        public int Count { get { return elements.Length; } }
        public IEnumerable<IRhsEntity> Elements { get { return elements; } }
        private IRhsEntity[] elements;
        public readonly RepetitionEnum Repetition;
        public bool CanBeMultiplied { get { return Repetition.CanBeMultiplied(); } }
        public readonly ModeEnum Mode;
        public readonly SymbolPosition Position;

        public override string ToString()
        {
            return (ObjName != null ? ObjName + ":":"")
                + Common.EnumExtensions.SwitchSelect(Mode, "[","(","<")
                +Elements.Select(it => it.ToString()).Join(" ")
            + Common.EnumExtensions.SwitchSelect(Mode,"]",")",">")
            + Common.EnumExtensions.SwitchSelect(Repetition,"?","*","+","","++","+?");
        }
        public static RhsGroup CreateSequence(SymbolPosition pos, RepetitionEnum repetition,params IRhsEntity[] symbols)
        {
            return CreateSequence(pos, null, repetition, symbols);
        }
        public static RhsGroup CreateSequence(SymbolPosition pos, string name, RepetitionEnum repetition, params IRhsEntity[] symbols)
        {
            return new RhsGroup(pos, name, symbols, repetition, ModeEnum.Sequence);
        }
        public static RhsGroup Create(IEnumerable<IRhsEntity> elems)
        {
            return new RhsGroup(SymbolPosition.None, elems, RepetitionEnum.Once, ModeEnum.Sequence);
        }

        public static RhsGroup CreateAltogether(SymbolPosition position, IEnumerable<IRhsEntity> chunks, RepetitionEnum repetition) 
        {
            return CreateAltogether(position, null, chunks, repetition);
        }

        public static RhsGroup CreateAltogether(SymbolPosition position, string name, IEnumerable<IRhsEntity> chunks, RepetitionEnum repetition)
        {
            return new RhsGroup(position, name, chunks, repetition, ModeEnum.Altogether);
            //return RhsGroup.CreateSet(position, name, chunks.Concat(RhsGroup.CreateSequence(position,RepetitionEnum.Once,chunks.ToArray())), repetition);
        }

        public static RhsGroup CreateSet(SymbolPosition position, IEnumerable<IRhsEntity> chunks, RepetitionEnum repetition) 
        {
            return CreateSet(position, null, chunks, repetition);
        }

        public static RhsGroup CreateSet(SymbolPosition position, string name, IEnumerable<IRhsEntity> chunks, RepetitionEnum repetition)
        {
            return new RhsGroup(position, name, chunks, repetition, ModeEnum.Set);
        }

        private RhsGroup(SymbolPosition position, IEnumerable<IRhsEntity> chunks, RepetitionEnum repetition, ModeEnum mode) 
            : this(position, null,chunks,repetition,mode)
        {
        }

        private RhsGroup(SymbolPosition position, string name, IEnumerable<IRhsEntity> chunks, RepetitionEnum repetition, ModeEnum mode)
        {
            this.Position = position;
            this.ObjName = name;

            if (chunks.Count() == 1 && chunks.Single().IsGroup() && chunks.Single().AsGroup().Repetition == RepetitionEnum.Once)
                // there is only one chunk, so each symbol is really a chunk -- recreate the list
                this.elements = chunks.Single().AsGroup().Elements.ToArray();
            else
                this.elements = chunks.ToArray();

            this.Repetition = repetition;
            this.Mode = mode;


            string err = findError(this.ObjName,this.Elements,this.Repetition,this.Mode);
            if (err != null)
                throw ParseControlException.NewAndRun(err);
        }

        private static string findError(string objName,IEnumerable<IRhsEntity> elements,RepetitionEnum repetition,ModeEnum mode)
        {
            if (mode == ModeEnum.Sequence)
            {
                IEnumerable<RhsGroup> subs = elements.Where(it => it.IsGroup()).Select(it => it.AsGroup());
                // we accepts only sets as sub groups
                if (subs.Any(it => it.Mode != ModeEnum.Set))
                    return "Double nested group.";
            }
            else if (mode == ModeEnum.Set)
            {
                foreach (RhsGroup gr in elements.Where(it => it.IsGroup()).Select(it => it.AsGroup()))
                {
                    IEnumerable<RhsGroup> subs = gr.Elements.Where(it => it.IsGroup()).Select(it => it.AsGroup());
                    if (subs.Any())
                        return "Double nested group.";
                }
            }
            else if (mode == ModeEnum.Altogether)
            {
                foreach (RhsGroup gr in elements.Where(it => it.IsGroup()).Select(it => it.AsGroup()))
                {
                    IEnumerable<RhsGroup> subs = gr.Elements.Where(it => it.IsGroup()).Select(it => it.AsGroup());
                    // we accepts only sets as (proper) sub groups
                    if (subs.Any(it => it.Mode != ModeEnum.Set))
                        return "Double nested group.";
                }
            }
            else
                throw new NotImplementedException();
            if (elements.Where(it => it.IsGroup()).Select(it => it.AsGroup()).Any(gr => gr.CanBeMultiplied))
                return "Multiplied nested group is not supported.";

            if (!elements.Any())
                return "Empty group.";

            if (mode == ModeEnum.Set)
            {
                if (objName != null)
                {
                    if (elements.Any(it => it.IsGroup()))
                        return "With group name cannot handle nested groups.";
                    else if (elements.GetSymbols().Any(s => s.ObjName != null))
                        return "With group name individual elements cannot be named.";
                }
            }
            else if (objName != null)
                return "Only a set can have common name";

            if (repetition.CanBeMultiplied())
            {
                if (elements.Any(it => it.Count > 1))
                    return "Multiplied group cannot handle multiple symbol chunks.";

                if (elements.GetSymbols().All(s => s.SkipInitially))
                    return "Cannot skip all symbols.";
            }
            else
            {
                if (elements.GetSymbols().Any(s => s.SkipInitially))
                    return "Cannot skip symbols outside multiplied group.";
            }

            if (mode== ModeEnum.Altogether && repetition.CanBeMultiplied())
                return "Altogether group cannot be multiplied.";


            return null;
        }

        public IEnumerable<RhsSymbol> GetSymbols()
        {
            return Elements.GetSymbols();
        }


        internal RhsSymbol AsSingleSymbol()
        {
            IRhsEntity elem = elements.Single();
            if (elem.IsSymbol())
                return elem.AsSymbol();
            else
                return elem.AsGroup().Elements.Single().AsSymbol();
        }

        // wrap standalone symbols in non-repeated sequence groups
        internal static List<RhsGroup> RebuildAsGroups(List<IRhsEntity> list)
        {
            var result = new List<RhsGroup>();

            foreach (IRhsEntity elem in list)
            {
                if (elem.IsGroup())
                    result.Add(elem.AsGroup());
                else if (elem.IsSymbol())
                    result.Add(RhsGroup.Create(new[] { elem.AsSymbol() }));
                else
                    throw new NotImplementedException();
            }
            return result;
        }
    }
}
