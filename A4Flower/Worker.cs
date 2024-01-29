﻿using NewLife;
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
    private readonly IConfigProvider _configProvider;
    private FlowerSetting _setting = new();
    private readonly ITracer _tracer;
    private String _cron;
    private TimerX[] _timers;

    public Worker(A4 a4, IConfigProvider configProvider, ITracer tracer)
    {
        _a4 = a4;
        _configProvider = configProvider;
        _tracer = tracer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 绑定参数到配置中心，支持热更新
            _configProvider.LoadAll();
            if (_configProvider.Keys.Count < 3)
                _configProvider.Save(_setting);
            else
                _configProvider.Bind(_setting, true, null);
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        //var p = _setting.Period;
        //if (p <= 0) p = 3600;

        //_timer = new TimerX(DoWork, null, 0, p * 1000) { Async = true };

        if (!_setting.Cron.IsNullOrEmpty())
        {
            _cron = _setting.Cron;
            SetTimer(_cron);
        }

        return Task.CompletedTask;
    }

    private void SetTimer(String cron)
    {
        _timers.TryDispose();

        // 支持多个Cron表达式，分号隔开
        var ts = new List<TimerX>();
        foreach (var item in cron.Split(";"))
        {
            ts.Add(new TimerX(DoWork, null, item) { Async = true });
        }

        _timers = ts.ToArray();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timers.TryDispose();

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

            var t = _setting.BuzzerTime;
            if (t > 0)
            {
                buzzer.Write(true);
                Thread.Sleep(t);
                buzzer.Write(false);
            }

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

        // 定时时间有改变
        //if (_setting.Period > 0) _timer.Period = _setting.Period * 1000;
        if (!_setting.Cron.IsNullOrEmpty() && _setting.Cron != _cron)
        {
            _cron = _setting.Cron;
            SetTimer(_cron);
        }
    }
}