
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
 
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    public class AScanReturnInfo
    {
        public string Value { get; set; }//结果
        public DynamicObject ResObj { get; set; }//结果

        public string Sort { get; set; }// 

        public string BZRJHRowId { get; set; }//  

        public string FQBH { get; set; }// 
    }
}
