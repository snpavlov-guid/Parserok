using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeyWordParser
{
    public static class ExprParser
    {
        public static GraphNode[] DeclareSchema()
        {
            var nodes = new GraphNode[0];

            var startGraph = new GraphNode("StartGroup", Expectation.Whitespace, Expectation.EntryLogic, Expectation.OpenGroup, Expectation.Keyword, Expectation.CloseGroup);

            //var unaryLogic = new GraphNode("EntryLogic", Expectation.Whitespace, Expectation.OpenGroup, Expectation.Keyword);

            var joinLogic = new GraphNode("JoinLogic", Expectation.Whitespace, Expectation.JoinLogic, Expectation.OpenGroup, Expectation.Keyword);

            var exprKeyword = new GraphNode("Keyword", Expectation.Whitespace, Expectation.Keyword, Expectation.CloseGroup);

            var exprOperator = new GraphNode("Operator", Expectation.Operator, Expectation.Whitespace);

            var exprValue = new GraphNode("Value", Expectation.Whitespace, Expectation.NotBlank);

            var exprPost = new GraphNode("CloseGroup", Expectation.Whitespace, Expectation.JoinLogic, Expectation.Keyword, Expectation.OpenGroup, Expectation.CloseGroup);


            startGraph[Expectation.Whitespace].Link = () => (startGraph, PassWhitespace);
            startGraph[Expectation.OpenGroup].Link =  () => (startGraph, OpenGroup);
            startGraph[Expectation.CloseGroup].Link = () => (exprPost, CloseGroup);
            startGraph[Expectation.EntryLogic].Link = () => (startGraph, ReadEntryLogic);
            startGraph[Expectation.Keyword].Link = () => (exprOperator, ReadKeword);

            joinLogic[Expectation.Whitespace].Link = () => (joinLogic, PassWhitespace);
            joinLogic[Expectation.JoinLogic].Link = () => (startGraph, ReadJoinLogic);
            joinLogic[Expectation.OpenGroup].Link = () => (exprKeyword, OpenGroup);
            joinLogic[Expectation.Keyword].Link = () => (exprOperator, ReadKeword);

            exprKeyword[Expectation.Whitespace].Link = () => (exprKeyword, PassWhitespace);
            exprKeyword[Expectation.Keyword].Link = () => (exprOperator, ReadKeword);
            exprKeyword[Expectation.CloseGroup].Link = () => ( startGraph, CloseGroup);

            exprOperator[Expectation.Whitespace].Link = () => (exprOperator, PassWhitespace);
            exprOperator[Expectation.Operator].Link = () => (exprValue, ReadOperator);

            exprValue[Expectation.Whitespace].Link = () => (exprValue, PassWhitespace);
            exprValue[Expectation.NotBlank].Link = () => (exprPost, ReadValue);

            exprPost[Expectation.Whitespace].Link = () => (exprPost, PassWhitespace);
            exprPost[Expectation.JoinLogic].Link = () => (startGraph, ReadJoinLogic);
            exprPost[Expectation.Keyword].Link = () => (exprOperator, ReadKeword);
            exprPost[Expectation.OpenGroup].Link = () => (startGraph, OpenGroup);
            exprPost[Expectation.CloseGroup].Link = () => (exprPost, CloseGroup);


            return new[] { startGraph, exprKeyword, exprOperator, exprValue, exprPost, joinLogic };
        }

        const char OpenGrp = '(';
        const char CloseGrp = ')';
        const char Quote = '"';
        static readonly char[] StopSymbols = new [] { OpenGrp, CloseGrp  };

        static bool IsStopSymbol(char x) => StopSymbols.Contains(x);

        #region Lexem handling

        static (bool, int, ExprLogic) PassWhitespace(int pos, string source, ExprLogic currectGrp)
        {
            var res = false;
            while (pos < source.Length && char.IsWhiteSpace(source[pos])) { pos++; res = true; }

            return (res, pos, currectGrp);
        }


        static (bool, int, ExprLogic) OpenGroup(int pos, string source, ExprLogic currectGrp)
        {
            // open group
            var res = false;
            if (source[pos] == OpenGrp) { pos++;  res = true; }

            return (res, pos, res ? currectGrp.AddGroup(ExprLogic.CreateGroup(currectGrp)) : currectGrp);
        }

        static (bool, int, ExprLogic) CloseGroup(int pos, string source, ExprLogic currectGrp)
        {
            // close group
            var res = false;
            if (source[pos] == CloseGrp)
            {
                if (currectGrp.Parent == null)
                    throw new KeywordParserException($"Unexpected character '{CloseGrp}' in the position {pos}.");

                pos++; res = true;
            }

            return (res, pos, currectGrp.Parent);
        }

        static (bool, int, ExprLogic) ReadKeword(int pos, string source, ExprLogic currectGrp)
        {
            var x = source[pos];
            var sb = new StringBuilder();

            if (!char.IsLetter(x)) return (false, pos, currectGrp);

            while (pos < source.Length && char.IsLetterOrDigit(x))
            {
                sb.Append(x);
                x = source[++pos];
            }

            currectGrp.ExprEntries.Add(ExprEntry.Create(currectGrp, sb.ToString()));

            return (true, pos, currectGrp);
        }

        static (bool, int, ExprLogic) ReadOperator(int pos, string source, ExprLogic currectGrp)
        {
            var res = false;

            foreach(var key in GraphMeta.Operators.Keys)
            {
                var oper = GraphMeta.Operators[key];
                if (pos + oper.Length > source.Length) continue;
                if (pos != source.IndexOf(oper, pos, oper.Length)) continue;

                currectGrp.ActiveExpr.Operator = key;
                pos += oper.Length;
                res = true;
                break;
            }

            return (res, pos, currectGrp);
        }

        static (bool, int, ExprLogic) ReadLogic(Dictionary<Logic, List<string>> logicDict, bool entry, 
            int pos, string source, ExprLogic currectGrp)
        {
            var res = false;
            var startPos = pos;

            foreach (var key in logicDict.Keys)
            {
                var logop = "";
                var logics = logicDict[key];

                foreach(var lop in logics)
                {
                    if (pos + lop.Length > source.Length) continue;
                    if (pos != source.IndexOf(lop, pos, lop.Length)) continue;

                    logop = lop;
                    break;
                }

                if (string.IsNullOrEmpty(logop)) continue;

                if (entry) currectGrp.ActiveExpr.EntryLogic = key;
                else currectGrp.ActiveEntry.JoinLogic = key;

                pos += logop.Length;
                res = true;
                break;
            }

            if (res)
            {
                //if (startPos > 0 && !char.IsWhiteSpace(source[startPos-1]))
                //    throw new KeywordParserException($"The space must precede the logical operator. Expected space in the the position {startPos + 1}");

                //if (!char.IsWhiteSpace(source[pos]))

                if (char.IsLetter(source[pos]))
                    throw new KeywordParserException($"A logical operator must be followed by a space. Expected space in the the position {pos + 1}");
            }

            return (res, pos, currectGrp);
        }

        static (bool, int, ExprLogic) ReadJoinLogic(int pos, string source, ExprLogic currectGrp)
        {
            return ReadLogic(GraphMeta.Logics, false, pos, source, currectGrp);
        }

        static (bool, int, ExprLogic) ReadEntryLogic(int pos, string source, ExprLogic currectGrp)
        {
            return ReadLogic(GraphMeta.Unaries, true, pos, source, currectGrp);
        }

        static (bool, int, ExprLogic) ReadValue(int pos, string source, ExprLogic currectGrp)
        {
            var x = source[pos];
            var sb = new StringBuilder();

            if (char.IsWhiteSpace(x)) return (false, pos, currectGrp);

            bool handleEscaped(char ch)
            {
                if (pos < source.Length - 1 && source[pos] == ch && source[pos + 1] == ch)
                {
                    sb.Append(ch);
                    pos++;
                    return false;
                }

                return true;
            }

            var inQuote = (x == Quote);
            if (inQuote) pos++;

            while (pos < source.Length)
            {
                x = source[pos];

                if (!inQuote)
                {
                    if (x == Quote)
                    {
                        if (handleEscaped(x)) throw new KeywordParserException($"Unescaped quote symbol {Quote} found in the position {pos + 1}!");
                    }
                    else if (IsStopSymbol(x))
                    {
                        break;
                    }
                    else
                    {
                        if (!char.IsWhiteSpace(x)) sb.Append(x); else break;
                    }

                } else
                {
                    if (x == Quote)
                    {
                        if (handleEscaped(x)) { inQuote = false; pos++; break; }
                    }
                    else if (IsStopSymbol(x))
                    {
                        if (handleEscaped(x)) throw new KeywordParserException($"Unescaped symbol {Quote} found in the position {pos + 1}!");
                    }
                    else
                    {
                        sb.Append(x);
                    }

                }

                pos++;
            }

            if (inQuote)
                throw new KeywordParserException($"Unexpected end of quoted string found!");

            currectGrp.ActiveExpr.Value = sb.ToString();

            return (true, pos, currectGrp);
        }

        #endregion

        #region Parsing

        static (int, ExprLogic, GraphNode) HandleNode(GraphNode graphNode, ExprLogic currentGroup, int pos, string source)
        {
            foreach (GraphNodeItem nodeItem in graphNode)
            {
                var (res, newPos, exprGrp) = nodeItem.Handler(pos, source, currentGroup);

                if (!res) continue;

                return (newPos, exprGrp, nodeItem.Next);
            }

            throw new KeywordParserException($"Unexpected character '{source[pos]}' in the position {pos + 1}.");
        }

        public static ExprLogic Parse(string exprText)
        {
            var exprSchema = DeclareSchema();
            var refPointer = exprSchema[0];

            var resultLogic = ExprLogic.CreateGroup(null);
            var currentGroup = resultLogic;

            var i = 0;
            var exprTextLen = exprText.Length;

            while (i < exprTextLen)
            {
                var (pos, exprGrp, schemaNode) = HandleNode(refPointer, currentGroup, i, exprText);

                refPointer = schemaNode;
                currentGroup = exprGrp;
                i = pos;
            }

            if (currentGroup.Parent != null) throw new KeywordParserException("Open and close brackets mismatch!");

            return resultLogic;
        }

        #endregion

    }

    public class KeywordParserException : Exception
    {
        public KeywordParserException(string message) : base(message) { }

    }

}
