﻿using g3;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;
using System;

namespace VL.Stride.Rendering.Models
{
    /// <summary>
    /// Generates a Disc mesh
    /// </summary>
    [DataContract("DiscMesh")]
    [Display("DiscMesh")] // This name shows up in the procedural model dropdown list
    public class DiscMesh : PrimitiveProceduralModelBase
    {
        /// <summary>
        /// Disc's outer radius
        /// </summary>
        [DataMember(10)]
        public float OuterRadius { get; set; } = 0.5f;

        /// <summary>
        /// Disc's inner radius
        /// </summary>
        [DataMember(11)]
        public float InnerRadius { get; set; } = 0.25f;

        /// <summary>
        /// Disc's initial angle in cycles 
        /// </summary>
        [DataMember(12)]
        public float FromAngle { get; set; } = 0f;

        /// <summary>
        /// Disc's final angle in cycles
        /// </summary>
        [DataMember(13)]
        public float ToAngle { get; set; } = 1f;

        /// <summary>
        /// Disc's axis to use as the Up vector
        /// </summary>
        [DataMember(14)]
        public NormalDirection Normal = NormalDirection.UpY;


        /// <summary>
        /// Disc's tessellation (amount of radial slices to split the cylinder into). Higher values result in smoother surfaces
        /// </summary>
        [DataMember(16)]
        public int Tessellation { get; set; } = 16;

        /// <summary>
        /// 
        /// </summary>
        [DataMember(15)]
        public bool Clockwise { get; set; } = false;

        /// <summary>
        /// Uses the DMesh3 instance generated from a PuncturedDiscGenerator to create an equivalent Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]>
        /// </summary>
        /// <returns>A Stride GeometricMeshData<![CDATA[<VertexPositionNormalTexture>]]> equivalent to the PuncturedDisc generated with the public property values</returns>
        protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        {
            bool closed = (1 - FromAngle) - (1 - ToAngle) == 1;
            g3.NormalDirection normal;
            bool clockwise = Clockwise;
            
            switch (Normal)
            {
                default:
                case NormalDirection.UpY: 
                    normal = g3.NormalDirection.UpY;
                    clockwise = !Clockwise; //TODO: unsure why this is the case but this seems to work. Needs review
                    break;
                case NormalDirection.UpZ: 
                    normal = g3.NormalDirection.UpZ; 
                    break;
                case NormalDirection.UpX: 
                    normal = g3.NormalDirection.UpX; 
                    break;
            }

            var generator = new PuncturedDiscGenerator
            {
                StartAngleDeg = (1 - ToAngle) * 360,
                EndAngleDeg = (1 - FromAngle) * 360,
                InnerRadius = InnerRadius,
                OuterRadius = OuterRadius,
                Slices = closed ? Math.Max(Tessellation, 2) : Math.Max(Tessellation + 1, 2),
                Clockwise = clockwise,
                TextureSpace = TextureSpace.DirectX,
                Normal = normal,
                AddSliceWhenOpen = true
            };

            return Utils.ToGeometricMeshData(generator.Generate(), "DiscMesh", UvScale);
        }
    }
}
