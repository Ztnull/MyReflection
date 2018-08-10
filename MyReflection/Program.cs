using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DB.Interface;
using DB.SqlServer;

namespace MyReflection
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("*************************反射*******************************");

                Console.WriteLine("************************Common*****************");
                //IDBHelper iDBHelper = new SqlServerHelper();//new MySqlHelper();
                //iDBHelper.Query();

                #region Common 反射
                {

                    Console.WriteLine("******************Reflection********************");
                    Assembly assembly = Assembly.Load("DB.MySql");//
                    #region MyRegion
                    foreach (var item in assembly.GetModules())
                    {
                        Console.WriteLine(item.Name);
                    }

                    foreach (var item in assembly.GetTypes())
                    {
                        Console.WriteLine(item.Name);
                    }

                    //foreach (var item in assembly.GetCustomAttributes())
                    //{
                    //    Console.WriteLine(item.ToString());
                    //} 
                    #endregion
                    //创建对象 
                    Type type = assembly.GetType("DB.MySql.MySqlHelper");//获取类型
                    var odbHelper = Activator.CreateInstance(type);//创建对象

                    IDBHelper dBHelper = odbHelper as IDBHelper;
                    dBHelper.Query();

                }
                #endregion

                #region 反射+配置文件+简单工厂 
                {
                    Console.WriteLine("************************ 反射+配置文件+简单工厂 ******************************");
                    IDBHelper dBHelper = SimpleFactory.CreateInstance();
                    dBHelper.Query();
                }
                #endregion

                #region 多构造函数 破坏单例 创建泛型 

                {
                    Console.WriteLine("************************ 多构造函数 破坏单例 创建泛型  ******************************");
                    Assembly assembly = Assembly.Load("DB.SqlServer");

                    //多构造函数
                    Type type = assembly.GetType("DB.SqlServer.ReflectionTest");//获取类型
                    foreach (var item in type.GetConstructors())
                    {
                        Console.WriteLine(item.Name);
                    }
                    object oTest0 = Activator.CreateInstance(type);
                    object oTest = Activator.CreateInstance(type, new object[] { 123 });
                    object oTest1 = Activator.CreateInstance(type, new object[] { "ssss" });


                    Type Singletontype = assembly.GetType("DB.SqlServer.Singleton");
                    Singleton singleton = Singleton.GetInstance();
                    //通过反射破坏单例
                    object oSingletion1 = Activator.CreateInstance(Singletontype, true);
                    object oSingletion2 = Activator.CreateInstance(Singletontype, true);
                    object oSingletion3 = Activator.CreateInstance(Singletontype, true);

                    //泛型调用
                    Type Generictype = assembly.GetType("DB.SqlServer.GenericClass`3");
                    Type markType = Generictype.MakeGenericType(typeof(int), typeof(int), typeof(string));//创建对象必须使用此对象，因为这个已经指定了类型
                    object oGeneric = Activator.CreateInstance(markType);

                }

                #endregion


                Console.ReadKey();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }
    }
}
