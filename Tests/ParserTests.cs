using NUnit.Framework;
using Sprache;

namespace CodeWriter.ExpressionParser.Tests
{
    public class ParserTests
    {
        [Test]
        // Constants
        [TestCase("1", ExpectedResult = 1)]
        [TestCase("123", ExpectedResult = 123)]
        [TestCase("12.0", ExpectedResult = 12)]
        [TestCase("12.34", ExpectedResult = 12.34f)]
        [TestCase("TRUE", ExpectedResult = 1)]
        [TestCase("FALSE", ExpectedResult = 0)]
        // Expressions
        [TestCase("(1)", ExpectedResult = 1)]
        [TestCase("(-1)", ExpectedResult = -1)]
        [TestCase("-(-1)", ExpectedResult = 1)]
        [TestCase("1 + 2", ExpectedResult = 3)]
        [TestCase("1+2", ExpectedResult = 3)]
        [TestCase(" 1  +   2    ", ExpectedResult = 3)]
        [TestCase("6 + 2", ExpectedResult = 8)]
        [TestCase("6 - 2", ExpectedResult = 4)]
        [TestCase("6 * 2", ExpectedResult = 12)]
        [TestCase("6 / 2", ExpectedResult = 3)]
        [TestCase("6 % 2", ExpectedResult = 0)]
        [TestCase("6 ^ 2", ExpectedResult = 36)]
        [TestCase("1 + 2 + 3 + 4", ExpectedResult = 10)]
        [TestCase("1 + 2 * 3", ExpectedResult = 7)]
        [TestCase("1 + (2 * 3)", ExpectedResult = 7)]
        [TestCase("(1 + (2 * 3))", ExpectedResult = 7)]
        [TestCase("(1 + 2) * 3", ExpectedResult = 9)]
        [TestCase("5 + 4 * 3 ^ 2", ExpectedResult = 41)]
        [TestCase("2 ^ 3 * 4 + 5", ExpectedResult = 37)]
        [TestCase("5 * 4 + 3 ^ 2", ExpectedResult = 29)]
        [TestCase("-2^2", ExpectedResult = 4)]
        [TestCase("-2^-2", ExpectedResult = 0.25f)]
        // NOT
        [TestCase("NOT(0)", ExpectedResult = 1)]
        [TestCase("NOT(1)", ExpectedResult = 0)]
        [TestCase("NOT(-5)", ExpectedResult = 0)]
        [TestCase("NOT(5)", ExpectedResult = 0)]
        [TestCase("NOT(NOT(0))", ExpectedResult = 0)]
        // AND
        [TestCase("0 AND 0", ExpectedResult = 0)]
        [TestCase("0 AND 1", ExpectedResult = 0)]
        [TestCase("1 AND 0", ExpectedResult = 0)]
        [TestCase("1 AND 1", ExpectedResult = 1)]
        // OR
        [TestCase("0 OR 0", ExpectedResult = 0)]
        [TestCase("0 OR 1", ExpectedResult = 1)]
        [TestCase("1 OR 0", ExpectedResult = 1)]
        [TestCase("1 OR 1", ExpectedResult = 1)]
        // Compare
        [TestCase("1 = 1", ExpectedResult = 1)]
        [TestCase("1 = 2", ExpectedResult = 0)]
        [TestCase("1 != 1", ExpectedResult = 0)]
        [TestCase("1 != 2", ExpectedResult = 1)]
        [TestCase("1 < 2", ExpectedResult = 1)]
        [TestCase("1 <= 2", ExpectedResult = 1)]
        [TestCase("1 > 2", ExpectedResult = 0)]
        [TestCase("1 >= 2", ExpectedResult = 0)]
        [TestCase("2 < 2", ExpectedResult = 0)]
        [TestCase("2 <= 2", ExpectedResult = 1)]
        [TestCase("2 > 2", ExpectedResult = 0)]
        [TestCase("2 >= 2", ExpectedResult = 1)]
        [TestCase("3 < 2", ExpectedResult = 0)]
        [TestCase("3 <= 2", ExpectedResult = 0)]
        [TestCase("3 > 2", ExpectedResult = 1)]
        [TestCase("3 >= 2", ExpectedResult = 1)]
        // Logical
        [TestCase("4 AND 5", ExpectedResult = 5)]
        [TestCase("0 AND 5", ExpectedResult = 0)]
        [TestCase("4 OR 5", ExpectedResult = 4)]
        [TestCase("0 OR 5", ExpectedResult = 5)]
        // Logical
        [TestCase("1 >= 0 AND 2 < 3", ExpectedResult = 1)]
        [TestCase("0 >= 2 OR -4 < -5", ExpectedResult = 0)]
        [TestCase("1 > 0 AND 5", ExpectedResult = 5)]
        [TestCase("1 > 0 OR 5", ExpectedResult = 1)]
        [TestCase("1 > 2 AND 5", ExpectedResult = 0)]
        [TestCase("1 > 2 OR 5", ExpectedResult = 5)]
        [TestCase("1 AND 0 OR 1", ExpectedResult = 1)]
        [TestCase("1 AND 0 OR 0", ExpectedResult = 0)]
        [TestCase("1 AND (0 OR 1)", ExpectedResult = 1)]
        public float Parse(string input) => Execute(input, null);

