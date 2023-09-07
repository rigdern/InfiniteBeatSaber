using Zenject;

namespace InfiniteBeatSaber.Installers
{
    internal class MenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<InfiniteBeatSaberMenuUI>().AsSingle().NonLazy();
        }
    }
}
