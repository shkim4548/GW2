using GameServerAdmin.Common.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameServerAdmin.Domain.Game.Gacha;

[Table("gacha_item")]
public class GachaItem
{
    protected GachaItem() { }

    public GachaItem(string name, GachaGrade grade, int duplicateCompensation)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("아이템 이름은 필수입니다.");
        if (duplicateCompensation < 0)
            throw new DomainException("중복 보상은 0 이상이어야 합니다.");

        Name                  = name;
        Grade                 = grade;
        DuplicateCompensation = duplicateCompensation;
        IsActive              = true;
    }

    [Key]
    [Column("gacha_item_id")]
    public long GachaItemId { get; private set; }

    [Column("name")]
    public string Name { get; private set; } = null!;

    [Column("grade")]
    public GachaGrade Grade { get; private set; }

    [Column("duplicate_compensation")]
    public int DuplicateCompensation { get; private set; }

    [Column("is_active")]
    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
