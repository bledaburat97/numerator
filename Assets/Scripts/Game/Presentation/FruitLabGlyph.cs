using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // Small functional symbols stay sharp at every UI scale and don't need a font glyph.
    public class FruitLabGlyph : MaskableGraphic
    {
        public enum Symbol { Check, Swap, Reset, Cross, Pair, Rope, Split, Dynamite, Information=9, Ultra=14, Helper=15, Eraser=16, Home=17, Garden=18, Collection=19, Market=20, Players=21 }
        public Symbol Type;
        private Color _meshTint;
        private static readonly Vector2[] FirstPairCard = {
            new Vector2(-.4f,-.27f), new Vector2(-.4f,.27f), new Vector2(-.12f,.27f), new Vector2(-.12f,-.27f), new Vector2(-.4f,-.27f) };
        private static readonly Vector2[] SecondPairCard = {
            new Vector2(.12f,-.27f), new Vector2(.12f,.27f), new Vector2(.4f,.27f), new Vector2(.4f,-.27f), new Vector2(.12f,-.27f) };
        private static readonly Vector2[] PairCord = { new Vector2(-.12f,.02f), new Vector2(0,-.1f), new Vector2(.12f,.02f) };
        private static readonly Vector2[] RopePath = MakeRope();
        private static Vector2[] MakeRope()
        {
            var points = new Vector2[17];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / 16f;
                points[i] = new Vector2(t - .5f, .08f - .25f * Mathf.Sin(t * Mathf.PI));
            }
            return points;
        }
        private static readonly Vector2[] CheckPath = { new Vector2(-.31f, 0), new Vector2(-.105f, -.205f), new Vector2(.32f, .255f) };
        private static readonly Vector2[] CrossA = { new Vector2(-.3f, -.3f), new Vector2(.3f, .3f) };
        private static readonly Vector2[] CrossB = { new Vector2(-.3f, .3f), new Vector2(.3f, -.3f) };
        private static readonly Vector2[] UndoHead = { new Vector2(-.12f, .35f), new Vector2(-.31f, .15f), new Vector2(-.055f, -.005f) };
        private static readonly Vector2[] UndoCurve = CreateUndoCurve();

        private static Vector2[] CreateUndoCurve()
        {
            var points = new Vector2[25];
            Vector2 start = new Vector2(.28f, -.26f), controlA = new Vector2(.52f, .10f), controlB = new Vector2(.10f, .43f), end = UndoHead[1];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1), u = 1 - t;
                points[i] = u * u * u * start + 3 * u * u * t * controlA + 3 * u * t * t * controlB + t * t * t * end;
            }
            return points;
        }

        public static FruitLabGlyph Create(Transform parent, Symbol type, Color color)
        {
            GameObject item = new GameObject(type.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(FruitLabGlyph));
            item.transform.SetParent(parent, false);
            FruitLabGlyph glyph = item.GetComponent<FruitLabGlyph>();
            glyph.Type = type;
            glyph.color = color;
            glyph.raycastTarget = false;
            return glyph;
        }

        private Sprite Artwork
        {
            get
            {
                string name = Type == Symbol.Check ? "Confirm" : Type == Symbol.Swap ? "Swap"
                    : Type == Symbol.Information ? "Information" : Type == Symbol.Eraser ? "Eraser"
                    : Type == Symbol.Helper ? "Helper" : Type == Symbol.Pair ? "Pair"
                    : Type == Symbol.Split ? "Split" : Type == Symbol.Ultra ? "Ultra" : null;
                return name != null ? FruitLabStyle.Icon(name) : null;
            }
        }

        public override Texture mainTexture => Artwork != null ? Artwork.texture : base.mainTexture;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Sprite artwork = Artwork;
            if (artwork != null)
            {
                Rect rect = GetPixelAdjustedRect();
                float aspect = artwork.rect.width / artwork.rect.height;
                float width = Mathf.Min(rect.width,rect.height*aspect), height = width/aspect;
                Vector2 bottomLeft = rect.center - new Vector2(width,height) * .5f;
                // Keep original artwork colours; functional checks can be tinted
                // dark on the mint submit button, or green in result feedback.
                Color tint = Type == Symbol.Check ? color : new Color(1, 1, 1, color.a);
                mesh.AddVert(bottomLeft, tint, new Vector2(0, 0));
                mesh.AddVert(bottomLeft + Vector2.up * height, tint, new Vector2(0, 1));
                mesh.AddVert(bottomLeft + new Vector2(width,height), tint, new Vector2(1, 1));
                mesh.AddVert(bottomLeft + Vector2.right * width, tint, new Vector2(1, 0));
                mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(0, 2, 3);
                return;
            }
            _meshTint = color;
            Rect r = GetPixelAdjustedRect();
            Vector2 center = r.center;
            float size = Mathf.Min(r.width, r.height);
            float stroke = size * 0.095f;
            if (size <= 0) return;
            if (Type == Symbol.Check || Type == Symbol.Helper)
            {
                RoundedStroke(mesh, CheckPath, center, size, stroke);
            }
            else if(Type==Symbol.Information)
            {
                var previous=center+Vector2.right*size*.39f;
                for(int i=1;i<=32;i++)
                {float angle=i*Mathf.PI*2/32;var point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size*.39f;Line(mesh,previous,point,stroke*.7f);previous=point;}
                Disc(mesh,center+Vector2.up*size*.19f,stroke*.55f);
                Line(mesh,center+Vector2.down*size*.24f,center+Vector2.up*size*.04f,stroke);
            }
            else if(Type==Symbol.Ultra)
            {
                RoundedStroke(mesh,CheckPath,center,size*.78f,stroke);
                Color original=_meshTint;_meshTint=FruitLabStyle.Pink;
                var star=center+size*new Vector2(.28f,.30f);
                for(int i=0;i<4;i++){float angle=i*Mathf.PI/4;var axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size*.11f;Line(mesh,star-axis,star+axis,stroke*.5f);}
                _meshTint=original;
            }
            else if (Type == Symbol.Home)
            {
                RaisedStroke(mesh,new[] {new Vector2(-.36f,-.02f),new Vector2(0,.32f),new Vector2(.36f,-.02f)},center,size,stroke,FruitLabStyle.Gold);
                RaisedStroke(mesh,new[] {new Vector2(-.25f,0),new Vector2(-.25f,-.32f),new Vector2(.25f,-.32f),new Vector2(.25f,0)},center,size,stroke,FruitLabStyle.Text);
                RoundedStroke(mesh,new[] {new Vector2(-.06f,-.3f),new Vector2(-.06f,-.12f),new Vector2(.07f,-.12f),new Vector2(.07f,-.3f)},center,size,stroke*.6f);
            }
            else if (Type == Symbol.Garden)
            {
                _meshTint=FruitLabStyle.Mint; RoundedStroke(mesh,new[] {new Vector2(0,-.25f),new Vector2(0,.23f)},center,size,stroke);
                RoundedStroke(mesh,new[] {new Vector2(0,.05f),new Vector2(-.22f,.26f),new Vector2(-.28f,.08f),new Vector2(0,-.02f),new Vector2(.26f,.2f),new Vector2(.23f,.34f),new Vector2(0,.18f)},center,size,stroke*.7f);
                _meshTint=FruitLabStyle.Gold; RoundedStroke(mesh,new[] {new Vector2(-.25f,-.18f),new Vector2(-.18f,-.36f),new Vector2(.18f,-.36f),new Vector2(.25f,-.18f),new Vector2(-.25f,-.18f)},center,size,stroke*.8f);
            }
            else if (Type == Symbol.Collection)
            {
                _meshTint=FruitLabStyle.Muted; RoundedStroke(mesh,new[] {new Vector2(-.31f,-.28f),new Vector2(-.36f,.19f),new Vector2(-.05f,.25f)},center,size,stroke);
                _meshTint=FruitLabStyle.Gold; RoundedStroke(mesh,new[] {new Vector2(-.15f,-.32f),new Vector2(-.15f,.31f),new Vector2(.30f,.31f),new Vector2(.30f,-.32f),new Vector2(-.15f,-.32f)},center,size,stroke);
                _meshTint=FruitLabStyle.Pink;Disc(mesh,center+size*new Vector2(.075f,-.035f),size*.11f);
                _meshTint=FruitLabStyle.Mint;Line(mesh,center+size*new Vector2(.08f,.09f),center+size*new Vector2(.17f,.17f),stroke*.7f);
            }
            else if (Type == Symbol.Players)
            {
                _meshTint=FruitLabStyle.Gold;Disc(mesh,center+size*new Vector2(.18f,.13f),size*.10f);
                RoundedStroke(mesh,new[] {new Vector2(.08f,-.28f),new Vector2(.34f,-.28f),new Vector2(.32f,-.08f),new Vector2(.2f,-.02f)},center,size,stroke);
                _meshTint=FruitLabStyle.Mint;Disc(mesh,center+size*new Vector2(-.13f,.19f),size*.12f);
                RoundedStroke(mesh,new[] {new Vector2(-.34f,-.31f),new Vector2(-.31f,-.06f),new Vector2(-.13f,.01f),new Vector2(.05f,-.06f),new Vector2(.08f,-.31f),new Vector2(-.34f,-.31f)},center,size,stroke);
            }
            else if (Type == Symbol.Market)
            {
                _meshTint=FruitLabStyle.Gold; RoundedStroke(mesh,new[] {new Vector2(-.29f,.05f),new Vector2(-.24f,-.33f),new Vector2(.24f,-.33f),new Vector2(.29f,.05f),new Vector2(-.29f,.05f)},center,size,stroke);
                RoundedStroke(mesh,new[] {new Vector2(-.13f,.03f),new Vector2(-.13f,.22f),new Vector2(0,.30f),new Vector2(.13f,.22f),new Vector2(.13f,.03f)},center,size,stroke*.85f);
                _meshTint=FruitLabStyle.Text;Disc(mesh,center+size*new Vector2(0,-.13f),size*.055f);
            }
            else if (Type == Symbol.Dynamite) DrawDynamite(mesh,center,size);
            else if (Type == Symbol.Swap)
            {
                Arrow(mesh, center + size * new Vector2(-0.33f, 0.2f), center + size * new Vector2(0.32f, 0.2f), stroke);
                Arrow(mesh, center + size * new Vector2(0.33f, -0.2f), center + size * new Vector2(-0.32f, -0.2f), stroke);
            }
            else if (Type == Symbol.Pair)
            {
                RoundedStroke(mesh, FirstPairCard, center, size, stroke * .72f);
                RoundedStroke(mesh, SecondPairCard, center, size, stroke * .72f);
                RoundedStroke(mesh, PairCord, center, size, stroke * .7f);
            }
            else if (Type == Symbol.Split)
            {
                RoundedStroke(mesh,FirstPairCard,center,size,stroke*.8f);
                RoundedStroke(mesh,SecondPairCard,center,size,stroke*.8f);
                for (int index = 0; index < 3; index++)
                    Line(mesh,center+size*new Vector2(0,-.32f+index*.22f),center+size*new Vector2(0,-.21f+index*.22f),stroke*.55f);
            }
            else if (Type == Symbol.Rope)
            {
                Color tint = _meshTint;
                _meshTint = new Color(tint.r * .35f, tint.g * .35f, tint.b * .35f, tint.a);
                RoundedStroke(mesh, RopePath, center, size, stroke * 1.25f);
                _meshTint = tint;
                RoundedStroke(mesh, RopePath, center, size, stroke * .7f);
                Disc(mesh, center + RopePath[0] * size, stroke * .7f);
                Disc(mesh, center + RopePath[RopePath.Length - 1] * size, stroke * .7f);
            }
            else if (Type == Symbol.Cross)
            {
                RaisedStroke(mesh, CrossA, center, size, size * .13f, color);
                RaisedStroke(mesh, CrossB, center, size, size * .13f, color);
            }
            else
            {
                RoundedStroke(mesh, UndoCurve, center, size, stroke);
                RoundedStroke(mesh, UndoHead, center, size, stroke);
            }
        }

        private void RaisedStroke(VertexHelper mesh, Vector2[] path, Vector2 center, float size, float stroke, Color tint)
        {
            _meshTint = new Color(.025f, .07f, .08f, tint.a * .5f);
            RoundedStroke(mesh, path, center + Vector2.down * size * .035f, size, stroke * 1.14f);
            _meshTint = tint;
            RoundedStroke(mesh, path, center, size, stroke);
            _meshTint = Color.Lerp(tint, Color.white, .22f); _meshTint.a = tint.a;
            RoundedStroke(mesh, path, center + Vector2.up * size * .022f, size, stroke * .32f);
        }

        private static readonly Vector2[] DynamiteFuse = { new Vector2(0,.18f),new Vector2(.02f,.32f),new Vector2(.16f,.29f),new Vector2(.23f,.41f) };
        private void DrawDynamite(VertexHelper mesh,Vector2 center,float size)
        {
            float alpha = _meshTint.a;
            for (int i = 0; i < 3; i++)
            {
                float x = -.20f+i*.20f, top = i == 1 ? .16f : .10f;
                Vector2 a = center+size*new Vector2(x,-.30f), b = center+size*new Vector2(x,top);
                _meshTint = new Color(.24f,.04f,.055f,alpha); Line(mesh,a,b,size*.23f); Disc(mesh,a,size*.115f); Disc(mesh,b,size*.115f);
                _meshTint = new Color(.94f,.20f,.24f,alpha); Line(mesh,a,b,size*.17f); Disc(mesh,a,size*.085f); Disc(mesh,b,size*.085f);
                _meshTint = new Color(1f,.48f,.40f,alpha); Line(mesh,a+Vector2.left*size*.035f,b+Vector2.left*size*.035f,size*.035f);
            }
            _meshTint = new Color(.24f,.16f,.13f,alpha);
            Line(mesh,center+size*new Vector2(-.31f,-.18f),center+size*new Vector2(.31f,-.18f),size*.08f);
            Line(mesh,center+size*new Vector2(-.31f,-.01f),center+size*new Vector2(.31f,-.01f),size*.08f);
            _meshTint = new Color(1f,.80f,.37f,alpha); RoundedStroke(mesh,DynamiteFuse,center,size,size*.045f);
            Vector2 spark = center+size*DynamiteFuse[3];
            for (int i = 0; i < 5; i++)
            {
                float angle = i*Mathf.PI*2f/5f; var direction = new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                Line(mesh,spark+direction*size*.035f,spark+direction*size*.11f,size*.035f);
            }
        }

        private void RoundedStroke(VertexHelper mesh, Vector2[] points, Vector2 center, float size, float stroke)
        {
            for (int i = 1; i < points.Length; i++) Line(mesh, center + points[i - 1] * size, center + points[i] * size, stroke);
            // Round caps and joins keep small HUD icons and larger buttons in the same family.
            foreach (Vector2 point in points) Disc(mesh, center + point * size, stroke * .5f);
        }

        private void Disc(VertexHelper mesh, Vector2 center, float radius)
        {
            const int segments = 12;
            int first = mesh.currentVertCount;
            mesh.AddVert(center, _meshTint, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, _meshTint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(first, first + i, first + i + 1);
            }
        }

        private void Arrow(VertexHelper mesh, Vector2 start, Vector2 end, float stroke)
        {
            Line(mesh, start, end, stroke);
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Line(mesh, end, end - direction * stroke * 2.8f + normal * stroke * 2.2f, stroke);
            Line(mesh, end, end - direction * stroke * 2.8f - normal * stroke * 2.2f, stroke);
        }

        private void Line(VertexHelper mesh, Vector2 start, Vector2 end, float stroke)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * stroke * 0.5f;
            int index = mesh.currentVertCount;
            mesh.AddVert(start - normal, _meshTint, Vector2.zero);
            mesh.AddVert(start + normal, _meshTint, Vector2.zero);
            mesh.AddVert(end + normal, _meshTint, Vector2.zero);
            mesh.AddVert(end - normal, _meshTint, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
