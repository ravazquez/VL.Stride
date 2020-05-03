using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VL.Stride.Rendering;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Stride.Engine.Design;

namespace VL.Stride.Games
{
    public class VLGame : Game
    {
        /// <summary>
        /// The current game instance, each game region sets this field before its inner Create/Update.
        /// It gets restored to the previouse value (if it was not null) after Update of the region.
        /// </summary>
        // TODO: Get rid of me once we have scoped variables

        [ThreadStatic]
        public static Game GameInstance;

        public Action RunCallback { get; set; }

        public VLGame()
            : base()
        {
            GameInstance = this;
        }

        protected override void PrepareContext()
        {
            base.PrepareContext();
        }

        protected override void OnWindowCreated()
        {
            Window.AllowUserResizing = true;
            base.OnWindowCreated();
        }

        protected override void Initialize()
        {
            Settings.EffectCompilation = EffectCompilationMode.Local;
            Settings.RecordUsedEffects = false;
            base.Initialize();
        }

        public void AddLayerRenderFeature()
        {
            var renderStages = SceneSystem.GraphicsCompositor.RenderStages;
            var opaqueStage = renderStages.FirstOrDefault(s => s.Name == "Opaque") ?? renderStages.FirstOrDefault();

            if (opaqueStage != null && SceneSystem.GraphicsCompositor.RenderFeatures.None(rf => rf is LayerRenderFeature))
            {
                var stageSelector = new SimpleGroupToRenderStageSelector()
                {
                    RenderStage = opaqueStage
                };

                var layerRenderer = new LayerRenderFeature();

                layerRenderer.RenderStageSelectors.Add(stageSelector);
                SceneSystem.GraphicsCompositor.RenderFeatures.Add(layerRenderer); 
            }
        }
    }
}
