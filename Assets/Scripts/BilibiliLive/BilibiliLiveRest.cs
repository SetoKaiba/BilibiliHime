using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class BilibiliLiveRest
{
    private readonly string _baseUrl;

    private readonly Dictionary<string, UnityWebRequestAsyncOperation> CachingCount =
        new Dictionary<string, UnityWebRequestAsyncOperation>();

    private readonly Dictionary<string, string> TextCache = new Dictionary<string, string>();
    private readonly Dictionary<string, long> CachingTime = new Dictionary<string, long>();
    private const long DefaultTimeout = 300000000;

    public BilibiliLiveRest(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public int GetCachingCount()
    {
        return CachingCount.Count;
    }

    public void ClearCache()
    {
        CachingCount.Clear();
        TextCache.Clear();
        CachingTime.Clear();
    }

    public void MsgSend(string msg, int roomid, string csrf, string cookies, Action<string> onCompleted,
        Action<string, long, string> onError)
    {
        var form = new WWWForm();
        form.AddField("color", 14893055);
        form.AddField("fontsize", 25);
        form.AddField("mode", 1);
        form.AddField("msg", msg);
        form.AddField("rnd", Random.Range(0, short.MaxValue));
        form.AddField("roomid", roomid);
        form.AddField("bubble", 0);
        form.AddField("csrf_token", csrf);
        form.AddField("csrf", csrf);

        SendWebRequestTextForm($"{_baseUrl}/msg/send", resultText => { onCompleted?.Invoke(resultText); }, onError,
            form, cookies);
    }

    private void SendWebRequestTextForm(string url, Action<string> onCompleted, Action<string, long, string> onError,
        WWWForm form = null, string cookie = null, long timeout = DefaultTimeout)
    {
        if (CachingCount.ContainsKey(url))
        {
            var www = CachingCount[url].webRequest;
            CachingCount[url].completed += operation => { OnComplete(url, onCompleted, onError, www); };
        }
        else
        {
            if (form != null || !CachingTime.ContainsKey(url) || DateTime.UtcNow.Ticks - CachingTime[url] > timeout)
            {
                var www = form == null ? UnityWebRequest.Get(url) : UnityWebRequest.Post(url, form);
                if (cookie != null)
                {
                    www.SetRequestHeader("Cookie",  Escape(cookie));
                }

                var request = www.SendWebRequest();
                CachingCount[url] = request;
                request.completed += operation =>
                {
                    OnComplete(url, onCompleted, onError, www);
                    CachingCount.Remove(url);
                };
            }
            else
            {
                onCompleted(TextCache[url]);
            }
        }
    }
    
    string Escape(string s)
    {
        s = s.Replace("(", "&#40");
        s = s.Replace(")", "&#41");
        return s;
    }

    private void SendWebRequestTextJson(string url, Action<string> onCompleted, Action<string, long, string> onError,
        string body = null, string cookie = null, long timeout = DefaultTimeout)
    {
        if (CachingCount.ContainsKey(url))
        {
            var www = CachingCount[url].webRequest;
            CachingCount[url].completed += operation => { OnComplete(url, onCompleted, onError, www); };
        }
        else
        {
            if (!string.IsNullOrEmpty(body) || !CachingTime.ContainsKey(url) ||
                DateTime.UtcNow.Ticks - CachingTime[url] > timeout)
            {
                var www = new UnityWebRequest(url, string.IsNullOrEmpty(body) ? "GET" : "POST");
                if (!string.IsNullOrEmpty(body))
                {
                    UploadHandler uh = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    uh.contentType = "application/json";
                    www.uploadHandler = uh;
                }

                www.downloadHandler = new DownloadHandlerBuffer();
                if (cookie != null)
                {
                    www.SetRequestHeader("Cookie", cookie);
                }

                var request = www.SendWebRequest();
                CachingCount[url] = request;
                request.completed += operation =>
                {
                    OnComplete(url, onCompleted, onError, www);
                    CachingCount.Remove(url);
                };
            }
            else
            {
                onCompleted(TextCache[url]);
            }
        }
    }

    private void OnComplete(string url, Action<string> onCompleted, Action<string, long, string> onError,
        UnityWebRequest www)
    {
        if (www.isNetworkError || www.isHttpError)
        {
            onError?.Invoke(www.url, www.responseCode, www.error);
        }
        else
        {
            CachingTime[url] = DateTime.UtcNow.Ticks;
            TextCache[url] = www.downloadHandler.text;
            onCompleted?.Invoke(TextCache[url]);
        }
    }
}