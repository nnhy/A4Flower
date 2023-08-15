using NewLife;
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
    private readonly ITracer _tracer;
    private TimerX _timer;

    public Worker(A4 a4, ITracer tracer)
    {
        _a4 = a4;
        _tracer = tracer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new TimerX(DoWork, null, 0, 3600_000);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();

        return Task.CompletedTask;
    }

    void DoWork(Object state)
    {
        using var span = _tracer?.NewSpan("DoWork");
        try
        {
            var buzzer = _a4.Buzzer;
            var usb = _a4.UsbPower;

            buzzer.Write(true);
            Thread.Sleep(200);
            buzzer.Write(false);

            usb.Write(true);
            Thread.Sleep(3000);
            usb.Write(false);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            XTrace.WriteException(ex);
        }
    }
}