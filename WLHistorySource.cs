using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Ecng.Collections;
using Ecng.Common;
using StockSharp;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.History;

using StockSharp.BusinessEntities;
using StockSharp.Hydra;
using StockSharp.Hydra.Core;
using StockSharp.Logging;
using WealthLab;
using WealthLab.DataProviders.Common;

namespace StockSharp.Hydra.WLDataSource
{
     public  class WLHistorySource : BaseHistorySource
      {
          private  List<string> _errorSecurititesList = new List<string>();

          private DateTime _cachedBeginDate ;
          private DateTime _cachedEndDate;
          private Security _cachedSecurity;
          private TimeSpan _cachedTimeframe;
          private List<TimeFrameCandle> _cachedCandleList=null;
          private DateTime _lastUpdate;

          private static RoadRunner _roadRunner;

          public WLHistorySource(ISecurityStorage securityStorage) : base(securityStorage)
          {
              var securities = new List<Security>();
              string filepath = Directory.GetCurrentDirectory() + "\\WLSourceTickers.txt";

              var fileInfo = new FileInfo(filepath);
              if (!fileInfo.Exists)
              {
                  File.CreateText(filepath);
                  
              }

              _roadRunner = new RoadRunner();

          }

          private void SetRoadRunnerTimeFrame(TimeSpan timeFrame)
          {


              if (timeFrame == TimeSpan.FromDays(1.0))
              {
                  _roadRunner.Scale = BarScale.Daily;
                  _roadRunner.BarInterval = 1;

              }
              else
                  if (timeFrame == TimeSpan.FromHours(1.0))
                  {
                      _roadRunner.Scale = BarScale.Minute;
                      _roadRunner.BarInterval = 60;

                  }
                  else
                      if (timeFrame == TimeSpan.FromMinutes(30.0))
                      {
                          _roadRunner.Scale = BarScale.Minute;
                          _roadRunner.BarInterval = 30;

                      }
                      else
                          if (timeFrame == TimeSpan.FromMinutes(15.0))
                          {
                              _roadRunner.Scale = BarScale.Minute;
                              _roadRunner.BarInterval = 15;

                          }
                          else
                              if (timeFrame == TimeSpan.FromMinutes(10.0))
                              {
                                  _roadRunner.Scale = BarScale.Minute;
                                  _roadRunner.BarInterval = 10;

                              }
                              else

                                  if (timeFrame == TimeSpan.FromMinutes(5.0))
                                  {
                                      _roadRunner.Scale = BarScale.Minute;
                                      _roadRunner.BarInterval = 5;

                                  }
                                  else
                                      if (timeFrame == TimeSpan.FromMinutes(3.0))
                                      {
                                          _roadRunner.Scale = BarScale.Minute;
                                          _roadRunner.BarInterval = 3;

                                      }
                                      else
                                          if (timeFrame == TimeSpan.FromMinutes(2.0))
                                          {
                                              _roadRunner.Scale = BarScale.Minute;
                                              _roadRunner.BarInterval = 2;

                                          }
                                          else
                                              if (timeFrame == TimeSpan.FromMinutes(1.0))
                                              {
                                                  _roadRunner.Scale = BarScale.Minute;
                                                  _roadRunner.BarInterval = 1;

                                              }
                                              else throw new ArgumentException("TimeFrame для исторических данных не поддерживается.");




          }

