using CI.TaskParallel;
using UnityEngine;

public class BilibiliHime : SingletonUtil<BilibiliHime>
{
    public Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        BilibiliLiveNetty.Instance.onDanmakuMessage = (uname, message) =>
        {
            UnityTask.RunOnUIThread(() => animator.SetTrigger("DAMAGED00"));
        };
    }

    // Update is called once per frame
    void Update()
    {
    }
}