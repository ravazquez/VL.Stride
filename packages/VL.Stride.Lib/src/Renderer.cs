﻿using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lang.PublicAPI;
using VL.Lib.Basics.Resources;
using VL.Xenko.Games;
using VL.Xenko.Layer;
using VL.Xenko.Rendering;
using Xenko.Core.Mathematics;
using Xenko.Core.MicroThreading;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Rendering;

namespace VL.Xenko
{
    public class Renderer : IDisposable
    {
        private readonly NodeContext FNodeContext;
        private readonly IResourceHandle<Game> FGameHandle;
        private readonly IResourceHandle<GameWindow> FWindowHandle;
        private RectangleF FBounds = RectangleF.Empty;
        private readonly bool FSaveBounds;
        private readonly bool FBoundToDocument;
        private readonly bool FShowDialogIfDocumentChanged;
        private readonly SerialDisposable sizeChangedSubscription = new SerialDisposable();
        private readonly SceneLink FSceneLink;
        private readonly EntitySceneLink FEntitySceneLink;
        private Int2 FLastPosition;

        public Renderer(NodeContext nodeContext, RectangleF bounds, bool saveBounds = true, bool boundToDocument = false, bool dialogIfDocumentChanged = false)
        {
            FNodeContext = nodeContext;
            FBounds = bounds;
            FSaveBounds = saveBounds;
            FBoundToDocument = boundToDocument;
            FShowDialogIfDocumentChanged = dialogIfDocumentChanged;

            FGameHandle = nodeContext.GetGameHandle();
            FWindowHandle = nodeContext.GetGameWindowProvider().GetHandle();

            var game = FGameHandle.Resource;
            if (bounds.Width > 1 && bounds.Height > 1)
            {
                game.GraphicsDeviceManager.PreferredBackBufferWidth = (int)bounds.Width;
                game.GraphicsDeviceManager.PreferredBackBufferHeight = (int)bounds.Height;
                game.GraphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
                game.GraphicsDeviceManager.ApplyChanges();

                Window.Position = new Int2((int)bounds.X, (int)bounds.Y);
            }

            SetupEvents(Window);

            //init scene graph links 
            var rootScene = game.SceneSystem.SceneInstance.RootScene;
            FSceneLink = new SceneLink(rootScene);
            FEntitySceneLink = new EntitySceneLink(rootScene);
        }

        public GameWindow Window => FWindowHandle.Resource;

        public void Update(Entity entity, Scene scene, Color4 color, bool clear = true, bool verticalSync = false, bool enabled = true, bool reset = false, float depth = 1, byte stencilValue = 0, ClearRendererFlags clearFlags = ClearRendererFlags.ColorAndDepth)
        {
            var game = (VLGame)FGameHandle.Resource;

            //v-sync
            if (verticalSync != game.GraphicsDeviceManager.SynchronizeWithVerticalRetrace)
            {
                game.GraphicsDeviceManager.SynchronizeWithVerticalRetrace = verticalSync;
                game.GraphicsDeviceManager.ApplyChanges();
            }

            //write bounds to patch?
            if (Window.Position != FLastPosition)
            {
                UpdateBounds(null);
                FLastPosition = Window.Position;
            }

            if (enabled)
            { 
                game.RunCallback.Invoke(); //calls Game.Tick();

                game.SceneSystem.GraphicsCompositor.GetFirstForwardRenderer(out var forwardRenderer);
                forwardRenderer?.SetClearOptions(color, depth, stencilValue, clearFlags, clear);
                
                FEntitySceneLink.Update(entity);
                FSceneLink.Update(scene);
            }
        }

        void SetupEvents(GameWindow window)
        {
            //register events handlers
            sizeChangedSubscription.Disposable = Observable.Merge(
                Observable.FromEventPattern(window, nameof(Window.ClientSizeChanged)), 
                Observable.FromEventPattern(window, nameof(Window.OrientationChanged)))
                .Throttle(TimeSpan.FromSeconds(0.5))
                .Subscribe(UpdateBounds);

            window.Closing += Window_Closing;
        }

        private void UpdateBounds(EventPattern<object> obj)
        {
            //write bounds into pin
            if (FSaveBounds)
            {
                var b = Window.ClientBounds;
                var p = Window.Position;
                FBounds = new RectangleF(p.X, p.Y, b.Width, b.Height);
                var solution = VL.Model.VLSession.Instance.CurrentSolution as ISolution;
                solution = solution?.SetPinValue(FNodeContext.Path.Stack.Peek(), "Bounds", FBounds);
                solution?.Confirm(Model.SolutionUpdateKind.DontCompile); 
            }
        }
        private void Window_Closing(object sender, EventArgs e)
        {
            //close doument, if requested
            if (FBoundToDocument)
                Session.CloseDocumentOfNode(FNodeContext.Path.Stack.Peek(), FShowDialogIfDocumentChanged);

            Window.Closing -= Window_Closing;
        }

        public void Dispose()
        {
            FEntitySceneLink.Dispose();
            FSceneLink.Dispose();
            FWindowHandle.Dispose();
            FGameHandle.Dispose();
        }
    }
}
