using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Chapter prototype coordinator. It owns only local orchestration; network authority is not claimed here.
    /// </summary>
    public sealed class PrototypeGame : MonoBehaviour
    {
        private readonly List<PrototypeTeammate> _teammates = new List<PrototypeTeammate>();
        private readonly List<PrototypeCapsuleMonster> _monsters = new List<PrototypeCapsuleMonster>();

        private PrototypeMapBuilder _mapBuilder;
        private PrototypeHud _hud;
        private Vector3 _playerSpawn;
        [SerializeField] private int chapterNumber = 1;
        private bool _chapterTransitionQueued;
        private float _chapterTransitionDelay;

        public PrototypeCapsulePlayer Player { get; private set; }
        public PrototypeExitScoring Exit { get; private set; }
        public int ChapterNumber => chapterNumber;
        public PrototypeChapterDefinition Chapter => PrototypeChapterCatalog.Get(chapterNumber);
        public string ChapterDisplayName => Chapter.ChapterLabel;
        public string StatusMessage { get; private set; } = "Chapter 1 started.";
        public IReadOnlyList<PrototypeTeammate> Teammates => _teammates;

        public void ConfigureChapter(int value)
        {
            chapterNumber = Mathf.Clamp(value, 1, 7);
            StatusMessage = ChapterDisplayName + " ready.";
        }

        private void Start()
        {
            _mapBuilder = GetComponent<PrototypeMapBuilder>();
            if (_mapBuilder == null)
            {
                _mapBuilder = gameObject.AddComponent<PrototypeMapBuilder>();
            }
            _mapBuilder.Build(this);
            _playerSpawn = _mapBuilder.PlayerSpawn;
            _hud = GetComponent<PrototypeHud>();
            if (_hud == null)
            {
                _hud = gameObject.AddComponent<PrototypeHud>();
            }
            _hud.Configure(this);
            StatusMessage = ChapterDisplayName + " started. Reach the glowing exit.";
        }

        private void Update()
        {
            if (Exit != null && Exit.IsSettled && !_chapterTransitionQueued)
            {
                _chapterTransitionQueued = true;
                _chapterTransitionDelay = 1.5f;
                StatusMessage = "Chapter clear. Opening the story scene...";
            }

            if (_chapterTransitionQueued)
            {
                var queuedKeyboard = Keyboard.current;
                if (queuedKeyboard != null && queuedKeyboard.nKey.wasPressedThisFrame)
                {
                    LoadStoryInterlude();
                    return;
                }

                _chapterTransitionDelay -= Time.deltaTime;
                if (_chapterTransitionDelay <= 0f)
                {
                    LoadStoryInterlude();
                }

                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ResetRun();
            }

        }

        private void LoadStoryInterlude()
        {
            _chapterTransitionQueued = false;
            SceneManager.LoadScene(Chapter.StorySceneName);
        }

        public void RegisterWorld(PrototypeMapBuilder mapBuilder, PrototypeCapsulePlayer player, PrototypeExitScoring exit)
        {
            _mapBuilder = mapBuilder;
            Player = player;
            Exit = exit;
            _playerSpawn = mapBuilder.PlayerSpawn;
        }

        public void RegisterTeammate(PrototypeTeammate teammate)
        {
            if (!_teammates.Contains(teammate))
            {
                _teammates.Add(teammate);
            }
        }

        public void RegisterMonster(PrototypeCapsuleMonster monster)
        {
            if (!_monsters.Contains(monster))
            {
                _monsters.Add(monster);
            }
        }

        public void HandlePlayerDown()
        {
            StatusMessage = "You were downed by a hazard or monster. Prototype respawn applied.";
            Player.ResetForRun();
        }

        public void ResetRun()
        {
            if (Exit != null)
            {
                Exit.ResetRun();
            }

            _chapterTransitionQueued = false;
            _chapterTransitionDelay = 0f;

            if (Player != null)
            {
                Player.ResetForRun();
            }

            foreach (var teammate in _teammates)
            {
                teammate.ResetForRun();
            }

            foreach (var monster in _monsters)
            {
                monster.ResetForRun();
            }

            StatusMessage = "Run reset. The stage-start roster remains four participants.";
        }
    }
}
