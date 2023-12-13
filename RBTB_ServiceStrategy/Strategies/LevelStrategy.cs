using RBTB_ServiceStrategy.Markets.Binance;
using BinanceMapper.Requests;
using BinanceMapper.Spot.Exchange.V3.Data;
using BinanceMapper.Spot.Exchange.V3.Responses;
using BinanceMapper.Spot.MarketWS.Events;
using BinanceMapper.Spot.UserStream.Events;
using Microsoft.Extensions.Options;
using RBTB_ServiceStrategy.Database.Entities;
using RBTB_ServiceStrategy.Domain.Options;
using RBTB_ServiceStrategy.Domain.States;
using RBTB_ServiceStrategy.Notification.Telergam;

namespace RBTB_ServiceStrategy.Strategies
{
    public class LevelStrategy
    {
        private TelegramClient _tg;
        private readonly ILogger<LevelStrategy> _logger;
		private readonly LevelStrategyOption _lso;
        private readonly string _wsurl;
        private BinanceRestClient _client;
        private BinanceWebSocket _socket;
        private List<decimal> PriceLevel = new List<decimal>();
        private List<decimal> PriceLevelUse = new List<decimal>();
        public LevelStrategyState StateNow { get; set; } = new LevelStrategyState();

        public decimal ScopePrice { get; set; } = 5;
        public string Symbol { get; set; } = "BTCUSDT";
        public bool IsStart { get; set; } = false;
        public int ScoopPriceDown { get; set; } = 50;
        public List<decimal> PriceBufferComeDown { get; set; } = new List<decimal>();

        public LevelStrategy(TelegramClient telegramClient, IOptions<LevelStrategyOption> lso, ILogger<LevelStrategy> logger 
			)
        {
            _tg = telegramClient;
            _logger = logger;
			_lso = lso.Value;
            _wsurl = _lso.WsUrl;

            ScopePrice = _lso.ScopePrice;
            Symbol = _lso.Symbol;
            IsStart = _lso.IsStart;
            
            _client = new BinanceRestClient(new RequestArranger());
            _client.SetUrl(_lso.Url);
            
            _socket = new BinanceWebSocket();
            _socket.ExecEv += SocketOnExecEv;
            _socket.DepthEv += SocketOnDepthEv;
            _socket.TradeEv += SocketOnTradeEv;
            _socket.UserEv += SocketOnUserEv;
        }


        public void Start()
        {

            Thread ma = new Thread(() =>
            {
                while (true)
                {
                    CalcTrend();
                    Thread.Sleep(14400000);
                }
            });
            ma.Start();
            
            Thread str = new Thread(() =>
            {
                while (true)
                {
                    var res = Handle();
                    Thread.Sleep(res ? 100 : 60000);
                }
            });
            str.Start();
        }

        public void Init()
        {
            _logger.LogInformation("Инициализация стратегии уровней");
            _socket.Symbol = this.Symbol;
            _socket.Start(_wsurl);
           // GetLevels();
        }


        public void CalcTrend()
        {
            if (_client.RequestCandles(out var candles, Symbol, CandleInterval.FourHours, limit: 200))
            {
				
				this.StateNow.IsUpTrend = CalcTrend( candles );
			}
        }

        public bool Handle()
        {
            if (!IsStart)
            { return false; }

            AccumulationPriceDown_Long();
            
            var finder = PriceLevel
                .FirstOrDefault(x => (x + ScopePrice >= StateNow.PriceNow) 
                                            && (x - ScopePrice <= StateNow.PriceNow)
                                            && StateNow.PriceNow != 0);

            if (finder != 0 && IsPriceDown_Long())
            {
                //_logger.LogInformation("Все условия стратегии соблюдены, отсылаю сигнал на торговлю: " + finder);
                WsEvents.InvokeSTE(StateNow.PriceNow, Symbol, finder);
            }

            return true;
        }

        private void AccumulationPriceDown_Long()
        {
            if (StateNow.PriceNow == 0)
                return;

            if (PriceBufferComeDown.Count == 0)
            { PriceBufferComeDown.Add(StateNow.PriceNow); return; }
            
            if (PriceBufferComeDown.Any(x => x < StateNow.PriceNow))
            { PriceBufferComeDown.Clear(); }
            else
            { PriceBufferComeDown.Add(StateNow.PriceNow); }

            if (PriceBufferComeDown.Count > 60)
            { PriceBufferComeDown.RemoveAt(0); }
        }

        private bool IsPriceDown_Long() 
        {
            //_logger.LogInformation("Найден уровень");
            //_logger.LogInformation("Проверяю историю цены на отскок..");

            if (PriceBufferComeDown.Count >= ScoopPriceDown)
            {
                //_logger.LogInformation("История подтвердила отскок");
                return true;
            }
            else
            {
                //_logger.LogInformation("Отскок не подтвержден, список цен:");
                //_logger.LogInformation(string.Join("\r\n", PriceBufferComeDown));
                return false;
            }
        }

