using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public enum GardenCellEffect { Soil, Growth, Flower, Ripe }

    // A single short-lived UI mesh per cell; no particle GameObjects or idle Update.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GardenCellBurstGraphic : MaskableGraphic
    {
        public GardenCellEffect Effect;
        private float _progress;
        public float Progress { get=>_progress; set { _progress=value;SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(_progress<=0 || _progress>=1)return;
            Rect r=rectTransform.rect;float p=_progress,size=Mathf.Min(r.width,r.height);
            int count=Effect==GardenCellEffect.Soil?6:5;
            for(int i=0;i<count;i++)
            {
                float angle=(25+i*130)*Mathf.Deg2Rad;
                var center=new Vector2(r.center.x+Mathf.Cos(angle)*size*.30f*p,
                    r.yMin+size*.24f+Mathf.Sin(p*Mathf.PI)*size*(.24f+(i%2)*.07f));
                float radius=size*(Effect==GardenCellEffect.Soil?.035f:.045f)*(1-p*.5f);
                Color tint=Effect==GardenCellEffect.Soil?new Color(.69f,.44f,.24f):
                    Effect==GardenCellEffect.Growth?new Color(.65f,.84f,.39f):
                    Effect==GardenCellEffect.Flower?new Color(1,.94f,.74f):new Color(1,.80f,.32f);
                tint.a=(1-p)*.85f;tint*=color;
                int sides=Effect==GardenCellEffect.Ripe?8:6;
                for(int j=0;j<sides;j++)
                {
                    float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;
                    float ra=radius*(Effect==GardenCellEffect.Ripe && j%2==1?.28f:1);
                    float rb=radius*(Effect==GardenCellEffect.Ripe && (j+1)%2==1?.28f:1);
                    int n=mesh.currentVertCount;
                    mesh.AddVert(center,tint,Vector2.zero);
                    mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*ra,tint,Vector2.zero);
                    mesh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*rb,tint,Vector2.zero);
                    mesh.AddTriangle(n,n+1,n+2);
                }
            }
        }
    }
}
