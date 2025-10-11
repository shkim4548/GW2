using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Google.Protobuf.Protocol;
using UnityEngine;

// 전역 Network Service 관리자
public class Bootstrapper : MonoBehaviour
{
    //public static DIContainer GlobalContainer { get; private set; }

    [Inject]
    private INetworkService _networkService;

    private ScopeContext _sceneScope;

    void Awake()
    {
        Debug.Log("BootStrapper");
        _sceneScope = new ScopeContext();

        DontDestroyOnLoad(gameObject);
        //GlobalContainer = new DIContainer();

        // 전역 싱글톤 서비스 등록
        DI.Container.Register<INetworkService, NetworkService>(Define.ServiceLifetime.Singleton);
        DI.Container.Register<IUIService, UIService>(Define.ServiceLifetime.Singleton);
        DI.Container.Register<IResourceService, ResourceService>(Define.ServiceLifetime.Singleton);
        DI.Container.Register<ISceneService, SceneService>(Define.ServiceLifetime.Singleton);
        //Container.RegisterSingleton<IAudioService, AudioService>();

        // 등록된 것을 미리 꺼내두기
        _networkService = DI.Container.Resolve<INetworkService>();
        _networkService.Init();
    }

    private void Update()
    {
        _networkService.Update();
    }

}