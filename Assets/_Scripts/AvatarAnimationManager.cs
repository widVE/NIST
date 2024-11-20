using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarAnimationManager : MonoBehaviour
{
    public Animator animator;
    
    private bool holding = false;
    private bool dropped = false;

    public void HoldAvatar()
    {
       if (animator)
        {
            animator.SetBool("Holding", true);
            animator.SetBool("Dropped", false);
        }
    }

    public void DropAvatar()
    {
        if (animator)
        {
            animator.SetBool("Holding", false);
            animator.SetBool("Dropped", true);
        }
    }
}
