using GameServerAdmin.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Gacha;

[Table("gacha_pool")]
public class GachaPool
{
    protected GachaPool() { }

    public GachaPool(string name, int singlePullCost, int tenPullCost,
                     int pityThreshold, GachaGrade pityGrade,
                     DateTime startAt, DateTime endAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("풀 이름은 필수입니다.");
        if (singlePullCost <= 0)
            throw new DomainException("1회 뽑기 비용은 0보다 커야 합니다.");
        if (tenPullCost <= 0)
            throw new DomainException("10연챠 비용은 0보다 커야 합니다.");
        if (pityThreshold <= 0)
            throw new DomainException("천장 횟수는 0보다 커야 합니다.");
        if (endAt <= startAt)
            throw new DomainException("종료일은 시작일 이후여야 합니다.");

        Name            = name;
        SinglePullCost  = singlePullCost;
        TenPullCost     = tenPullCost;
        PityThreshold   = pityThreshold;
        PityGrade       = pityGrade;
        StartAt         = startAt;
        EndAt           = endAt;
        IsActive        = true;
    }

    [Key]
    [Column("gacha_pool_id")]
    public long GachaPoolId { get; private set; }

    [Column("name")]
    public string Name { get; private set; } = null!;

    [Column("single_pull_cost")]
    public int SinglePullCost { get; private set; }

    [Column("ten_pull_cost")]
    public int TenPullCost { get; private set; }

    [Column("pity_threshold")]
    public int PityThreshold { get; private set; }

    [Column("pity_grade")]
    public GachaGrade PityGrade { get; private set; }

    [Column("is_active")]
    public bool IsActive { get; private set; }

    [Column("start_at")]
    public DateTime StartAt { get; private set; }

    [Column("end_at")]
    public DateTime EndAt { get; private set; }

    public bool IsAvailable(DateTime now) =>
        IsActive && now >= StartAt && now <= EndAt;

    public void Deactivate() => IsActive = false;
}
