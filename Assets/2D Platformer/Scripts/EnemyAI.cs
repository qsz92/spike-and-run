using UnityEngine;

namespace Platformer
{
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private float moveSpeed    = 1.5f;
        [SerializeField] private float patrolRadius = 3f;

        private int            direction;
        private float          leftBound;
        private float          rightBound;
        private SpriteRenderer sr;
        private Collider2D     col;

        private void Start()
        {
            sr  = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            var rng = new System.Random(Mathf.RoundToInt(transform.position.x * 100f));
            direction  = rng.NextDouble() < 0.5 ? 1 : -1;
            leftBound  = transform.position.x - patrolRadius;
            rightBound = transform.position.x + patrolRadius;
        }

        private void Update()
        {
            transform.position += Vector3.right * moveSpeed * direction * Time.deltaTime;

            if (direction == 1  && transform.position.x >= rightBound) Flip();
            if (direction == -1 && transform.position.x <= leftBound)  Flip();

            CheckTransparency();
        }

        private void CheckTransparency()
        {
            if (col == null) return;
            Bounds b = col.bounds;
            var hits = Physics2D.OverlapBoxAll(b.center, b.size * 0.8f, 0f);
            bool insideSolid = false;
            foreach (var h in hits)
            {
                if (h.gameObject == gameObject) continue;
                if (h.isTrigger) continue;
                if (!h.CompareTag("Platform") && !h.CompareTag("Ground")) continue;
                insideSolid = true;
                break;
            }
            SetAlpha(insideSolid ? 0.35f : 1f);
        }

        private void SetAlpha(float a)
        {
            if (sr == null) return;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }

        private void Flip()
        {
            direction *= -1;
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x) * direction,
                transform.localScale.y,
                transform.localScale.z);
        }
    }
}
