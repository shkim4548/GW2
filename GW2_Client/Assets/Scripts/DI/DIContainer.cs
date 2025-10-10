using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class InjectAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class LazyInjectAttribute : Attribute { }

public class DIContainer
{
    private class Registration
    {
        public Define.ServiceLifetime _lifeTime;
        public Type _implType;
        public object _singletonInstance;
    }

    private readonly Dictionary<Type, Registration> _map = new Dictionary<Type, Registration>();

    // 3.1 서비스 등록
    public void Register<TService, TImpl>(Define.ServiceLifetime lifetime) where TImpl : TService
    {
        _map[typeof(TService)] = new Registration
        {
            _lifeTime = lifetime,
            _implType = typeof(TImpl),
            _singletonInstance = null
        };
    }

    // 서비스 해석
    public TService Resolve<TService>(ScopeContext scope = null)
    {
        return (TService)Resolve(typeof(TService), scope ?? ScopeContext.Root);
    }

    private object Resolve(Type serviceType, ScopeContext scope = null)
    {
        // 1) Lazy<T> 요청이면 CreateLazy<T>로 반환
        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(Lazy<>))
        {
            var innerType = serviceType.GetGenericArguments()[0];
            var method = typeof(DIContainer)
                .GetMethod(nameof(CreateLazy),
                           BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(innerType);

            return method.Invoke(this, new object[] { scope });
        }

        if (!_map.TryGetValue(serviceType, out var reg))
        {
            throw new Exception($"No registration for {serviceType.Name}");
        }

        switch (reg._lifeTime)
        {
            case Define.ServiceLifetime.Singleton:
                if (reg._singletonInstance == null)
                    reg._singletonInstance = CreateInstance(reg._implType, scope);
                return reg._singletonInstance;

            case Define.ServiceLifetime.Scoped:
                return scope.GetOrAdd(serviceType, () => CreateInstance(reg._implType, scope));

            case Define.ServiceLifetime.Transient:
                return CreateInstance(reg._implType, scope);

            default:
                throw new Exception("Unknown lifetime");

        }
    }

    private object CreateInstance(Type implType, ScopeContext scope)
    {
        var ctor = implType.GetConstructors()
                           .OrderByDescending(c => c.GetParameters().Length)
                           .First();

        var args = ctor.GetParameters().Select(p =>
        {
            var paramType = p.ParameterType;

            // Lazy<T> 지원
            if (paramType.IsGenericType &&
                paramType.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                var innerType = paramType.GetGenericArguments()[0];

                var method = typeof(DIContainer)
                    .GetMethod(nameof(CreateLazy), BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(innerType);

                return method.Invoke(this, new object[] { scope });
            }

            // ★ 반드시 반환해야 함: 일반 의존성
            return Resolve(paramType, scope);

        }).ToArray();

        return ctor.Invoke(args);
    }


    public void Inject(MonoBehaviour mb, ScopeContext scope = null)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        foreach (var field in mb.GetType().GetFields(flags))
        {
            // [Inject]
            if (field.GetCustomAttribute<InjectAttribute>() != null)
            {
                var service = Resolve(field.FieldType, scope ?? ScopeContext.Root);
                field.SetValue(mb, service);
            }

            // [LazyInject]
            else if (Attribute.IsDefined(field, typeof(LazyInjectAttribute)))
            {
                var fieldType = field.FieldType;

                // Lazy<> 타입인지 먼저 확인
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Lazy<>))
                {
                    var genericType = fieldType.GetGenericArguments()[0];
                    var method = typeof(DIContainer)
                        .GetMethod(nameof(CreateLazy))!
                        .MakeGenericMethod(genericType);

                    var lazyObject = method.Invoke(this, new object[] { scope });
                    field.SetValue(mb, lazyObject);
                }
                else
                {
                    Debug.LogError($"[LazyInject]는 Lazy<T> 타입에만 적용할 수 있습니다. ({field.Name})");
                }
            }
        }
    }

    private Lazy<T> CreateLazy<T>(ScopeContext scope)
    {
        return new Lazy<T>(() => Resolve<T>(scope), LazyThreadSafetyMode.None);
    }
}
