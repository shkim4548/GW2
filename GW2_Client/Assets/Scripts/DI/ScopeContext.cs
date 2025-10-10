using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScopeContext
{
    private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
    
    // 최상위 Root 스코프
    public static readonly ScopeContext Root = new ScopeContext();

    // 하위(씬, 매치) 스코프
    public ScopeContext CreateChild() => new ScopeContext();

    // Scoped 전용 인스턴스 캐싱
    public object GetOrAdd(Type serviceType, Func<object> factory)
    {
        if(_instances.TryGetValue(serviceType, out var inst))
        {
            inst = factory();
            _instances[serviceType] = inst;
        }
        return inst;
    }
}
