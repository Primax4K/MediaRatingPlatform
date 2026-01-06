using System.Data;
using System.Data.Common;

namespace Domain.Data {
	public abstract class DbHelper {
		protected static async Task OpenAsync(IDbConnection c, CancellationToken ct) {
			if (c is DbConnection dbc)
				await dbc.OpenAsync(ct);
			else
				c.Open();
		}

		protected static DbCommand Command(IDbConnection c, string sql) {
			var cmd = (DbCommand)c.CreateCommand();
			cmd.CommandText = sql;
			return cmd;
		}

		protected static void Param(DbCommand cmd, string name, object? value) {
			var p = cmd.CreateParameter();
			p.ParameterName = name;
			p.Value = value ?? DBNull.Value;
			cmd.Parameters.Add(p);
		}
	}
}