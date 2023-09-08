using Zenject;

namespace InfiniteBeatSaber.Installers
{
    internal class AppInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
#if DEBUG
            Container.BindInterfacesAndSelfTo<DebugTools.Eval>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<DebugTools.WebSocketServer>().AsSingle();
#endif
        }
    }
}
