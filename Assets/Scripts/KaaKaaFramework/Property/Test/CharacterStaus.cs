using UnityEngine;
using System.Collections;

public class CharacterStats : MonoBehaviour
{
    [Header("配置文件（可选）")]
    public PropertyGroupConfig moveSpeedConfig;

    private PropertyHandler _handler;

    private void Awake()
    {
        _handler = gameObject.AddComponent<PropertyHandler>();
        SetupProperties();
    }

    private void SetupProperties()
    {
        // ===== 方式1：预设模板（最简单）=====

        // 一行创建完整属性
        PropertyPresets.Standard(_handler, "MoveSpeed", 100f);
        PropertyPresets.Standard(_handler, "Attack", 50f);
        PropertyPresets.Standard(_handler, "Defence", 30f);

        // 简单属性
        PropertyPresets.Simple(_handler, "HP", 100f, 0f, 999f);
        PropertyPresets.Simple(_handler, "MP", 50f, 0f, 999f);

        // 百分比属性
        PropertyPresets.Percent(_handler, "CritRate", 5f);

        // ===== 方式2：Builder（灵活定制）=====

        new PropertyGroupBuilder(_handler, "Luck")
            .Basic("-Base", 10f, "")
            .Basic("-Equip", 0f, "")
            .Basic("-Buff", 0f, "")
            .Sum("", null, "-Base", "-Equip", "-Buff")
            .Clamp("", 0f, 100f)
            .Build();

        // ===== 方式3：配置文件 =====

        if (moveSpeedConfig != null)
        {
            PropertyFactory.CreateFromConfig(_handler, moveSpeedConfig);
        }

        LogStats();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            LogStats();
        }
    }

    private void LogStats()
    {
        Debug.Log($"MoveSpeed: {_handler.GetProperty<float>("MoveSpeed").GetValue()}");
        Debug.Log($"Attack: {_handler.GetProperty<float>("Attack").GetValue()}");
        Debug.Log($"HP: {_handler.GetProperty<float>("HP").GetValue()}");
        Debug.Log($"CritRate: {_handler.GetProperty<float>("CritRate").GetValue()}%");
    }

    // ===== 效果应用示例 =====

    public void UseSpeedPotion(float bonus, float duration)
    {
        var mod = new AdditiveModifier<float>(bonus);
        _handler.GetProperty("MoveSpeed-Value-Buff").AddModifier(mod);
        StartCoroutine(RemoveAfter(duration, "MoveSpeed-Value-Buff", mod));
    }

    public void EquipItem(float speedBonus)
    {
        var mod = new AdditiveModifier<float>(speedBonus);
        _handler.GetProperty("MoveSpeed-Value-Other").AddModifier(mod);
    }

    private IEnumerator RemoveAfter(float duration, string propId, IPropertyModifier mod)
    {
        yield return new WaitForSeconds(duration);
        _handler.GetProperty(propId)?.RemoveModifier(mod);
    }
}