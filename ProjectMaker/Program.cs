using System;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

using System.Security.Cryptography;

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Text;
using System.IO;
using System.Dynamic;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using System.Net;
using System.Net.Sockets;

using System.Web;

using IPA.DN.CoreUtil.Basic;
//using IPA.DN.CoreUtil.Basic.BigInt;
using IPA.DN.CoreUtil.WebApi;

using Org.BouncyCastle;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YamlDotNet.Serialization;


using static System.Console;

using IPA.DN.CoreUtil.Helper.Basic;
using IPA.DN.CoreUtil.Helper.SlackApi;

using YamlDotNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Routing;

using ProjectMaker;

#pragma warning disable 162

namespace DotNetCoreUtilTestApp
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2 " + DateTime.Now.ToString() };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

    [Serializable]
    public class DBTestSettings
    {
        public string DBConnectStr { get; set; }
    }

    public struct STTEST
    {
        public string A;
    }

    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ProjectMaker");
            Console.WriteLine();

            Console.WriteLine("Specify the base directory name.");
            Console.Write("Directory: ");

            string dir_name = Console.ReadLine();

            vc_project_maker(dir_name);
        }

        static void weak_task_test()
        {
            SemaphoreSlim sem = new SemaphoreSlim(1, 1);
            int num = 100000;
            List<AsyncManualResetEvent> event_list = new List<AsyncManualResetEvent>();

            "init".Print();
            for (int i = 0; i < num; i++)
            {
                event_list.Add(new AsyncManualResetEvent());
            }
            System.GC.Collect();
            System.GC.WaitForFullGCApproach();
            System.GC.WaitForPendingFinalizers();
            System.GC.WaitForFullGCComplete();
            "set".Print();

            for (int i = 0; i < num; i++)
            {
                Task t = Task.Run(async () =>
                {
                    await sem.WaitAsync();
                    try
                    {
                        foreach (var e in event_list)
                        {
                            e.Set();
                        }
                    }
                    finally
                    {
                        sem.Release();
                    }
                });
            }

            "wait".Print();

            foreach (var e in event_list)
            {
                Task.Run(async () =>
                {
                    await e.WaitAsync();
                }).Wait();
            }

            "done".Print();
        }

        static void weak_task_test__()
        {
            int num = 100000;
            List<TaskCompletionSource<bool>> tcs_list = new List<TaskCompletionSource<bool>>();
            List<Task> task_list = new List<Task>();
            "init".Print();
            for (int i = 0; i < num; i++)
            {
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                tcs_list.Add(tcs);
                //task_list.Add(tcs.Task);
                //task_list.Add(TaskUtil.CreateWeakTaskFromTask(tcs.Task));
            }
            System.GC.Collect();
            System.GC.WaitForFullGCApproach();
            System.GC.WaitForPendingFinalizers();
            System.GC.WaitForFullGCComplete();
            "set".Print();

            Task t = Task.Run(() =>
            {
                Dbg.Where();
                foreach (var tcs in tcs_list)
                {
                    tcs.TrySetResult(true);
                }
                Dbg.Where();
            });

            t.Wait();
            "wait".Print();

            foreach (var tcs in tcs_list)
            {
                //TaskUtil.CreateWeakTaskFromTask(tcs.Task).Wait();
            }

            //foreach (var task in task_list)
            //{
            //    task.Wait();
            //}

            "done".Print();
        }

        static void http_client_test2()
        {
            Benchmark b = new Benchmark();

            WebApi a = new WebApi();

            {
                ThreadObj.StartMany(100, param =>
                {
                    //using (WebApi a = new WebApi())
                    {
                        while (true)
                        {
                            try
                            {
                                WebRet ret = a.RequestWithQuery(WebApiMethods.GET, "http://mail.coe.ad.jp/").Result;
                                b.IncrementMe++;
                                //ret.ToString().Print();
                            }
                            catch (Exception e)
                            {
                                Con.WriteLine(e.ToString());
                            }
                            //                    Kernel.SleepThread(1000);
                        }
                    }
                });
            }
        }

        static void http_client_test()
        {
            using (WebApi a = new WebApi())
            {
                while (true)
                {
                    try
                    {
                        //a.SslAcceptCertSHA1HashList.Add("b0416f96fe4ed8ac7dddee5316c92ee12a3f745a");
                        a.SslAcceptCertSHA1HashList.Add("b0416f96fe4ed8ac7ddd0e5316c92ee12a3f745a");
                        WebRet ret = a.RequestWithQuery(WebApiMethods.GET, "https://api.vpngate.net/").Result;
                        ret.ToString().Print();
                    }
                    catch (Exception e)
                    {
                        Con.WriteLine(e.ToString());
                    }
                    Kernel.SleepThread(1000);
                }
            }
        }

        static void auto_reset_event_test()
        {
            int num = 10000;
            List<AsyncAutoResetEvent> events = new List<AsyncAutoResetEvent>();
            for (int i = 0; i < num; i++)
            {
                AsyncAutoResetEvent ae = new AsyncAutoResetEvent();
                events.Add(ae);
            }

            object LockTest = new object();

            ThreadObj.StartMany(100, param =>
            {
                while (true)
                {
                    Dbg.Where();
                    //lock (LockTest)
                    {
                        foreach (AsyncAutoResetEvent e in events)
                        {
                            e.WaitOneAsync().Wait();
                        }
                    }
                }
            });

            while (true)
            {
                //lock (LockTest)
                {
                    Dbg.Where();
                    foreach (AsyncAutoResetEvent e in events)
                    {
                        e.Set();
                    }
                }
                Kernel.SleepThread(1000);
            }
        }

        static void event_gc_test()
        {
            Benchmark b = new Benchmark("event_gc_test");

            List<AsyncManualResetEvent> events2 = new List<AsyncManualResetEvent>();

            while (true)
            {
                //b.IncrementMe++;

                int num = 10;
                List<AsyncManualResetEvent> events = new List<AsyncManualResetEvent>();
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < num; i++)
                {
                    AsyncManualResetEvent ae = new AsyncManualResetEvent();
                    Task t = ae.WaitAsync();

                    events.Add(ae);
                    events2.Add(ae);

                    tasks.Add(t);
                }

                foreach (AsyncManualResetEvent e in events)
                {
                    e.Set();
                }

                foreach (Task t in tasks)
                {
                    t.Wait();
                }

                events = null;
                tasks = null;

                int num_a = 0, num_total = 0;
                foreach (AsyncManualResetEvent ae in events2)
                {
                    /*if (ae.IsAbandoned)
                    {
                        num_a++;
                    }*/
                    num_total++;
                }
                Con.WriteLine($"{num_a} {num_total}");

                //GC.Collect();
            }
        }

        static void event_test()
        {
            AsyncManualResetEvent ae = new AsyncManualResetEvent();

            ThreadObj t = new ThreadObj(param =>
            {
                while (true)
                {
                    Kernel.SleepThread(1000);
                    ae.Set();
                }
            });

            IntervalDebug id = new IntervalDebug();
            while (true)
            {
                id.Start();
                ae.WaitAsync().Wait();
                id.PrintElapsed();
                ae.Reset();

                GC.Collect();
            }
        }

        static void sleep_task_test2()
        {
            Task t = TaskUtil.PreciseDelay(1000);

            Dbg.Where();
            t.Wait();
            Dbg.Where();

            Kernel.SleepThread(-1);
        }

        static void sleep_task_gc_test()
        {
            Benchmark b = new Benchmark("num_newtask");
            while (true)
            {
                List<Task> o = new List<Task>();
                for (int i = 0; i < 100000; i++)
                {
                    b.IncrementMe++;
                    Task t = TaskUtil.PreciseDelay(10000);
                    //o.Add(t);
                }
                //t.Wait();
                //Dbg.Where();
                //Task.Delay(1000);
                //Util.AddToBlackhole(t);
                Dbg.Where();
                foreach (Task t in o)
                {
                    t.Wait();
                }
                Dbg.Where();
                GC.Collect();
            }
            while (true)
            {
                b.IncrementMe++;
                Task t = TaskUtil.PreciseDelay(1000);
                //t.Wait();
                //Dbg.Where();
                //Task.Delay(1000);
                Util.AddToBlackhole(t);
            }
        }

        static void sleep_test()
        {
            Dbg.SetDebugMode(false);

            Ref<int> interval = new Ref<int>(100);

            new ThreadObj(param =>
            {
                while (true)
                {
                    long tick = Time.Tick64;

                    //Task.Delay(interval.Value).Wait();
                    //Thread.Sleep(50);
                    //new genstr_params();
                    //Task.Delay(1000);
                    TaskUtil.PreciseDelay(interval.Value).Wait();
                    //b.IncrementMe++;

                    long tick2 = Time.Tick64;

                    long diff = tick2 - tick;
                    Con.WriteLine(diff);
                    //GlobalIntervalReporter.Singleton.Report("diff", diff);
                }
            });

            while (true)
            {
                string line = Con.ReadLine("interval(msec)>");
                int msec = line.ToInt();
                interval.Set(msec);
            }

        }

        public class genstr_params
        {
            public string apiKey;
            public int n, length;
            public string characters;
        }

        public class genstr_response
        {
            public class random_t
            {
                public string[] data { get; set; }
                public DateTime completionTime { get; set; }
            }

            public random_t random { get; set; }
            public int bitsUsed { get; set; }
            public int bitsLeft { get; set; }
            public int requestsLeft { get; set; }
            public int advisoryDelay { get; set; }
        }

        public class rpc_t
        {
            public string Str1;
            public int Int1;
        }

        [RpcInterface]
        public interface rpc_server_api_interface_test
        {
            Task<rpc_t> Test1(rpc_t a);
            Task Test2(rpc_t a);
            Task<string> Test3(int a, int b, int c);
            Task<int> Divide(int a, int b);
            Task<rpc_t> Test5(int a, string b);
            Task<string[]> Test6();
            Task<string> Test7(string[] p);
        }

