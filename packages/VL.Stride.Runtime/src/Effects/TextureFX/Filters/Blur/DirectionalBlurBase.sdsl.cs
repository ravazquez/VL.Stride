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
    public static partial class DirectionalBlurBaseKeys
    {
        public static readonly ValueParameterKey<Vector2> Direction = ParameterKeys.NewValue<Vector2>(new Vector2(0.25f,0.0f));
        public static readonly ValueParameterKey<Vector2> SampleCenter = ParameterKeys.NewValue<Vector2>(new Vector2(0.5f,0.5f));
        public static readonly ValueParameterKey<bool> Aspect = ParameterKeys.NewValue<bool>(true);
    }
}
