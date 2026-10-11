using UnityEngine;
namespace SandwichGame
{
    public class BlinkCustomerVisual : MonoBehaviour
    {
        public GameObject[] Variants;
        public int VariantIndex = -1;
        public Animator ReactionAnimator;
        public float ReactionDuration = 1.25f;
        public void ChooseAppearance(BlinkCustomerVisual[] others)
        {
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
