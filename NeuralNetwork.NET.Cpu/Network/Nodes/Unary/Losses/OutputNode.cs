﻿using JetBrains.Annotations;
using NeuralNetworkDotNet.APIs.Enums;
using NeuralNetworkDotNet.APIs.Models;
using NeuralNetworkDotNet.APIs.Structs;
using NeuralNetworkDotNet.Network.Cost;
using NeuralNetworkDotNet.Network.Cost.Delegates;
using NeuralNetworkDotNet.Network.Nodes.Interfaces;

namespace NeuralNetworkDotNet.Network.Nodes.Unary.Losses
{
    /// <summary>
    /// A custom <see cref="OutputNode"/> used as output in a graph, that also contains a specific cost function
    /// </summary>
    internal class OutputNode : ActivationNode
    {
        /// <summary>
        /// Gets the cost function for the current layer
        /// </summary>
        public CostFunctionType CostFunctionType { get; }

        /// <summary>
        /// Gets the cost function implementations used in the current layer
        /// </summary>
        public (CostFunction Cost, CostFunctionPrime CostPrime) CostFunctions { get; }

        public OutputNode([NotNull] INode input, Shape output, ActivationType activation, CostFunctionType costFunctionType)
            : base(input, output, activation)
        {
            CostFunctionType = costFunctionType;
            CostFunctions = CostFunctionProvider.GetCostFunctions(costFunctionType);
        }

        /// <inheritdoc/>
        public override Tensor Backward(Tensor x, Tensor y, Tensor dy)
        {
            var dx = Tensor.Like(x);
            CostFunctions.CostPrime(dy, y, x, ActivationFunctions.ActivationPrime, dx);

            return dx;
        }

        /// <inheritdoc/>
        public override bool Equals(INode other)
        {
            if (!base.Equals(other)) return false;

            return other is OutputNode node &&
                   CostFunctionType.Equals(node.CostFunctionType);
        }
    }
}