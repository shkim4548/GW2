using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    public static Bootstrapper Instance { get; private set; }

    public INetworkService NetworkService { get; private set; }
    public IUIService UIService { get; private set; }
    public IResourceService ResourceService { get; private set; }
    public ISceneService SceneService { get; private set; }
    public IObjectService ObjectService { get; private set; }
    public IInputService InputService { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeServices();
    }

    private void InitializeServices()
    {
        DI.Container.Register<INetworkService, NetworkService>(Define.ServiceLifetime.Singleton);
        DI.Container.Register<IInputService, InputService>(Define.ServiceLifetime.Singleton);

        NetworkService = new NetworkService();
        UIService = new UIService();
        ResourceService = new ResourceService();
        SceneService = new SceneService();
        ObjectService = new ObjectService();
        InputService = new InputService();

        NetworkService.Init();
    }

    void Update()
    {
        NetworkService.Update();
    }
}