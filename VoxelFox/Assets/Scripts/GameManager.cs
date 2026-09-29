using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 게임 진행 상태: 아이템 개수를 세고(5단계), 떨어지면 GAME OVER · 다 모으면 CLEAR(6단계).
// 결과가 나오면 여우 조작을 멈추고 걸린 시간도 멈춘다.
// 재시작(7단계): R 키(언제든) 또는 결과 패널의 [다시 하기] → 씬을 다시 불러 여우·코인·UI·시간을 모두 처음 상태로.
public class GameManager : MonoBehaviour
{
    public enum State { Playing, GameOver, Clear }

    [SerializeField] MapBuilder map;
    [Tooltip("여우가 이 높이 아래로 떨어지면 GAME OVER (타일 윗면 y 0, 바닥 -0.5)")]
    [SerializeField] float fallLimitY = -5f;

    public State Current { get; private set; } = State.Playing;
    public int ItemsTotal { get; private set; }
    public int ItemsCollected { get; private set; }
    public float ElapsedTime { get; private set; }
    public float FallLimitY => fallLimitY;

    public event Action ItemsChanged;
    public event Action<State> StateChanged;

    // R 키(게임패드는 Start). InputSystem_Actions에는 재시작 동작이 없어서 여기서 만든다.
    InputAction restartAction;
    bool restarting;

    void Awake()
    {
        restartAction = new InputAction("Restart", InputActionType.Button);
        restartAction.AddBinding("<Keyboard>/r");
        restartAction.AddBinding("<Gamepad>/start");
    }

    void OnEnable()
    {
        Collectible.Collected += OnCollected;
        restartAction.Enable();
    }

    void OnDisable()
    {
        // 씬을 다시 불러도 정적 이벤트에 옛 GameManager가 남지 않도록 반드시 뗀다.
        Collectible.Collected -= OnCollected;
        restartAction.Disable();
    }

    void OnDestroy() => restartAction.Dispose();

    // 씬을 통째로 다시 불러 처음 상태로 돌아간다. 같은 프레임에 여러 번 불려도 한 번만 한다.
    public void Restart()
    {
        if (restarting) return;
        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 모든 Awake(MapBuilder가 코인을 만드는 시점) 뒤에 센다.
    void Start()
    {
        ItemsTotal = map.Coins.Count;
        ItemsCollected = 0;
        ItemsChanged?.Invoke();
    }

    void Update()
    {
        if (restartAction.WasPressedThisFrame())
        {
            Restart();
            return;
        }
        if (Current != State.Playing) return;
        ElapsedTime += Time.deltaTime;
        if (map.Player.transform.position.y < fallLimitY) End(State.GameOver);
    }

    void OnCollected(Collectible item)
    {
        if (Current != State.Playing) return;
        ItemsCollected++;
        ItemsChanged?.Invoke();
        if (ItemsCollected >= ItemsTotal) End(State.Clear);
    }

    void End(State result)
    {
        Current = result;
        map.Player.ControlEnabled = false;
        StateChanged?.Invoke(result);
    }
}
