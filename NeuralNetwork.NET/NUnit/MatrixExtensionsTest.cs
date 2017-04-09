﻿using System;
using NeuralNetwork.NET.Helpers;
using NUnit.Framework;

namespace NeuralNetwork.NET.NUnit
{
    /// <summary>
    /// Test class for the <see cref="MatrixExtensions"/> class
    /// </summary>
    [TestFixture]
    [Category(nameof(MatrixExtensionsTest))]
    internal static class MatrixExtensionsTest
    {
        /// <summary>
        /// Vector-matrix multiplication test
        /// </summary>
        [Test]
        public static void LinearMultiplication()
        {
            // Test values
            double[,] m =
            {
                { 1, 1, 1, 1 },
                { 0, 2, -1, 0 },
                { 1, 1, 1, 1 },
                { 0, 0, -1, 1 }
            };
            double[]
                v = { 1, 2, 0.1, -2 },
                r = { 1.1, 5.1, 1.1, -0.9 },
                t = v.Multiply(m);
            Assert.True(t.ContentEquals(r));

            // Exception test
            double[] f = { 1, 2, 3, 4, 5, 6 };
            Assert.Throws<ArgumentOutOfRangeException>(() => f.Multiply(m));
        }

        /// <summary>
        /// Matrix-matrix multiplication test
        /// </summary>
        [Test]
        public static void SpatialMultiplication()
        {
            // Test values
            double[,]
                m1 =
                {
                    { 1, 2, 3 },
                    { 5, 0.1, -2 }
                },
                m2 =
                {
                    { 5, 2, -1, 3 },
                    { -5, 2, -7, 0.9 },
                    { 0.1, 0.2, -0.1, 2 }
                },
                r =
                {
                    { -4.7, 6.6, -15.3, 10.8 },
                    { 24.3, 9.7999, -5.5, 11.09 }
                },
                t = m1.Multiply(m2);
            Assert.True(t.ContentEquals(r));

            // Exception test
            double[,] f =
            {
                { 1, 2, 1, 0, 0 },
                { 5, 0.1, 0, 0, 0 }
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => f.Multiply(m1));
            Assert.Throws<ArgumentOutOfRangeException>(() => m2.Multiply(f));
        }

        /// <summary>
        /// Matrix transposition
        /// </summary>
        [Test]
        public static void Transposition()
        {
            // Test values
            double[,]
                m =
                {
                    { 1, 1, 1, 1 },
                    { 0, 2, -1, 0 }
                },
                r =
                {
                    { 1, 0 },
                    { 1, 2 },
                    { 1, -1 },
                    { 1, 0 }
                },
                t = m.Transpose();
            Assert.True(t.ContentEquals(r));
        }

        /// <summary>
        /// Matrix array flattening
        /// </summary>
        [Test]
        public static void Flattening()
        {
            // Test values
            double[][,] mv =
            {
                new[,]
                {
                    { 1.0, 2.0 },
                    { 3.0, 4.0 }
                },
                new[,]
                {
                    { 0.1, 0.2 },
                    { 0.3, 0.4 }
                },
                new[,]
                {
                    { -1.0, -2.0 },
                    { -3.0, -4.0 }
                }
            };
            double[]
                r = { 1.0, 2.0, 3.0, 4.0, 0.1, 0.2, 0.3, 0.4, -1.0, -2.0, -3.0, -4.0 },
                t = mv.Flatten();
            Assert.True(t.ContentEquals(r));
        }
    }
}