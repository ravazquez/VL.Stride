﻿using SharpDX.Direct3D11;
using SkiaSharp;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System;
using System.Reactive.Disposables;
using VL.Skia;
using VL.Skia.Egl;
using VL.Stride.Input;
using CommandList = Stride.Graphics.CommandList;
using PixelFormat = Stride.Graphics.PixelFormat;
using SkiaRenderContext = VL.Skia.RenderContext;

namespace VL.Stride.Windows
{
    /// <summary>
    /// Renders into a temporary texture which will then get drawn on the render target coming from downstream.
    /// </summary>
    public partial class SkiaRenderer : RendererBase
    {
        private static readonly SKColorSpace srgbLinearColorspace = SKColorSpace.CreateSrgbLinear();
        private static readonly SKColorSpace srgbColorspace = SKColorSpace.CreateSrgb();

        private readonly SerialDisposable inputSubscription = new SerialDisposable();
        private IInputSource lastInputSource;
        private Int2 lastRenderTargetSize;
        private readonly InViewportUpstream viewportLayer = new InViewportUpstream();
        
        public ILayer Layer { get; set; }

        protected override void DrawCore(RenderDrawContext context)
        {
            if (Layer is null)
                return;

            var commandList = context.CommandList;
            var renderTarget = commandList.RenderTarget;

            // Fetch the skia render context (uses ANGLE -> DirectX11)
            var interopContext = GetInteropContext(context.GraphicsDevice);
            var skiaRenderContext = interopContext.SkiaRenderContext;

            var eglContext = skiaRenderContext.EglContext;

            // Subscribe to input events - in case we have many sinks we assume that there's only one input source active
            var renderTargetSize = new Int2(renderTarget.Width, renderTarget.Height);
            var inputSource = context.RenderContext.GetWindowInputSource();
            if (inputSource != lastInputSource || renderTargetSize != lastRenderTargetSize)
            {
                lastInputSource = inputSource;
                lastRenderTargetSize = renderTargetSize;
                inputSubscription.Disposable = SubscribeToInputSource(inputSource, context, canvas: null, skiaRenderContext.SkiaContext);
            }

            using (interopContext.Scoped(commandList))
            {
                var nativeTempRenderTarget = SharpDXInterop.GetNativeResource(renderTarget) as Texture2D;
                using var eglSurface = eglContext.CreateSurfaceFromClientBuffer(nativeTempRenderTarget.NativePointer);
                // Make the surface current (becomes default FBO)
                skiaRenderContext.MakeCurrent(eglSurface);

                // Uncomment for debugging
                // SimpleStupidTestRendering();

                // Setup a skia surface around the currently set render target
                using var surface = CreateSkSurface(skiaRenderContext.SkiaContext, renderTarget, GraphicsDevice.ColorSpace == ColorSpace.Linear);

                // Render
                var canvas = surface.Canvas;
                var viewport = context.RenderContext.ViewportState.Viewport0;
                canvas.ClipRect(SKRect.Create(viewport.X, viewport.Y, viewport.Width, viewport.Height));
                viewportLayer.Update(Layer, SKRect.Create(viewport.X, viewport.Y, viewport.Width, viewport.Height), CommonSpace.PixelTopLeft, out var layer);

                layer.Render(CallerInfo.InRenderer(renderTarget.Width, renderTarget.Height, canvas, skiaRenderContext.SkiaContext));

                // Flush
                surface.Flush();

                // Ensures surface gets released
                eglContext.MakeCurrent(default);
            }
        }

        SKSurface CreateSkSurface(GRContext context, Texture texture, bool isLinearColorspace)
        {
            var colorType = GetColorType(texture.ViewFormat);
            NativeGles.glGetIntegerv(NativeGles.GL_FRAMEBUFFER_BINDING, out var framebuffer);
            NativeGles.glGetIntegerv(NativeGles.GL_STENCIL_BITS, out var stencil);
            NativeGles.glGetIntegerv(NativeGles.GL_SAMPLES, out var samples);
            var maxSamples = context.GetMaxSurfaceSampleCount(colorType);
            if (samples > maxSamples)
                samples = maxSamples;

            var glInfo = new GRGlFramebufferInfo(
                fboId: (uint)framebuffer,
                format: colorType.ToGlSizedFormat());

            using var renderTarget = new GRBackendRenderTarget(
                width: texture.Width,
                height: texture.Height,
                sampleCount: samples,
                stencilBits: stencil,
                glInfo: glInfo);

            return SKSurface.Create(
                context, 
                renderTarget, 
                GRSurfaceOrigin.TopLeft, 
                colorType, 
                colorspace: isLinearColorspace ? srgbLinearColorspace : srgbColorspace);
        }