        [Test]
        [TestCase("a", 1, 2, ExpectedResult = 1)]
        [TestCase("b", 1, 2, ExpectedResult = 2)]
        [TestCase("a + b", 3, 4, ExpectedResult = 7)]
        [TestCase("b ^ a + 1", 3, 4, ExpectedResult = 65)]
        [TestCase("a AND NOT(b)", 1, 0, ExpectedResult = 1)]
        [TestCase("NOT(a) AND NOT(b)", 1, 0, ExpectedResult = 0)]
        public float Parse_Variable(string input, float a, float b)
        {
            var context = new ExpresionContext<float>(new[] {"a", "b"});
            context.GetVariable("a").Value = a;
            context.GetVariable("b").Value = b;
            return Execute(input, context);
        }

        [Test]
        public void ComplexVariableName()
        {
            var context = new ExpresionContext<float>(new[] {"Some_Variable"});
            context.GetVariable("Some_Variable").Value = 1;
            Assert.AreEqual(1, Execute("Some_Variable", context));
        }

        [Test]
        public void ReadmeSample()
        {
            var context = new ExpresionContext<float>(new[] {"a", "b", "c"});

            context.GetVariable("a").Value = 1;
            context.GetVariable("b").Value = 2;
            context.GetVariable("c").Value = 3;

            var input = "a >= b AND NOT(b) OR (a + b) >= c";
            var compiledExpr = FloatExpressionParser.Instance.Compile(input, context, true);
            var result = compiledExpr.Invoke();

            Assert.AreEqual(1, result);
        }

        [Test]
        public void Parse_Variable_Invalid()
        {
            var context = new ExpresionContext<float>(new[] {"a"});
            context.GetVariable("a").Value = 1;
            Assert.AreEqual(1, Execute("a", context));
            Assert.Throws<VariableNotDefinedException>(() => Compile("b", context));
        }

        [Test]
        public void Parse_Compare_Invalid()
        {
            Assert.Throws<ParseException>(() => Compile("0 > 0 > 0", null));
            Assert.Throws<ParseException>(() => Compile("0 <= 0 > 0", null));
            Assert.Throws<ParseException>(() => Compile("0 = 0 != 0", null));
        }

        [Test]
        public void Parse_If()
        {
            var context = new ExpresionContext<float>(new[] {"n"});
            context.GetVariable("n").Value = 0;

            var expr = Compile("IF(n < 1, 1, n < 5, 5, n < 10, 10, 20)", context);

            Assert.AreEqual(1, expr.Invoke());

            context.GetVariable("n").Value = 4;
            Assert.AreEqual(5, expr.Invoke());

            context.GetVariable("n").Value = 9;
            Assert.AreEqual(10, expr.Invoke());

            context.GetVariable("n").Value = 15;
            Assert.AreEqual(20, expr.Invoke());

            Assert.Throws<FunctionNotDefinedException>(() => Compile("IF(1)", context));
            Assert.Throws<FunctionNotDefinedException>(() => Compile("IF(1, 1)", context));
            Assert.Throws<FunctionNotDefinedException>(() => Compile("IF(1, 1, 1, 1)", context));
        }

        [Test]
        public void ContextTree()
        {
            var rootContext = new ExpresionContext<float>(new[] {"a", "b"});
            var subContext = new ExpresionContext<float>(rootContext, new[] {"b"});

            subContext.GetVariable("b").Value = 3;

            rootContext.GetVariable("a").Value = 1;
            rootContext.GetVariable("b").Value = 2;

            var expr = Compile("a + b", subContext);

            Assert.AreEqual(4, expr.Invoke()); // 1 + 3

            rootContext.GetVariable("a").Value = 10;
            Assert.AreEqual(13, expr.Invoke()); // 10 + 3

            subContext.GetVariable("b").Value = 20;
            Assert.AreEqual(30, expr.Invoke()); // 10 + 20

            rootContext.GetVariable("b").Value = 100;
            Assert.AreEqual(30, expr.Invoke()); // 10 + 20
        }

        [Test]
        [TestCase("0", ExpectedResult = false)]
        [TestCase("1", ExpectedResult = true)]
        [TestCase("5", ExpectedResult = true)]
        public bool Parse_Predicate(string input)
        {
            return FloatExpressionParser.Instance.CompilePredicate(input, null, false).Invoke();
        }

        private static float Execute(string input, ExpresionContext<float> context)
        {
            return Compile(input, context).Invoke();
        }

        private static Expression<float> Compile(string input, ExpresionContext<float> context)
        {
            return FloatExpressionParser.Instance.Compile(input, context, false);
        }
    }
}