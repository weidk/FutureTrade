using FutureTrade.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutureTrade
{

    public class MQServer
    {
        public static BlockingCollection<RequestClass> requestQueues = new BlockingCollection<RequestClass>();
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string workQueueName;
        private readonly string stateQueueName;
        object _lock = new object();
        object _lock1 = new object();

        List<string> FinishedState = new List<string> { "5", "7", "8", "9", "F", "E" };

        public MQServer()
        {
            var factory = new ConnectionFactory();
            factory.UserName = Funs.CONFIG.UserName;
            factory.Password = Funs.CONFIG.Password;
            factory.HostName = Funs.CONFIG.HostName;
            factory.AutomaticRecoveryEnabled = true;
            //factory.RequestedHeartbeat = 0;

            connection = factory.CreateConnection();
            workQueueName = Funs.CONFIG.WorkQueueName;
            stateQueueName = Funs.CONFIG.OrderStateQueueName;
            //account_code = ConfigurationManager.AppSettings["account_code"];
            //asset_no = ConfigurationManager.AppSettings["asset_no"];

            channel = connection.CreateModel();
            channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }


        public void ClientRequestHandle(Initials Init)
        {
            #region 接收客户端发送来的请求

            
            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                string response = null;
                var body = ea.Body;
                var props = ea.BasicProperties;
                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(message);
                    RequestClass request  = Newtonsoft.Json.JsonConvert.DeserializeObject<RequestClass>(message);

                    if (request.businessType == "1")  //新增订单
                    {
                        SendOrder(request);
                    }
                    else // 撤销订单
                    {
                        CancldOrder(request);
                    }
                    
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(" [.] " + e.Message);
                    response = "";
                }
                finally
                {
                    AckMsg();

                }

                void AckMsg()
                {
                    try
                    {
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (System.IO.IOException ioex)
                    {
                        AckMsg();
                    }
                }
            };
            channel.BasicConsume(queue: workQueueName, autoAck: false, consumer: consumer);
            
            #endregion

            // 发送订单
            void SendOrder(RequestClass request)
            {

                #region 自成交检查


                //string ordersStr = Init.rClient.StringGet("orders");
                //var ordersQueues = Newtonsoft.Json.JsonConvert.DeserializeObject<List<RequestClass>>(ordersStr);
                //request.entrust_price = request.original_price;
                //if (ordersQueues.Count > 0)
                //{
                //    if (request.direction == "1")
                //    {
                //        string CountPartPrice = ordersQueues.Where(o => o.entrust_direction == "2" && o.code == request.code && o.orderState !="7" && o.orderState != "5" && o.orderState != "9").Min(o => o.entrust_price);
                //        if (CountPartPrice != null)
                //        {
                //            double CountPartPriceD = double.Parse(CountPartPrice);
                //            double BuyPrice = double.Parse(request.original_price);
                //            if (BuyPrice >= CountPartPriceD)
                //            {
                //                // 触发自成交，修改买入价格
                //                request.entrust_price = (CountPartPriceD - 0.005).ToString();
                //            }
                //        }
                //    }
                //    else
                //    {
                //        string CountPartPrice = ordersQueues.Where(o => o.entrust_direction == "1" && o.code == request.code && o.orderState != "7" && o.orderState != "5" && o.orderState != "9").Max(o => o.entrust_price);
                //        if(CountPartPrice != null)
                //        {
                //            double CountPartPriceD = double.Parse(CountPartPrice);
                //            double SellPrice = double.Parse(request.original_price);
                //            if (SellPrice <= CountPartPriceD)
                //            {
                //                // 触发自成交，修改卖出价格
                //                request.entrust_price = (CountPartPriceD + 0.005).ToString();
                //            }
                //        }
                //    }
                //}

                #endregion

                //string userKey = request.queue_name.Split('_')[1];
                //string userKey = request.queue_name.Split(new string[] { "FutureReply_"}, StringSplitOptions.RemoveEmptyEntries)[1];
                string userKey = request.queue_name.Replace(Funs.CONFIG.ReplyName, "");


                //if (Initials.UserDicts[userKey].token != null)
                //{
                    FutureEntrust_Input objFutrueEntrust_Input = new FutureEntrust_Input();
                    objFutrueEntrust_Input.in_user_token = Funs.CONFIG.token;
                    objFutrueEntrust_Input.in_account_code = Initials.UserDicts[userKey].account_code;
                    objFutrueEntrust_Input.in_asset_no = Initials.UserDicts[userKey].asset_no;
                    objFutrueEntrust_Input.in_combi_no = Initials.UserDicts[userKey].combi_no;
                    objFutrueEntrust_Input.in_market_no = "7";
                    objFutrueEntrust_Input.in_stock_code = request.code;
                    objFutrueEntrust_Input.in_entrust_direction = request.direction;
                    objFutrueEntrust_Input.in_futures_direction = request.futures_direction;
                    objFutrueEntrust_Input.in_price_type = request.priceType;
                    objFutrueEntrust_Input.in_entrust_price = request.entrust_price;
                    objFutrueEntrust_Input.in_entrust_amount = request.entrust_amount;

                    var objFutureEntrust_Output = Init.hsPack.Fun91004_FutrueEntrust(objFutrueEntrust_Input);
                    request.error_info = objFutureEntrust_Output.out_error_info + objFutureEntrust_Output.out_fail_cause;
                    request.entrust_no = objFutureEntrust_Output.out_entrust_no;
                    request.entrust_direction = objFutrueEntrust_Input.in_entrust_direction;
                    request.futures_direction = objFutrueEntrust_Input.in_futures_direction;
                //}
                //else
                //{
                //    request.error_info = "O32密码错误，请联系管理员更新服务端密码";
                //}
                PublishMsg(request, channel, Init);




            }

            // 撤销订单
            void CancldOrder(RequestClass request)
            {
                string userKey = request.queue_name.Replace(Funs.CONFIG.ReplyName, "");
                var requestCancle = Funs.DeepCopy<RequestClass>(request);
                //if (Initials.UserDicts[userKey].token != null)
                //{
                    FutureentrustWithdraw_Input objFutureentrustWithdraw_Input = new FutureentrustWithdraw_Input();
                    objFutureentrustWithdraw_Input.in_user_token = Funs.CONFIG.token;
                    objFutureentrustWithdraw_Input.in_entrust_no = requestCancle.entrust_no;
                    objFutureentrustWithdraw_Input.account_code = Initials.UserDicts[userKey].account_code;
                    objFutureentrustWithdraw_Input.combi_no = Initials.UserDicts[userKey].combi_no;
                    var objFutureentrustWithdraw_Output = Init.hsPack.Fun91119_FutrueentrustWithdraw(objFutureentrustWithdraw_Input);
                    requestCancle.error_info = objFutureentrustWithdraw_Output.out_error_info + objFutureentrustWithdraw_Output.out_fail_cause;
                    requestCancle.entrust_no = objFutureentrustWithdraw_Output.out_entrust_no;
                    
                //}
                //else
                //{
                //    requestCancle.error_info = "O32密码错误，请联系管理员更新服务端密码";
                //}
                PublishCancle(requestCancle, channel, Init);
            } 
        }


        public void PublishMsg(RequestClass request, IModel channel, Initials Init)
        {
            try
            {
                string responsestring = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var responseBytes = Encoding.UTF8.GetBytes(responsestring);
                channel.BasicPublish(exchange: "", routingKey: request.queue_name, basicProperties: null, body: responseBytes);
                Init.db.Insertable(request).ExecuteCommand();
                if (request.entrust_no != null && request.entrust_no != "0")
                {
                    //requestQueues.Add(request);
                    channel.BasicPublish(exchange: "", routingKey: stateQueueName, basicProperties: null, body: responseBytes);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        public void PublishCancle(RequestClass request, IModel channel, Initials Init)
        {
            try
            {
                string responsestring = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var responseBytes = Encoding.UTF8.GetBytes(responsestring);
                channel.BasicPublish(exchange: "", routingKey: request.queue_name, basicProperties: null, body: responseBytes);
                Init.db.Insertable(request).ExecuteCommand();
                if (request.entrust_no != null && request.entrust_no != "0")
                {
                    //requestQueues.Add(request);
                    channel.BasicPublish(exchange: "", routingKey: stateQueueName, basicProperties: null, body: responseBytes);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        

        public void Close()
        {
            connection.Close();
        }


        /// <summary>
        /// 心跳线程
        /// </summary>
        /// <param name="Init"></param>
        /// <returns></returns>
        public async Task HeartBeatWorker(Initials Init)
        {
            void LongTask()
            {
                while (true)
                {
                    try
                    {
                        DateTime EndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 18, 00, 00);
                        if (DateTime.Now > EndTime)
                        {
                            System.Environment.Exit(0);
                        }

                        
                        Init.hsPack.Fun10000_heartBeat(Funs.CONFIG.token);

                        //foreach (KeyValuePair<string,UserInfo> kv in Initials.UserDicts)
                        //{
                        //    if (kv.Value.token != null)
                        //    {
                        //        // 发送心跳
                        //        Init.hsPack.Fun10000_heartBeat(kv.Value.token);
                        //    }
                            
                        //}
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());

                    }
                    finally
                    {
                        Thread.Sleep(5000);
                    }

                }
            }
            await Task.Factory.StartNew(() => LongTask(), TaskCreationOptions.LongRunning);
        }




    }




}
