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
using StockSharp.Algo.History.Finam;
using StockSharp.BusinessEntities;
using StockSharp.Hydra;
using StockSharp.Hydra.Core;
using StockSharp.Logging;
using WealthLab;
using WealthLab.DataProviders.Common;

namespace Yahoo
{
      class WLHistorySource : BaseHistorySource
      {
            private  List<string> _errorSecurititesList = new List<string>();

          
          private RoadRunner _roadRunner = new RoadRunner();

          public WLHistorySource(ISecurityStorage securityStorage) : base(securityStorage)
          {
             

              string filepath = Directory.GetCurrentDirectory() + "\\WLSourceTickers.txt";

              var fileInfo = new FileInfo(filepath);
              if (!fileInfo.Exists)
              {
                  File.CreateText(filepath);
                  
              }

            

          }

          private void SetRoadRunnerTimeFrame(TimeSpan timeFrame)
          {

            
              if (timeFrame.TotalSeconds == TimeSpan.FromDays(1).TotalSeconds)
              {
                  _roadRunner.Scale= BarScale.Daily;
                  _roadRunner.BarInterval = 1;

              }
              else
                  if (timeFrame.TotalSeconds == TimeSpan.FromHours(1).TotalSeconds)
                  {
                      _roadRunner.Scale = BarScale.Minute;
                      _roadRunner.BarInterval = 60;

                  }
                  else
                      if (timeFrame.TotalSeconds == TimeSpan.FromMinutes(30).TotalSeconds)
                      {
                          _roadRunner.Scale = BarScale.Minute;
                          _roadRunner.BarInterval = 30;

                      }
                      else
                          if (timeFrame.TotalSeconds == TimeSpan.FromMinutes(15).TotalSeconds)
                          {
                              _roadRunner.Scale = BarScale.Minute;
                              _roadRunner.BarInterval = 15;

                          }
                          else
                              if (timeFrame.TotalSeconds == TimeSpan.FromMinutes(10).TotalSeconds)
                              {
                                  _roadRunner.Scale = BarScale.Minute;
                                  _roadRunner.BarInterval = 10;

                              }
                              else

                              if (timeFrame.TotalSeconds == TimeSpan.FromMinutes(5).TotalSeconds)
                              {
                                  _roadRunner.Scale = BarScale.Minute;
                                  _roadRunner.BarInterval = 5;

                              }
                              else
                                  if (timeFrame.TotalSeconds == TimeSpan.FromMinutes(3).TotalSeconds)
                                  {
                                      _roadRunner.Scale = BarScale.Minute;
                                      _roadRunner.BarInterval = 3;

                                  }
                                  else
                                      if (timeFrame.TotalSeconds == TimeSpan.FromMinutes(2).TotalSeconds)
                                      {
                                          _roadRunner.Scale = BarScale.Minute;
                                          _roadRunner.BarInterval = 2;

                                      }
                                      else
                                          if (timeFrame.TotalSeconds == TimeSpan.FromMinutes(1).TotalSeconds)
                                          {
                                              _roadRunner.Scale = BarScale.Minute;
                                              _roadRunner.BarInterval = 1;

                                          }
                                       else throw new ArgumentException("TimeFrame для исторических данных не поддерживается.");
              

              

          }
          
          public IEnumerable<Candle> GetCandles(Security security, DateTime beginDate,DateTime endDate,TimeSpan timeframe)
          {
               
                  var candleList = new List<TimeFrameCandle>();

                  if (_errorSecurititesList.Any(c => c == security.Id)) return candleList;


                  SetRoadRunnerTimeFrame(timeframe);
                  _roadRunner.StartDate = beginDate;
                  _roadRunner.EndDate = endDate;

                var wlBars =  _roadRunner.RequestHistoricalData(security.Code);
                  for (int i = 0; i < wlBars.Count; i++)
                  {
                      var candle = new TimeFrameCandle();
                      candle.OpenTime = wlBars.Date[i] - timeframe;
                      candle.CloseTime = wlBars.Date[i];

                      candle.OpenPrice = (decimal) wlBars.Open[i];
                      candle.HighPrice = (decimal)wlBars.High[i];
                      candle.LowPrice = (decimal)wlBars.Low[i];
                      candle.ClosePrice = (decimal)wlBars.Close[i];
                      candle.TotalVolume = (decimal) wlBars.Volume[i];


                      candle.TimeFrame = timeframe;
                      candle.Security = security;

                      candleList.Add(candle);


                  }
                 
                  
              return candleList;

          }

          public ExchangeBoard GetSmartExchangeBoard()
          {

              var exchangeBoard = ExchangeBoard.GetOrCreateBoard("SMART", code => new ExchangeBoard
              {

                  Exchange = new Exchange { Name = "SMART Routing", EngName = "SMART Routing", RusName = "Смарт роутинг", TimeZoneInfo = Exchange.Nasdaq.TimeZoneInfo },
                  Code = code,
                  IsSupportAtomicReRegister = true,
                  IsSupportMarketOrders = true,
                  WorkingTime = ExchangeBoard.Nasdaq.WorkingTime
              });


              return exchangeBoard;

          }

          public List<Security> GetSecuritiesFromTxt()
          {
              var securities = new List<Security>();
              string filepath = Directory.GetCurrentDirectory() + "\\YahooGoogleSourceTickers.txt";

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
                          ExchangeBoard exchangeBoard;

                          if (exchangeBoardCode == "SMART") exchangeBoard = GetSmartExchangeBoard();

                          else
                          {
                              exchangeBoard = ExchangeBoard.GetOrCreateBoard(exchangeBoardCode,
                                                                             notExistingExchangeBoardCode =>
                                                                             new ExchangeBoard
                                                                                 {
                                                                                     Exchange =
                                                                                         new Exchange
                                                                                             {
                                                                                                 Name =
                                                                                                     notExistingExchangeBoardCode +
                                                                                                     " Exchange",
                                                                                                 EngName =
                                                                                                     notExistingExchangeBoardCode +
                                                                                                     " Exchange",
                                                                                                 RusName =
                                                                                                     notExistingExchangeBoardCode +
                                                                                                     " по-русски"
                                                                                             },
                                                                                     Code = notExistingExchangeBoardCode,
                                                                                     IsSupportAtomicReRegister = true,
                                                                                     IsSupportMarketOrders = true,
                                                                                     WorkingTime =
                                                                                         ExchangeBoard.Nasdaq.
                                                                                         WorkingTime
                                                                                 });
                          }


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
                          security.Class = "YahooGoogleSource";
                          security.ExtensionInfo = new Dictionary<object, object>();
 
                        
                          
                          SecurityStorage.Save(security);

                      }

                      securities.Add(security);

                  }



              }
              contractsFile.Close();
              return securities;

          }
          
        
      }

 

    

    
}
