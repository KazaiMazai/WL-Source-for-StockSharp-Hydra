using StockSharp.Algo;
using StockSharp.Algo.Storages;


namespace StockSharp.Hydra.WLDataSource
{
    using System;
    using System.Collections.Generic;

    using Ecng.Common;
    using Ecng.Collections;

    using StockSharp.Algo.History;
  
    using StockSharp.BusinessEntities;
    using StockSharp.Hydra.Core;

  public  class WLSecurityStorage : ISecurityStorage
    {
        private readonly IEntityRegistry _entityRegistry;
        private readonly SynchronizedDictionary<string, Security> _cacheById = new SynchronizedDictionary<string, Security>();

        private IEnumerable<Security> _securities;
        public IEnumerable<Security> WLSecurities { get { return _cacheById.Values; } }

      


        public WLSecurityStorage(IEntityRegistry entityRegistry)
        {
            if (entityRegistry == null)
                throw new ArgumentNullException("entityRegistry");

            _entityRegistry = entityRegistry;

            foreach (var security in entityRegistry.Securities)
                TryAddToCache(security);

             
        }

        public Security LoadBy(string fieldName, object fieldValue)
        {
            
            if (StringHelper.CompareIgnoreCase(fieldName, "Id"))
                return CollectionHelper.TryGetValue<string, Security>((IDictionary<string, Security>)this._cacheById, (string)fieldValue);
            else
                return _entityRegistry.Securities.LoadBy(fieldName, fieldValue);

             

           
        }

        public IEnumerable<Security> Lookup(Security criteria)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<Security> Securities
        {
            get
            {
                return (IEnumerable<Security>)_entityRegistry.Securities;
            }
        }

        public void Save(Security security)
        {
            _entityRegistry.Securities.Save(security);
            TryAddToCache(security);
        }

        private void TryAddToCache(Security security)
        {
            if (security == null)
                throw new ArgumentNullException("security");

            _cacheById.SafeAdd(security.Id, key => security);
            
        }

        
         
    }
}
