using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
