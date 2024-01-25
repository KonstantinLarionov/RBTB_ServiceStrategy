﻿using BybitMapper.UTA.MarketStreamsV5.Events;
using BybitMapper.UTA.RestV5.Data.Enums;
using BybitMapper.UTA.RestV5.Data.ObjectDTO.Market.Kline;
using Microsoft.Extensions.Options;
using RBTB_ServiceStrategy.Database;
using RBTB_ServiceStrategy.Database.Entities;
using RBTB_ServiceStrategy.Domain.Options;
using RBTB_ServiceStrategy.Domain.States;
using RBTB_ServiceStrategy.Markets.Bybit;
using RBTB_ServiceStrategy.Notification.Telergam;

namespace RBTB_ServiceStrategy.Strategies;
public class LevelStrategy
{
    private TelegramClient _tg;
    private readonly ILogger<LevelStrategy> _logger;
    private readonly LevelStrategyOption _lso;
    private readonly string _wsurl;
    private readonly string _url;
    private BybitRestClient _client;
    private BybitWebSocket _socket;
    private List<decimal> PriceLevel = new List<decimal>();
    private List<decimal> PriceLevelUse = new List<decimal>();
    private Timer? _pingSender;
    private object _lockerHandle = new();

    private LevelStrategyState _stateNow = new LevelStrategyState();
    private decimal _scopePrice = 5;
    private string _symbol = "BTCUSDT";
    private bool _isStart = false;
    private int _scopePriceDown = 50;
    private int _counterReconnectWS = 40;
    private List<decimal> _priceBufferComeDown = new List<decimal>();

    public LevelStrategy(TelegramClient telegramClient, IOptions<LevelStrategyOption> lso, ILogger<LevelStrategy> logger, AnaliticContext context)
    {
        _tg = telegramClient;
        _logger = logger;
        _lso = lso.Value;
        _wsurl = _lso.WsUrl;
        _url = _lso.Url;
        _scopePrice = _lso.ScopePrice;
        _symbol = _lso.Symbol;
        _isStart = _lso.IsStart;
        _counterReconnectWS = _lso.CounterReconnectWS;

        _socket = new BybitWebSocket(_wsurl, _counterReconnectWS);
        _socket.ExecEvent += SocketOnExecEv;
        _socket.TradeEvent += SocketOnTradeEv;
        _socket.OpenEvent += SocketOnOpen;
        _socket.CloseEvent += SocketOnClose;

        _client = new BybitRestClient(_url);
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

        _socket.Symbol = this._symbol;
        _socket.Start();

        _pingSender = new Timer((_) => _socket.Ping(), null, TimeSpan.Zero, TimeSpan.FromSeconds(20));
    }


