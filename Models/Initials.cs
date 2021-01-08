using RabbitMQ.Client;
using SqlSugar;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FutureTrade.Models
{
    public class Initials
    {
        public Login_Output_O32 logOut = null;
        public SqlSugarClient db;
        public HSPack_Syn hsPack;
        public IDatabase rClient;
        public static Dictionary<string, UserInfo> UserDicts = new Dictionary<string, UserInfo>();
    }
}
