using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // One scalable drawing is shared by the live frozen slot and archived rows.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IceCardGraphic : MaskableGraphic
    {
        public static IceCardGraphic Ensure(Transform parent)
        {
            var child=parent.Find("FrozenCard");
            if(child!=null)return child.GetComponent<IceCardGraphic>();
            var root=new GameObject("FrozenCard",typeof(RectTransform),typeof(CanvasRenderer),typeof(IceCardGraphic));
            root.transform.SetParent(parent,false);root.layer=parent.gameObject.layer;
            var graphic=root.GetComponent<IceCardGraphic>();graphic.raycastTarget=false;
            FruitLabStyle.Stretch(graphic.rectTransform);graphic.gameObject.SetActive(false);return graphic;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();Rect rect=GetPixelAdjustedRect();float size=Mathf.Min(rect.width,rect.height);if(size<=0)return;
            Vector2 center=rect.center;
            Polygon(mesh,center,size,new[]{new Vector2(-.44f,-.37f),new Vector2(-.46f,.32f),new Vector2(-.29f,.46f),new Vector2(.35f,.44f),new Vector2(.47f,.29f),new Vector2(.45f,-.37f),new Vector2(.29f,-.47f)},new Color(.23f,.50f,.64f,.90f));
            Polygon(mesh,center,size,new[]{new Vector2(-.38f,-.33f),new Vector2(-.40f,.29f),new Vector2(.31f,.29f),new Vector2(.31f,-.39f)},new Color(.59f,.85f,.94f,1));
            Polygon(mesh,center,size,new[]{new Vector2(-.40f,.29f),new Vector2(-.27f,.40f),new Vector2(.34f,.39f),new Vector2(.40f,.29f),new Vector2(.31f,.29f)},new Color(.84f,.98f,1,1));
            Polygon(mesh,center,size,new[]{new Vector2(.31f,.29f),new Vector2(.40f,.29f),new Vector2(.39f,-.32f),new Vector2(.31f,-.39f)},new Color(.29f,.65f,.84f,1));
            Line(mesh,center,size,new Vector2(-.33f,.23f),new Vector2(-.31f,-.22f),.025f,new Color(1,1,1,.75f));
            for(int arm=0;arm<6;arm++)
            {
                float angle=arm*Mathf.PI/3;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));var normal=new Vector2(-direction.y,direction.x);
                Line(mesh,center,size,new Vector2(-.03f,-.02f),new Vector2(-.03f,-.02f)+direction*.25f,.027f,Color.white);
                var end=new Vector2(-.03f,-.02f)+direction*.19f;
                Line(mesh,center,size,end,end-direction*.04f+normal*.045f,.019f,Color.white);
                Line(mesh,center,size,end,end-direction*.04f-normal*.045f,.019f,Color.white);
            }
            Line(mesh,center,size,new Vector2(.18f,.19f),new Vector2(.27f,.24f),.03f,Color.white);
        }
        private void Polygon(VertexHelper mesh,Vector2 center,float size,Vector2[] points,Color tint)
        {
            int first=mesh.currentVertCount;var average=Vector2.zero;foreach(var point in points)average+=point;average/=points.Length;
            tint.a*=color.a;mesh.AddVert(center+average*size,tint,Vector2.zero);
            foreach(var point in points)
            {
                var light=Color.Lerp(tint,new Color(1,1,1,tint.a),Mathf.Clamp01((point.y+.45f)*.24f));
                mesh.AddVert(center+point*size,light,Vector2.zero);
            }
            for(int i=0;i<points.Length;i++)mesh.AddTriangle(first,first+1+i,first+1+(i+1)%points.Length);
        }
        private void Line(VertexHelper mesh,Vector2 center,float size,Vector2 start,Vector2 end,float thickness,Color tint)
        {
            var offset=new Vector2(-(end-start).y,(end-start).x).normalized*thickness*.5f;
            Polygon(mesh,center,size,new[]{start-offset,start+offset,end+offset,end-offset},tint);
        }
    }
}
