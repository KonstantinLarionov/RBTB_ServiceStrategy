using RBTB_ServiceStrategy.Strategies;

namespace RBTB_ServiceStrategy.Background;

public class StrategyLevelHostedService : IHostedService
{
    private readonly LevelStrategy _strategy;
    private Timer? _timerTrend = null;
    private Timer? _timerStr = null;

    public StrategyLevelHostedService(LevelStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentException(nameof(strategy));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _strategy.Init();
        
        _timerTrend = new Timer((_)=>_strategy.CalcTrend(), null, TimeSpan.Zero,
            TimeSpan.FromSeconds(5));
        
        _timerStr = new Timer((_)=>_strategy.Handle(), null, TimeSpan.Zero,
            TimeSpan.FromMilliseconds(100));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}