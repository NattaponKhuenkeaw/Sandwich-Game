using UnityEngine;
using UnityEngine.UI;
namespace SandwichGame
{
    public class CustomerOrderUI : MonoBehaviour
    {
        public Canvas Canvas;
        public Image OrderIcon;
        public Text Quantity;
        public Image TimerFill;
        public bool FollowHead;
        public float RightOffset = .65f;
        public float UpOffset = .18f;
        public float WorldScale = .0033f;
        public GameObject TimerTrack;
        Transform head;
        CustomerAgent owner;
        void LateUpdate()
        {
            if (!FollowHead || Canvas == null) return;
            if (owner == null) owner = GetComponentInParent<CustomerAgent>();
            if (owner == null) return;
            if (head == null || !head.gameObject.activeInHierarchy)
            {
                head = null;
                foreach (var animator in owner.GetComponentsInChildren<Animator>())
                    if (animator.isHuman && animator.gameObject.activeInHierarchy)
                    { head = animator.GetBoneTransform(HumanBodyBones.Head); if (head != null) break; }
            }
            var camera = Canvas.worldCamera != null ? Canvas.worldCamera : Camera.main;
            if (camera == null) return;
            Vector3 position = head != null ? head.position : owner.transform.position + Vector3.up * 1.6f;
            transform.position = position + camera.transform.right * RightOffset + camera.transform.up * UpOffset;
            var viewport = camera.WorldToViewportPoint(transform.position);
            if (viewport.z > 0)
            {
                float halfWidth = GetComponent<RectTransform>().rect.width * WorldScale * .5f;
                var edge = camera.WorldToViewportPoint(transform.position + camera.transform.right * halfWidth);
                float margin = Mathf.Min(.45f, Mathf.Abs(edge.x - viewport.x) + .015f);
                viewport.x = Mathf.Clamp(viewport.x, margin, 1f - margin);
                transform.position = camera.ViewportToWorldPoint(viewport);
            }
            transform.rotation = camera.transform.rotation;
            var parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(WorldScale / Mathf.Max(.001f, Mathf.Abs(parentScale.x)), WorldScale / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), WorldScale / Mathf.Max(.001f, Mathf.Abs(parentScale.z)));
        }
        public void SetRemaining(int remaining)
        {
            if (!FollowHead) return;
            if (TimerTrack != null) TimerTrack.SetActive(remaining > 0);
            Quantity.rectTransform.anchoredPosition = remaining > 0 ? new Vector2(59,18) : new Vector2(8,4);
            Quantity.rectTransform.sizeDelta = remaining > 0 ? new Vector2(104,70) : new Vector2(205,70);
            if (remaining <= 0) Quantity.text = "THANKS!";
        }
    }
}