        static SKColorType GetColorType(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.B5G6R5_UNorm:
                    return SKColorType.Rgb565;
                case PixelFormat.B8G8R8A8_UNorm:
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                    return SKColorType.Bgra8888;
                case PixelFormat.R8G8B8A8_UNorm:
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                    return SKColorType.Rgba8888;
                case PixelFormat.R10G10B10A2_UNorm:
                    return SKColorType.Rgba1010102;
                case PixelFormat.R16G16B16A16_Float:
                    return SKColorType.RgbaF16;
                case PixelFormat.R16G16B16A16_UNorm:
                    return SKColorType.Rgba16161616;
                case PixelFormat.R32G32B32A32_Float:
                    return SKColorType.RgbaF32;
                case PixelFormat.R16G16_Float:
                    return SKColorType.RgF16;
                case PixelFormat.R16G16_UNorm:
                    return SKColorType.Rg1616;
                case PixelFormat.R8G8_UNorm:
                    return SKColorType.Rg88;
                case PixelFormat.A8_UNorm:
                    return SKColorType.Alpha8;
                case PixelFormat.R8_UNorm:
                    return SKColorType.Gray8;
                default:
                    return SKColorType.Unknown;
            }
        }

        //static void SimpleStupidTestRendering()
        //{
        //    if (++i % 2 == 0)
        //        Gles.glClearColor(1, 0, 0, 1);
        //    else
        //        Gles.glClearColor(1, 1, 0, 1);

        //    Gles.glClear(Gles.GL_COLOR_BUFFER_BIT);
        //    Gles.glFlush();
        //}
        //int i = 0;

        // Works, also simple Gles drawing commands work but SkSurface.Flush causes device lost :(
        static InteropContext GetInteropContext(GraphicsDevice graphicsDevice)
        {
            return graphicsDevice.GetOrCreateSharedData("VL.Stride.Skia.InteropContext", gd =>
            {
                if (SharpDXInterop.GetNativeDevice(gd) is Device device)
                {
                    // https://github.com/google/angle/blob/master/src/tests/egl_tests/EGLDeviceTest.cpp#L272
                    var angleDevice = EglDevice.FromD3D11(device.NativePointer);
                    var d1 = device.QueryInterface<Device1>();
                    var contextState = d1.CreateDeviceContextState<Device1>(
                        CreateDeviceContextStateFlags.None,
                        new[] { device.FeatureLevel },
                        out _);

                    return new InteropContext(SkiaRenderContext.New(angleDevice), d1.ImmediateContext1, contextState);
                }
                return null;
            });
        }

        sealed class InteropContext : IDisposable
        {
            public readonly SkiaRenderContext SkiaRenderContext;
            public readonly DeviceContext1 DeviceContext;
            public readonly DeviceContextState ContextState;

            public InteropContext(SkiaRenderContext skiaRenderContext, DeviceContext1 deviceContext, DeviceContextState contextState)
            {
                SkiaRenderContext = skiaRenderContext;
                DeviceContext = deviceContext;
                ContextState = contextState;
            }

            public ScopedDeviceContext Scoped(CommandList commandList)
            {
                return new ScopedDeviceContext(commandList, DeviceContext, ContextState);
            }

            public void Dispose()
            {
                ContextState.Dispose();
                SkiaRenderContext.Dispose();
            }
        }

        readonly struct ScopedDeviceContext : IDisposable
        {
            readonly CommandList commandList;
            readonly DeviceContext1 deviceContext;
            readonly DeviceContextState oldContextState;

            public ScopedDeviceContext(CommandList commandList, DeviceContext1 deviceContext, DeviceContextState contextState)
            {
                this.commandList = commandList;
                this.deviceContext = deviceContext;
                deviceContext.SwapDeviceContextState(contextState, out oldContextState);
            }

            public void Dispose()
            {
                // Ensure no references are held to the render targets (would prevent resize)
                var currentRenderTarget = commandList.RenderTarget;
                var currentDepthStencil = commandList.DepthStencilBuffer;
                commandList.UnsetRenderTargets();
                deviceContext.SwapDeviceContextState(oldContextState, out _);
                commandList.SetRenderTarget(currentDepthStencil, currentRenderTarget);

                // Doesn't work - why?
                //var renderTargets = deviceContext.OutputMerger.GetRenderTargets(8, out var depthStencilView);
                //deviceContext.OutputMerger.ResetTargets();
                //deviceContext.SwapDeviceContextState(oldContextState, out _);
                //deviceContext.OutputMerger.SetTargets(depthStencilView, renderTargets);
            }
        }
    }
}
