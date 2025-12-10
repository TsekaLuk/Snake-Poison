using UnityEngine;

namespace SnakePoison.Presentation.Input
{
    /// <summary>
    /// 输入控制器 - 处理玩家输入
    /// "单手即可控制，支持轻扫、长按、节奏操作"
    /// </summary>
    public class InputController : MonoBehaviour
    {
        [Header("Touch Settings")]
        [SerializeField] private float swipeThreshold = 50f;
        [SerializeField] private float tapThreshold = 0.2f;

        private Vector2 _touchStartPosition;
        private float _touchStartTime;
        private bool _isTouching;

        private void Update()
        {
            HandleKeyboardInput();
            HandleTouchInput();
        }

        private void HandleKeyboardInput()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.W))
            {
                SetDirection(Vector2Int.up);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                SetDirection(Vector2Int.down);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                SetDirection(Vector2Int.left);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || UnityEngine.Input.GetKeyDown(KeyCode.D))
            {
                SetDirection(Vector2Int.right);
            }

            // 游戏控制
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                TogglePause();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
            {
                StartOrRestart();
            }
        }

        private void HandleTouchInput()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _touchStartPosition = touch.position;
                        _touchStartTime = Time.time;
                        _isTouching = true;
                        break;

                    case TouchPhase.Ended:
                        if (_isTouching)
                        {
                            ProcessSwipe(touch.position);
                            _isTouching = false;
                        }
                        break;

                    case TouchPhase.Canceled:
                        _isTouching = false;
                        break;
                }
            }

            // 鼠标模拟（用于编辑器测试）
            #if UNITY_EDITOR
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _touchStartPosition = UnityEngine.Input.mousePosition;
                _touchStartTime = Time.time;
                _isTouching = true;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _isTouching)
            {
                ProcessSwipe(UnityEngine.Input.mousePosition);
                _isTouching = false;
            }
            #endif
        }

        private void ProcessSwipe(Vector2 endPosition)
        {
            Vector2 swipeDelta = endPosition - _touchStartPosition;
            float swipeTime = Time.time - _touchStartTime;

            // 判断是轻点还是滑动
            if (swipeDelta.magnitude < swipeThreshold)
            {
                if (swipeTime < tapThreshold)
                {
                    // 轻点 - 可用于其他功能
                    OnTap();
                }
                return;
            }

            // 判断滑动方向
            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                // 水平滑动
                SetDirection(swipeDelta.x > 0 ? Vector2Int.right : Vector2Int.left);
            }
            else
            {
                // 垂直滑动
                SetDirection(swipeDelta.y > 0 ? Vector2Int.up : Vector2Int.down);
            }
        }

        private void SetDirection(Vector2Int direction)
        {
            Application.GameManager.Instance?.SetDirection(direction);
        }

        private void TogglePause()
        {
            var gameManager = Application.GameManager.Instance;
            if (gameManager == null) return;

            // Toggle pause state - 简单实现，实际需要检查状态
        }

        private void StartOrRestart()
        {
            var gameManager = Application.GameManager.Instance;
            if (gameManager == null) return;

            gameManager.StartGame();
        }

        private void OnTap()
        {
            // 轻点可用于：
            // - 开始游戏
            // - 暂停/恢复
            // - 使用能力
            StartOrRestart();
        }
    }
}
