using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarAnimationManager : MonoBehaviour
{
    public Animator animator;
    
    private bool held = false;
    private bool dropped = false;

    void HoldAvatar()
    {
       if (animator)
        {
            animator.SetBool("Held", true);
            animator.SetBool("Dropped", false);
        }
    }

    void DropAvatar()
    {
        if (animator)
        {
            animator.SetBool("Held", false);
            animator.SetBool("Dropped", true);
        }
    }
}
