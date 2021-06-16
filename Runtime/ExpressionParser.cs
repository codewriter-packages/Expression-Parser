using System;
using System.Collections.Generic;
using System.Linq;
using Sprache;
using static Sprache.Parse;

namespace CodeWriter.ExpressionParser
{
    public delegate T Expression<out T>();

    public abstract class ExpressionParser<T>
    {
        private readonly Dictionary<string, ExprBuilder> _builderCached = new Dictionary<string, ExprBuilder>();
        private Parser<ExprBuilder> _parserCached;

        public Expression<T> CompileCached(string input, ExpresionContext<T> context)
        {
            return Compile(input, context, true);
        }

        public Expression<T> Compile(string input, ExpresionContext<T> context, bool cache = false)
        {
            context = context ?? new ExpresionContext<T>();

            _parserCached = _parserCached ?? CreateParser();

            ExprBuilder builder;
            if (cache)
            {
                if (!_builderCached.TryGetValue(input, out builder))
                {
                    builder = _parserCached.Parse(input);
                    _builderCached.Add(input, builder);
                }
            }
            else
            {
                builder = _parserCached.Parse(input);
            }

            return builder.Invoke(context);
        }

        private static Parser<BinaryFunc> Operator(string op, BinaryFunc fun) => String(op).Token().Return(fun);

        private static Parser<ExprBuilder> BinaryOperator(Parser<BinaryFunc> op,
            Parser<ExprBuilder> operand, Func<BinaryFunc, ExprBuilder, ExprBuilder, ExprBuilder> apply) =>
        (
            from l in operand
            from rest in
            (
                from f in op
                from r in operand
                select apply(f, l, r)
            ).Optional()
            select rest.IsEmpty ? l : rest.Get()
        );

        private Parser<ExprBuilder> CreateParser()
        {
            var constant = (
                from number in DecimalInvariant
                select MakeConstant(number, Parse)
            ).Named("number");

            var add = Operator("+", Add);
            var sub = Operator("-", Sub);
            var mul = Operator("*", Mul);
            var div = Operator("/", Div);
            var mod = Operator("%", Mod);
            var pow = Operator("^", Pow);
            var eq = Operator("=", Equal);
            var neq = Operator("!=", NotEqual);
            var and = Operator("AND", And);
            var or = Operator("OR", Or);
            var lt = Operator("<", LessThan);
            var lte = Operator("<=", LessThanOrEqual);
            var gt = Operator(">", GreaterThan);
            var gte = Operator(">=", GreaterThanOrEqual);

            var variable =
            (
                from variableName in Letter.Many().Text()
                select MakeVariable(variableName)
            ).Named("variable");

            Parser<ExprBuilder> expression = null;

            var function =
            (
                from name in Letter.AtLeastOnce().Text()
                from lparen in Char('(')
                // ReSharper disable once AccessToModifiedClosure
                from expr in Ref(() => expression).DelimitedBy(Char(',').Token())
                from rparen in Char(')')
                select MakeFunction(name, expr.ToList())
            ).Named("function");

            var factor =
                (
                    from lparen in Char('(')
                    // ReSharper disable once AccessToModifiedClosure
                    from expr in Ref(() => expression)
                    from rparen in Char(')')
                    select expr
                ).Named("expression")
                .XOr(constant)
                .XOr(function)
                .Or(variable);

            var operand =
            (
                (
                    from sign in Char('-')
                    from fact in factor
                    select MakeUnary(Negate, fact)
                )
                .XOr(factor)
            ).Token();

            var term1 = ChainRightOperator(pow, operand, MakeBinary);
            var term2 = ChainOperator(mul.Or(div).Or(mod), term1, MakeBinary);
            var term3 = ChainOperator(add.Or(sub), term2, MakeBinary);
            var term4 = BinaryOperator(lte.Or(lt).Or(gte).Or(gt), term3, MakeBinary);
            var term5 = BinaryOperator(eq.Or(neq), term4, MakeBinary);
            var term6 = ChainOperator(and, term5, MakeBinary);
            var term7 = ChainOperator(or, term6, MakeBinary);

            expression = term7;

            return expression.End();
        }

