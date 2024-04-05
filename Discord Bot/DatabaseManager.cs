using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
namespace Discord_Bot
{
    public class DatabaseManager
    {
        private string _connectingString;
        public DatabaseManager(string connectionString)
        {
            _connectingString = connectionString;
        }
        public List<Dictionary<string, object>> ExecuteQuery(string query, List<SQLiteParameter> parameters = null)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            using (var connection = new SQLiteConnection(_connectingString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                    }
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(reader.GetName(i), reader[i]);
                            }
                            result.Add(row);
                        }                        
                    }
                }
            }
            return result;
        }
    }
}
