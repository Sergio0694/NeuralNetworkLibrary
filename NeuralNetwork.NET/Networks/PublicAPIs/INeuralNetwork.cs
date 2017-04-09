﻿using System.Collections.Generic;
using JetBrains.Annotations;

namespace NeuralNetwork.NET.Networks.PublicAPIs
{
    /// <summary>
    /// An interface to mask a neural network implementation
    /// </summary>
    public interface INeuralNetwork
    {
        /// <summary>
        /// Gets the size of the input layer
        /// </summary>
        int InputLayerSize { get; }

        /// <summary>
        /// Gets the size of the output layer
        /// </summary>
        int OutputLayerSize { get; }

        /// <summary>
        /// Gets the description of the network hidden layers
        /// </summary>
        [NotNull]
        IReadOnlyList<int> HiddenLayers { get; }

        /// <summary>
        /// Forwards the input through the network
        /// </summary>
        /// <param name="input">The input to process</param>
        /// <remarks>This methods processes a single input row and outputs a single result</remarks>
        [Pure]
        [NotNull]
        [CollectionAccess(CollectionAccessType.Read)]
        double[] Forward([NotNull] double[] input);

        /// <summary>
        /// Calculates the cost function for the current instance and the input values
        /// </summary>
        /// <param name="input">The input values for the network</param>
        /// <param name="y">The expected result to use to calculate the error</param>
        [Pure]
        [CollectionAccess(CollectionAccessType.Read)]
        double CalculateCost([NotNull] double[] input, [NotNull] double[] y);
    }
}