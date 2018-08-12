using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DB.Interface;
using Model;

namespace DB.SqlServer
{
    /// <summary>
    /// SqlServer实现
    /// </summary>
    public class SqlServerHelper : IDBHelper
    {
        private static string ConnectionStringCustomers = ConfigurationManager.ConnectionStrings["Customers"].ConnectionString;

        public SqlServerHelper()
        {
            //Console.WriteLine("{0}被构造", this.GetType().Name);
        }

        public void Query()
        {
            //Console.WriteLine("{0}.Query", this.GetType().Name);
        }

        /// <summary>
        /// 一个方法满足不同的数据实体查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(int id)
        {
            Type type = typeof(T);
            string columnStrings = string.Join(",", type.GetProperties().Select(p => string.Format("[{0}]", p.Name)));

            string sql = string.Format("SELECT {0} FROM [{1}] Where Id={2}"
               , columnStrings
               , type.Name
               , id);

            object t = Activator.CreateInstance(type);
            using (SqlConnection conn = new SqlConnection(ConnectionStringCustomers))
            {
                SqlCommand command = new SqlCommand(sql, conn);
                conn.Open();
                System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    foreach (var item in type.GetProperties())
                    {
                        item.SetValue(t, reader[item.Name]);
                    }
                }
            }

            return (T)t;
        }

        #region 最牛逼的yield+IEnumerable<T>封装

