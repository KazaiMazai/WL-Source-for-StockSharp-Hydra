using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ecng.Collections;
using Ecng.Common;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Hydra.Core;
using StockSharp.Logging;

namespace StockSharp.Hydra.WLDataSource
{
 
     [DisplayName("Настройки источника WL")]
    public sealed class WLTaskSettings : HydraTaskSettings
    {
        public WLTaskSettings(HydraTaskSettings settings) : base(settings)
        {
        }

        [Category("WL")]
        [DisplayName("Временной отступ")]
        [Description("Временной отступ в днях от текущей даты, начиная c которого необходимо загружать данные")]
        public int Offset
        {
            get { return ExtensionInfo["Offset"].To<int>(); }
            set { ExtensionInfo["Offset"] = value; }
        }


        [Category("WL")]
        [DisplayName("Начальная дата")]
        [Description("Начальная дата для перезагрузки всех данных")]
        public DateTime StartFrom
        {
            get { return ExtensionInfo["StartFrom"].To<DateTime>(); }
            set { ExtensionInfo["StartFrom"] = value; }
        }

        [Category("WL")]
        [DisplayName("Режим перезагрузки")]
        [Description("Начальная дата для перезагрузки всех данных")]
        public bool ReDownLoad
        {
            get { return ExtensionInfo["ReDownLoad"].To<bool>(); }
            set { ExtensionInfo["ReDownLoad"] = value; }
        }
    }

    public class WLTask : BaseHydraTask, ISecuritySource
    {
        private readonly Dictionary<Security, HashSet<DateTime>> _candlesSkipDates;
        private WLSecurityStorage _finamSecurityStorage;
        private Action<IHydraTask, IEnumerable<Security>> _newSecurities;
        private IEnumerable<Security> _selectedSecurities;
        private WLTaskSettings _settings;
        private WLHistorySource _source;

        public WLTask()
        {
            _candlesSkipDates = new Dictionary<Security, HashSet<DateTime>>();
        }


        public override Uri Icon
        {
            get { return null; }
        }

        public override HydraTaskSettings Settings
        {
            get { return _settings; }
        }

        public override string Name
        {
            get { return "WL"; }
        }

        public override string Description
        {
            get { return "Источник предназначен для получения данных с Fidelity."; }
        }

        public override TaskTypes Type
        {
            get { return 0; }
        }

        public override Type[] SupportedDataTypes
        {
            get
            {
                return new Type[1]
                           {
                               typeof (Candle)
                           };
            }
        }

        public override bool IsSupportHistoricalData
        {
            get { return true; }
        }

        public event Action<IHydraTask, IEnumerable<Security>> NewSecurities
        {
            add
            {
                Action<IHydraTask, IEnumerable<Security>> action = _newSecurities;
                Action<IHydraTask, IEnumerable<Security>> comparand;
                do
                {
                    comparand = action;
                    action = Interlocked.CompareExchange(ref _newSecurities,
                                                         (Action<IHydraTask, IEnumerable<Security>>)
                                                         Delegate.Combine(comparand, value), comparand);
                } while (action != comparand);
            }
            remove
            {
                Action<IHydraTask, IEnumerable<Security>> action = _newSecurities;
                Action<IHydraTask, IEnumerable<Security>> comparand;
                do
                {
                    comparand = action;
                    action = Interlocked.CompareExchange(ref _newSecurities,
                                                         (Action<IHydraTask, IEnumerable<Security>>)
                                                         Delegate.Remove(comparand, value), comparand);
                } while (action != comparand);
            }
        }

        protected override void ApplySettings(HydraTaskSettings settings, bool isNew)
        {
            _settings = new WLTaskSettings(settings);
            if (isNew)
            {
                _settings.Offset = 1;
                _settings.StartFrom = new DateTime(2001, 1, 1);
                _settings.Interval = TimeSpan.FromDays(1.0);
                _settings.ReDownLoad = false;
            }
            else if (_settings.Interval == TimeSpan.FromSeconds(1.0))
                _settings.Interval = (TimeSpan.FromDays(1.0));
            _finamSecurityStorage = new WLSecurityStorage(EntityRegistry);

            var historySource = new WLHistorySource(_finamSecurityStorage);
            historySource.DumpFolder = GetTempPath();

            _source = historySource;
        }

        public override void SaveSettings()
        {
            base.SaveSettings();
            if (_source == null)
                return;
            _source.DumpFolder = GetTempPath();
        }