#pragma warning disable CS1998
        public class rpc_server_api_test : JsonRpcServerApi, rpc_server_api_interface_test
        {
            public async Task<rpc_t> Test1(rpc_t a)
            {
                Dbg.WhereThread();
                //await TaskUtil.PreciseDelay(500);
                Dbg.WhereThread();
                return new rpc_t()
                {
                    Int1 = a.Int1,
                    Str1 = a.Str1,
                };
            }

            public async Task Test2(rpc_t a)
            {
                Dbg.WhereThread();
                //await TaskUtil.PreciseDelay(500);
                Dbg.WhereThread();
                return;
            }

            public async Task<string> Test3(int a, int b, int c)
            {
                //await TaskUtil.PreciseDelay(500);
                return Str.CombineStringArray2(",", a, b, c);
            }

            //static Benchmark bm = new Benchmark();
            public async Task<int> Divide(int a, int b)
            {
                //this.ClientInfo.ToString().Print();
                //bm.IncrementMe++;
                return a / b;
            }
            public async Task<rpc_t> Test5(int a, string b)
            {
                return new rpc_t()
                {
                    Int1 = a,
                    Str1 = b,
                };
            }

            public async Task<string[]> Test6()
            {
                List<string> ret = new List<string>();
                foreach (var d in IO.EnumDirEx(Env.AppRootDir))
                {
                    ret.Add(d.FullPath);
                }
                return ret.ToArray();
            }

            public async Task<string> Test7(string[] p)
            {
                return Str.CombineStringArray(p, ",");
            }

            public override object StartCall(JsonRpcClientInfo client_info)
            {
                return null;
            }

            public override async Task<object> StartCallAsync(JsonRpcClientInfo client_info, object param)
            {
                return null;
            }

            public override void FinishCall(object param)
            {
                Util.DoNothing();
            }

            public override Task FinishCallAsync(object param)
            {
                return Task.CompletedTask;
            }
        }
