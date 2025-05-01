using Zenject;
using ClockApp.Services;

namespace ClockApp.Installers
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Services
            Container.BindInterfacesAndSelfTo<ClockService>().AsSingle();
            Container.BindInterfacesAndSelfTo<TimerService>().AsSingle();
            Container.BindInterfacesAndSelfTo<StopwatchService>().AsSingle();

        }
    }
}
