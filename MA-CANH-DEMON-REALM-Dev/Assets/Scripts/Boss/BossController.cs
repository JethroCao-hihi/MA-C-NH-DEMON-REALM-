using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private CutsceneDirector bossIntroCutsceneDirector;

    [Header("Intro Cutscene")]
    [SerializeField] private bool playBossIntroCutscene = true;
    [SerializeField] private bool lockBossAiDuringIntro = true;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 3f;
    private float nextAttackTime;

    [Header("Shoot Attack")]
    [SerializeField] private GameObject ballBulletPrefab;
    [SerializeField] private Transform[] firePoints;

    [Header("Slam Wave Attack")]
    [SerializeField] private GameObject wavePrefab;
    [SerializeField] private Transform leftWavePoint;
    [SerializeField] private Transform rightWavePoint;

    [Header("SFX")]
    [SerializeField] private string ballAttackSfxName = "ball_attack";
    [SerializeField] private string waveAttackSfxName = "wave_attack";

    private static readonly int AttackShootHash = Animator.StringToHash("Attack_Shoot");
    private static readonly int AttackSlamHash = Animator.StringToHash("Attack_Slam");
    private static string lastIntroSceneName;
    private static bool hasPlayedIntroInScene;
    private bool introLockActive;

    private void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private void Start()
    {
        RefreshIntroSceneSessionGuard();
        bool shouldPlayIntro = playBossIntroCutscene && !hasPlayedIntroInScene;
        if (shouldPlayIntro)
        {
            bossIntroCutsceneDirector = EnsureBossCutsceneDirector();
            if (bossIntroCutsceneDirector != null)
            {
                bossIntroCutsceneDirector.SetUseTimelineForRuntime(false);
                bossIntroCutsceneDirector.SetRuntimeCameraMovementEnabled(false);
                bossIntroCutsceneDirector.ConfigureRuntimeCutscene(
                    "boss_intro",
                    BuildDefaultBossIntroNarrative(),
                    allowCheckpointSkip: false,
                    allowWatchedSkip: false,
                    replaceSubtitles: true);

                if (lockBossAiDuringIntro)
                    introLockActive = true;

                StartCoroutine(RunBossIntroCutsceneFlow());
                return;
            }
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    private CutsceneDirector EnsureBossCutsceneDirector()
    {
        if (bossIntroCutsceneDirector != null)
        {
            BindDirectorToSharedCutsceneUI(bossIntroCutsceneDirector);
            return bossIntroCutsceneDirector;
        }

        if (CutsceneDirector.Instance != null)
        {
            bossIntroCutsceneDirector = CutsceneDirector.Instance;
            BindDirectorToSharedCutsceneUI(bossIntroCutsceneDirector);
            return bossIntroCutsceneDirector;
        }

        bossIntroCutsceneDirector = Object.FindFirstObjectByType<CutsceneDirector>();
        if (bossIntroCutsceneDirector != null)
        {
            BindDirectorToSharedCutsceneUI(bossIntroCutsceneDirector);
            return bossIntroCutsceneDirector;
        }

        GameObject runtimeCutsceneSystem = new GameObject("===== BOSS INTRO CUTSCENE SYSTEM =====");
        bossIntroCutsceneDirector = runtimeCutsceneSystem.AddComponent<CutsceneDirector>();
        bossIntroCutsceneDirector.SetPlayOnStartForRuntime(false);
        runtimeCutsceneSystem.AddComponent<CutsceneSetup>();
        BindDirectorToSharedCutsceneUI(bossIntroCutsceneDirector);
        return bossIntroCutsceneDirector;
    }

    private static void RefreshIntroSceneSessionGuard()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (lastIntroSceneName == currentSceneName)
            return;

        lastIntroSceneName = currentSceneName;
        hasPlayedIntroInScene = false;
    }

    private void BindDirectorToSharedCutsceneUI(CutsceneDirector director)
    {
        if (director == null)
            return;

        GameObject existingCutsceneUI = GameObject.Find("CutsceneUI");
        if (existingCutsceneUI == null)
            return;

        CanvasGroup existingFade = existingCutsceneUI.transform.Find("FadePanel")?.GetComponent<CanvasGroup>();
        if (existingFade == null)
            return;

        director.SetupReferences(existingCutsceneUI, existingFade);
    }

    private void Update()
    {
        if (introLockActive)
            return;

        if (Time.time < nextAttackTime)
            return;

        ChooseRandomAttack();
        nextAttackTime = Time.time + attackCooldown;
    }

    private void ChooseRandomAttack()
    {
        if (anim == null) return;

        int randomAttack = Random.Range(0, 2);

        anim.ResetTrigger(AttackShootHash);
        anim.ResetTrigger(AttackSlamHash);

        if (randomAttack == 0)
            anim.SetTrigger(AttackShootHash);
        else
            anim.SetTrigger(AttackSlamHash);
    }

    private static void TrySetDirectionIfHasBossBullet(GameObject go, Vector2 dir)
    {
        if (go == null) return;

        BossBullet bullet = go.GetComponent<BossBullet>();
        if (bullet != null)
            bullet.SetDirection(dir);
    }

    private Vector2 GetDirectionFromPoint(Transform point)
    {
        // Convention: if the point is to the left of the boss, shoot left; otherwise shoot right.
        // Uses world positions to avoid local flip/rotation issues.
        if (point == null) return Vector2.right;

        float dx = point.position.x - transform.position.x;
        return dx < 0f ? Vector2.left : Vector2.right;
    }

    private Vector2 GetInvertedDirectionFromPoint(Transform point)
    {
        return -GetDirectionFromPoint(point);
    }

    // --- Animation Events ---

    public void SpawnBullet()
    {
        if (ballBulletPrefab == null || firePoints == null || firePoints.Length == 0)
            return;

        foreach (Transform point in firePoints)
        {
            if (point == null) continue;

            GameObject bulletGo = Instantiate(ballBulletPrefab, point.position, point.rotation);
            TrySetDirectionIfHasBossBullet(bulletGo, GetInvertedDirectionFromPoint(point));
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBossSfx(ballAttackSfxName);
    }

    public void SpawnWave()
    {
        if (wavePrefab == null)
            return;

        if (leftWavePoint != null)
        {
            GameObject left = Instantiate(wavePrefab, leftWavePoint.position, leftWavePoint.rotation);
            TrySetDirectionIfHasBossBullet(left, GetInvertedDirectionFromPoint(leftWavePoint));
        }

        if (rightWavePoint != null)
        {
            GameObject right = Instantiate(wavePrefab, rightWavePoint.position, rightWavePoint.rotation);
            TrySetDirectionIfHasBossBullet(right, GetInvertedDirectionFromPoint(rightWavePoint));
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBossSfx(waveAttackSfxName);
    }

    private SubtitleEntry[] BuildDefaultBossIntroNarrative()
    {
        return new SubtitleEntry[]
        {
            new SubtitleEntry { text = "Chuông tang của pháo đài ma giới vang lên, từng hồi nặng như lời tuyên án.", triggerTime = 0.6f, displayDuration = 3.3f },
            new SubtitleEntry { text = "Cổng huyết ấn tách làm đôi, để lộ đại điện ngập trong tro và xương cháy.", triggerTime = 4.3f, displayDuration = 3.5f },
            new SubtitleEntry { text = "Từ vương tọa mục nát, Ma Chủ đứng dậy cùng đôi cánh phủ kín bầu trời.", triggerTime = 8.2f, displayDuration = 3.7f },
            new SubtitleEntry { text = "Mỗi bước chân hắn kéo theo tiếng gào khóc của những linh hồn từng bị hiến tế.", triggerTime = 12.4f, displayDuration = 3.7f },
            new SubtitleEntry { text = "\"Kẻ phàm tục... ngươi đến để cầu xin, hay đến để chết trong bóng tối của ta?\"", triggerTime = 16.6f, displayDuration = 3.9f },
            new SubtitleEntry { text = "Lưỡi kiếm đã rút khỏi vỏ, và số mệnh của cả Ma Cảnh chỉ còn được quyết định bằng máu.", triggerTime = 21.0f, displayDuration = 4.0f }
        };
    }

    private IEnumerator RunBossIntroCutsceneFlow()
    {
        while (bossIntroCutsceneDirector != null && bossIntroCutsceneDirector.IsPlaying)
            yield return null;

        if (bossIntroCutsceneDirector != null)
            bossIntroCutsceneDirector.StartCutscene();

        while (bossIntroCutsceneDirector != null && bossIntroCutsceneDirector.IsPlaying)
            yield return null;

        lastIntroSceneName = SceneManager.GetActiveScene().name;
        hasPlayedIntroInScene = true;
        introLockActive = false;
        nextAttackTime = Time.time + attackCooldown;
    }
}
