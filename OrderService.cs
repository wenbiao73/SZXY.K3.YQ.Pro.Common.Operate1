using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Computing;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Computing;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DependencyRules;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace SZXY.K3.YQ.Pro.Common.Operate
{
    [Description("重置机台种子")]
    public class OrderService : IScheduleService
    {
        /// <summary>
        /// 实现IScheduleService的函数，实例化
        /// </summary>
        /// <param name = "ctx" ></ param >
        /// < param name="schedule"></param>
        public void Run(Context ctx, Schedule schedule)
        {
            string orgSQL = "/*dialect*/SELECT FORGID FROM T_ORG_Organizations";
            DataSet orgds = DBServiceHelper.ExecuteDataSet(ctx, orgSQL);
            if (orgds != null && orgds.Tables.Count > 0 && orgds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in orgds.Tables[0].Rows)
                {
                    string Id = Convert.ToString(row[0]);
                    for (int i = 1; i < 51; i++)
                    {
                        //流延
                        //string zttemTableName = "Z_SZXY_BGJ_XYLYList" + Id + i.ToString();
                        string ttemTableName = "Z_SZXY_BGJ_XYLYList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                        //fuhe
                        //zttemTableName = "Z_SZXY_BGJ_XYFHList" + Id + i.ToString();
                        ttemTableName = "Z_SZXY_BGJ_XYFHList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                        //lashen
                        //zttemTableName = "Z_SZXY_BGJ_XYLSList" + Id + i.ToString();
                        ttemTableName = "Z_SZXY_BGJ_XYLSList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                        //fenceng
                        //zttemTableName = "Z_SZXY_BGJ_XYFCList" + Id + i.ToString();
                        ttemTableName = "Z_SZXY_BGJ_XYFCList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                        // fenceng
                        //zttemTableName = "Z_SZXY_BGJ_XYFQList" + Id + i.ToString();
                        ttemTableName = "Z_SZXY_BGJ_XYFQList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                        // SHIFA
                        //zttemTableName = "Z_SZXY_BGJ_XYSFList" + Id + i.ToString();
                        ttemTableName = "Z_SZXY_BGJ_XYSFList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                        // JIANGLIAO
                        //zttemTableName = "Z_SZXY_BGJ_XYSFList" + Id + i.ToString();
                        ttemTableName = "Z_SZXY_BGJ_XYJLList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                        // TUFU
                        //zttemTableName = "Z_SZXY_BGJ_XYTFList" + Id + i.ToString();
                        ttemTableName = "Z_SZXY_BGJ_XYTFList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                        // BAOZHUANG
                        //zttemTableName = "Z_SZXY_BGJ_XYTFList" + Id + i.ToString();
                        ttemTableName = "Z_SZXY_BGJ_XYBZList" + Id + i.ToString();
                        updateSQL(ctx, ttemTableName);
                    }
                }
            }

        }
        private void updateSQL(Context ctx, string ttemTableName)
        {
            DataSet ds = null;
            string SQL = "";          
            SQL = "/*dialect*/SELECT 1 FROM SYSOBJECTS WHERE NAME ='" + ttemTableName + "'";
            ds = DBServiceHelper.ExecuteDataSet(ctx, SQL);
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                SQL = "/*dialect*/DROP TABLE  " + ttemTableName + "";
                DBServiceHelper.Execute(ctx, SQL);
                SQL = "/*dialect*/CREATE TABLE " + ttemTableName + "(Id BIGINT IDENTITY(1,1) NOT NULL, Column1 INT NOT NULL)";
                DBServiceHelper.Execute(ctx, SQL);
            }
            else
            {
                SQL = "/*dialect*/CREATE TABLE " + ttemTableName + "(Id BIGINT IDENTITY(1,1) NOT NULL, Column1 INT NOT NULL)";
                DBServiceHelper.Execute(ctx, SQL);
            }
            //DoSQL(ztemTableName, zttemTableName, temTableName, ttemTableName);
        }
    }
}
