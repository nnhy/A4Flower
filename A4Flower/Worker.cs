using NewLife;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Model;
using NewLife.Threading;
using SmartA4;

namespace A4Flower;

/// <summary>
/// 后台任务。支持构造函数注入服务
/// </summary>
public class Worker : IHostedService
{
    private readonly A4 _a4;
    private FlowerSetting _setting = new();
    private readonly ITracer _tracer;
    private TimerX _timer;

    public Worker(A4 a4, IConfigProvider configProvider, ITracer tracer)
    {
        _a4 = a4;
        _tracer = tracer;

        // 绑定参数到配置中心，支持热更新
        if (configProvider.Keys.Count < 3)
            configProvider.Save(_setting);
        else
            configProvider.Bind(_setting, true, null);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var p = _setting.Period;
        if (p <= 0) p = 3600;

        _timer = new TimerX(DoWork, null, 0, p * 1000);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();

        return Task.CompletedTask;
    }

    void DoWork(Object state)
    {
        XTrace.WriteLine("打开开关");

        using var span = _tracer?.NewSpan("DoWork");
        try
        {
            var buzzer = _a4.Buzzer;
            var usb = _a4.UsbPower;

            var t = _setting.BuzzerTime > 0 ? _setting.BuzzerTime : 200;
            buzzer.Write(true);
            Thread.Sleep(t);
            buzzer.Write(false);

            t = _setting.UsbTime > 0 ? _setting.UsbTime : 3000;
            usb.Write(true);
            Thread.Sleep(t);
            usb.Write(false);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteException(ex);
        }

        XTrace.WriteLine("关闭电源");
    }
}