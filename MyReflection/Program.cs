using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DB.Interface;
using DB.SqlServer;
using Model;

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

                    //通过反射破坏单例，调用私有方法
                    object oSingletion1 = Activator.CreateInstance(Singletontype, true);
                    object oSingletion2 = Activator.CreateInstance(Singletontype, true);
                    object oSingletion3 = Activator.CreateInstance(Singletontype, true);

                    //泛型调用类
                    Type Generictype = assembly.GetType("DB.SqlServer.GenericClass`3");
                    Type markType = Generictype.MakeGenericType(typeof(int), typeof(int), typeof(string));//创建对象必须使用此对象，因为这个已经指定了类型
                    object oGeneric = Activator.CreateInstance(markType);


                    //GenericMethod

                }

                #endregion

                #region 反射调用实例方法、静态方法、重载方法 
                {
                    Console.WriteLine("************************  反射调用实例方法、静态方法、重载方法  ******************************");
                    Assembly assembly = Assembly.Load("DB.SqlServer");
                    Type type = assembly.GetType("DB.SqlServer.ReflectionTest");
                    object oTest = Activator.CreateInstance(type);

                    foreach (var item in type.GetMethods())
                    {
                        Console.WriteLine(item.Name);
                    }


                    {
                        Console.WriteLine("************************  调用方法实例  ******************************");
                        MethodInfo method = type.GetMethod("Show1");
                        method.Invoke(oTest, null);
                    }
                    {
                        Console.WriteLine("************************  调用静态方法实例  ******************************");
                        MethodInfo method = type.GetMethod("Show5");
                        method.Invoke(null, new object[] { "装逼" });
                    }
                    {
                        Console.WriteLine("************************  调用重载方法实例  ******************************");
                        MethodInfo method = type.GetMethod("Show3", new Type[] { typeof(int), typeof(string) });
                        method.Invoke(oTest, new object[] { 123, "装逼" });

                        MethodInfo method1 = type.GetMethod("Show3", new Type[] { typeof(string), typeof(int) });
                        method1.Invoke(oTest, new object[] { "装逼", 123 });
                    }
                }
                #endregion


                #region 反射调用    私有+泛型方法

                {
                    Assembly assembly = Assembly.Load("DB.SqlServer");

                    {
                        Console.WriteLine("************************  调用私有方法  ******************************");
                        Type type = assembly.GetType("DB.SqlServer.ReflectionTest");//获取类型

                        object oTest = Activator.CreateInstance(type);
                        MethodInfo method = type.GetMethod("Show4", BindingFlags.Instance | BindingFlags.NonPublic);
                        method.Invoke(oTest, new object[] { "小刘" });
                    }

                    {
                        Console.WriteLine("************************  调用泛型方法  ******************************");
                        Type type = assembly.GetType("DB.SqlServer.GenericMethod");//获取类型
                        object oTest = Activator.CreateInstance(type);

                        MethodInfo method = type.GetMethod("Show");
                        MethodInfo methodNew = method.MakeGenericMethod(typeof(int), typeof(int), typeof(string));
                        methodNew.Invoke(oTest, new object[] { 123, 123, "泛型" });

                    }

                }

                #endregion

                #region 字段属性

                {
                    Console.WriteLine("************************  字段属性  ******************************");
                    //People people = new People();
                    Type type = typeof(People);
                    object oPeople = Activator.CreateInstance(type);

                    //foreach (var item in type.GetProperties())
                    foreach (var item in type.GetFields())
                    {
                        //获取名称，值

                        if (item.Name.Equals("Id"))
                        {
                            item.SetValue(oPeople, 123);
                        }
                        else if (item.Name.Equals("Name"))
                        {
                            item.SetValue(oPeople, "小猪佩奇");
                        }
                        else if (item.Name.Equals("Description"))
                        {
                            item.SetValue(oPeople, "不是一只猪");
                        }
                    }


                }

                #endregion



                #region SQL

                {

                    SqlServerHelper sql = new SqlServerHelper();
                    var list = sql.GetList<Model.Book>(" ID>100 ");

                    foreach (var item in list)
                    {
                        Console.WriteLine(item.Id + "***" + item.ISBN + "***" + item.PublishDate + "***" + item.PublisherId + "***" + item.Title + "***" + item.TOC + "***" + item.UnitPrice + "***" + item.WordsCount);
                       
                    }

                    Console.WriteLine(list.Count());
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