        protected abstract T Parse(string input);
        protected abstract T Negate(T v);
        protected abstract T Add(T a, T b);
        protected abstract T Sub(T a, T b);
        protected abstract T Mul(T a, T b);
        protected abstract T Div(T a, T b);
        protected abstract T Mod(T a, T b);
        protected abstract T Pow(T a, T b);
        protected abstract T Not(T v);
        protected abstract T And(T a, T b);
        protected abstract T Or(T a, T b);
        protected abstract T Equal(T a, T b);
        protected abstract T NotEqual(T a, T b);
        protected abstract T LessThan(T a, T b);
        protected abstract T LessThanOrEqual(T a, T b);
        protected abstract T GreaterThan(T a, T b);
        protected abstract T GreaterThanOrEqual(T a, T b);
        protected abstract bool IsTrue(T v);

        private delegate Expression<T> ExprBuilder(ExpresionContext<T> context);

        private delegate T UnaryFunc(T a);

        private delegate T BinaryFunc(T a, T b);

        private ExprBuilder MakeFunction(string name, List<ExprBuilder> parameterBuilders)
        {
            switch (name)
            {
                case "NOT":
                    return MakeFunction1(Not);

                case "IF":
                    if (parameterBuilders.Count < 3 ||
                        parameterBuilders.Count % 2 != 1)
                    {
                        throw new FunctionNotDefinedException(name, "Wrong parameters count");
                    }

                    return context =>
                    {
                        var conditions = new List<Expression<T>>();
                        var results = new List<Expression<T>>();
                        var defaultResult = parameterBuilders[parameterBuilders.Count - 1].Invoke(context);

                        for (var i = 0; i < parameterBuilders.Count - 1; i += 2)
                        {
                            conditions.Add(parameterBuilders[i].Invoke(context));
                            results.Add(parameterBuilders[i + 1].Invoke(context));
                        }

                        return () =>
                        {
                            for (var i = 0; i < conditions.Count; i++)
                            {
                                if (IsTrue(conditions[i].Invoke()))
                                {
                                    return results[i].Invoke();
                                }
                            }

                            return defaultResult.Invoke();
                        };
                    };

                default: throw new FunctionNotDefinedException(name, "Unknown name");
            }

            ExprBuilder MakeFunction1(Func<T, T> func)
            {
                if (parameterBuilders.Count != 1)
                {
                    throw new FunctionNotDefinedException(name, "Wrong parameters count");
                }

                return context =>
                {
                    var inner = parameterBuilders[0].Invoke(context);
                    return () => func(inner());
                };
            }
        }

        private static ExprBuilder MakeUnary(UnaryFunc func, ExprBuilder innerBuilder) => context =>
        {
            var inner = innerBuilder.Invoke(context);
            return () => func(inner.Invoke());
        };

        private static ExprBuilder MakeBinary(BinaryFunc func, ExprBuilder l, ExprBuilder r) => context =>
        {
            var left = l.Invoke(context);
            var right = r.Invoke(context);
            return () => func(left.Invoke(), right.Invoke());
        };

        private static ExprBuilder MakeVariable(string name) => context =>
        {
            var variable = context.GetVariable(name, createIfUndefined: false);
            return () => variable.Value;
        };

        private static ExprBuilder MakeConstant(string valueString, Func<string, T> parser) => context =>
        {
            var value = parser(valueString);
            return () => value;
        };
    }

    public class ExpresionContext<T>
    {
        private readonly Dictionary<string, ExpressionVariable<T>> _variables =
            new Dictionary<string, ExpressionVariable<T>>();

        public ExpressionVariable<T> GetVariable(string name, bool createIfUndefined = true)
        {
            if (_variables.TryGetValue(name, out var variable))
            {
                return variable;
            }

            if (!createIfUndefined)
            {
                throw new VariableNotDefinedException(name);
            }

            variable = new ExpressionVariable<T>();
            _variables.Add(name, variable);
            return variable;
        }
    }

    public class ExpressionVariable<T>
    {
        public T Value { get; set; }
    }

    public class VariableNotDefinedException : Exception
    {
        public VariableNotDefinedException(string name)
            : base($"Variable '{name}' not defined")
        {
        }
    }

    public class FunctionNotDefinedException : Exception
    {
        public FunctionNotDefinedException(string name, string reason) : base(
            $"Function '{name}' not defined: {reason}")
        {
        }
    }
}