﻿using JetBrains.Annotations;
using NeuralNetworkNET.APIs.Enums;
using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;

namespace NeuralNetworkNET.APIs.Structs
{
    /// <summary>
    /// A <see cref="struct"/> containing all the info on a convolution operation
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public readonly struct ConvolutionInfo : IEquatable<ConvolutionInfo>
    {
        /// <summary>
        /// Gets the current convolution mode for the layer
        /// </summary>
        public readonly ConvolutionMode Mode;

        /// <summary>
        /// Gets the optional vertical padding for the convolution operation
        /// </summary>
        public readonly int VerticalPadding;

        /// <summary>
        /// Gets the optional horizontal padding for the convolution operation
        /// </summary>
        public readonly int HorizontalPadding;

        /// <summary>
        /// Gets the vertical stride length while sliding the receptive window over the input
        /// </summary>
        public readonly int VerticalStride;

        /// <summary>
        /// Gets the horizontal stride length while sliding the receptive window over the input
        /// </summary>
        public readonly int HorizontalStride;

        #region Constructors

        // Internal constructor
        private ConvolutionInfo(
            ConvolutionMode mode,
            int verticalPadding, int horizontalPadding,
            int verticalStride, int horizontalStride)
        {
            if (verticalPadding < 0) throw new ArgumentOutOfRangeException(nameof(verticalPadding), "The vertical padding must be greater than or equal to 0");
            if (horizontalPadding < 0) throw new ArgumentOutOfRangeException(nameof(horizontalPadding), "The horizontal padding must be greater than or equal to 0");
            if (verticalStride < 1) throw new ArgumentOutOfRangeException(nameof(verticalStride), "The vertical stride must be at least equal to 1");
            if (horizontalStride < 1) throw new ArgumentOutOfRangeException(nameof(horizontalStride), "The horizontal stride must be at least equal to 1");

            Mode = mode;
            VerticalPadding = verticalPadding;
            HorizontalPadding = horizontalPadding;
            VerticalStride = verticalStride;
            HorizontalStride = horizontalStride;
        }

        /// <summary>
        /// Gets the default convolution info, with no padding and a stride of 1 in both directions
        /// </summary>
        public static ConvolutionInfo Default { get; } = new ConvolutionInfo(ConvolutionMode.Convolution, 0, 0, 1, 1);

        /// <summary>
        /// Creates a new convolution operation description with the input parameters
        /// </summary>
        /// <param name="mode">The desired convolution mode to use</param>
        /// <param name="verticalPadding">The optional convolution vertical padding</param>
        /// <param name="horizontalPadding">The optional convolution horizontal padding</param>
        /// <param name="verticalStride">The convolution vertical stride size</param>
        /// <param name="horizontalStride">The convolution horizontal stride size</param>
        [PublicAPI]
        [Pure]
        public static ConvolutionInfo New(
            ConvolutionMode mode = ConvolutionMode.Convolution,
            int verticalPadding = 0, int horizontalPadding = 0,
            int verticalStride = 1, int horizontalStride = 1)
            => new ConvolutionInfo(mode, verticalPadding, horizontalPadding, verticalStride, horizontalStride);

        #endregion

        #region Equality

        /// <inheritdoc/>
        public bool Equals(ConvolutionInfo other) => this == other;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ConvolutionInfo info ? this == info : false;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            unchecked
            {
                hash = hash * 31 + (int)Mode;
                hash = hash * 31 + VerticalPadding;
                hash = hash * 31 + HorizontalPadding;
                hash = hash * 31 + VerticalStride;
                hash = hash * 31 + HorizontalStride;
            }
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ConvolutionInfo a, in ConvolutionInfo b) => a.Mode == b.Mode &&
                                                                                      a.VerticalPadding == b.VerticalPadding && a.HorizontalPadding == b.HorizontalPadding &&
                                                                                      a.VerticalStride == b.VerticalStride && a.HorizontalStride == b.HorizontalStride;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ConvolutionInfo a, in ConvolutionInfo b) => !(a == b);

        #endregion
    }
}