    public void CalcTrend()
    {
        try
        {
            var klines = _client.RequestGetKlineAsync(MarketCategory.Spot, _symbol, IntervalType.OneMinute, limit: 200).GetAwaiter().GetResult()!;
            if (klines.List != null)
            {
                this._stateNow.IsUpTrend = CalcTrend(klines.List);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Calc trend] - error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public bool Handle()
    {
        if (!_isStart)
        { return false; }

        if (Monitor.TryEnter(_lockerHandle))
        {
            try
            {
                AccumulationPriceDown_Long();

                var finder = PriceLevel
                    .FirstOrDefault(x => (x + _scopePrice >= _stateNow.PriceNow)
                                                && (x - _scopePrice <= _stateNow.PriceNow)
                                                && _stateNow.PriceNow != 0);

                if (finder != 0 && IsPriceDown_Long())
                {
                    _logger.LogInformation("Все условия стратегии соблюдены, отсылаю сигнал на торговлю: " + finder);
                    WsEvents.InvokeSTE(_stateNow.PriceNow, _symbol, finder);
                }
            }
            finally
            {
                Monitor.Exit(_lockerHandle);
            }
        }

        return true;
    }

    private void AccumulationPriceDown_Long()
    {
        if (_stateNow.PriceNow == 0)
            return;

        if (_priceBufferComeDown.Count == 0)
        { _priceBufferComeDown.Add(_stateNow.PriceNow); return; }

        if (_priceBufferComeDown.Any(x => x < _stateNow.PriceNow))
        { _priceBufferComeDown.Clear(); }
        else
        { _priceBufferComeDown.Add(_stateNow.PriceNow); }

        if (_priceBufferComeDown.Count > 60)
        { _priceBufferComeDown.RemoveAt(0); }
    }

    private bool IsPriceDown_Long()
    {
        _logger.LogInformation("Найден уровень");
        _logger.LogInformation("Проверяю историю цены на отскок..");

        if (_priceBufferComeDown.Count >= _scopePriceDown)
        {
            _logger.LogInformation("История подтвердила отскок");
            return true;
        }
        else
        {
            _logger.LogInformation("Отскок не подтвержден, список цен:");
            _logger.LogInformation(string.Join("\r\n", _priceBufferComeDown));
            return false;
        }
    }

    #region [LevelTool]

    public void CreateTradingLevel(List<Level> levels_buffer)
    {
        List<Level> levels = new List<Level>();

        levels = levels_buffer
            .OrderByDescending(x => x.Price)
            .Select(x => { x.Price = (int)x.Price; return x; })
            .ToList();
        levels = levels
            .GroupBy(x => x.Price)
            .Select(x => { return new Level() { Price = x.Key, Volume = x.Sum(v => v.Volume) }; })
            .ToList();

        List<(decimal, decimal)> first_fractals = FindFractalsLow(levels.Select(x => (x.Price, x.Volume)).ToList());
        List<(decimal, decimal)> second_fractals = FindFractalsLow(first_fractals);

        if (second_fractals.Count != 0)
        {
            PriceLevel.Clear();
            PriceLevel.AddRange(second_fractals.Select(x => x.Item1).ToList());
            if (PriceLevel.Count != 0)
                Console.Write($"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] Уровни для торговли: {String.Join(" ", PriceLevel.Select(p => p.ToString()).ToArray())}");
        }
    }

    private static List<(decimal, decimal)> FindFractals(List<(decimal Price, decimal Volume)> levels)
    {
        List<(decimal, decimal)> fractals = new List<(decimal, decimal)>();
        for (int i = 0; i < levels.Count; i++)
        {
            if (i == 0)
            {
                if (levels[i].Volume > levels[i + 1].Volume &&
                     levels[i].Volume > levels[i + 2].Volume &&
                     levels[i].Volume > levels[i + 3].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else if (i == 1)
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                         levels[i].Volume > levels[i + 1].Volume &&
                         levels[i].Volume > levels[i + 2].Volume &&
                         levels[i].Volume > levels[i + 3].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else if (i == 2)
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                        levels[i].Volume > levels[i - 2].Volume &&
                         levels[i].Volume > levels[i + 1].Volume &&
                         levels[i].Volume > levels[i + 2].Volume &&
                         levels[i].Volume > levels[i + 3].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else if (i == levels.Count - 1)
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                         levels[i].Volume > levels[i - 2].Volume &&
                         levels[i].Volume > levels[i - 3].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else if (i == levels.Count - 2)
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                         levels[i].Volume > levels[i - 2].Volume &&
                         levels[i].Volume > levels[i - 3].Volume &&
                         levels[i].Volume > levels[i + 1].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else if (i == levels.Count - 3)
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                         levels[i].Volume > levels[i - 2].Volume &&
                         levels[i].Volume > levels[i - 3].Volume &&
                         levels[i].Volume > levels[i + 1].Volume &&
                         levels[i].Volume > levels[i + 2].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                     levels[i].Volume > levels[i - 2].Volume &&
                     levels[i].Volume > levels[i - 3].Volume &&
                     levels[i].Volume > levels[i + 1].Volume &&
                     levels[i].Volume > levels[i + 2].Volume &&
                     levels[i].Volume > levels[i + 3].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
        }

        return fractals;
    }

    private static List<(decimal, decimal)> FindFractalsLow(List<(decimal Price, decimal Volume)> levels)
    {
        List<(decimal, decimal)> fractals = new List<(decimal, decimal)>();

        for (int i = 0; i < levels.Count; i++)
        {
            if (i == 0)
            {
                if (levels[i].Volume > levels[i + 1].Volume &&
                     levels[i].Volume > levels[i + 2].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else if (i == 1)
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                         levels[i].Volume > levels[i + 1].Volume &&
                         levels[i].Volume > levels[i + 2].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else if (i == levels.Count - 1)
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                         levels[i].Volume > levels[i - 2].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }
            else if (i == levels.Count - 2)
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                         levels[i].Volume > levels[i - 2].Volume &&
                         levels[i].Volume > levels[i + 1].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
                }
            }

            else
            {
                if (levels[i].Volume > levels[i - 1].Volume &&
                     levels[i].Volume > levels[i - 2].Volume &&
                     levels[i].Volume > levels[i + 1].Volume &&
                     levels[i].Volume > levels[i + 2].Volume)
                {
                    fractals.Add((levels[i].Price, levels[i].Volume));
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
    private bool CalcTrend(IReadOnlyList<KlineRaw> candles)
    {
        //return true;
        var ma = new MovingAverage();
        foreach (var candle in candles)
        {
            ma.ComputeAverage(candle.ClosePrice == null ? 0 : candle.ClosePrice.Value);
        }
        return this._stateNow.PriceNow > ma.Average;
    }

    public void SetPriceLevel(decimal price)
    {
        if (!PriceLevel.Contains(price))
        {
            PriceLevel.Add(price);
            _stateNow.Levels += " " + price;
        }
    }

    public void SetUsePriceLevel(decimal price)
    {
        if (!PriceLevelUse.Contains(price))
        {
            PriceLevelUse.Add(price);
            _stateNow.LevelsUse += " " + price;
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

    private void SocketOnTradeEv(TradeEvent tradeevent)
    {
        if (!_isStart)
        { return; }
        _stateNow.PriceNow = tradeevent.Data[0].Price!.Value;
        _stateNow.VolumeNow = tradeevent.Data[0].Volume!.Value;
        if (tradeevent.Data == null)
        {
            return;
        }
    }

    private void SocketOnClose(object sender, WebSocketSharp.CloseEventArgs e)
    {
        if (_pingSender != null)
        {
            _pingSender.Dispose();
        }
    }

    private void SocketOnOpen(object sender, EventArgs e)
    {
        _socket.PublicSubscribe(_symbol, BybitMapper.UTA.MarketStreamsV5.Data.Enums.PublicEndpointType.Trade, IntervalType.OneMinute);
        _pingSender = new Timer((_) => _socket.Ping(), null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
    }

    private void SocketOnExecEv(BaseEvent exec)
    {
        if (_pingSender != null)
        {
            _pingSender.Dispose();
        }
    }
    #endregion
}