        private string GetTempPath()
        {
            string path =
                Path.Combine(((_settings.Drive as LocalMarketDataDrive) ?? DriveCache.Instance.DefaultDrive).Path,
                             "TemporaryFiles");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        protected override void OnStarted()
        {
            _selectedSecurities = (Equatable<Security>) (this).GetAllSecurity() != (Security) null
                                      ? _finamSecurityStorage.WLSecurities
                                      : Securities;
            _candlesSkipDates.Clear();

            base.OnStarted();
        }

        private string GetTempCandlesFilePath(Security security, DateTime beginDate, DateTime endDate, TimeSpan tf)
        {
            return
                Path.Combine(Path.Combine(_source.DumpFolder, security.Id,
                                          StringHelper.Put(
                                              "candles_{0}m_{1:0000}_{2:00}_{3:00}_{4:0000}_{5:00}_{6:00}.txt",
                                              (object) tf.TotalMinutes, (object) beginDate.Year,
                                              (object) beginDate.Month, (object) beginDate.Day, (object) endDate.Year,
                                              (object) endDate.Month, (object) endDate.Day)));
        }


       
        protected override TimeSpan OnProcess()
        {
            DateTime dateTime1;

            if (!_settings.ReDownLoad)
            {
                dateTime1 = DateTime.Today - TimeSpan.FromDays(_settings.Offset);
            }
            else
            {
                dateTime1 = _settings.StartFrom;
            }
            DateTime dateTime2 = DateTime.Today;
            var list = new List<DateTime>();
            for (; dateTime1 <= dateTime2; dateTime1 = dateTime1.AddDays(1.0))
                list.Add(dateTime1);
            foreach (Security security in _selectedSecurities)
            {
                if (CanProcess())
                {

                    {
                        
                        IEnumerable<KeyValuePair<Type, object>> candleArgs = security.GetCandleArgs(this);
                        IEnumerable<KeyValuePair<Type, object>> source1 =
                            candleArgs.Where(c => c.Key == typeof (TimeFrameCandle));
                        IEnumerable<TimeSpan> source2 = source1.Select(s => s.Value.To<TimeSpan>());
                        if (security.ContainsTask<Candle>(this) && !source2.IsEmpty())
                        {
                            HashSet<DateTime> hashSet = (_candlesSkipDates).SafeAdd(security);
                            if (CanProcess())
                            {
                                

                                foreach (TimeSpan tf in source2)
                                {
                                    if (CanProcess())
                                    {
                                        IMarketDataStorage<Candle> candleStorage =
                                            StorageRegistry.GetCandleStorage(
                                                new CandleSeries(typeof (TimeFrameCandle), security, tf),
                                                _settings.Drive);
                                        DateTime[] source3 =
                                            (list).Except(candleStorage.Dates).Except(hashSet).ToArray();
                                        if (source3.IsEmpty())
                                        {
                                            (this).AddInfoLog("Нет несохраненных дней для загрузки {0} свечек по {1}.",
                                                              (object) tf, (object) security.Id);
                                        }
                                        else
                                        {
                                            foreach (DateTime dateTime3 in source3)
                                            {
                                                if (CanProcess())
                                                {
                                                    hashSet.Add(dateTime3);
                                                    (this).AddInfoLog("Старт загрузки {0} свечек за {1} для {2}.",
                                                                         (object)tf,
                                                                         (object)dateTime3.ToShortDateString(),
                                                                         (object)security.Id);
                                                    IEnumerable<Candle> candles = _source.GetCandles(security, dateTime3,
                                                                                                     dateTime3 + TimeSpan.FromHours(23)+TimeSpan.FromMinutes(59)+TimeSpan.FromSeconds(59), tf);
                                                    if (candles.Any())
                                                    {
                                                       
                                                        SaveCandles(security, tf, candles, true);
                                                        hashSet.Remove(dateTime3);
                                                    }
                                                    var path = GetTempCandlesFilePath(security, dateTime3, dateTime3,
                                                                                      tf);
                                                    var info = new FileInfo(path);
                                                    if (info.Exists)
                                                    {
                                                        File.Delete(path);
                                                    }
                                                }
                                                else
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                        break;
                                }
                            }
                            else
                                break;
                        }
                    }
                }
                else
                    break;
            }
            if (CanProcess())
                (this).AddInfoLog("Окончание итерации.", new object[0]);
            return base.OnProcess();
        }


        public void LookupSecurities(Security criteria)
        {
            _newSecurities.SafeInvoke(this, _source.GetNewSecurities());
        }


        public override void ValidateSecurityInfo()
        {
            var list = new List<Security>();
            list.AddRange(Securities);
            foreach (Security security in  Securities)
            {
                if (security.MinStepSize == new Decimal(0))
                {
                    (this).AddWarningLog("Инструмент {0} имеет нулевой шаг цены. Сохранение данных невозможно.",
                                         new object[1]
                                             {
                                                 security.Id
                                             });
                    list.Remove(security);
                }
            }
            Securities = (list);
        }
    }
}
