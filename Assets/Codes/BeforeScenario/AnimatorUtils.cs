using UnityEngine;

public static class AnimatorUtils
{
    public static bool HasAnimatorParam(Animator animator, string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        for (int i = 0; i < animator.parameters.Length; i++)
        {
            if (animator.parameters[i].name == paramName) return true;
        }
        return false;
    }
}
