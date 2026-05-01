public enum ServerMode
{
    Local,    // 로컬 테스트 (127.0.0.1)
    Desktop,  // 집 데스크탑 고정 사설 IP
    Hotspot   // 노트북 핫스팟 (Windows 기본값 고정)
}

public static class ConfigService
{
#if UNITY_EDITOR
    private static readonly ServerMode Mode = ServerMode.Local;   // 에디터: 항상 로컬
#else
    private static readonly ServerMode Mode = ServerMode.Desktop; // 빌드: 실제 IP
#endif

    public static string GameServerIp => Mode switch
    {
        ServerMode.Desktop => "192.168.75.184",
        ServerMode.Hotspot => "192.168.137.1",
        _ => "127.0.0.1"
    };

    public static int GameServerPort => 7777;
    public static string WebServerUrl => $"http://{GameServerIp}:5000";
}