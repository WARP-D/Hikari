using System;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hikari.Puzzle {
    public class Match : IDisposable {
        public bool Paused { get; set; }
        public Game game1;
        public Game game2;
        private Game[] games;
        private GameObject gameObject;

        private float countdownTimer;
        private IDisposable updateSubscription;

        public IObservable<IMatchEvent> EventStream { get; }
        private readonly Subject<IMatchEvent> eventSubject;

        public Match() {
            eventSubject = new Subject<IMatchEvent>();
            EventStream = eventSubject.AsObservable();
            game1 = new Game(this, new Game.PlayerInfo {ID = 0, Name = "Player1", Kind = PlayerKind.Human});
            game2 = new Game(this, new Game.PlayerInfo {ID = 1, Name = "Hikari", Kind = PlayerKind.AI});
            games = new[] {game1, game2};
            updateSubscription = Observable.EveryUpdate().Where(l => !Paused).Subscribe(Countdown);

            EventStream.OfType<IMatchEvent, MatchFinishEvent>()
                .First().Subscribe(e => {
                    Debug.Log("Match finished");
                    Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(t => {
                        Debug.Log("Loading Scene");
                        SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
                    });
                });
        }

        private void Countdown(long ticks) {
            if (ticks == 60) eventSubject.OnNext(new CountdownEvent {count = 3});
            if (ticks == 120) eventSubject.OnNext(new CountdownEvent {count = 2});
            if (ticks == 180) eventSubject.OnNext(new CountdownEvent {count = 1});
            if (ticks == 240) {
                eventSubject.OnNext(new CountdownEvent {count = 0});
                eventSubject.OnNext(new StartEvent());
                updateSubscription.Dispose();
                updateSubscription = Observable.EveryUpdate().Where(l => !Paused).Subscribe(GameLoop);
            }
        }

        private void GameLoop(long ticks) {
            eventSubject.OnNext(UpdateEvent.Default);

            if (games.Count(game => !game.IsDead) <= 1) {
                eventSubject.OnNext(MatchFinishEvent.Default);
            }
        }

        public void DistributeDamage(int sender, uint attack) {
            foreach (var game in games.Where((g, i) => i != sender)) {
                game.AddDamage(attack);
            }
        }

        public void Dispose() {
            eventSubject.Dispose();
            // updateSubscription.Dispose();
        }

        public interface IMatchEvent { }

        public class CountdownEvent : IMatchEvent {
            public int count;
            public bool stop;
        }
        
        public class StartEvent : IMatchEvent { }
        
        public class UpdateEvent : IMatchEvent {
            public static readonly UpdateEvent Default = new UpdateEvent();
        }
        
        public class MatchFinishEvent : IMatchEvent {
            public static readonly MatchFinishEvent Default = new MatchFinishEvent();
        }
    }
}