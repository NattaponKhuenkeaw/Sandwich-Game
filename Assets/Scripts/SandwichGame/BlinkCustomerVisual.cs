using UnityEngine;
namespace SandwichGame
{
    public class BlinkCustomerVisual : MonoBehaviour
    {
        public GameObject[] Variants;
        public int VariantIndex = -1;
        public Animator ReactionAnimator;
        public float ReactionDuration = 1.25f;
        void OnEnable()
        {
            var customer = GetComponentInParent<CustomerAgent>();
            if (customer == null) return;
            var visuals = customer.GetComponentsInChildren<BlinkCustomerVisual>(true);
            if (visuals.Length > 1 && visuals[0] != this) gameObject.SetActive(false);
        }
        public void ChooseAppearance(BlinkCustomerVisual[] others)
        {
            var customer = GetComponentInParent<CustomerAgent>();
            if (customer != null)
                foreach (var visual in customer.GetComponentsInChildren<BlinkCustomerVisual>(true))
                    if (visual != this) visual.gameObject.SetActive(false);
            var choices = new System.Collections.Generic.List<int>();
            for (int i = 0; i < Variants.Length; i++)
            {
                bool used = i == VariantIndex;
                foreach (var other in others) if (other != null && other != this && other.VariantIndex == i) used = true;
                if (!used) choices.Add(i);
            }
            if (choices.Count == 0) for (int i = 0; i < Variants.Length; i++) choices.Add(i);
            VariantIndex = choices[Random.Range(0, choices.Count)];
            for (int i = 0; i < Variants.Length; i++) Variants[i].SetActive(i == VariantIndex);
            if (ReactionAnimator != null) ReactionAnimator.Play("Waiting", 0, 0);
        }
        public void SetMoving(bool moving)
        {
            foreach (var variant in Variants)
            {
                if (variant == null || !variant.activeInHierarchy) continue;
                var animator = variant.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                    animator.CrossFadeInFixedTime(moving ? "Moving" : "Waiting", .15f);
            }
            if (moving && ReactionAnimator != null) ReactionAnimator.Play("Waiting", 0, 0);
        }

        public void React(bool happy)
        {
            if (ReactionAnimator != null) ReactionAnimator.CrossFadeInFixedTime(happy ? "Received" : "NotReceived", .1f);
        }
    }
}
