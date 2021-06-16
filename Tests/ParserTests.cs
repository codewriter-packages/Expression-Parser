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
        // NOT
        [TestCase("NOT(0)", ExpectedResult = 1)]
        [TestCase("NOT(1)", ExpectedResult = 0)]
        [TestCase("NOT(-5)", ExpectedResult = 0)]
        [TestCase("NOT(5)", ExpectedResult = 0)]
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
        [TestCase("1 >= 0 AND 2 < 3", ExpectedResult = 1)]
        [TestCase("0 >= 2 OR -4 < -5", ExpectedResult = 0)]
        [TestCase("1 > 0 AND 5", ExpectedResult = 5)]
        [TestCase("1 > 0 OR 5", ExpectedResult = 1)]
        [TestCase("1 > 2 AND 5", ExpectedResult = 0)]
        [TestCase("1 > 2 OR 5", ExpectedResult = 5)]
        [TestCase("1 AND 0 OR 1", ExpectedResult = 1)]
        [TestCase("1 AND 0 OR 0", ExpectedResult = 0)]
        [TestCase("1 AND (0 OR 1)", ExpectedResult = 1)]
        public float Parse(string input) => Execute(input);

        [Test]
        [TestCase("a", 1, 2, ExpectedResult = 1)]
        [TestCase("b", 1, 2, ExpectedResult = 2)]
        [TestCase("a + b", 3, 4, ExpectedResult = 7)]
        [TestCase("b ^ a + 1", 3, 4, ExpectedResult = 65)]
        [TestCase("a AND NOT(b)", 1, 0, ExpectedResult = 1)]
        [TestCase("NOT(a) AND NOT(b)", 1, 0, ExpectedResult = 0)]
        public float Parse_Variable(string input, float a, float b)
        {
            var context = new ExpresionContext<float>();
            context.GetVariable("a").Value = a;
            context.GetVariable("b").Value = b;
            return Execute(input, context);
        }

        [Test]
        public void Parse_Variable_Invalid()
        {
            var context = new ExpresionContext<float>();
            context.GetVariable("a").Value = 1;
            Assert.AreEqual(1, Execute("a", context));
            Assert.Throws<VariableNotDefinedException>(() => Compile("b", context));
        }

        [Test]
        public void Parse_Compare_Invalid()
        {
            Assert.Throws<ParseException>(() => Compile("0 > 0 > 0"));
            Assert.Throws<ParseException>(() => Compile("0 <= 0 > 0"));
            Assert.Throws<ParseException>(() => Compile("0 = 0 != 0"));
        }

        [Test]
        public void Parse_If()
        {
            var context = new ExpresionContext<float>();
            context.GetVariable("n").Value = 0;
            Assert.AreEqual(1, Execute("IF(n < 1, 1, n < 5, 5, n < 10, 10, 20)", context));

            context.GetVariable("n").Value = 4;
            Assert.AreEqual(5, Execute("IF(n < 1, 1, n < 5, 5, n < 10, 10, 20)", context));

            context.GetVariable("n").Value = 9;
            Assert.AreEqual(10, Execute("IF(n < 1, 1, n < 5, 5, n < 10, 10, 20)", context));

            context.GetVariable("n").Value = 15;
            Assert.AreEqual(20, Execute("IF(n < 1, 1, n < 5, 5, n < 10, 10, 20)", context));

            Assert.Throws<FunctionNotDefinedException>(() => Compile("IF(1)", context));
            Assert.Throws<FunctionNotDefinedException>(() => Compile("IF(1, 1)", context));
            Assert.Throws<FunctionNotDefinedException>(() => Compile("IF(1, 1, 1, 1)", context));
        }

        private static float Execute(string input, ExpresionContext<float> context = null)
        {
            return Compile(input, context).Invoke();
        }

        private static Expression<float> Compile(string input, ExpresionContext<float> context = null)
        {
            return FloatExpressionParser.Instance.Compile(input, context);
        }
    }
}