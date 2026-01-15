using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace FunctionApps.Functions;

public class TestTimer
{
    private readonly ILogger<TestTimer> _logger;

    public TestTimer(ILogger<TestTimer> logger)
    {
        _logger = logger;
    }

    [Function("TestTimer")]
    public void Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Current timer scheduled for: {myTimer.ScheduleStatus.Next}");
        }
    }
}