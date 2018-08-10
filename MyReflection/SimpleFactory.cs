using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DB.Interface;

namespace MyReflection
{
    public class SimpleFactory
    {
        private static string IDBHelperConfig = ConfigurationManager.AppSettings["IDBHelperConfig"];
        private static string DllNmae = IDBHelperConfig.Split(',')[1];
        private static string TypeNmae = IDBHelperConfig.Split(',')[0];
        public static IDBHelper CreateInstance()
        {
            Assembly assembly = Assembly.Load(DllNmae); 

            //创建对象 
            Type dbMySqlHlpertype = assembly.GetType(TypeNmae);//获取类型
            var odbHelper = Activator.CreateInstance(dbMySqlHlpertype);//创建对象
            return odbHelper as IDBHelper;
        }

    }
}