#pragma warning restore CS1998

        public class rpctmp1
        {
            public rpc_t a;
        }

        public static async Task jsonrpc_server_invoke_test()
        {
            rpc_server_api_test m = new rpc_server_api_test();

            Type tt = m.RpcInterface;

            rpc_t t = new rpc_t()
            {
                Int1 = 1,
                Str1 = "a",
            };
            rpctmp1 t2 = new rpctmp1()
            {
                a = t,
            };

            Dbg.WhereThread();
            for (int i = 0; i < 100; i++)
            {
                object r = await m.InvokeMethod("Test2", (JObject)(t2.ObjectToJson().JsonToDynamic()));
                Util.DoNothing();
            }
            Dbg.WhereThread();

            //r.ObjectToJson().Print();

            Util.DoNothing();
        }

        public class TMP1
        {
            public int a, b;
        }

        public static void jsonrpc_benchmark_test()
        {
            //jspnrpc_benchmark_inmemory_server();return;

            Con.WriteLine("Empty to start a server.");
            Con.WriteLine("IP to start a client.");
            string s = Con.ReadLine(">");

            IntervalReporter.StartThreadPoolStatReporter();

            if (s.IsEmpty())
            {
                jsonrpc_benchmark_server();
            }
            else
            {
                jsonrpc_benchmark_client(s.Trim());
            }
        }


        static int json_test_value = 1;
        public static void jsonrpc_benchmark_client(string ip)
        {
            bool is_simple_mode = false;
            if (ip.StartsWith("@"))
            {
                ip = ip.Substring(1);
                is_simple_mode = true;
            }

            Benchmark b = new Benchmark("testcall");
            Benchmark b2 = new Benchmark("error");

            RefInt max_conn = new RefInt(int.MaxValue);
            RefInt current_conn = new RefInt(0);

            JsonRpcHttpClient<rpc_server_api_interface_test> c = new JsonRpcHttpClient<rpc_server_api_interface_test>($"http://{ip}:80/rpc");
            ThreadObj.StartMany(256, par =>
            {

                if (is_simple_mode)
                {
                    Con.WriteLine("Simple mode: " + ip);
                    WebApi api = new WebApi();
                    while (true)
                    {
                        while (current_conn.Value >= max_conn.Value)
                        {
                            Kernel.SleepThread(Secure.Rand31i() % 1000);
                        }

                        Interlocked.Increment(ref current_conn.Value);
                        try
                        {
                            try
                            {
                                WebRet ret = api.RequestWithQuery(WebApiMethods.GET, $"http://{ip}:80/rpc").Result;
                                //ret.ToString().Print();
                                b.IncrementMe++;
                            }
                            catch
                            {
                                b2.IncrementMe++;
                                Kernel.SleepThread(Secure.Rand31i() % 4000);
                            }
                        }
                        finally
                        {
                            Interlocked.Decrement(ref current_conn.Value);
                        }
                    }
                }
                else
                {
                    Con.WriteLine("JSON-RPC mode: " + ip);
                    while (true)
                    {
                        /*while (current_conn.Value >= max_conn.Value)
                        {
                            Kernel.SleepThread(Secure.Rand31i() % 1000);
                        }*/

                        Interlocked.Increment(ref current_conn.Value);

                        try
                        {
                            TMP1 a = new TMP1() { a = json_test_value++, b = 2 };
                            //if ((json_test_value % 1000) == 0) System.GC.Collect();
                            try
                            {
                                if (false)
                                {
                                    lock (c)
                                    {
                                        c.ST_CallOne<object>("Divide", a, true).Wait();
                                    }
                                    b.IncrementMe++;
                                }
                                else
                                {
                                    //object res = c.Call<object>("Divide", a, true).Result.Result;
                                    int res_int = c.Call.Divide(a.a, a.b).Result;

                                    if (res_int != (a.a / 2))
                                    {
                                        Kernel.SelfKill("(res != (value / 2))");
                                    }
                                    //Thread.Sleep(Secure.Rand31i() % 40);
                                    b.IncrementMe++;
                                }
                            }
                            catch (Exception ex)
                            {
                                ("ERR: " + ex.ToString()).Print();
                                b2.IncrementMe++;
                                Kernel.SleepThread(Secure.Rand31i() % 4000);
                            }
                        }
                        finally
                        {
                            Interlocked.Decrement(ref current_conn.Value);
                            //System.GC.Collect();
                        }
                    }
                }
            }
            );

            while (true)
            {
                string line = Con.ReadLine();
                int n = line.ToInt();
                if (n >= 1)
                {
                    max_conn.Set(n);
                }
            }
        }

        public static void jspnrpc_benchmark_inmemory_server()
        {
            rpc_server_api_test h = new rpc_server_api_test();
            Benchmark b = new Benchmark("call");

            TMP1 arg = new TMP1()
            {
                a = 8,
                b = 2,
            };
            string json_str = arg.ObjectToJson();

            var info = h.GetMethodInfo("Divide");

            while (true)
            {

                JObject jobj = json_str.JsonToDynamic();
                //h.InvokeMethod("Divide", jobj, null).Wait();
                object o = info.InvokeMethod(h, "Divide", jobj).Result;
                //Task t = (Task)info.Method.Invoke(h, new object[] { 8, 2 });
                //t.Wait();

                o.ObjectToJson();

                b.IncrementMe++;
            }
        }

        public static void jsonrpc_benchmark_server()
        {
            HttpServerBuilderConfig http_cfg = new HttpServerBuilderConfig()
            {
                DebugToConsole = false,
                HttpPortsList = new List<int>(new int[] { 80 }),
            };
            JsonRpcServerConfig rpc_cfg = new JsonRpcServerConfig()
            {
            };
            rpc_server_api_test h = new rpc_server_api_test();
            var s = JsonHttpRpcListener.StartServer(http_cfg, rpc_cfg, h);

            Kernel.SuspendForDebug();
        }

        public static void jsonrpc_client_server_both_test()
        {
            //jsonrpc_server_invoke_test().Wait();return;

            // start server
            HttpServerBuilderConfig http_cfg = new HttpServerBuilderConfig()
            {
                DebugToConsole = false,
            };
            JsonRpcServerConfig rpc_cfg = new JsonRpcServerConfig()
            {
            };
            rpc_server_api_test h = new rpc_server_api_test();
            var s = JsonHttpRpcListener.StartServer(http_cfg, rpc_cfg, h);

            Ref<bool> client_stop_flag = new Ref<bool>();

            // start client
            ThreadObj client_thread = ThreadObj.Start(param =>
            {
                //Kernel.SleepThread(-1);
                if (false)
                {
                    Benchmark b = new Benchmark("testcall");

                    ThreadObj.StartMany(256, par =>
                    {

                        WebApi a = new WebApi();

                        while (true)
                        {
                            WebRet ret = a.RequestWithQuery(WebApiMethods.GET, "http://127.0.0.1:88/rpc").Result;
                            //ret.ToString().Print();
                            b.IncrementMe++;
                        }
                    }
                    );

                    Kernel.SuspendForDebug();
                }

                //using ()
                {
                    //c.AddHeader("X-1", "Hello");

                    rpctmp1 t = new rpctmp1();
                    t.a = new rpc_t()
                    {
                        Int1 = 2,
                        Str1 = "Neko",
                    };

                    //JsonRpcResponse<object> ret = c.CallOne<object>("Test1", t).Result;
                    //JsonRpcResponse<object> ret = c.CallOne<object>("Test2", t).Result;

                    Benchmark b = new Benchmark("rpccall");

                    JsonRpcHttpClient<rpc_server_api_interface_test> c = new JsonRpcHttpClient<rpc_server_api_interface_test>("http://127.0.0.1:88/rpc");
                    var threads = ThreadObj.StartMany(256, par =>
                    {

                        while (client_stop_flag.Value == false)
                        {
                            //c.Call.Divide(8, 2).Wait();
                            TMP1 a = new TMP1() { a = 2, b = 1 };
                            c.MT_Call<object>("Divide", a, true).Wait();
                            //c.ST_CallOne<object>("Divide", a, true).Wait();
                            b.IncrementMe++;
                        }
                    }
                    );

                    foreach (var thread in threads)
                    {
                        thread.WaitForEnd();
                    }

                    //c.Call.Divide(8, 2).Result.Print();
                    //c.Call.Divide(8, 2).Result.Print();
                    //c.Call.Test3(1, 2, 3).Result.Print();
                    //c.Call.Test5(1, "2").Result.ObjectToJson().Print();
                    //var fnlist = c.Call.Test6().Result;
                    ////foreach (var fn in fnlist) fn.Print();
                    //c.Call.Test7(fnlist).Result.Print();

                    //Con.WriteLine(ret.ObjectToJson());
                }
            }, null);

            Con.ReadLine("Enter>");

            client_stop_flag.Set(true);

            client_thread.WaitForEnd();

            s.StopAsync().Wait();


        }

        public static void jsonrpc_http_server_test()
        {
            /*rpc_handler_test x = new rpc_handler_test();
            object o = x.InvokeMethod("Test", 3).Result;
            string r = (string)o;
            r.Print();
            return;*/

            HttpServerBuilderConfig http_cfg = new HttpServerBuilderConfig()
            {
            };
            JsonRpcServerConfig rpc_cfg = new JsonRpcServerConfig()
            {
            };
            rpc_server_api_test h = new rpc_server_api_test();
            var s = JsonHttpRpcListener.StartServer(http_cfg, rpc_cfg, h);

            Con.ReadLine("Enter>");

            s.StopAsync().Wait();
        }

        public static void jsonrpc_test_with_random_api()
        {
            string key = "193ede53-7bd8-44b1-9662-40bd17ff0e67";
            string url = "https://api.random.org/json-rpc/1/invoke";

            JsonRpcHttpClient c = new JsonRpcHttpClient(url);

            genstr_params p = new genstr_params()
            {
                apiKey = key,
                n = 8,
                length = 8,
                characters = "0123456789",
            };

            /*
            var res = c.CallAdd<genstr_response>("generateStrings", p);
            var res2 = c.CallAdd<genstr_response>("generateStrings", p);
            c.CallAll(false).Wait();

            Con.WriteLine(res.ToString());
            Con.WriteLine(res2.ToString());*/

            var res3 = c.ST_CallOne<genstr_response>("generateStrings", p, true).Result;
            Con.WriteLine(res3.ToString());
        }

        public static void json_test()
        {
            /*List<DBTestSettings> o = new List<DBTestSettings>();
            o.Add(new DBTestSettings() { DBConnectStr = "Hello" });
            o.Add(new DBTestSettings() { DBConnectStr = "Neko" });
            o.Add(new DBTestSettings() { DBConnectStr = "Cat" });
            o.Add(new DBTestSettings() { DBConnectStr = "Dog\ncat\"z" });
            o.Add(null);
            string json = Json.SerializeLog(o.ToArray());
            json.Print();

            StringReader r = new StringReader(json.ReplaceStr("}",""));
            Json.DeserializeLargeArrayAsync<DBTestSettings>(r, item => { item.ObjectToJson().Print(); return true; }, (str, exc) => { exc.ToString().Print(); return true; }).Wait();*/

            DBTestSettings db1 = new DBTestSettings();
            DBTestSettings db2 = new DBTestSettings();
            DBTestSettings db3 = new DBTestSettings();

            DBTestSettings[] dbs = new DBTestSettings[] { db1, db2, db3, };
            List<DBTestSettings> o = new List<DBTestSettings>(dbs);

            o.ObjectToJson(true).Print();
        }

        public static void db_test()
        {
            Cfg<DBTestSettings> cfg = new Cfg<DBTestSettings>();

            Database db = new Database(cfg.ConfigSafe.DBConnectStr);

            db.Tran(() =>
            {
                db.Query("select * from test");

                Data d = db.ReadAllData();

                Json.Serialize(d).Print();

                return true;
            });
        }

        [Serializable]
        public class SlackTestSecretSettings
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string AccessToken { get; set; }
        }

        public static void slack_test()
        {
            Cfg<SlackTestSecretSettings> secret = new Cfg<SlackTestSecretSettings>();
            {
                SlackApi a = new SlackApi(secret.ConfigSafe.ClientId, secret.ConfigSafe.AccessToken);

                a.DebugPrintResponse = true;

                a.AuthGenerateAuthorizeUrl("client identify", "https://tools.sehosts.com/", "abc").Print();

                //var at = a.AuthGetAccessToken(secret.ConfigSafe.ClientSecret, "47656437648.414467157330.e53934f3cd8a1d28c64b2e17b2f97422f609bf702fd6eb5267765e7ddfbd7011", "https://tools.sehosts.com/");
                //at.InnerDebug();

                var cl = a.GetChannelsListAsync().Result;
                string channel_id = "";
                foreach (var c in cl.Channels)
                {
                    if (c.name.IsSamei("test"))
                    {
                        channel_id = c.id;
                        Con.WriteLine(c.created.ToDateTimeOfSlack().ToLocalTime().ToDtStr());
                    }
                }

                a.PostMessageAsync(channel_id, $"こんにちは！ \t{Time.NowDateTimeLocal.ToDtStr(true, DtstrOption.All, true)}", true).Wait();
            }
        }

        public class TwConfig
        {
        }

        public static void twitter_test()
        {
        }

        class vcp_replace_str_list
        {
            public string __PROJ_GUID__;
            public string __APPNAME__;
            public StringWriter __INCLUDE_FILE_LIST__ = new StringWriter();
            public StringWriter __COMPILE_FILE_LIST__ = new StringWriter();
            public StringWriter __NONE_FILE_LIST__ = new StringWriter();

            public StringWriter __FILTER_LIST__ = new StringWriter();
            public StringWriter __INCLUDE_LIST__ = new StringWriter();
            public StringWriter __COMPILE_LIST__ = new StringWriter();
            public StringWriter __NONE_LIST__ = new StringWriter();
        }

        public static void vc_project_maker(string base_dir)
        {
            // scan files
            var files = IO.EnumDirWithCancel(base_dir);

            SortedSet<string> dir_list = new SortedSet<string>();

            List<DirEntry> include_list = new List<DirEntry>();
            List<DirEntry> compile_list = new List<DirEntry>();
            List<DirEntry> none_list = new List<DirEntry>();

            foreach (var file in files)
            {
                if (file.IsFolder == false)
                {
                    string relative_dir = file.RelativePath.GetDirectoryName();
                    if (relative_dir.IsFilled())
                    {
                        dir_list.Add(relative_dir);

                        if (file.FileName.IsExtensionMatch(".c .cpp .s .asm"))
                        {
                            compile_list.Add(file);
                        }
                        else if (file.FileName.IsExtensionMatch(".h"))
                        {
                            include_list.Add(file);
                        }
                        else
                        {
                            none_list.Add(file);
                        }
                    }
                }
            }

            vcp_replace_str_list r = new vcp_replace_str_list()
            {
                __PROJ_GUID__ = Str.NewGuid(true),
                __APPNAME__ = base_dir.RemoteLastEnMark().GetFileName(),
            };

            foreach (var e in include_list)
            {
                r.__INCLUDE_FILE_LIST__.WriteLine($"    <ClInclude Include=\"{e.RelativePath}\" />");

                r.__INCLUDE_LIST__.WriteLine($"    <ClInclude Include=\"{e.RelativePath}\">");
                r.__INCLUDE_LIST__.WriteLine($"      <Filter>{e.RelativePath.GetDirectoryName()}</Filter>");
                r.__INCLUDE_LIST__.WriteLine($"    </ClInclude>");
            }

            foreach (var e in compile_list)
            {
                r.__COMPILE_FILE_LIST__.WriteLine($"    <ClCompile Include=\"{e.RelativePath}\" />");

                r.__COMPILE_LIST__.WriteLine($"    <ClCompile Include=\"{e.RelativePath}\">");
                r.__COMPILE_LIST__.WriteLine($"      <Filter>{e.RelativePath.GetDirectoryName()}</Filter>");
                r.__COMPILE_LIST__.WriteLine($"    </ClCompile>");
            }

            foreach (var e in none_list)
            {
                r.__NONE_FILE_LIST__.WriteLine($"    <None Include=\"{e.RelativePath}\" />");

                r.__NONE_LIST__.WriteLine($"    <None Include=\"{e.RelativePath}\">");
                r.__NONE_LIST__.WriteLine($"      <Filter>{e.RelativePath.GetDirectoryName()}</Filter>");
                r.__NONE_LIST__.WriteLine($"    </None>");
            }

            foreach (var dir in dir_list)
            {
                r.__FILTER_LIST__.WriteLine($"    <Filter Include=\"{dir}\">");
                r.__FILTER_LIST__.WriteLine($"      <UniqueIdentifier>{Str.NewGuid(true)}</UniqueIdentifier>");
                r.__FILTER_LIST__.WriteLine("    </Filter>");
            }

            string vcxproj = AppRes.vcxproj.ReplaceStrWithReplaceClass(r);
            string filters = AppRes.vcxfilter.ReplaceStrWithReplaceClass(r);

            IO.WriteAllTextWithEncoding(base_dir.CombinePath($"{r.__APPNAME__}.vcxproj"), vcxproj, Str.Utf8Encoding, false);
            IO.WriteAllTextWithEncoding(base_dir.CombinePath($"{r.__APPNAME__}.vcxproj.filters"), filters, Str.Utf8Encoding, false);
        }

        static void linux_c_h_add_autoconf_test()
        {
            var files = IO.EnumDirWithCancel(@"C:\git\DN-LinuxKernel-Learn\linux-2.6.39", "*.c *.h");
            foreach (var file in files)
            {
                if (file.IsFolder == false)
                {
                    if (file.FileName.IsSamei("autoconf.h") == false)
                    {
                        Encoding enc;

                        string tag = "#include \"linux/generated/autoconf.h\"\n";

                        try
                        {
                            string txt = IO.ReadAllTextWithAutoGetEncoding(file.FullPath, out enc, out _);
                            if (txt.StartsWith(tag) == false)
                            {
                                txt = tag + txt;
                                txt.WriteTextFile(file.FullPath, enc);
                                //file.FullPath.Print();
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.ToString().Print();
                        }
                    }
                }
            }
        }

        static void linux_kernel_conf_test()
        {
            var enable_config_list = IO.ReadAllTextWithAutoGetEncoding(@"c:\tmp\test.txt").GetLines().ToList(true, true, false);

            string current_config = IO.ReadAllTextWithAutoGetEncoding(@"c:\tmp\current_config.txt").NormalizeCrlfUnix();

            foreach (string s in enable_config_list)
            {
                string old_str1 = $"\n{s}=m\n";
                string old_str2 = $"\n# {s} is not set\n";
                string new_str = $"\n{s}=y\n";

                current_config = current_config.ReplaceStr(old_str1, new_str, true);
                current_config = current_config.ReplaceStr(old_str2, new_str, true);
            }

            current_config.WriteTextFile(@"c:\tmp\new_config.txt");
        }

        static void process_test()
        {
            ChildProcess p = new ChildProcess(" / bin/bash", "", "#!/bin/bash\r\necho aaa > aaa.txt\r\necho bbb\ndate\n\r\n\r\n".NormalizeCrlfThisPlatform(), true, 1000);

            //ChildProcess p = new ChildProcess(@"C:\git\dn-rlogin\rlogin_src\openssl-1.1.0h-x32\apps\openssl.exe", "", "version\n\n", true, 1000);

            WriteLine(p.StdOut);
            WriteLine(p.StdErr);

            p.InnerPrint();
        }

        static void time_test()
        {
            while (true)
            {
                WriteLine(Time.NowLong100Usecs);
                ThreadObj.Sleep(5);
            }
        }

        static void fullroute_test()
        {
            FullRouteSetThread t = new FullRouteSetThread(true);

            t.WaitForReady(ThreadObj.Infinite);

            while (true)
            {
                string ip = Con.ReadLine("IP>");

                if (Str.StrCmpi(ip, "exit"))
                {
                    break;
                }

                FullRouteSetResult ret = t.FullRouteSet.Lookup(ip);

                if (ret == null)
                {
                    Con.WriteLine("Not found.");
                }
                else
                {
                    Con.WriteLine("IP: {0}\nIPNet: {1}/{2}\nAS: {3} ({4})\nAS_PATH: {5}\nCountry: {6} ({7})",
                        ret.IPAddress, ret.IPRouteNetwork,
                        ret.IPRouteSubnetLength, ret.ASNumber, ret.ASName,
                        ret.AS_PathString, ret.CountryCode2, ret.CountryName);
                }
            }

            t.Stop();

            return;
        }

        static void rsa_test()
        {
            string hello = "Hello World";
            byte[] hello_data = hello.GetBytes();
            WriteLine("src: " + hello_data.GetHexString());

            Rsa rsa_private = new Rsa("@test1024.key");
            byte[] signed = rsa_private.SignData(hello_data);
            WriteLine("signed: " + signed.GetHexString());

            Cert cert = new Cert("@test1024.cer");
            Rsa rsa_public = new Rsa(cert);
            WriteLine("verify: " + rsa_public.VerifyData(hello_data, signed));

            byte[] encryped = rsa_public.Encrypt(hello_data);
            //encryped = "1C813B8396104AB1436C9AE208D5FC1A12CA15955A773F49F246F80FEDF13F914DF792A991B245601E13CFEE7B53B9117B35E54ACE465140D853F1901A0E8E33D603B65C6ECF0E6AB390AF7CB404D325EAF1669BD5C4F68FBE52888F44FE0CD596EF7BEEB44133A77D847FF177545D8678D6D0EFC6E4F1DB86CC48FE263C481E".GetHexBytes();
            WriteLine("encrypted: " + encryped.GetHexString());

            byte[] decrypted = rsa_private.Decrypt(encryped);
            WriteLine("decrypted: " + decrypted.GetHexString());

            WriteLine("cert_hash: " + cert.Hash.GetHexString());
        }


        static void rsa_test2()
        {
            string hello = "Hello World";
            byte[] hello_data = hello.GetBytes();
            WriteLine(hello_data.GetHexString());

            PemReader private_pem = new PemReader(new StringReader(Str.ReadTextFile("@testcert.key")));
            AsymmetricKeyParameter private_key = (AsymmetricKeyParameter)private_pem.ReadObject();

            PemReader cert_pem = new PemReader(new StringReader(Str.ReadTextFile("@testcert.cer")));
            X509Certificate cert = (X509Certificate)cert_pem.ReadObject();
            AsymmetricKeyParameter public_key = cert.GetPublicKey();

            IAsymmetricBlockCipher cipher = new Pkcs1Encoding(new RsaEngine());
            cipher.Init(true, public_key);

            byte[] encryped = cipher.ProcessBlock(hello_data, 0, hello_data.Length);

            WriteLine(encryped.GetHexString());

            cipher = new Pkcs1Encoding(new RsaEngine());
            cipher.Init(false, private_key);

            byte[] decryped = cipher.ProcessBlock(encryped, 0, encryped.Length);
            WriteLine(decryped.GetHexString());

            ISigner signer = SignerUtilities.GetSigner("SHA1withRSA");
            signer.Init(true, private_key);
            byte[] signed = signer.GenerateSignature();
            WriteLine(signed.GetHexString());

            signer = SignerUtilities.GetSigner("SHA1withRSA");
            signer.Init(false, public_key);
            WriteLine(signer.VerifySignature(signed));
        }

        static List<Sock> sock_test3_socket_list = new List<Sock>();
        static SockEvent sock_test3_event = new SockEvent();

        static void sock_test3_loop_thread(object param)
        {
            while (true)
            {
                sock_test3_event.Wait(1000);

                Sock[] socks = null;
                lock (sock_test3_socket_list)
                {
                    socks = sock_test3_socket_list.ToArray();
                }

                foreach (Sock s in socks)
                {
                    byte[] data = s.Recv(65536);
                    if (data == null)
                    {
                        WriteLine($"Client {s.RemoteIP}:{s.RemotePort} disconnected.");
                        lock (sock_test3_socket_list)
                        {
                            sock_test3_socket_list.Remove(s);
                        }
                        s.Disconnect();
                    }
                    else if (data.Length == 0)
                    {
                        // later
                    }
                    else
                    {
                        WriteLine($"Client {s.RemoteIP}:{s.RemotePort}: recv {data.Length} bytes.");
                    }
                }
            }
        }

        static void sock_test3()
        {
            int port = 80;

            ThreadObj loop_thread = new ThreadObj(sock_test3_loop_thread);

            Sock a = Sock.Listen(port);

            Console.WriteLine($"Listening {a.LocalPort} ...");

            while (true)
            {
                Sock s = a.Accept();

                WriteLine($"Connected from {s.LocalIP}");

                sock_test3_event.JoinSock(s);

                lock (sock_test3_socket_list)
                {
                    sock_test3_socket_list.Add(s);
                }

                sock_test3_event.Set();
            }
        }

        static void sock_test4_accept_proc(Listener listener, Sock sock, object param)
        {
            Sock s = sock;
            WriteLine($"Connected from {s.LocalIP}");

            s.SetTimeout(3000);

            s.SendAll("Hello\n".GetBytes_Ascii());

            byte[] recv = s.Recv(4096);
            if (recv == null)
            {
                Console.WriteLine("Disconnected.");
            }
            else
            {
                Console.WriteLine($"recv size = {recv.Length}");
            }

            s.Disconnect();
        }

        static void sock_test4()
        {
            Listener x = new Listener(80, sock_test4_accept_proc, null);
            WriteLine($"Listening {x.Port}");

            ReadLine();

            WriteLine("Stop listening...");
            x.Stop();
            WriteLine("Stopped.");
            ReadLine();
        }

        static void sock_test2()
        {
            string hostname = "www.tsukuba.ac.jp";
            WriteLine("Connecting...");
            IPAddress ip = Domain.GetIP(hostname)[0];
            IPEndPoint endPoint = new IPEndPoint(ip, 80);
            Socket s = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s.LingerState = new LingerOption(false, 0);
            s.Connect(endPoint);
            string send_str = $"GET / HTTP/1.1\r\nHOST: {hostname}\r\n\r\n";

            s.Send(send_str.GetBytes_UTF8());
            WriteLine("Sent.");

            byte[] tmp = new byte[65536];

            int ret = s.Receive(tmp);
            Con.WriteLine($"ret = {ret}");
            s.Disconnect(false);
        }

        static void sock_test()
        {
            //Con.WriteLine("Enter key!");
            //ReadLine();
            string hostname = "www.softether.com";
            WriteLine("Connecting...");
            Sock s = Sock.Connect(hostname, 80);
            WriteLine("Connected.");

            string send_str = $"GET / HTTP/1.1\r\nHOST: {hostname}\r\n\r\n";

            if (s.SendAll(send_str.GetBytes()) == false)
            {
                throw new ApplicationException("Disconnected");
            }
            //s.Socket.Send(send_str.GetBytes());
            WriteLine("Sent.");

            byte[] recv_data = s.Recv(65536 * 100);
            if (recv_data == null)
            {
                throw new ApplicationException("Disconnected");
            }

            WriteLine($"recv_data.length = {recv_data.Length}");

            WriteLine(recv_data.GetString());
            /*
            byte[] tmp = new byte[65536];
            int ret = s.Socket.Receive(tmp);
            Con.WriteLine($"ret = {ret}");*/

            s.Disconnect();
        }

        static void dns_test()
        {
            IPAddress[] list = Domain.GetIP46("www.google.com");

            foreach (IPAddress a in list)
            {
                Con.WriteLine(a.ToString());
            }

            foreach (string hostname in Domain.GetHostName(Domain.StrToIP("130.158.6.51")))
            {
                Con.WriteLine(hostname);
            }
        }

        static void mail_test()
        {
            SendMail sm = new SendMail("10.32.0.14");
            sm.Send("Test <noreply@icscoe.jp>", "Ahosan <da.ahosan1@softether.co.jp>", "こんにちは2", "これはテストです2");
        }

        static void httpclient_test()
        {
            DnHttpClient c = new DnHttpClient();
            Buf b = c.Get(new Uri("https://www.vpngate.net/ja/"));
            WriteLine(Str.Utf8Encoding.GetString(b.ByteData));
        }

        static void ipinfo_test()
        {
            while (true)
            {
                Console.WriteLine();
                Console.Write("IP>");
                string line = Console.ReadLine();
                if (Str.IsEmptyStr(line) == false)
                {
                    if (Str.StrCmp(line, "exit"))
                    {
                        break;
                    }

                    try
                    {
                        FullRouteIPInfoEntry e = FullRouteIPInfo.Search(line);

                        if (e == null)
                        {
                            Console.WriteLine("not found.");
                        }
                        else
                        {
                            Console.WriteLine(e.Country2);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        static void mutex_test3_thread(object param)
        {
            GlobalLock g = new GlobalLock("test");

            Con.WriteLine($"thread #{ThreadObj.CurrentThreadId}: before lock");
            using (g.Lock())
            {
                Con.WriteLine($"thread #{ThreadObj.CurrentThreadId}:locked");
                Con.WriteLine($"thread #{ThreadObj.CurrentThreadId}:sleeping.");
                Thread.Sleep(1000);
                Con.WriteLine($"thread #{ThreadObj.CurrentThreadId}:before release");
            }
            Con.WriteLine($"thread #{ThreadObj.CurrentThreadId}:released");
        }

        static void mutex_test3()
        {
            List<ThreadObj> tl = new List<ThreadObj>();
            for (int i = 0; i < 5; i++)
            {
                ThreadObj t = new ThreadObj(mutex_test3_thread);

                tl.Add(t);
            }
            foreach (ThreadObj t in tl) t.WaitForEnd();
        }

        static void mutex_test2()
        {
            GlobalLock g = new GlobalLock("test");

            Con.WriteLine("before lock");
            using (g.Lock())
            {
                Con.WriteLine("locked");
                Con.WriteLine("sleeping.");
                Thread.Sleep(5000);
                Con.WriteLine("before release");
            }
            Con.WriteLine("released");
        }

        static void mutex_test()
        {
            Mutant m = Mutant.Create("test1");
            Con.WriteLine("before acquire");
            m.Lock();
            Con.WriteLine("acquired.");
            Con.WriteLine("sleeping.");
            Thread.Sleep(5000);
            Con.WriteLine("before release");
            m.Unlock();
            Con.WriteLine("released");
        }

        static void basic_test()
        {
            WriteLine(Kernel.InternalCheckIsWow64());
            WriteLine(Kernel.GetOsPlatform().ToString());
            WriteLine(System.Environment.OSVersion.VersionString);
            WriteLine("home: " + Env.HomeDir);
            WriteLine("path char: " + System.IO.Path.DirectorySeparatorChar);

            //Console.WriteLine(Debug.GetVarsFromClass(typeof(Env)));
            Dbg.PrintObjectInnerString(typeof(Env));
            //Debug.PrintObjectInnerString(typeof(System.Runtime.InteropServices.RuntimeInformation));

            Util.DoNothing();
        }

        private static readonly ConcurrentExclusiveSchedulerPair _concurrentPair
    = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, 2);

        static void async_test()
        {
            //Task.Run(async_task1);

            //Func<Task> x = async_task1;
            //Task.Factory.StartNew(async_task1);

            /*TaskVm<string, int> tt = new TaskVm<string, int>(async_task1, 12345);

            Dbg.WhereThread("abort");
            string str = tt.GetResult();
            Con.WriteLine("ret = " + str);*/

            //string s = async_test_x().Result;
            //Dbg.WhereThread("ret = " + s);

            try
            {
                var x = async_test_x().Result;
                //Task<string> t = async_task_main_proc(123);
                //CancellationTokenSource tsc = new CancellationTokenSource(2000);
                //t.Wait(tsc.Token);
            }
            catch (Exception ex)
            {
                ex.ToString().Print();
            }

            Con.ReadLine(">");
        }

        static async Task<string> async_test_x()
        {
            CancellationTokenSource glaceful = new CancellationTokenSource(100);
            CancellationTokenSource abort = new CancellationTokenSource(1000);
            Task<string> task1 = TaskVm<string, int>.NewTaskVm(async_task_main_proc_2, 123, glaceful.Token, abort.Token);

            Dbg.WhereThread("async_test_x: before await");
            string ret = await task1;
            Dbg.WhereThread("async_test_x: after await. ret = " + ret);

            return ret;
        }

        static async Task<string> async_task_main_proc_2(int arg)
        {
            await Task.Delay(100);

            try
            {
                string s = await async_task_main_proc(arg);

                return s;
            }
            finally
            {
                Dbg.WhereThread("Finally2 start");
                try
                {
                    string s = await async_task_main_proc(arg);
                }
                finally
                {
                    Dbg.WhereThread("Finally2 end");
                }
            }
        }

        static async Task<string> async_task_main_proc(int arg)
        {
            try
            {
                long last = Time.Tick64;
                long start = Time.Tick64;
                while (true)
                {
                    long now = Time.Tick64;
                    long diff = now - last;
                    last = now;

                    Dbg.WhereThread("tick = " + diff);

                    var e = new AsyncManualResetEvent();

                    //if (TaskUtil.CurrentTaskVmGracefulCancel.IsCancellationRequested)
                    //{
                    //    throw new TaskCanceledException();
                    //}

                    if (true)
                    {
                        await fire_test(e);

                        //await Task.Delay(5);
                        //await AsyncWaiter.Sleep(5);
                        //await e.Wait();
                        //await e.Wait();
                    }
                    else
                    {
                        ThreadObj.Sleep(100);
                    }

                    //await Task.Delay(100, tsc.Token);

                    if ((now - start) >= 3000)
                    {
                        //break;
                        throw new ApplicationException("ねこ");
                    }
                }
                return "Hello";
            }
            finally
            {
                Dbg.WhereThread("Finally1");
            }
        }

        static async Task fire_test(AsyncManualResetEvent e)
        {
            await AsyncPreciseDelay.PreciseDelay(200);
            e.Set();
        }

        static async Task<string> async_task1(int arg)
        {
            Dbg.WhereThread("a " + arg.ToString());

            //throw new ApplicationException("zzz");
            await Task.Delay(200);

            Dbg.WhereThread("u");

            await Task.Yield();

            Dbg.WhereThread("v");

            await async_task2();
            Dbg.WhereThread("b");

            CancellationTokenSource tsc = new CancellationTokenSource();

            Dbg.WhereThread("cancel test start");

            async_task_cancel_fire_test(tsc);

            Dbg.WhereThread("cancel test c");

            await TaskUtil.WhenCanceledOrTimeouted(tsc.Token, 1000);

            Dbg.WhereThread("cancel test end");

            return "aho";
        }

        static async void async_task_cancel_fire_test(CancellationTokenSource tsc)
        {
            Dbg.WhereThread("async_task_cancel_fire_test a");
            await Task.Delay(200);
            Dbg.WhereThread("async_task_cancel_fire_test b");

            tsc.Cancel();
        }

        static async Task async_task2()
        {
            Dbg.WhereThread("c");
            await Task.Delay(200);
            //throw new ApplicationException("aho");
            Dbg.WhereThread("d");
        }


        static int race_test_int = 0;

        static async Task<int> task_race_main()
        {
            int num = 10;
            List<Task<int>> tasks = new List<Task<int>>();
            for (int i = 0; i < num; i++)
            {
                tasks.Add(task_race_worker());
            }

            var t = Task.WhenAny(tasks.ToArray());

            await t;

            int a = t.Result.Result;

            return 0;
        }

        static async Task<int> task_race_worker()
        {
            while (true)
            {
                if ((race_test_int % 2) != 0)
                {
                    throw new ApplicationException("race_test_int != 0");
                }

                race_test_int++;

                Thread.Sleep(Secure.Rand31i() % 100);

                race_test_int++;

                await Task.Delay(Secure.Rand31i() % 1000);

                Con.WriteLine(race_test_int);
            }
        }

        static async Task<string> async1()
        {
            //await Task.WhenAny(Task.Run(async2), Task.Run(async3));
            await Task.WhenAny(async2(), async3());
            //await async2();
            return "";
        }

        static async Task<string> async2()
        {
            while (true)
            {
                Console.WriteLine("async2 start ID=" + ThreadObj.CurrentThreadId);
                Thread.Sleep(Secure.Rand31i() % 1000);
                Console.WriteLine("async2 stop ID=" + ThreadObj.CurrentThreadId);
                await Task.Delay(Secure.Rand31i() % 1000);
            }
        }

        static async Task<string> async3()
        {
            while (true)
            {
                Console.WriteLine("async3 start ID=" + ThreadObj.CurrentThreadId);
                Thread.Sleep(Secure.Rand31i() % 1000);
                Console.WriteLine("async3 stop ID=" + ThreadObj.CurrentThreadId);
                await Task.Delay(Secure.Rand31i() % 1000);
            }
        }
    }
}
