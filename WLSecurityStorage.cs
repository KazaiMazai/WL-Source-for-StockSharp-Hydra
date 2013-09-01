using Yahoo;

namespace StockSharp.Hydra.Yahoo
{
    using System;
    using System.Collections.Generic;

    using Ecng.Common;
    using Ecng.Collections;

    using StockSharp.Algo.History;
    using StockSharp.Algo.History.Finam;
    using StockSharp.BusinessEntities;
    using StockSharp.Hydra.Core;

    class WLSecurityStorage : ISecurityStorage
    {
        private readonly ISecurityStorage _underlyingStorage;
        private readonly SynchronizedDictionary<string, Security> _cacheById = new SynchronizedDictionary<string, Security>();

        public IEnumerable<Security> WLSecurities { get { return _cacheById.Values; } }

        public WLSecurityStorage(ISecurityStorage underlyingStorage, HydraEntityRegistry entityRegistry)
        {
            if (underlyingStorage == null)
                throw new ArgumentNullException("underlyingStorage");

            

            foreach (var security in entityRegistry.Securities)
                TryAddToCache(security);

            _underlyingStorage = underlyingStorage;
        }

        public Security LoadBy(string fieldName, object fieldValue)
        {
            if (fieldName.CompareIgnoreCase("Id"))
                return _underlyingStorage.LoadBy(fieldName, fieldValue);

            return _cacheById.TryGetValue((string)fieldValue);
        }

        public IEnumerable<Security> Lookup(Security criteria)
        {
            return new[] { criteria };

        }

        public void Save(Security security)
        {
            _underlyingStorage.Save(security);
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
