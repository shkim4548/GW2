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
    private int _lastProcessedFrame = -1;

    public Action KeyAction { get; set; }
    public Action<Define.MouseEvent> MouseAction { get; set; }

    public void OnUpdate()
    {
        if (Time.frameCount == _lastProcessedFrame)
            return;
        _lastProcessedFrame = Time.frameCount;

        if (Input.anyKey && KeyAction != null)
            KeyAction.Invoke();

        if (MouseAction != null)
        {
            if (Input.GetMouseButton(1))
            {
                MouseAction.Invoke(Define.MouseEvent.Press);
                _pressed = true;
            }
            else
            {
                if (_pressed)
                {
                    try
                    {
                        MouseAction.Invoke(Define.MouseEvent.Click);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[InputService] MouseAction.Click exception: {e}");
                    }
                }
                _pressed = false;
            }

            if (Input.GetMouseButtonDown(0))
                MouseAction.Invoke(Define.MouseEvent.LeftClick);
        }
    }
}
