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
        
        [Header("Notification Settings")]
        [SerializeField] private AudioClip _timerCompletionSound;

        public override void InstallBindings()
        {
            // Services
            Container.BindInterfacesAndSelfTo<ClockService>().AsSingle();
            Container.BindInterfacesAndSelfTo<TimerService>().AsSingle();
            Container.BindInterfacesAndSelfTo<StopwatchService>().AsSingle();
            Container.Bind<TabStyleSettings>().FromInstance(_tabStyleSettings).AsSingle();
            Container.Bind<TabController>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<AudioService>().AsSingle().NonLazy();
            
            Container.Bind<AudioClip>().WithId("TimerCompletionSound")
                .FromInstance(_timerCompletionSound).AsSingle();
            
            // always active
            Container.BindInterfacesAndSelfTo<NotificationManager>().FromNewComponentOnNewGameObject()
                .WithGameObjectName("Global_NotificationManager")
                .AsSingle()
                .NonLazy();
                
            // persists between scenes
            Container.BindExecutionOrder<NotificationManager>(-10000);
        }
    }
}

