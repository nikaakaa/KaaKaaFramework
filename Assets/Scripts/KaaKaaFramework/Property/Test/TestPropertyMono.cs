using System.Collections;
using UnityEngine;

/// <summary>
/// 属性系统完整测试脚本
/// 测试场景：角色移动速度的复杂计算
/// 
/// 公式：
/// MoveSpeed = MoveSpeed-Value * MoveSpeed-Mul
/// MoveSpeed-Value = MoveSpeed-Value-Config + MoveSpeed-Value-Buff + MoveSpeed-Value-Other
/// MoveSpeed-Mul = (MoveSpeed-Mul-Buff + 1) * MoveSpeed-Mul-Other
/// </summary>
public class PropertySystemTest : MonoBehaviour
{
    private PropertyHandler handler;

    private void Awake()
    {
        // 确保数学运算已初始化
        MathInitialization.Initialize();
    }

    private void Start()
    {
        handler = gameObject.AddComponent<PropertyHandler>();

        SetupMoveSpeedProperties();

        StartCoroutine(RunAllTests());
    }

    /// <summary>
    /// 配置移动速度属性树
    /// </summary>
    private void SetupMoveSpeedProperties()
    {
        Debug.Log("========== 初始化属性系统 ==========");

        // ===== 基础属性 =====

        // 配置的基础速度
        new BasicProperty<float>(100f, "MoveSpeed-Value-Config")
            .Register(handler)
            .NotifyParentOnDirty("MoveSpeed-Value");

        // Buff 速度加成（覆盖关系，只取优先级最高的）
        new BasicProperty<float>(0f, "MoveSpeed-Value-Buff")
            .Register(handler)
            .NotifyParentOnDirty("MoveSpeed-Value");

        // 其他速度加成（累加关系，限制最大50）
        new BasicProperty<float>(0f, "MoveSpeed-Value-Other")
            .Register(handler)
            .NotifyParentOnDirty("MoveSpeed-Value")
            .AddModifier(new ClampModifier<float>(-999f, 50f));

        // Buff 倍率（覆盖关系，限制 -1 到 0.5）
        new BasicProperty<float>(0f, "MoveSpeed-Mul-Buff")
            .Register(handler)
            .NotifyParentOnDirty("MoveSpeed-Mul")
            .AddModifier(new ClampModifier<float>(-1f, 0.5f));

        // 其他倍率（累乘关系，限制 0 到 1.5）
        new BasicProperty<float>(1f, "MoveSpeed-Mul-Other")
            .Register(handler)
            .NotifyParentOnDirty("MoveSpeed-Mul")
            .AddModifier(new ClampModifier<float>(0f, 1.5f));

        // ===== 计算属性 =====

        // MoveSpeed-Value = Config + Buff + Other
        new ComputedProperty<float>(
            () => handler.GetProperty<float>("MoveSpeed-Value-Config").GetValue() +
                  handler.GetProperty<float>("MoveSpeed-Value-Buff").GetValue() +
                  handler.GetProperty<float>("MoveSpeed-Value-Other").GetValue(),
            "MoveSpeed-Value")
            .Register(handler)
            .NotifyParentOnDirty("MoveSpeed")
            // 限制最大不超过 Config 的两倍
            .AddModifier(new ClampModifier<float>(
                () => 0f,
                () => handler.GetProperty<float>("MoveSpeed-Value-Config").GetValue() * 2f));

        // MoveSpeed-Mul = (Buff + 1) * Other
        new ComputedProperty<float>(
            () => (handler.GetProperty<float>("MoveSpeed-Mul-Buff").GetValue() + 1f) *
                  handler.GetProperty<float>("MoveSpeed-Mul-Other").GetValue(),
            "MoveSpeed-Mul")
            .Register(handler)
            .NotifyParentOnDirty("MoveSpeed");

        // MoveSpeed = Value * Mul
        new ComputedProperty<float>(
            () => handler.GetProperty<float>("MoveSpeed-Value").GetValue() *
                  handler.GetProperty<float>("MoveSpeed-Mul").GetValue(),
            "MoveSpeed")
            .Register(handler);

        PrintAllProperties("初始化完成");
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    private IEnumerator RunAllTests()
    {
        yield return new WaitForSeconds(0.5f);

        yield return TestBasicBuff();
        yield return TestBuffOverride();
        yield return TestOtherAdditive();
        yield return TestMultiplier();
        yield return TestFlyingShoes();
        yield return TestComplexScenario();

        Debug.Log("========== 所有测试完成 ==========");
    }

    /// <summary>
    /// 测试1：基础 Buff 效果
    /// </summary>
    private IEnumerator TestBasicBuff()
    {
        Debug.Log("\n----- 测试1: 基础 Buff 效果 -----");

        // 使用加速药水，速度 +20
        var buffMod = new OverrideModifier<float>(20f, priority: 100);
        handler.GetProperty("MoveSpeed-Value-Buff").AddModifier(buffMod);

        PrintAllProperties("使用加速药水(+20)");
        AssertEqual("MoveSpeed", 120f); // 100 + 20 = 120

        yield return new WaitForSeconds(0.3f);

        // 药水效果结束
        handler.GetProperty("MoveSpeed-Value-Buff").RemoveModifier(buffMod);

        PrintAllProperties("加速药水效果结束");
        AssertEqual("MoveSpeed", 100f);

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// 测试2：Buff 覆盖关系（高优先级覆盖低优先级）
    /// </summary>
    private IEnumerator TestBuffOverride()
    {
        Debug.Log("\n----- 测试2: Buff 覆盖关系 -----");

        // 普通加速药水 +10（优先级100）
        var normalBuff = new OverrideModifier<float>(10f, priority: 100);
        handler.GetProperty("MoveSpeed-Value-Buff").AddModifier(normalBuff);

        PrintAllProperties("普通加速药水(+10, 优先级100)");
        AssertEqual("MoveSpeed-Value-Buff", 10f);

        yield return new WaitForSeconds(0.3f);

        // 高级加速药水 +30（优先级200，覆盖普通药水）
        var superBuff = new OverrideModifier<float>(30f, priority: 200);
        handler.GetProperty("MoveSpeed-Value-Buff").AddModifier(superBuff);

        PrintAllProperties("高级加速药水(+30, 优先级200) - 应覆盖普通药水");
        AssertEqual("MoveSpeed-Value-Buff", 30f);
        AssertEqual("MoveSpeed", 130f);

        yield return new WaitForSeconds(0.3f);

        // 高级药水结束，普通药水还在
        handler.GetProperty("MoveSpeed-Value-Buff").RemoveModifier(superBuff);

        PrintAllProperties("高级药水结束，普通药水仍生效");
        AssertEqual("MoveSpeed-Value-Buff", 10f);

        yield return new WaitForSeconds(0.3f);

        // 清理
        handler.GetProperty("MoveSpeed-Value-Buff").RemoveModifier(normalBuff);

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// 测试3：Other 累加关系（受限制最大50）
    /// </summary>
    private IEnumerator TestOtherAdditive()
    {
        Debug.Log("\n----- 测试3: Other 累加关系 -----");

        // 环境加成 +20
        var envMod = new AdditiveModifier<float>(20f);
        handler.GetProperty("MoveSpeed-Value-Other").AddModifier(envMod);

        PrintAllProperties("环境加成(+20)");
        AssertEqual("MoveSpeed-Value-Other", 20f);

        yield return new WaitForSeconds(0.3f);

        // 伙伴加成 +25（累加后 45）
        var partnerMod = new AdditiveModifier<float>(25f);
        handler.GetProperty("MoveSpeed-Value-Other").AddModifier(partnerMod);

        PrintAllProperties("伙伴加成(+25) - 累加后45");
        AssertEqual("MoveSpeed-Value-Other", 45f);

        yield return new WaitForSeconds(0.3f);

        // 装备加成 +15（累加后60，但被限制为50）
        var equipMod = new AdditiveModifier<float>(15f);
        handler.GetProperty("MoveSpeed-Value-Other").AddModifier(equipMod);

        PrintAllProperties("装备加成(+15) - 累加60但被限制为50");
        AssertEqual("MoveSpeed-Value-Other", 50f); // 被 Clamp 限制

        yield return new WaitForSeconds(0.3f);

        // 清理
        handler.GetProperty("MoveSpeed-Value-Other").RemoveModifier(envMod);
        handler.GetProperty("MoveSpeed-Value-Other").RemoveModifier(partnerMod);
        handler.GetProperty("MoveSpeed-Value-Other").RemoveModifier(equipMod);

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// 测试4：倍率效果
    /// </summary>
    private IEnumerator TestMultiplier()
    {
        Debug.Log("\n----- 测试4: 倍率效果 -----");

        // Buff 倍率 +0.5（即 1.5 倍）
        var mulBuff = new OverrideModifier<float>(0.5f, priority: 100);
        handler.GetProperty("MoveSpeed-Mul-Buff").AddModifier(mulBuff);

        PrintAllProperties("速度倍率Buff(+0.5) - 总倍率1.5");
        AssertEqual("MoveSpeed-Mul", 1.5f);
        AssertEqual("MoveSpeed", 150f); // 100 * 1.5 = 150

        yield return new WaitForSeconds(0.3f);

        // 减速 Debuff -0.5（优先级更高，覆盖增益）
        var slowDebuff = new OverrideModifier<float>(-0.5f, priority: 200);
        handler.GetProperty("MoveSpeed-Mul-Buff").AddModifier(slowDebuff);

        PrintAllProperties("减速Debuff(-0.5) - 覆盖增益，总倍率0.5");
        AssertEqual("MoveSpeed-Mul", 0.5f);
        AssertEqual("MoveSpeed", 50f); // 100 * 0.5 = 50

        yield return new WaitForSeconds(0.3f);

        // 清理
        handler.GetProperty("MoveSpeed-Mul-Buff").RemoveModifier(mulBuff);
        handler.GetProperty("MoveSpeed-Mul-Buff").RemoveModifier(slowDebuff);

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// 测试5：小飞鞋效果（强制覆盖最终速度）
    /// </summary>
    private IEnumerator TestFlyingShoes()
    {
        Debug.Log("\n----- 测试5: 小飞鞋效果 -----");

        // 先添加一些效果
        var buffMod = new OverrideModifier<float>(20f, priority: 100);
        handler.GetProperty("MoveSpeed-Value-Buff").AddModifier(buffMod);

        PrintAllProperties("添加速度Buff(+20)");
        AssertEqual("MoveSpeed", 120f);

        yield return new WaitForSeconds(0.3f);

        // 装备小飞鞋：强制速度为基础速度的两倍，无视其他效果
        var flyingShoesMod = new OverrideModifier<float>(
            () => handler.GetProperty<float>("MoveSpeed-Value-Config").GetValue() * 2f,
            priority: 9999);
        handler.GetProperty("MoveSpeed").AddModifier(flyingShoesMod);

        PrintAllProperties("装备小飞鞋 - 速度强制为基础的两倍(200)");
        AssertEqual("MoveSpeed", 200f);

        yield return new WaitForSeconds(0.3f);

        // 卸下小飞鞋
        handler.GetProperty("MoveSpeed").RemoveModifier(flyingShoesMod);

        PrintAllProperties("卸下小飞鞋 - 恢复正常计算");
        AssertEqual("MoveSpeed", 120f);

        yield return new WaitForSeconds(0.3f);

        // 清理
        handler.GetProperty("MoveSpeed-Value-Buff").RemoveModifier(buffMod);

        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// 测试6：复杂场景（多种效果叠加）
    /// </summary>
    private IEnumerator TestComplexScenario()
    {
        Debug.Log("\n----- 测试6: 复杂场景 -----");

        // 1. 速度 Buff +15
        var speedBuff = new OverrideModifier<float>(15f, priority: 100);
        handler.GetProperty("MoveSpeed-Value-Buff").AddModifier(speedBuff);

        // 2. 环境加成 +10
        var envBonus = new AdditiveModifier<float>(10f);
        handler.GetProperty("MoveSpeed-Value-Other").AddModifier(envBonus);

        // 3. 倍率 +0.2
        var mulBonus = new OverrideModifier<float>(0.2f, priority: 100);
        handler.GetProperty("MoveSpeed-Mul-Buff").AddModifier(mulBonus);

        // 计算：
        // MoveSpeed-Value = 100 + 15 + 10 = 125
        // MoveSpeed-Mul = (0.2 + 1) * 1 = 1.2
        // MoveSpeed = 125 * 1.2 = 150

        PrintAllProperties("复杂叠加: Buff+15, 环境+10, 倍率+0.2");
        AssertEqual("MoveSpeed-Value", 125f);
        AssertEqual("MoveSpeed-Mul", 1.2f);
        AssertEqual("MoveSpeed", 150f);

        yield return new WaitForSeconds(0.3f);

        // 清理
        handler.GetProperty("MoveSpeed-Value-Buff").RemoveModifier(speedBuff);
        handler.GetProperty("MoveSpeed-Value-Other").RemoveModifier(envBonus);
        handler.GetProperty("MoveSpeed-Mul-Buff").RemoveModifier(mulBonus);

        PrintAllProperties("清理所有效果");
        AssertEqual("MoveSpeed", 100f);
    }

    /// <summary>
    /// 打印所有属性值
    /// </summary>
    private void PrintAllProperties(string title)
    {
        Debug.Log($"--- {title} ---");
        Debug.Log($"  MoveSpeed-Value-Config: {handler.GetProperty<float>("MoveSpeed-Value-Config").GetValue()}");
        Debug.Log($"  MoveSpeed-Value-Buff:   {handler.GetProperty<float>("MoveSpeed-Value-Buff").GetValue()}");
        Debug.Log($"  MoveSpeed-Value-Other:  {handler.GetProperty<float>("MoveSpeed-Value-Other").GetValue()}");
        Debug.Log($"  MoveSpeed-Value:        {handler.GetProperty<float>("MoveSpeed-Value").GetValue()}");
        Debug.Log($"  MoveSpeed-Mul-Buff:     {handler.GetProperty<float>("MoveSpeed-Mul-Buff").GetValue()}");
        Debug.Log($"  MoveSpeed-Mul-Other:    {handler.GetProperty<float>("MoveSpeed-Mul-Other").GetValue()}");
        Debug.Log($"  MoveSpeed-Mul:          {handler.GetProperty<float>("MoveSpeed-Mul").GetValue()}");
        Debug.Log($"  ★ MoveSpeed:            {handler.GetProperty<float>("MoveSpeed").GetValue()}");
    }

    /// <summary>
    /// 断言辅助方法
    /// </summary>
    private void AssertEqual(string propertyName, float expected)
    {
        float actual = handler.GetProperty<float>(propertyName).GetValue();
        if (Mathf.Approximately(actual, expected))
        {
            Debug.Log($"  ✅ {propertyName} = {actual} (期望 {expected})");
        }
        else
        {
            Debug.LogError($"  ❌ {propertyName} = {actual} (期望 {expected})");
        }
    }
}