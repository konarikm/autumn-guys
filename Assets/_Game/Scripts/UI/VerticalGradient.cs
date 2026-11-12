using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AutumnGuys
{
    /// <summary>Colours a UI graphic's vertices from <see cref="top"/> to <see cref="bottom"/> (add before Outline/Shadow).</summary>
    [AddComponentMenu("UI/Effects/Vertical Gradient")]
    public class VerticalGradient : BaseMeshEffect
    {
        public Color top = Color.white;
        public Color bottom = Color.black;

        readonly List<UIVertex> verts = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            verts.Clear();
            vh.GetUIVertexStream(verts);

            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var v in verts)
            {
                minY = Mathf.Min(minY, v.position.y);
                maxY = Mathf.Max(maxY, v.position.y);
            }
            float range = Mathf.Max(0.0001f, maxY - minY);
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                Color c = Color.Lerp(bottom, top, (v.position.y - minY) / range);
                v.color = c * (Color)v.color;
                verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}
