using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Credit to https://gist.github.com/mattyellen/d63f1f557d08f7254345bff77bfdc8b3
public static class ExtensionMethods
{
    // public static TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    // {
    //     var tcs = new TaskCompletionSource<object>();
    //     asyncOp.completed += obj => { tcs.SetResult(null); };
    //     return ((Task)tcs.Task).GetAwaiter();
    // }
}

/* Example:
var getRequest = UnityWebRequest.Get("http://www.google.com");
await getRequest.SendWebRequest();
var result = getRequest.downloadHandler.text;
*/