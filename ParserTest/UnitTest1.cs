using NUnit.Framework;
using KeyWordParser;
using System.Linq;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TestCase("")]
        [TestCase("  ")]
        public void EmptyStringTest(string text)
        {
            var expr = ExprParser.Parse(text);

            Assert.IsNotNull(expr);
            Assert.False(expr.ExprEntries.Any());

        }

        [TestCase("()", 1)]
        [TestCase("  (  )  ", 1)]
        public void EmptyGroupsTest(string text, int grpn)
        {
            var expr = ExprParser.Parse(text);

            Assert.IsNotNull(expr);
            Assert.AreEqual(grpn, expr.ExprEntries.Count);

        }

        [TestCase("(")]
        [TestCase(" (  ")]
        [TestCase(")")]
        [TestCase("  )  ")]
        public void UnexpectedBracketsTest(string text)
        {
            Assert.Throws<KeywordParserException>(() => ExprParser.Parse(text));
        }

        [TestCase("path:foo", "path", Operator.Contains, "foo")]
        [TestCase("  path  :  foo  ", "path", Operator.Contains, "foo")]
        [TestCase("path:\"foo bar\"", "path", Operator.Contains, "foo bar")]
        [TestCase("  path  :  \"foo bar\"  ", "path", Operator.Contains, "foo bar")]
        [TestCase("path:foo\"\"", "path", Operator.Contains, "foo\"")]
        [TestCase("path:\"foo \"\"bar\"\"\"", "path", Operator.Contains, "foo \"bar\"")]
        public void SingleExpressionTest(string text, string keyword, Operator oper, string value)
        {
            var expr = ExprParser.Parse(text);

            Assert.IsNotNull(expr);
            Assert.AreEqual(1, expr.ExprEntries.Count);
            Assert.AreEqual(typeof(ExprEntry), expr.ExprEntries[0].GetType());

            Assert.AreEqual(keyword, ((ExprEntry)expr.ExprEntries[0]).Keyword);
            Assert.AreEqual(oper, ((ExprEntry)expr.ExprEntries[0]).Operator);
            Assert.AreEqual(value, ((ExprEntry)expr.ExprEntries[0]).Value);

        }

        [TestCase("path:foo\"")]
        [TestCase("path:\"foo \"bar\"")]
        [TestCase("path:\"foo bar\"\"")]
        public void UnescapedQuoteTest(string text)
        {
            //ExprParser.Parse(text);

            Assert.Throws<KeywordParserException>(() => ExprParser.Parse(text));
        }

        [TestCase("path:foo ext=rar", 2)]
        [TestCase("path:foo ext=rar dir^doc", 3)]
        public void CompositeExpressionTest(string text, int nextpr)
        {
            var expr = ExprParser.Parse(text);

            Assert.IsNotNull(expr);
            Assert.AreEqual(nextpr, expr.ExprEntries.Count);
            Assert.True(expr.ExprEntries.All(p => p is ExprEntry));

        }

        [TestCase("(path:foo)", 1, new int[] { 1 })]
        [TestCase("( path:foo )", 1, new int[] { 1 })]
        [TestCase("(path:foo ext=rar)(dir^doc)", 2, new int[] { 2, 1 })]
        [TestCase("(path:foo ext=rar) (dir^doc) (author^\"Dima K.\" modified=22.05.2019)", 3, new int[] { 2, 1, 2 })]
        public void GrouppedExpressionTest(string text, int ngrp, int[] nexprPerGrp)
        {
            var expr = ExprParser.Parse(text);

            Assert.IsNotNull(expr);
            Assert.AreEqual(ngrp, expr.ExprEntries.Count);
            Assert.True(expr.ExprEntries.Cast<ExprLogic>().Select((p, i) => nexprPerGrp[i] == p.ExprEntries.Count).All(p => p));

        }

        [TestCase("(path:foo (author^\"Dima K.\" modified=22.05.2019) dir^doc)", 1, 3, 2)]
        [TestCase("(path:foo) (author^\"Dima K.\" modified=22.05.2019) dir^doc", 3, 1, 1)]
        public void NestedGroupsTest(string text, int ntop, int nent, int sent)
        {
            var expr = ExprParser.Parse(text);

            Assert.IsNotNull(expr);
            Assert.AreEqual(ntop, expr.ExprEntries.Count);
            Assert.AreEqual(nent, ((ExprLogic)expr.ExprEntries[0]).ExprEntries.Count);

            var firstSubGroup = (ExprLogic)((ExprLogic)expr.ExprEntries[0]).ExprEntries.FirstOrDefault(p => p is ExprLogic);
            if (firstSubGroup != null)
            {
                Assert.AreEqual(sent, firstSubGroup.ExprEntries.Count);
            }

        }


    }
}