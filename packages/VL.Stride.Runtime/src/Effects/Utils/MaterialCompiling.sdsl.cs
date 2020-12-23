﻿// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Stride Shader Mixin Code Generator.
// To generate it yourself, please install Stride.VisualStudio.Package .vsix
// and re-save the associated .sdfx.
// </auto-generated>

using System;
using Stride.Core;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Shaders;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering
{
    public static partial class MaterialCompilingKeys
    {
        public static readonly ValueParameterKey<Color4> OriginalColor = ParameterKeys.NewValue<Color4>();
        public static readonly ValueParameterKey<bool> HasError = ParameterKeys.NewValue<bool>();
        public static readonly ValueParameterKey<bool> HasTexture = ParameterKeys.NewValue<bool>();
        public static readonly ObjectParameterKey<Texture> OriginalTexture = ParameterKeys.NewObject<Texture>();
    }
}
