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

    public Action KeyAction { get; set; }
    public Action<Define.MouseEvent> MouseAction { get; set; }

    public void OnUpdate()
    {
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
        }
    }
}
