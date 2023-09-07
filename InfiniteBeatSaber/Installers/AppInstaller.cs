using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
