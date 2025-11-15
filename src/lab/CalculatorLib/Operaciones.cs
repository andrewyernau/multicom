using System;

namespace CalculatorLib
{
    /// <summary>
    /// Class that provides basic arithmetic operations.
    /// Inherits from MarshalByRefObject for remoting capabilities.
    /// </summary>
    public class Operaciones : MarshalByRefObject
    {
        /// <summary>
        /// Adds two numbers.
        /// </summary>
        /// <param name="a">First operand.</param>
        /// <param name="b">Second operand.</param>
        /// <returns>The sum of a and b.</returns>
        public double Sumar(double a, double b)
        {
            return a + b;
        }

        /// <summary>
        /// Subtracts the second number from the first.
        /// </summary>
        /// <param name="a">First operand.</param>
        /// <param name="b">Second operand.</param>
        /// <returns>The difference of a and b.</returns>
        public double Restar(double a, double b)
        {
            return a - b;
        }

        /// <summary>
        /// Multiplies two numbers.
        /// </summary>
        /// <param name="a">First operand.</param>
        /// <param name="b">Second operand.</param>
        /// <returns>The product of a and b.</returns>
        public double Multiplicar(double a, double b)
        {
            return a * b;
        }

        /// <summary>
        /// Divides the first number by the second.
        /// </summary>
        /// <param name="a">Dividend.</param>
        /// <param name="b">Divisor.</param>
        /// <returns>The quotient of a and b.</returns>
        /// <exception cref="DivideByZeroException">[AGENT] Cannot divide by zero.</exception>
        public double Dividir(double a, double b)
        {
            if (b == 0)
            {
                throw new DivideByZeroException("[AGENT] Cannot divide by zero.");
            }
            return a / b;
        }
    }
}
