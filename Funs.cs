using FutureTrade.Models;
using RabbitMQ.Client;
using SqlSugar;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutureTrade
{
    public class Funs
    {

        public static ConfigPars CONFIG;

        //public static BlockingCollection<string> OrderQueues = new BlockingCollection<string>();
        public static ConcurrentBag<FuturepositionQry_Output> positionsQueues = new ConcurrentBag<FuturepositionQry_Output>();
        public static ConcurrentBag<FutureentrustQry_Output> ordersQueues = new ConcurrentBag<FutureentrustQry_Output>();
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public static Initials Init()
        {
            #region 初始化：连接及登录
            Login_Output_O32 logOut = null;
            SqlSugarClient db = SugarTools.GetInstance();

            CONFIG = db.Queryable<ConfigPars>().First();


            var UserList = db.Queryable<UserInfo>().ToList();
            foreach(UserInfo user in UserList)
            {
                Initials.UserDicts.Add(user.combi_no, user);
            }
            HSPack_Syn hsPack = new HSPack_Syn();
            // 连接
            if (hsPack.InitT2())
            {
                Console.WriteLine("连接成功");

                Login();
            }
            else
            {
                Console.WriteLine("连接失败");
            }

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(Funs.CONFIG.redis);
            IDatabase rClient = redis.GetDatabase();

            ConnectionMultiplexer redisHQ = ConnectionMultiplexer.Connect(Funs.CONFIG.redisHQ);
            IDatabase rClientHQ = redisHQ.GetDatabase();

            #endregion
            Initials Init = new Initials()
            {
                logOut = logOut,
                db = db,
                hsPack = hsPack,
                rClient = rClient,
                rClientHQ = rClientHQ,
            };
            return Init;


            void Login()
            {
                Login_Input_O32 objLogin_Input = new Login_Input_O32
                {
                    in_operator_no = Funs.CONFIG.operator_no,
                    in_password = Funs.CONFIG.O32Psw,
                    in_station_add = Funs.CONFIG.station_add,
                    in_ip_address = Funs.CONFIG.ip_address,
                    in_mac_address = hsPack.GetMac(true),
                    authorization_id = Funs.CONFIG.authorization_id,
                    app_id = Funs.CONFIG.app_id,
                    authorize_code = Funs.CONFIG.authorize_code,
                    port_id = Funs.CONFIG.port_id,
                };
                logOut = hsPack.Fun10001_Login_S(objLogin_Input);
                if (logOut.out_error_no == "0")
                {
                    Funs.CONFIG.token = logOut.out_user_token;
                    Console.WriteLine($"{Funs.CONFIG.operator_no} 登录成功");
                }
                else
                {
                    Console.WriteLine($"{Funs.CONFIG.operator_no}  {logOut.out_error_info}");
                }


                //foreach (KeyValuePair<string, UserInfo> kv in Initials.UserDicts)
                //{
                //    Login_Input_O32 objLogin_Input = new Login_Input_O32
                //    {
                //        in_operator_no = kv.Key,
                //        in_password = kv.Value.PASSWORD,
                //        in_station_add = ConfigurationManager.AppSettings["station_add"],
                //        in_ip_address = ConfigurationManager.AppSettings["ip_address"],
                //        in_mac_address = hsPack.GetMac(true),
                //        authorization_id = ConfigurationManager.AppSettings["authorization_id"],
                //        app_id = ConfigurationManager.AppSettings["app_id"],
                //        authorize_code = ConfigurationManager.AppSettings["authorize_code"],
                //        port_id = ConfigurationManager.AppSettings["port_id"],
                //    };
                //    logOut = hsPack.Fun10001_Login_S(objLogin_Input);
                //    if (logOut.out_error_no == "0")
                //    {
                //        kv.Value.token = logOut.out_user_token;
                //        Console.WriteLine($"{kv.Key} 登录成功");
                //    }
                //    else
                //    {
                //        Console.WriteLine($"{kv.Key}  {logOut.out_error_info}");
                //    }
                //}
                
            }

        }

        /// <summary>
        /// 心跳、成交、委托流水读取线程
        /// </summary>
        /// <param name="Init"></param>
        /// <returns></returns>
        public async static Task AsyHSWorker(Initials Init)
        {
            string account_code = ConfigurationManager.AppSettings["account_code"];
            string asset_no = ConfigurationManager.AppSettings["asset_no"];
            int interval = int.Parse(ConfigurationManager.AppSettings["interval"]);
            void LongTask()
            {
                while (true)
                {
                    try
                    {
                        // 发送心跳
                        Init.hsPack.Fun10000_heartBeat(Init.logOut.out_user_token);

                        // 查询持仓
                        FuturepositionQry_Input objFuturepositionQry_Input = new FuturepositionQry_Input();
                        objFuturepositionQry_Input.in_user_token = Init.logOut.out_user_token;
                        objFuturepositionQry_Input.in_asset_no = asset_no;
                        objFuturepositionQry_Input.in_account_code = account_code;
                        //objFuturepositionQry_Input.in_position_str = "0";

                        List<FuturepositionQry_Output> pos = Init.hsPack.Fun31003_FuturepositionQry(objFuturepositionQry_Input);
                        
                        if (pos.Count > 0)
                        {
                            if (pos[0].out_stock_code != null)
                            {
                                positionsQueues = new ConcurrentBag<FuturepositionQry_Output>(pos);
                            }

                        }
                        //positionsQueues
                        // 查询成交
                        FuturedealQry_Input objFuturedealQry_Input = new FuturedealQry_Input();
                        objFuturedealQry_Input.in_user_token = Init.logOut.out_user_token;
                        objFuturedealQry_Input.in_asset_no = asset_no;
                        objFuturedealQry_Input.in_account_code = account_code;
                        objFuturedealQry_Input.in_position_str = "0";

                        List<FuturedealQry_Output>  deals = Init.hsPack.Fun33003_FuturedealQry(objFuturedealQry_Input);

                        // 查询委托
                        FutureentrustQry_Input objFutureentrustQry_Input = new FutureentrustQry_Input();

                        objFutureentrustQry_Input.in_user_token = Init.logOut.out_user_token;
                        objFutureentrustQry_Input.in_asset_no = asset_no;
                        objFutureentrustQry_Input.in_account_code = account_code;
                        List<FutureentrustQry_Output>  orders = Init.hsPack.Fun32003_FutureentrustQry(objFutureentrustQry_Input);
                        if (orders.Count>0)
                        {
                            if(orders[0].out_stock_code != null)
                            {
                                ordersQueues = new ConcurrentBag<FutureentrustQry_Output>(orders);
                            }
                            
                        }

                        

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        
                    }
                    finally
                    {
                        Thread.Sleep(interval);
                    }

                }
            }
            await Task.Factory.StartNew(()=> LongTask(), TaskCreationOptions.LongRunning);
        }



        public static T DeepCopy<T>(T obj)
        {
            //如果是字符串或值类型则直接返回
            if (obj is string || obj.GetType().IsValueType) return obj;

            object retval = Activator.CreateInstance(obj.GetType());
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                try { field.SetValue(retval, DeepCopy(field.GetValue(obj))); }
                catch { }
            }
            return (T)retval;
        }

    }
}
