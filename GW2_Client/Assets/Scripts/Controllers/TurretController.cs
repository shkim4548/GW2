using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TurretController : BaseController
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private GameObject _deathEffect;

    // 터렛이 공격
    public void OnAttack(int targetId)
    {
        IObjectService objectService = Bootstrapper.Instance.ObjectService;
        GameObject target = objectService.FindById(targetId);
        if (target == null) return;

        StartCoroutine(FireProjectile(target));
    }

    // 터렛이 사망
    public void OnDead()
    {
        if (_deathEffect != null)
            Instantiate(_deathEffect, transform.position, Quaternion.identity);

        IObjectService objectService = Bootstrapper.Instance.ObjectService;
        objectService.Remove(GetComponent<BaseController>().Id);
    }

    //시각 효과
    private IEnumerator FireProjectile(GameObject target)
    {
        if (_projectilePrefab == null || _firePoint == null)
            yield break;

        GameObject projectile = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);
        Vector3 startPos = _firePoint.position;
        Vector3 targetPos = target.transform.position + Vector3.up;

        float elapsed = 0f;
        float duration = 0.2f; // 투사체 이동 시간

        while (elapsed < duration)
        {
            if (projectile == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            projectile.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        Destroy(projectile);
    }
}