        #region 传入Sql语句执行查询，返回一个强类型的IEnumerable集合+（注意：主要用于多表连接查询）
        /// <summary>
        /// 传入Sql语句执行查询，返回一个强类型的IEnumerable集合
        /// （注意：主要用于多表连接查询）
        /// </summary>
        /// <typeparam name="T">传入的类型</typeparam>
        /// <param name="sql">sql语句</param>
        /// <returns>返回的IEnumerable<T> 集合</returns>
        public IEnumerable<T> GetList<T>(string sql) where T : new()
        {
            using (SqlDataReader reader = ExecuteReader(sql))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        yield return MapEntity<T>(reader);
                    }
                }
            }
        }
        #endregion

        #region  根据传入的条件执行Sql语句，并返回一个IEnumerable<T>类型的集合

        /// <summary>
        /// 根据传入的条件执行Sql语句，并返回一个IEnumerable<T>类型的集合
        /// （注意传入的 T 必须约束为 where T : class, new()）
        /// </summary>
        /// <typeparam name="T">类型：【 约束为 where T : class, new() 】</typeparam>
        /// <param name="where">查询的条件，请省略 Where 关键字</param>
        /// <returns></returns>
        public IEnumerable<T> GetList<T>(Expression<Func<T, bool>> where) where T : new()
        {
            Type type = typeof(T);
            //遍历获得字段
            string columnString = string.Join(",", type.GetProperties().Select(p => string.Format("[{0}]", p.Name)));
            string sql = string.Format("SELECT {0} FROM [{1}] Where  {2} ",
                columnString,
                type.Name,
                DealExpress(where));

            //return GetList<T>(sql);
            using (SqlDataReader reader = ExecuteReader(sql))
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        yield return MapEntity<T>(reader);
                    }
                }
            }
        }
        #endregion

        #region Common static+公共的调用查询方法 返回 DataReader + T
        #region ExecuteReader +static SqlDataReader ExecuteReader(string cmdText, params SqlParameter[] parameters)
        /// <summary>
        /// 执行一个查询的T-SQL语句, 返回一个SqlDataReader对象, 如果出现SQL语句执行错误, 将会关闭连接通道抛出异常
        ///  ( 注意：调用该方法后，一定要对SqlDataReader进行Close )
        /// </summary>
        /// <param name="cmdText">要执行的T-SQL语句</param>
        /// <param name="parameters">参数列表</param>
        /// <exception cref="链接数据库异常"></exception>
        /// <returns>SqlDataReader对象</returns>
        public static SqlDataReader ExecuteReader(string cmdText, params SqlParameter[] parameters)
        {
            return ExecuteReader(cmdText, CommandType.Text, parameters);
        }
        #endregion

        #region ExecuteReader +static SqlDataReader ExecuteReader(string cmdText, CommandType type, params SqlParameter[] parameters)
        /// <summary>
        /// 执行一个查询的T-SQL语句, 返回一个SqlDataReader对象, 如果出现SQL语句执行错误, 将会关闭连接通道抛出异常
        ///  ( 注意：调用该方法后，一定要对SqlDataReader进行Close )
        /// </summary>
        /// <param name="cmdText">要执行的T-SQL语句</param>
        /// <param name="type">命令类型</param>
        /// <param name="parameters">参数列表</param>
        /// <exception cref="链接数据库异常"></exception>
        /// <returns>SqlDataReader对象</returns>
        public static SqlDataReader ExecuteReader(string cmdText, CommandType type, params SqlParameter[] parameters)
        {
            SqlConnection conn = new SqlConnection(ConnectionStringCustomers);
            using (SqlCommand cmd = new SqlCommand(cmdText, conn))
            {
                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddRange(parameters);
                }
                cmd.CommandType = type;
                conn.Open();
                try
                {
                    SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    cmd.Parameters.Clear();
                    return reader;
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    //出现异常关闭连接并且释放
                    conn.Close();
                    throw ex;
                }
            }
        }
        #endregion

        #region 将一个SqlDataReader对象转换成一个实体类对象 +static T MapEntity<T>(SqlDataReader reader) where T : class,new()
        /// <summary>
        /// 将一个SqlDataReader对象转换成一个实体类对象
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="reader">当前指向的reader</param>
        /// <returns>实体对象</returns>
        public static T MapEntity<T>(SqlDataReader reader)
        {
            try
            {
                Type type = typeof(T);
                var props = type.GetProperties();

                object entity = Activator.CreateInstance(type);//创建返回的单个对象
                foreach (var prop in props)
                {
                    if (prop.CanWrite)
                    {
                        try
                        {
                            var index = reader.GetOrdinal(prop.Name);
                            var data = reader.GetValue(index);
                            if (data != DBNull.Value)
                            {
                                prop.SetValue(entity, Convert.ChangeType(data, prop.PropertyType), null);
                            }
                        }
                        catch (IndexOutOfRangeException)
                        {
                            continue;
                        }
                    }
                }
                return (T)entity;
            }
            catch
            {
                return default(T);
            }
        }
        #endregion 
        #endregion 

        #region 表达式树的解析

        public static string DealExpress(Expression exp)
        {
            if (exp is LambdaExpression)
            {
                LambdaExpression l_exp = exp as LambdaExpression;
                return DealExpress(l_exp.Body);
            }
            if (exp is BinaryExpression)
            {
                return DealBinaryExpression(exp as BinaryExpression);
            }
            if (exp is MemberExpression)
            {
                return DealMemberExpression(exp as MemberExpression);
            }
            if (exp is ConstantExpression)
            {
                return DealConstantExpression(exp as ConstantExpression);
            }
            if (exp is UnaryExpression)
            {
                return DealUnaryExpression(exp as UnaryExpression);
            }

            return "";
        }
        public static string DealUnaryExpression(UnaryExpression exp)
        {
            return DealExpress(exp.Operand);
        }
        public static string DealConstantExpression(ConstantExpression exp)
        {
            object vaule = exp.Value;
            string v_str = string.Empty;
            if (vaule == null)
            {
                return "NULL";
            }
            if (vaule is string)
            {
                v_str = string.Format("'{0}'", vaule.ToString());
            }
            else if (vaule is DateTime)
            {
                DateTime time = (DateTime)vaule;
                v_str = string.Format("'{0}'", time.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                v_str = vaule.ToString();
            }
            return v_str;
        }
        public static string DealBinaryExpression(BinaryExpression exp)
        {

            string left = DealExpress(exp.Left);
            string oper = GetOperStr(exp.NodeType);
            string right = DealExpress(exp.Right);
            if (right == "NULL")
            {
                if (oper == "=")
                {
                    oper = " is ";
                }
                else
                {
                    oper = " is not ";
                }
            }
            return left + oper + right;
        }
        public static string DealMemberExpression(MemberExpression exp)
        {
            return exp.Member.Name;
        }
        public static string GetOperStr(ExpressionType e_type)
        {
            switch (e_type)
            {
                case ExpressionType.OrElse: return " OR ";
                case ExpressionType.Or: return "|";
                case ExpressionType.AndAlso: return " AND ";
                case ExpressionType.And: return "&";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.NotEqual: return "<>";
                case ExpressionType.Add: return "+";
                case ExpressionType.Subtract: return "-";
                case ExpressionType.Multiply: return "*";
                case ExpressionType.Divide: return "/";
                case ExpressionType.Modulo: return "%";
                case ExpressionType.Equal: return "=";
            }
            return "";
        }

        #endregion

        #endregion


    }
}
