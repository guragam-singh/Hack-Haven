using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;

[InitializeOnLoad]
public static class HackHavenMcpSetup
{
    static HackHavenMcpSetup() { EditorApplication.update += ConnectWhenReady; }
    private static async void ConnectWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= ConnectWhenReady;
        if (SessionState.GetBool("HackHavenMcpConfigured", false)) return;
        SessionState.SetBool("HackHavenMcpConfigured", true);
        var config = EditorConfigurationCache.Instance;
        config.SetUseHttpTransport(true);
        config.SetHttpTransportScope("local");
        config.SetHttpBaseUrl("http://127.0.0.1:8080");
        config.SetUvxPathOverride("D:/Hack hevan/work/uv/uvx.exe");
        bool connected = await MCPServiceLocator.Bridge.StartAsync();
        Debug.Log("[Hack Haven MCP] Connected: " + connected);
    }
}
