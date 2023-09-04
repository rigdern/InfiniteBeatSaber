using Zenject;

namespace InfiniteBeatSaber.Installers
{
    internal class PlayerInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<InfiniteBeatSaberMode>().AsSingle().NonLazy();
        }
    }
}
