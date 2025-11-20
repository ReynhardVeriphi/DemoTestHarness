using NUnit.Framework;
using System;

namespace Mathy.Tests
{
    public class CalculatorTests
    {
        [Test]
        public void Add_AddsTwoNumbers()
        {
            Assert.That(Calculator.Add(2, 3), Is.EqualTo(5));
        }

        [Test]
        public void Divide_ByNonZero_Works()
        {
            Assert.That(Calculator.Divide(10, 4), Is.EqualTo(2.5).Within(1e-9));
        }

        [Test]
        public void Divide_ByZero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() => Calculator.Divide(1, 0));
        }
    }
}