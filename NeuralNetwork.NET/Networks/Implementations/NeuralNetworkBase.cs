﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NeuralNetworkNET.APIs;
using NeuralNetworkNET.APIs.Enums;
using NeuralNetworkNET.APIs.Interfaces;
using NeuralNetworkNET.APIs.Structs;
using NeuralNetworkNET.Extensions;
using NeuralNetworkNET.Networks.Layers.Abstract;
using NeuralNetworkNET.SupervisedLearning.Data;
using NeuralNetworkNET.SupervisedLearning.Optimization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NeuralNetworkNET.Networks.Implementations
{
    /// <summary>
    /// An abstract class used within the library that is the base for all the types of neural networks
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal abstract class NeuralNetworkBase : INeuralNetwork
    {
        // Internal constructor 
        protected NeuralNetworkBase(NetworkType type) => NetworkType = type;

        #region Properties

        /// <inheritdoc/>
        public NetworkType NetworkType { get; }

        // JSON-targeted property
        [JsonProperty(nameof(InputInfo), Order = 2)]
        private TensorInfo _InputInfo => InputInfo;

        /// <inheritdoc/>
        public abstract ref readonly TensorInfo InputInfo { get; }

        // JSON-targeted property
        [JsonProperty(nameof(OutputInfo), Order = 3)]
        private TensorInfo _OutputInfo => OutputInfo;
        
        /// <inheritdoc/>
        public abstract ref readonly TensorInfo OutputInfo { get; }

        /// <inheritdoc/>
        public abstract IReadOnlyList<INetworkLayer> Layers { get; }

        /// <inheritdoc/>
        public int Parameters => Layers.Sum(l => l is WeightedLayerBase weighted ? weighted.Weights.Length + weighted.Biases.Length : 0);

        /// <summary>
        /// Gets the list of indexes corresponding to layers with weights to update during training
        /// </summary>
        public int[] WeightedLayersIndexes { get; protected set; }

        #endregion

        #region Public APIs

        /// <inheritdoc/>
        public unsafe float[] Forward(float[] x)
        {
            fixed (float* px = x)
            {
                Tensor.Reshape(px, 1, x.Length, out Tensor xTensor);
                Forward(xTensor, out Tensor yHatTensor);
                float[] yHat = yHatTensor.ToArray();
                yHatTensor.Free();
                return yHat;
            }
        }

        /// <inheritdoc/>
        public unsafe float[,] Forward(float[,] x)
        {
            fixed (float* px = x)
            {
                Tensor.Reshape(px, x.GetLength(0), x.GetLength(1), out Tensor xTensor);
                Forward(xTensor, out Tensor yHatTensor);
                float[,] yHat = yHatTensor.ToArray2D();
                yHatTensor.Free();
                return yHat;
            }
        }

        /// <inheritdoc/>
        public abstract IReadOnlyList<(float[] Z, float[] A)> ExtractDeepFeatures(float[] x);

        /// <inheritdoc/>
        public abstract IReadOnlyList<(float[,] Z, float[,] A)> ExtractDeepFeatures(float[,] x);

        /// <inheritdoc/>
        public unsafe float CalculateCost(float[] x, float[] y)
        {
            fixed (float* px = x, py = y)
            {
                Tensor.Reshape(px, 1, x.Length, out Tensor xTensor);
                Tensor.Reshape(py, 1, y.Length, out Tensor yTensor);
                return CalculateCost(xTensor, yTensor);
            }
        }

        /// <inheritdoc/>
        public unsafe float CalculateCost(float[,] x, float[,] y)
        {
            fixed (float* px = x, py = y)
            {
                Tensor.Reshape(px, x.GetLength(0), x.GetLength(1), out Tensor xTensor);
                Tensor.Reshape(py, y.GetLength(0), y.GetLength(1), out Tensor yTensor);
                return CalculateCost(xTensor, yTensor);
            }
        }

        #endregion

        #region Implementation
        
        /// <summary>
        /// Forwards the input <see cref="Tensor"/> through the network
        /// </summary>
        /// <param name="x">The <see cref="Tensor"/> instance to process</param>
        /// <param name="yHat">The resulting <see cref="Tensor"/></param>
        protected abstract void Forward(in Tensor x, out Tensor yHat);

        /// <summary>
        /// Calculates the cost for the input <see cref="Tensor"/> inputs and expected outputs
        /// </summary>
        /// <param name="x">The input <see cref="Tensor"/></param>
        /// <param name="y">The expected results</param>
        protected abstract float CalculateCost(in Tensor x, in Tensor y);

        /// <summary>
        /// Calculates the gradient of the cost function with respect to the individual weights and biases
        /// </summary>
        /// <param name="batch">The input training batch</param>
        /// <param name="dropout">The dropout probability for eaach neuron in a <see cref="LayerType.FullyConnected"/> layer</param>
        /// <param name="updater">The function to use to update the network weights after calculating the gradient</param>
        internal abstract unsafe void Backpropagate(in SamplesBatch batch, float dropout, [NotNull] WeightsUpdater updater);

        #endregion

        #region Evaluation

        // Auxiliary function to forward a test batch
        private unsafe (float Cost, int Classified) Evaluate(in Tensor x, in Tensor y)
        {
            // Feedforward
            Forward(x, out Tensor yHat);

            // Function that counts the correctly classified items
            float* pyHat = yHat, pY = y;
            int wy = y.Length, total = 0;
            void Kernel(int i)
            {
                int offset = i * wy;
                if (NetworkSettings.AccuracyTester(new Span<float>(pyHat + offset, wy), new Span<float>(pY + offset, wy))) 
                    Interlocked.Increment(ref total);
            }

            // Check the correctly classified samples and calculate the cost
            Parallel.For(0, x.Entities, Kernel).AssertCompleted();
            float cost = CalculateCost(yHat, y);
            yHat.Free();
            return (cost, total);
        }

        /// <summary>
        /// Calculates the current network performances with the given test samples
        /// </summary>
        /// <param name="evaluationSet">The inputs and expected outputs to test the network</param>
        internal unsafe (float Cost, int Classified, float Accuracy) Evaluate((float[,] X, float[,] Y) evaluationSet)
        {
            // Actual test evaluation
            int batchSize = NetworkSettings.MaximumBatchSize;
            fixed (float* px = evaluationSet.X, py = evaluationSet.Y)
            {
                int
                    h = evaluationSet.X.GetLength(0),
                    wx = evaluationSet.X.GetLength(1),
                    wy = evaluationSet.Y.GetLength(1),
                    batches = h / batchSize,
                    batchMod = h % batchSize,
                    classified = 0;
                float cost = 0;

                // Process the even batches
                for (int i = 0; i < batches; i++)
                {
                    Tensor.Reshape(px + i * batchSize * wx, batchSize, wx, out Tensor xTensor);
                    Tensor.Reshape(py + i * batchSize * wy, batchSize, wy, out Tensor yTensor);
                    (float pCost, int pClassified) = Evaluate(xTensor, yTensor);
                    cost += pCost;
                    classified += pClassified;
                }

                // Process the remaining samples, if any
                if (batchMod > 0)
                {
                    Tensor.Reshape(px + batches * batchSize * wx, batchMod, wx, out Tensor xTensor);
                    Tensor.Reshape(py + batches * batchSize * wy, batchMod, wy, out Tensor yTensor);
                    (float pCost, int pClassified) = Evaluate(xTensor, yTensor);
                    cost += pCost;
                    classified += pClassified;
                }
                return (cost, classified, (float)classified / h * 100);
            }
        }

        /// <summary>
        /// Calculates the current network performances with the given test samples
        /// </summary>
        /// <param name="batches">The training batches currently used to train the network</param>
        internal unsafe (float Cost, int Classified, float Accuracy) Evaluate([NotNull] BatchesCollection batches)
        {
            // Actual test evaluation
            int classified = 0;
            float cost = 0;
            for (int i = 0; i < batches.BatchesCount; i++)
            {
                ref readonly SamplesBatch batch = ref batches.Batches[i];
                fixed (float* px = batch.X, py = batch.Y)
                {
                    Tensor.Reshape(px, batch.X.GetLength(0), batch.X.GetLength(1), out Tensor xTensor);
                    Tensor.Reshape(py, xTensor.Entities, batch.Y.GetLength(1), out Tensor yTensor);
                    var partial = Evaluate(xTensor, yTensor);
                    cost += partial.Cost;
                    classified += partial.Classified;
                }
            }
            return (cost, classified, (float)classified / batches.Count * 100);
        }

        #endregion

        #region Serialization and misc

        /// <inheritdoc/>
        public string SerializeMetadataAsJson() => JsonConvert.SerializeObject(this, Formatting.Indented, new StringEnumConverter());

        /// <inheritdoc/>
        public void Save(FileInfo file)
        {
            using (FileStream stream = file.OpenWrite()) 
                Save(stream);
        }

        /// <inheritdoc/>
        public void Save(Stream stream)
        {
            using (GZipStream gzip = new GZipStream(stream, CompressionLevel.Optimal, true))
            {
                gzip.Write(NetworkType);
                foreach (NetworkLayerBase layer in Layers.Cast<NetworkLayerBase>()) 
                    layer.Serialize(gzip);
            }
        }

        /// <inheritdoc/>
        public bool Equals(INeuralNetwork other)
        {
            // Compare general features
            if (other.GetType() == GetType() &&
                other.InputInfo == InputInfo &&
                other.OutputInfo == OutputInfo &&
                other.Layers.Count == Layers.Count)
            {
                // Compare the individual layers
                return Layers.Zip(other.Layers, (l1, l2) => l1.Equals(l2)).All(b => b);
            }
            return false;
        }

        /// <inheritdoc/>
        public abstract INeuralNetwork Clone();

        #endregion
    }
}