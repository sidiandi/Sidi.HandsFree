// Copyright (c) 2016, Andreas Grimme

using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sidi.HandsFree
{
    public class SupportedIndicatorsParser
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SupportedIndicatorsParser()
        {
        }

        static readonly Parser<char> Plus = Sprache.Parse.Char('+');
        static readonly Parser<char> OpenParen = Sprache.Parse.Char('(');
        static readonly Parser<char> CloseParen = Sprache.Parse.Char(')');
        static readonly Parser<char> CellSeparator = Sprache.Parse.Char(',');
        static readonly Parser<char> RangeSep = Sprache.Parse.Char('-');
        static readonly Parser<char> QuotedStringDelimiter = Sprache.Parse.Char('"');

        static readonly Parser<char> QuoteEscape = Sprache.Parse.Char('"');
        static Parser<T> Escaped<T>(Parser<T> following)
        {
            return from escape in QuoteEscape
                   from f in following
                   select f;
        }

        static readonly Parser<string> UnquotedString = Sprache.Parse.AnyChar.Except(CellSeparator).Many().Text();

        static readonly Parser<char> QuotedStringContent =
            Sprache.Parse.AnyChar.Except(QuotedStringDelimiter).Or(Escaped(QuotedStringDelimiter));

        static readonly Parser<char> LiteralCellContent =
            Sprache.Parse.AnyChar.Except(CellSeparator).Except(Sprache.Parse.String(Environment.NewLine));

        static readonly Parser<string> QuotedString =
            from open in QuotedStringDelimiter
            from content in QuotedStringContent.Many().Text()
            from end in QuotedStringDelimiter
            select content;

        static readonly Parser<int> Integer =
            from d in Sprache.Parse.Digit.Many().Text()
            select Int32.Parse(d);

        static readonly Parser<string> StringValue = Sprache.Parse.Or(QuotedString, UnquotedString);

        static readonly Parser<Range> RangeToken =
            from open in OpenParen
            from text in Sprache.Parse.AnyChar.Except(CloseParen).Many().Text()
            from close in CloseParen
            select new Range { Text = text };

        static readonly Parser<Indicator> IndicatorToken =
            from open in OpenParen
            from name in QuotedString
            from sep in CellSeparator
            from range in RangeToken
            from close in CloseParen
            select new Indicator { Name = name, Range = range };

        static readonly Parser<IEnumerable<Indicator>> Indicators =
            from leading in IndicatorToken
            from rest in CellSeparator.Then(_ => IndicatorToken).Many().End()
            select new[] { leading }.Concat(rest);

        public static readonly Parser<IEnumerable<int>> IndicatorValues =
            from leading in Integer
            from rest in CellSeparator.Then(_ => Integer).Many().End()
            select new[] { leading }.Concat(rest);

        public static readonly Parser<string> AtCommand = Sprache.Parse.Identifier(Sprache.Parse.Upper, Sprache.Parse.Upper);

        public static readonly Parser<AtResponse> AtResponse =
            from plus in Plus
            from command in AtCommand
            from sep in Sprache.Parse.String(": ")
            from value in Sprache.Parse.AnyChar.Many().End().Text()
            select new AtResponse { Command = command, Value = value };

        public static readonly Parser<Indicator> IndicatorUpdate =
            from index in Integer
            from sep in CellSeparator
            from value in Integer
            select new Indicator { CurrentValue = value, Index = index };

        public static readonly Parser<string[]> CommaSeparatedStrings =
            from leading in StringValue
            from rest in CellSeparator.Then(_ => StringValue).Many().End()
            select new[] { leading }.Concat(rest).ToArray();

        public static IList<Indicator> Parse(string input)
        {
            var indicators = Indicators.Parse(input)
                .Select((x,i) => { x.Index = i + 1; return x; })
                .ToList();
            return indicators;
        }
    }
}
