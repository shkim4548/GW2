using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInputService
{
    public Action KeyAction { get; set; }
    public Action<Define.MouseEvent> MouseAction { get; set; }
    public void OnUpdate();
}

public class InputService : IInputService
{
    public bool _pressed = false;
    private int _lastProcessedFrame = -1;   // ← 추가

    public Action KeyAction { get; set; }
    public Action<Define.MouseEvent> MouseAction { get; set; }

    public void OnUpdate()
    {
        // 같은 프레임에서 여러 컨트롤러가 호출해도 한 번만 처리
        if (Time.frameCount == _lastProcessedFrame)
            return;
        _lastProcessedFrame = Time.frameCount;

        if (Input.anyKey && KeyAction != null)
            KeyAction.Invoke();

        if(MouseAction != null)
        {
            if(Input.GetMouseButton(1))
            {
                MouseAction.Invoke(Define.MouseEvent.Press);
                _pressed = true;
            }
            else
            {
                if (_pressed)
                    MouseAction.Invoke(Define.MouseEvent.Click);
                _pressed = false;
            }

            if (Input.GetMouseButtonDown(0))
                MouseAction.Invoke(Define.MouseEvent.LeftClick);
        }
    }
}
