using System.ComponentModel;

namespace A4Flower;

internal class FlowerSetting
{
    [Description("定时周期。默认3600秒")]
    public Int32 Period { get; set; } = 3600;

    [Description("蜂鸣器时间。默认200ms")]
    public Int32 BuzzerTime { get; set; } = 200;

    [Description("USB电源时间。默认3000ms")]
    public Int32 UsbTime { get; set; } = 3000;
}
