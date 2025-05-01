using Zenject;
using ClockApp.Services;
using UnityEngine;
using ClockApp.Controllers;

namespace ClockApp.Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("UI Settings")]
        [SerializeField] private TabStyleSettings _tabStyleSettings;

        public override void InstallBindings()
        {
            // Services
            Container.BindInterfacesAndSelfTo<ClockService>().AsSingle();
            Container.BindInterfacesAndSelfTo<TimerService>().AsSingle();
            Container.BindInterfacesAndSelfTo<StopwatchService>().AsSingle();
            Container.Bind<TabStyleSettings>().FromInstance(_tabStyleSettings).AsSingle();
            Container.Bind<TabController>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<AudioService>().AsSingle().NonLazy();
        }
    }
}

