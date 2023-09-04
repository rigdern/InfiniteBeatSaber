using Zenject;

namespace InfiniteBeatSaber.Installers
{
    internal class MenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<InfiniteBeatSaberMenuUI>().AsSingle().NonLazy();

#if DEBUG
            Container.BindInterfacesAndSelfTo<DebugTools.WebSocketServer>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<DebugTools.Eval>().AsSingle().NonLazy();
#endif
        }
    }
}
