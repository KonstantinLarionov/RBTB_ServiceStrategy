using RBTB_ServiceStrategy.Database;
using RBTB_ServiceStrategy.Database.Entities;
using RBTB_ServiceStrategy.Strategies;

namespace RBTB_ServiceStrategy.Background;
public class StrategyLevelHostedService : BackgroundService
{
    private readonly LevelStrategy _strategy;
	private readonly AnaliticContext _context;
	private Timer? _timerTrend = null;
    private Timer? _timerStr = null;
	private Timer _dbl;
	private Timer? _timerFractals;
	private List<Level> levels = new();
	private object locker = new();
	public StrategyLevelHostedService(LevelStrategy strategy, AnaliticContext context)
    {
        _strategy = strategy ?? throw new ArgumentException(nameof(strategy));
		_context = context;
		var b = _context.Database.CanConnect(); 
		_dbl = new Timer( ( _ ) =>
		{
			if(Monitor.TryEnter(locker))
			{
				try
				{
					levels = _context.Levels.ToList();
				}
				finally
				{
					Monitor.Exit(locker);
				}
			}
		}, null, 0, 10 );
	}

	protected override Task ExecuteAsync( CancellationToken stoppingToken )
	{
		_strategy.Init();
		
		_timerFractals = new Timer( ( _ ) => _strategy.CreateTradingLevel( levels ), null, TimeSpan.Zero,
			TimeSpan.FromHours( 2 ) );

		_timerTrend = new Timer( ( _ ) => _strategy.CalcTrend(), null, TimeSpan.Zero,
			TimeSpan.FromSeconds( 5 ) );

		_timerStr = new Timer( ( _ ) => _strategy.Handle(), null, TimeSpan.Zero,
			TimeSpan.FromMilliseconds( 100 ) );

		return Task.CompletedTask;
	}
}