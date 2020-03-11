using System.Collections;
using System.Collections.Generic;
using CI.TaskParallel;
using UnityEngine;

public class BilibiliHime : SingletonUtil<BilibiliHime>
{
    public Animator animator;
    public int roomid;
    public string csrf;
    public string cookies;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        var bilibiliLiveRest = new BilibiliLiveRest("https://api.live.bilibili.com");
        BilibiliLiveNetty.Instance.onDanmakuMessage = (uname, message) =>
        {
            if (message == "打你哟")
            {
                UnityTask.RunOnUIThread(() => animator.SetTrigger("DAMAGED00"));
            }
        };
        BilibiliLiveNetty.Instance.onSendGift = (uname, action, num, giftName) =>
        {
            UnityTask.RunOnUIThread(() =>
            {
                StartCoroutine(SendGiftResponse(uname, action, num, giftName, bilibiliLiveRest));
            });
        };
    }

    private IEnumerator SendGiftResponse(string uname, string action, int num, string giftName,
        BilibiliLiveRest bilibiliLiveRest)
    {
        string str = $"感谢{uname}{action}的{num}个{giftName}";
        List<string> list = new List<string>();
        while (str.Length > 20)
        {
            list.Add(str.Substring(0, 20));
            str = str.Substring(20);
        }

        list.Add(str);
        foreach (var s in list)
        {
            bilibiliLiveRest.MsgSend(s, roomid, csrf, cookies,
                result => { Debug.Log(result); },
                (url, responseCode, error) =>
                {
                    Debug.LogError($"url: {url}, responseCode: {responseCode}, error: {error}");
                });
            yield return new WaitForSeconds(1);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}