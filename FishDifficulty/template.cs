using StardewValley.Menus;

namespace SimpleFishingMod
{
    public interface diffidcultTemplate
    {
        // 根据当前 `BobberBar` 判断是否适用该难度实现；返回 true 时，`FishFightMenu` 会选用这个难度模板。
        bool CanBeChonse(BobberBar bar);
        // 挣扎阶段中鱼的移动速度；数值越大，鱼移动越快。
        float StruggleFishSpeed { get; }
        // 挣扎阶段持续时间（秒）；达到后会进入 Pull 阶段。
        float StrugglePhaseDurationSeconds { get; }
        // Pull 阶段持续时间（秒）；达到后会回到 Struggle 阶段。
        float PullPhaseDurationSeconds { get; }
    }
}
