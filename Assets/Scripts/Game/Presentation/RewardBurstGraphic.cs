using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // One UI mesh: soft halo, restrained rays and eight small stars. No particle GameObject churn.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RewardBurstGraphic : MaskableGraphic
    {
        public Color Accent=FruitLabStyle.Gold;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=rectTransform.rect;Vector2 center=rect.center;float size=Mathf.Min(rect.width,rect.height);
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                Triangle(mesh,center,center+Direction(a)*size*.49f,center+Direction(b)*size*.49f,
                    Tint(Accent,.22f),Tint(Accent,0),Tint(Accent,0));
            }
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;
                Triangle(mesh,center+Direction(a-.035f)*size*.17f,center+Direction(a)*size*.43f,center+Direction(a+.035f)*size*.17f,
                    Tint(Accent,.03f),Tint(Accent,.24f),Tint(Accent,.03f));
            }
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4+.18f;Vector2 p=center+Direction(a)*size*(i%2==0?.35f:.44f);
                float r=size*(i%2==0?.024f:.015f);var tint=Tint(i%2==0?FruitLabStyle.Text:Accent,.82f);
                for(int j=0;j<8;j++)Triangle(mesh,p,p+Direction(j*Mathf.PI/4)*r*(j%2==0?1:.25f),p+Direction((j+1)*Mathf.PI/4)*r*(j%2==0?.25f:1),tint,tint,tint);
            }
        }
        private Color Tint(Color tint,float alpha){tint.a=alpha;return tint*color;}
        private static Vector2 Direction(float angle)=>new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
        private static void Triangle(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Color ca,Color cb,Color cc)
        {
            int n=mesh.currentVertCount;mesh.AddVert(a,ca,Vector2.zero);mesh.AddVert(b,cb,Vector2.zero);mesh.AddVert(c,cc,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);
        }
    }
}