		#region [LevelTool]
		/// <summary>
		/// 
		/// </summary>
		public void CreateTradingLevel( List<Level> levels_buffer )
		{
			List<Level> levels = new List<Level>();

			levels = levels_buffer
				.OrderByDescending( x => x.Price )
				.Select( x => { x.Price = (int)x.Price; return x; } )
				.ToList();
			levels = levels
				.GroupBy( x => x.Price )
				.Select( x => { return new Level() { Price = x.Key, Volume = x.Sum( v => v.Volume ) }; } )
				.ToList();

			List<(decimal, decimal)> first_fractals = FindFractalsLow( levels.Select(x=> (x.Price, x.Volume)).ToList() );
			List<(decimal, decimal)> second_fractals = FindFractalsLow( first_fractals );


			if ( second_fractals.Count != 0 )
			{
				PriceLevel.Clear();
				PriceLevel.AddRange( second_fractals.Select(x=>x.Item1).ToList() );
				if ( PriceLevel.Count != 0 )
					Console.Write( $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] Уровни для торговли: {String.Join( " ", PriceLevel.Select( p => p.ToString() ).ToArray() )}" );
			}
		}

		private static List<(decimal, decimal)> FindFractals( List<(decimal Price, decimal Volume)> levels )
		{
			List<(decimal, decimal)> fractals = new List<(decimal, decimal)>();
			for ( int i = 0; i < levels.Count; i++ )
			{
				if ( i == 0 )
				{
					if ( levels[i].Volume > levels[i + 1].Volume &&
						 levels[i].Volume > levels[i + 2].Volume &&
						 levels[i].Volume > levels[i + 3].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}
				else if ( i == 1 )
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
							 levels[i].Volume > levels[i + 1].Volume &&
							 levels[i].Volume > levels[i + 2].Volume &&
							 levels[i].Volume > levels[i + 3].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}
				else if ( i == 2 )
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
							levels[i].Volume > levels[i - 2].Volume &&
							 levels[i].Volume > levels[i + 1].Volume &&
							 levels[i].Volume > levels[i + 2].Volume &&
							 levels[i].Volume > levels[i + 3].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}

				else if ( i == levels.Count - 1 )
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
							 levels[i].Volume > levels[i - 2].Volume &&
							 levels[i].Volume > levels[i - 3].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}
				else if ( i == levels.Count - 2 )
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
							 levels[i].Volume > levels[i - 2].Volume &&
							 levels[i].Volume > levels[i - 3].Volume &&
							 levels[i].Volume > levels[i + 1].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}
				else if ( i == levels.Count - 3 )
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
							 levels[i].Volume > levels[i - 2].Volume &&
							 levels[i].Volume > levels[i - 3].Volume &&
							 levels[i].Volume > levels[i + 1].Volume &&
							 levels[i].Volume > levels[i + 2].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}

				else
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
						 levels[i].Volume > levels[i - 2].Volume &&
						 levels[i].Volume > levels[i - 3].Volume &&
						 levels[i].Volume > levels[i + 1].Volume &&
						 levels[i].Volume > levels[i + 2].Volume &&
						 levels[i].Volume > levels[i + 3].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}
			}

			return fractals;
		}

		private static List<(decimal, decimal)> FindFractalsLow( List<(decimal Price, decimal Volume)> levels )
		{
			List<(decimal, decimal)> fractals = new List<(decimal, decimal)>();
			for ( int i = 0; i < levels.Count; i++ )
			{
				if ( i == 0 )
				{
					if ( levels[i].Volume > levels[i + 1].Volume &&
						 levels[i].Volume > levels[i + 2].Volume  )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}
				else if ( i == 1 )
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
							 levels[i].Volume > levels[i + 1].Volume &&
							 levels[i].Volume > levels[i + 2].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}

				else if ( i == levels.Count - 1 )
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
							 levels[i].Volume > levels[i - 2].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}
				else if ( i == levels.Count - 2 )
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
							 levels[i].Volume > levels[i - 2].Volume &&
							 levels[i].Volume > levels[i + 1].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}

				else
				{
					if ( levels[i].Volume > levels[i - 1].Volume &&
						 levels[i].Volume > levels[i - 2].Volume &&
						 levels[i].Volume > levels[i + 1].Volume &&
						 levels[i].Volume > levels[i + 2].Volume )
					{
						fractals.Add( (levels[i].Price, levels[i].Volume) );
					}
				}
			}

			return fractals;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="candles"></param>
		/// <returns>true - вверх</returns>
		private bool CalcTrend(IReadOnlyList<Candle> candles)
        {
			return true;
            var ma = new MovingAverage();
            foreach (var candle in candles)
            {
                ma.ComputeAverage(candle.ClosePrice);
            }
            return this.StateNow.PriceNow > ma.Average;
        }

        public void SetPriceLevel(decimal price)
        {
            if (!PriceLevel.Contains(price))
            {
                PriceLevel.Add(price);
                StateNow.Levels += " " + price;
            }
        }
        
        public void SetUsePriceLevel(decimal price)
        {
            if (!PriceLevelUse.Contains(price))
            {
                PriceLevelUse.Add(price);
                StateNow.LevelsUse += " " + price;
            }
        }

        public List<decimal> GetAllLevel()
        {
            return PriceLevel;
        }
        public List<decimal> GetUseLevel()
        {
            return PriceLevelUse;
        }
        #endregion

        #region  [Events]
        private void SocketOnUserEv(UserStreamEvent exec)
        {
        }

        private void SocketOnTradeEv(TradeEvent tradeevent)
        {
            if (!IsStart)
            { return; }
            StateNow.PriceNow = tradeevent.Price;
            StateNow.VolumeNow = tradeevent.Quantity;
        }

        private void SocketOnDepthEv(OrderBookEvent bookevent)
        {
        }

        private void SocketOnExecEv(UserStreamEvent exec)
        {
          
        }
        #endregion
    }

}