          public IEnumerable<Candle> GetCandles(Security security, DateTime beginDate, DateTime endDate, TimeSpan timeframe)
          {

              var candleList = new List<TimeFrameCandle>();

              

              if (_cachedCandleList != null)
                  {
                      if (_cachedSecurity.Code == security.Code && _cachedTimeframe == timeframe)
                          if (DateTime.Now - _lastUpdate < TimeSpan.FromMinutes(30))
                              if(_cachedBeginDate<=beginDate && _cachedEndDate>=endDate)
                              {
                                  if (_cachedCandleList.Count == 0) return _cachedCandleList;

                                  var candles =
                                      _cachedCandleList.Where(c => c.OpenTime >= beginDate && c.OpenTime <= endDate);

                                  if(candles.Any())
                                      candleList.AddRange(candles);

                                  return candleList;
                                   
                                 
                              }
                              
                  }

              SetRoadRunnerTimeFrame(timeframe);

              if(timeframe==TimeSpan.FromDays(1.0))
              {
                  _roadRunner.StartDate = beginDate;
                  _roadRunner.EndDate = DateTime.Today+TimeSpan.FromDays(1.0);

              }
              else
              {
                  _roadRunner.StartDate = beginDate;

                  var date2 = endDate + TimeSpan.FromDays(30.0);
                  if (date2 >= DateTime.Today) date2 = DateTime.Today + TimeSpan.FromDays(1.0); ;
                  _roadRunner.EndDate = date2;
              }

                
             
              var wlBars = _roadRunner.RequestHistoricalData(security.Code);
              

              for (int i = 0; i < wlBars.Count; i++)
              {
                  var candle = new TimeFrameCandle();
                  candle.OpenTime = wlBars.Date[i] - timeframe;
                  candle.CloseTime = wlBars.Date[i];

                  candle.OpenPrice = (decimal) Math.Round(wlBars.Open[i], 2, MidpointRounding.ToEven);
                  candle.HighPrice = (decimal)Math.Round(wlBars.High[i], 2, MidpointRounding.ToEven);
                  candle.LowPrice = (decimal)Math.Round(wlBars.Low[i], 2, MidpointRounding.ToEven);
                  candle.ClosePrice = (decimal)Math.Round(wlBars.Close[i], 2, MidpointRounding.ToEven);
                  candle.TotalVolume = (decimal)Math.Round(wlBars.Volume[i], 2, MidpointRounding.ToEven);


                  candle.TimeFrame = timeframe;
                  candle.Security = security;

                  candleList.Add(candle);


              }


              if (candleList.Count > 0)
              {
                  _cachedCandleList = candleList.OrderBy
                      (c => c.OpenTime).ToList();
              }
              else
              {
                  _cachedCandleList = candleList;
              }
                  
                  _cachedBeginDate = _roadRunner.StartDate;
                  _cachedEndDate = _roadRunner.EndDate;
                  _cachedSecurity = security;
                  _cachedTimeframe = timeframe;
                  _lastUpdate = DateTime.Now;
                  if (candleList.Count > 0)
                  return _cachedCandleList.Where(c => c.OpenTime >= beginDate && c.OpenTime <= endDate);


                  return _cachedCandleList;



          }

          public ExchangeBoard GetSmartExchangeBoard()
          {

              var exchangeBoard = ExchangeBoard.GetOrCreateBoard("SMART", notExistingExchangeBoardCode => new ExchangeBoard
              {
                  Exchange = new Exchange {Name = notExistingExchangeBoardCode + " Exchange", EngName = notExistingExchangeBoardCode + " Exchange", RusName = notExistingExchangeBoardCode + " по-русски" },
                  Code = notExistingExchangeBoardCode,
                  IsSupportAtomicReRegister = true,
                  IsSupportMarketOrders = true,
                  WorkingTime = ExchangeBoard.Nasdaq.WorkingTime
              });


              return exchangeBoard;

          }

          private List<Security> GetSecuritiesFromTxt()
          {
              var securities = new List<Security>();
              string filepath = Directory.GetCurrentDirectory() + "\\WLSourceTickers.txt";

              var fileInfo = new FileInfo(filepath);
              if (!fileInfo.Exists)
              {
                  File.CreateText(filepath);
                  return securities;
              }


              StreamReader contractsFile = File.OpenText(filepath);
           
              while (true)
              {
                  string line=   contractsFile.ReadLine();
                  if (line == null) break;
                  if(line.Length==0) break;

                  line = line.Trim();
                  string[] tickers = line.Split(' ');
                  foreach (var ticker in tickers)
                  {
                      var securityId = ticker.Trim();
                      var splitedId = securityId.Split('@');

                      var code = splitedId[0];
                      var exchangeBoardCode = splitedId[1];

                      var security = SecurityStorage.LoadBy("Id", securityId);
                      if(security==null)
                      {
                          var exchangeBoard = ExchangeBoard.GetOrCreateBoard(exchangeBoardCode, notExistingExchangeBoardCode => new ExchangeBoard
                          {
                              Exchange = new Exchange { Name = notExistingExchangeBoardCode + " Exchange", EngName = notExistingExchangeBoardCode + " Exchange", RusName = notExistingExchangeBoardCode + " по-русски"},
                              Code = notExistingExchangeBoardCode,
                              IsSupportAtomicReRegister = true,
                              IsSupportMarketOrders = true,
                              WorkingTime = ExchangeBoard.Nasdaq.WorkingTime
                          });


                          security = EntityFactory.Instance.CreateSecurity(securityId);

                          security.Id = securityId;
                          security.Type = SecurityTypes.Stock;
                          security.Currency = CurrencyTypes.USD;
                          security.Name = code;
                          security.ExchangeBoard = exchangeBoard;
                          security.MinStepPrice = 0.01m;
                          security.MinStepSize = 0.01m;
                          security.ShortName = code;
                          security.Code = code;
                          security.Class = "WLSource";
                          security.ExtensionInfo = new Dictionary<object, object>();
 
                        
                          
                          SecurityStorage.Save(security);

                      }

                      securities.Add(security);

                  }



              }
              contractsFile.Close();
              return securities;

          }
          
          public IEnumerable<Security> GetNewSecurities()
          {
              return GetSecuritiesFromTxt();

          }
        
      }

 

    

    
}
