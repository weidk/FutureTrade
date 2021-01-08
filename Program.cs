using CTPsysinfo;
using FutureTrade.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutureTrade
{
    class Program
    {

        public static void Main()
        {
            //var db = SugarTools.GetInstance();
            //db.DbFirst.Where("ConfigPars").CreateClassFile(@"D:\workspace\交易接口\总线\FutureTrade\Models", "FutureTrade.Models");


            // 初始化
            Initials Init = Funs.Init();

            
            //var db = GetInstance(Set.SqlserverconnString, DbType.SqlServer);

            //Init.db.CodeFirst.InitTables(typeof(RequestClass));

            MQServer mq = new MQServer();

            //mq.RecoverData(Init);

            mq.ClientRequestHandle(Init);

            mq.HeartBeatWorker(Init);

            //mq.OrderStateWorker(Init);

            Console.ReadLine();
            
        }


    }
}
