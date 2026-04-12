using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    Define.CameraMode _mode = Define.CameraMode.QuarterView;

    [SerializeField]
    Vector3 _delta = new Vector3(0.0f, 7.0f, -6.0f);

    [SerializeField]
    MyPlayerController _player = null;

    [SerializeField] float _edgeScrollSpeed = 20.0f;
    [SerializeField] float _edgeThreshold = 10.0f;     // 픽셀 단위

    private bool _isLockedToPlayer = true;
    private Vector3 _freeCameraTarget;

    public void SetPlayer(MyPlayerController player) { _player = player; }

    void Update()
    {
        // 스페이스: 플레이어 고정 토글
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isLockedToPlayer = !_isLockedToPlayer;
            // 잠금 해제 시 현재 플레이어 위치에서 자유 카메라 시작
            if (!_isLockedToPlayer && _player != null)
                _freeCameraTarget = _player.transform.position;
        }

        // 엣지 스크롤은 자유 모드에서만
        if (!_isLockedToPlayer)
            HandleEdgeScroll();
    }

    void HandleEdgeScroll()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 move = Vector3.zero;

        // 카메라 방향을 수평면으로 투영해 이동 방향 계산
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        if (mousePos.x < _edgeThreshold)
            move -= right;
        else if (mousePos.x > Screen.width - _edgeThreshold)
            move += right;

        if (mousePos.y < _edgeThreshold)
            move -= forward;
        else if (mousePos.y > Screen.height - _edgeThreshold)
            move += forward;

        _freeCameraTarget += move * _edgeScrollSpeed * Time.deltaTime;
    }

    void LateUpdate()
    {
        if (_mode != Define.CameraMode.QuarterView) return;

        if (_player == null)
        {
            Debug.Log("[CameraController] MyPlayer Controller is nullptr");
            return;
        }

        Vector3 anchor = _isLockedToPlayer ? _player.transform.position : _freeCameraTarget;

        RaycastHit hit;
        if (Physics.Raycast(anchor, _delta, out hit, _delta.magnitude, 1 << (int)Define.Layer.Block))
        {
            float dist = (hit.point - anchor).magnitude * 0.8f;
            transform.position = anchor + _delta.normalized * dist;
        }
        else
        {
            transform.position = anchor + _delta;
        }

        transform.LookAt(anchor);
    }

    public void SetQuarterView(Vector3 delta)
    {
        _mode = Define.CameraMode.QuarterView;
        _delta = delta;
    }
}
