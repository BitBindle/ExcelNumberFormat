namespace ExcelNumberFormat
{
    internal class Condition
    {
        public string Operator { get; set; }
        public double Value { get; set; }

        public bool Evaluate(double lhs)
        {
            switch (Operator)
            {
                case "<":
                    return lhs < Value;
                case "<=":
                    return lhs <= Value;
                case ">":
                    return lhs > Value;
                case ">=":
                    return lhs >= Value;
                case "<>":
                    return lhs != Value;
                case "=":
                    return lhs == Value;
            }

            return false;
        }

        /// <summary>
        /// True if a positive value can meet the condition.
        /// </summary>
        public bool CanMatchPositive()
        {
            switch (Operator)
            {
                case "<":
                case "<=":
                case "=":
                    return Value > 0;
            }

            return true;
        }
    }
}
