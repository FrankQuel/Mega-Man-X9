using UnityEngine;

namespace MegaManX9.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RainLadder : MonoBehaviour
    {
        private BoxCollider2D area;
        public Bounds Bounds => (area != null ? area : area = GetComponent<BoxCollider2D>()).bounds;
        public float Bottom => Bounds.min.y;
        public float Top => Bounds.max.y;
        public float CenterX => Bounds.center.x;

        public void Configure(float height, float width = .85f)
        {
            area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(Mathf.Max(.2f, width), Mathf.Max(.5f, height));
            area.offset = Vector2.zero;
        }
    }
}
