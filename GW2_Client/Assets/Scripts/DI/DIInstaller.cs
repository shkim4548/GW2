using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DIInstaller : MonoBehaviour
{
    private DIContainer _container;
    private ScopeContext _sceneScope;

    // Start is called before the first frame update
    void Awake()
    {
        _sceneScope = new ScopeContext();

        // DIInstaller.Awake()
        
        //DI.Container.Register<IResourceService, ResourceService>(Define.ServiceLifetime.Singleton);
        //DI.Container.Register<IUIService, UIService>(Define.ServiceLifetime.Singleton);
        //DI.Container.Register<IInputService, InputService>(Define.ServiceLifetime.Singleton);
        //DI.Container.Register<ISceneService, SceneService>(Define.ServiceLifetime.Singleton);
        //DI.Container.Register<INetworkService, NetworkService>(Define.ServiceLifetime.Singleton);

        //DI.Container.Inject(this, _sceneScope);

        //foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
            //DI.Container.Inject(mb, _sceneScope);
    }
}
