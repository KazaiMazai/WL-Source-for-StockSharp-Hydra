using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WealthLab.DataProviders.Common;

namespace WealthLabDataSource
{
    public class Class1
    {
        public  void Wow()
        {
            var r = new RoadRunner();
          var bars =  r.RequestHistoricalData("AAPL");
            


        }

    }
}
